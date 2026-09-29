// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataMiddleware.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Fetch Metadata policy middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed partial middleware class)
//   Depends on : FetchMetadataOptions, CsrfOptions, FetchMetadataEvaluator,
//                CsrfEndpointMetadata, ErrorResponseResult, ApiError,
//                ErrorResponse, ResponseMetadata, ICorrelationIdAccessor,
//                ILogger, IOptions
//   Used by    : FetchMetadataMiddlewareExtensions
//   See also   : FetchMetadataEvaluator.cs, OriginMiddleware.cs, SPEC.md (Fetch Metadata)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Origin;

/// <summary>
/// Applies the Fetch Metadata policy to protected requests. The
/// middleware runs after the Origin policy and passes through on safe
/// methods. On protected methods it evaluates the Sec-Fetch-Site and
/// Sec-Fetch-Mode headers and rejects a disallowed value with a 403.
/// </summary>
/// <remarks>
/// The middleware respects the same per-endpoint policy as the CSRF
/// middleware. [ApiPilotRequireCsrf] enforces even on safe methods.
/// [ApiPilotSkipCsrf] bypasses unconditionally. The precedence rule
/// is:
///
///     Require > Skip > global ProtectedMethods set
///
/// The Fetch Metadata policy is defense-in-depth. It supplements the
/// Origin policy and the CSRF check. The default profile is Off; when
/// the profile is Off the middleware passes every request through.
///
/// The middleware resolves the correlation ID from
/// ICorrelationIdAccessor with TraceIdentifier as fallback. Every
/// rejection envelope carries the correlation ID.
/// </remarks>
public sealed partial class FetchMetadataMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<FetchMetadataOptions> _fetchMetadataOptions;
    private readonly IOptions<CsrfOptions> _csrfOptions;
    private readonly ILogger<FetchMetadataMiddleware> _logger;
    private readonly ICorrelationIdAccessor? _correlationAccessor;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="fetchMetadataOptions">
    /// The Fetch Metadata options. Must not be null.
    /// </param>
    /// <param name="csrfOptions">
    /// The CSRF options, read for the ProtectedMethods set. Must not be null.
    /// </param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="correlationAccessor">
    /// Optional accessor for the current correlation ID. When null, the
    /// middleware falls back to HttpContext.TraceIdentifier.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    public FetchMetadataMiddleware(
        RequestDelegate next,
        IOptions<FetchMetadataOptions> fetchMetadataOptions,
        IOptions<CsrfOptions> csrfOptions,
        ILogger<FetchMetadataMiddleware> logger,
        ICorrelationIdAccessor? correlationAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(fetchMetadataOptions);
        ArgumentNullException.ThrowIfNull(csrfOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _fetchMetadataOptions = fetchMetadataOptions;
        _csrfOptions = csrfOptions;
        _logger = logger;
        _correlationAccessor = correlationAccessor;
    }

    /// <summary>
    /// Executes the middleware. Resolves the endpoint policy and the
    /// request method; passes through when the request is not enforced;
    /// evaluates Fetch Metadata otherwise.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = _fetchMetadataOptions.Value;

        if (options.Profile == FetchMetadataProfile.Off)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var policy = ResolvePolicy(httpContext);

        if (policy == CsrfPolicy.Skip)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var csrfOptions = _csrfOptions.Value;
        var enforce = policy == CsrfPolicy.Require
            || csrfOptions.ProtectedMethods.Contains(httpContext.Request.Method);

        if (!enforce)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        if (FetchMetadataEvaluator.IsRequestAcceptable(httpContext, options, out var reason))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        LogRejectedDelegate(
            _logger,
            reason,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty,
            null);

        await WriteErrorAsync(httpContext).ConfigureAwait(false);
    }

    private static CsrfPolicy ResolvePolicy(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return CsrfPolicy.UseGlobal;
        }

        CsrfPolicy resolved = CsrfPolicy.UseGlobal;
        foreach (var metadata in endpoint.Metadata.GetOrderedMetadata<CsrfEndpointMetadata>())
        {
            if (metadata.Policy == CsrfPolicy.Require)
            {
                resolved = CsrfPolicy.Require;
            }
            else if (metadata.Policy == CsrfPolicy.Skip
                && resolved != CsrfPolicy.Require)
            {
                resolved = CsrfPolicy.Skip;
            }
        }

        return resolved;
    }

    private async Task WriteErrorAsync(HttpContext httpContext)
    {
        var accessorId = _correlationAccessor?.RequestId;
        var requestId = string.IsNullOrEmpty(accessorId)
            ? httpContext.TraceIdentifier
            : accessorId;

        var meta = new ResponseMetadata { RequestId = requestId };
        var error = ApiError.Create(ApiErrorCode.Forbidden, "The request was rejected by the Fetch Metadata policy.");
        var response = new ErrorResponse(error, meta);
        await new ErrorResponseResult(response).ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}

