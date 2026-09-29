// filepath: dotnet/src/ApiPilot.Security/Origin/OriginMiddleware.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Defense-in-depth Origin policy middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed partial middleware class)
//   Depends on : OriginPolicyOptions, CsrfOptions, OriginValidator,
//                CsrfEndpointMetadata, ErrorResponseResult, ApiError,
//                ErrorResponse, ResponseMetadata, ICorrelationIdAccessor,
//                ILogger, IOptions
//   Used by    : OriginMiddlewareExtensions
//   See also   : OriginValidator.cs, CsrfMiddleware.cs, SPEC.md (Origin policy)
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
/// Applies the defense-in-depth Origin policy to protected requests.
/// The middleware runs before CSRF protection and passes through on
/// safe methods. On protected methods it compares the request Origin
/// against the allow-list and rejects a non-matching origin with the
/// configured rejection code.
/// </summary>
/// <remarks>
/// The middleware respects the same per-endpoint policy as the CSRF
/// middleware. [ApiPilotRequireCsrf] enforces even on safe methods.
/// [ApiPilotSkipCsrf] bypasses unconditionally, because an endpoint
/// that opts out of CSRF protection is also opting out of the
/// CSRF-adjacent Origin policy. The precedence rule is:
///
///     Require > Skip > global ProtectedMethods set
///
/// The middleware never echoes the offending Origin in the response
/// envelope. The reason is logged server-side.
///
/// The middleware resolves the correlation ID from
/// ICorrelationIdAccessor with TraceIdentifier as fallback. Every
/// rejection envelope carries the correlation ID.
/// </remarks>
public sealed partial class OriginMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<OriginPolicyOptions> _originOptions;
    private readonly IOptions<CsrfOptions> _csrfOptions;
    private readonly ILogger<OriginMiddleware> _logger;
    private readonly ICorrelationIdAccessor? _correlationAccessor;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="originOptions">The Origin policy options. Must not be null.</param>
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
    public OriginMiddleware(
        RequestDelegate next,
        IOptions<OriginPolicyOptions> originOptions,
        IOptions<CsrfOptions> csrfOptions,
        ILogger<OriginMiddleware> logger,
        ICorrelationIdAccessor? correlationAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(originOptions);
        ArgumentNullException.ThrowIfNull(csrfOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _originOptions = originOptions;
        _csrfOptions = csrfOptions;
        _logger = logger;
        _correlationAccessor = correlationAccessor;
    }

    /// <summary>
    /// Executes the middleware. Resolves the endpoint policy and the
    /// request method; passes through when the request is not enforced;
    /// checks the Origin otherwise.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = _originOptions.Value;

        if (!options.Enabled)
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

        if (OriginValidator.IsRequestOriginAcceptable(httpContext, options, out var reason))
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

        await WriteErrorAsync(httpContext, options.RejectionCode).ConfigureAwait(false);
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

    private async Task WriteErrorAsync(HttpContext httpContext, string code)
    {
        var accessorId = _correlationAccessor?.RequestId;
        var requestId = string.IsNullOrEmpty(accessorId)
            ? httpContext.TraceIdentifier
            : accessorId;

        var meta = new ResponseMetadata { RequestId = requestId };
        var error = ApiError.Create(ApiErrorCode.From(code), "The request origin is not allowed.");
        var response = new ErrorResponse(error, meta);
        await new ErrorResponseResult(response).ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}

