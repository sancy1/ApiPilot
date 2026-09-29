// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfMiddleware.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Middleware that enforces CSRF protection on state-changing requests
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed partial middleware class)
//   Depends on : ICsrfService, CsrfOptions, CsrfEndpointMetadata,
//                ErrorResponseResult, ApiError, ErrorResponse, ResponseMetadata,
//                ICorrelationIdAccessor, ILogger, IOptions
//   Used by    : CsrfMiddlewareExtensions
//   See also   : CsrfOptions.cs, CsrfAttributes.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Enforces CSRF protection on state-changing requests. The middleware
/// runs after authentication so that the CSRF binding is the
/// authenticated subject. It reads the token from the configured
/// header, validates it through ICsrfService, and writes the standard
/// ApiPilot error envelope on failure.
/// </summary>
/// <remarks>
/// The precedence rule for endpoint policy is:
///
///     Require (from [ApiPilotRequireCsrf])
///     > Skip (from [ApiPilotSkipCsrf])
///     > global ProtectedMethods set
///
/// [ApiPilotRequireCsrf] wins over the safe-method bypass, so the
/// attribute can enforce CSRF on a GET endpoint that is sensitive.
///
/// The middleware resolves the correlation ID from
/// ICorrelationIdAccessor with TraceIdentifier as fallback. Every
/// rejection envelope carries the correlation ID. This matches the
/// A-120 discipline applied across every ApiPilot middleware.
///
/// The effective exemption set is computed per request: the bootstrap
/// path from CsrfOptions.BootstrapPath plus the paths in
/// CsrfOptions.ExemptPaths, plus the predicate when it is set.
/// </remarks>
public sealed partial class CsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICsrfService _service;
    private readonly IOptions<CsrfOptions> _options;
    private readonly ILogger<CsrfMiddleware> _logger;
    private readonly ICorrelationIdAccessor? _correlationAccessor;
    private readonly IOptions<CorrelationOptions>? _correlationOptions;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="service">The CSRF service. Must not be null.</param>
    /// <param name="options">The CSRF options. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="correlationAccessor">
    /// Optional accessor for the current correlation ID. When null, the
    /// middleware falls back to HttpContext.TraceIdentifier.
    /// </param>
    /// <param name="correlationOptions">
    /// Optional correlation options. When null, EchoInResponseBody
    /// is treated as true.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    public CsrfMiddleware(
        RequestDelegate next,
        ICsrfService service,
        IOptions<CsrfOptions> options,
        ILogger<CsrfMiddleware> logger,
        ICorrelationIdAccessor? correlationAccessor = null,
        IOptions<CorrelationOptions>? correlationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _service = service;
        _options = options;
        _logger = logger;
        _correlationAccessor = correlationAccessor;
        _correlationOptions = correlationOptions;
    }

    /// <summary>
    /// Executes the middleware. Reads the endpoint policy and the request
    /// method; short-circuits when the request is exempt; validates the
    /// token otherwise.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var options = _options.Value;

        var policy = ResolvePolicy(httpContext);

        // Policy == Skip bypasses unconditionally.
        if (policy == CsrfPolicy.Skip)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        // Policy == Require enforces unconditionally, even on safe methods.
        // Policy == UseGlobal enforces only when the method is in the set.
        var enforce = policy == CsrfPolicy.Require
            || options.ProtectedMethods.Contains(httpContext.Request.Method);

        if (!enforce)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        // The request is enforced. Check exemptions (bootstrap path,
        // configured paths, predicate). A path exemption never invokes
        // the predicate.
        if (IsExempt(httpContext, options))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        // Read the header value. A missing header is null; the service
        // maps null to the missing-header code.
        string? headerValue = null;
        if (httpContext.Request.Headers.TryGetValue(options.HeaderName, out var rawValues))
        {
            headerValue = rawValues.ToString();
        }

        var result = await _service
            .ValidateAsync(httpContext, headerValue, httpContext.RequestAborted)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        LogRejectedDelegate(
            _logger,
            result.Reason.ToString(),
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty,
            null);

        await WriteErrorAsync(httpContext, result.PublicCode).ConfigureAwait(false);
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
            // Require beats Skip. A later Require wins over an earlier Skip,
            // and a later Skip does not override an earlier Require.
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

    private static bool IsExempt(HttpContext httpContext, CsrfOptions options)
    {
        var path = httpContext.Request.Path;

        // The bootstrap path is read from the options at request time so
        // a later change to BootstrapPath is honored.
        if (path.StartsWithSegments(new Microsoft.AspNetCore.Http.PathString(options.BootstrapPath)))
        {
            return true;
        }

        foreach (var exempt in options.ExemptPaths)
        {
            if (path.StartsWithSegments(exempt))
            {
                return true;
            }
        }

        if (options.ExemptPredicate is not null && options.ExemptPredicate(httpContext))
        {
            return true;
        }

        return false;
    }

    private async Task WriteErrorAsync(HttpContext httpContext, string code)
    {
        var requestId = CorrelationEnvelope.ResolveRequestId(
            httpContext,
            _correlationAccessor,
            _correlationOptions);

        var meta = new ResponseMetadata { RequestId = requestId };
        var error = ApiError.Create(ApiErrorCode.From(code), "CSRF validation failed.");
        var response = new ErrorResponse(error, meta);
        await new ErrorResponseResult(response).ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}

