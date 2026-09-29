// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotContentNegotiationMiddleware.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Rejects requests whose Accept or Content-Type headers are not acceptable
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed middleware class)
//   Depends on : ContentNegotiationOptions, ErrorResponseResult, ApiError,
//                ApiErrorCode, ErrorResponse, ResponseMetadata,
//                ICorrelationIdAccessor, ILogger
//   Used by    : ApiPilotContentNegotiationExtensions
//   See also   : SPEC.md (content negotiation), ContentNegotiationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Configuration;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Rejects requests whose Accept header does not include an acceptable
/// response media type with HTTP 406, and requests whose Content-Type
/// header on a body-carrying method is not acceptable with HTTP 415.
/// Both responses use the standard ApiPilot error envelope.
/// </summary>
/// <remarks>
/// The Accept header check only recognizes the full wildcard */*. Type-level
/// wildcards such as application/* are not supported. Applications that need
/// them configure AcceptableResponseMediaTypes to include the specific media
/// types their clients send, or replace the middleware.
///
/// The middleware resolves the correlation ID from ICorrelationIdAccessor
/// when one is available, falling back to HttpContext.TraceIdentifier. This
/// matches the A-040 fix in the exception middleware and keeps every error
/// envelope consistent on the wire.
///
/// Place the middleware after UseApiPilotCorrelation and after
/// UseApiPilotExceptions so the correlation ID is available and any
/// downstream exception is caught.
/// </remarks>
public sealed partial class ApiPilotContentNegotiationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<ContentNegotiationOptions> _options;
    private readonly ILogger<ApiPilotContentNegotiationMiddleware> _logger;
    private readonly ICorrelationIdAccessor? _correlationAccessor;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="options">The content negotiation options. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="correlationAccessor">
    /// Optional accessor for the current correlation ID. When null, the
    /// middleware falls back to HttpContext.TraceIdentifier.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any non-optional argument is null.
    /// </exception>
    public ApiPilotContentNegotiationMiddleware(
        RequestDelegate next,
        IOptions<ContentNegotiationOptions> options,
        ILogger<ApiPilotContentNegotiationMiddleware> logger,
        ICorrelationIdAccessor? correlationAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options;
        _logger = logger;
        _correlationAccessor = correlationAccessor;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Check 1: the Accept header.
        var accept = httpContext.Request.Headers.Accept;
        if (accept.Count > 0 && !IsAcceptHeaderAcceptable(accept.ToString()))
        {
            LogNotAcceptableDelegate(_logger, accept.ToString(),
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? string.Empty,
                null);
            await WriteErrorAsync(httpContext, ApiErrorCode.NotAcceptable,
                "The requested media type is not supported.");
            return;
        }

        // Check 2: the Content-Type header on body-carrying methods.
        if (_options.Value.BodyCarryingMethods.Contains(httpContext.Request.Method))
        {
            var contentType = httpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                if (!_options.Value.AcceptMissingContentType)
                {
                    LogUnsupportedMediaTypeDelegate(_logger, "(missing)",
                        httpContext.Request.Method,
                        httpContext.Request.Path.Value ?? string.Empty,
                        null);
                    await WriteErrorAsync(httpContext, ApiErrorCode.UnsupportedMediaType,
                        "A Content-Type header is required for this request.");
                    return;
                }
            }
            else if (!IsContentTypeAcceptable(contentType))
            {
                LogUnsupportedMediaTypeDelegate(_logger, contentType,
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value ?? string.Empty,
                    null);
                await WriteErrorAsync(httpContext, ApiErrorCode.UnsupportedMediaType,
                    "The request content type is not supported.");
                return;
            }
        }

        await _next(httpContext);
    }

    private bool IsAcceptHeaderAcceptable(string headerValue)
    {
        // The Accept header is a comma-separated list of media types with
        // optional parameters. The check ignores the parameters (including q=)
        // and asks whether any entry matches an acceptable response media type
        // or the full wildcard.
        foreach (var rawEntry in headerValue.Split(','))
        {
            var entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            var mediaType = ExtractMediaType(entry);
            if (mediaType.Length == 0)
            {
                continue;
            }

            if (_options.Value.AcceptWildcard && mediaType == "*/*")
            {
                return true;
            }

            if (_options.Value.AcceptableResponseMediaTypes.Contains(mediaType))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsContentTypeAcceptable(string headerValue)
    {
        var mediaType = ExtractMediaType(headerValue);
        return _options.Value.AcceptableRequestMediaTypes.Contains(mediaType);
    }

    private static string ExtractMediaType(string headerValue)
    {
        // Take the segment before the first semicolon, trim it.
        var semi = headerValue.IndexOf(';');
        var segment = semi < 0 ? headerValue : headerValue.Substring(0, semi);
        return segment.Trim();
    }

    private async Task WriteErrorAsync(HttpContext httpContext, ApiErrorCode code, string message)
    {
        var accessorId = _correlationAccessor?.RequestId;
        var requestId = string.IsNullOrEmpty(accessorId)
            ? httpContext.TraceIdentifier
            : accessorId;

        var meta = new ResponseMetadata { RequestId = requestId };
        var error = ApiError.Create(code, message);
        var response = new ErrorResponse(error, meta);
        await new ErrorResponseResult(response).ExecuteAsync(httpContext);
    }
}

