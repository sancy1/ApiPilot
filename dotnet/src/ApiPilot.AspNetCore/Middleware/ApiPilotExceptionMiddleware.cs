// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotExceptionMiddleware.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Middleware that catches exceptions, maps them safely, and writes an ErrorResponse
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed middleware class)
//   Depends on : IApiExceptionMapper, ApiExceptionOptions, ErrorResponse,
//                ErrorResponseResult, ResponseMetadata, ILogger,
//                ICorrelationIdAccessor, IOptions<CorrelationOptions>
//   Used by    : ApiPilotExceptionExtensions
//   See also   : DefaultApiExceptionMapper.cs, ErrorResponseResult.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Catches exceptions that escape the endpoint pipeline, maps them to a
/// safe <see cref="ApiError"/>, logs safely, and writes the standard
/// <see cref="ErrorResponse"/> envelope to the HTTP response. The HTTP
/// status is derived from the mapped error code.
/// </summary>
/// <remarks>
/// The middleware resolves the request ID from the correlation accessor
/// when one is available, falling back to HttpContext.TraceIdentifier.
/// When CorrelationOptions.EchoInResponseBody is false, the request ID
/// in the response envelope is set to the empty string.
/// </remarks>
public sealed partial class ApiPilotExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IApiExceptionMapper _mapper;
    private readonly IOptions<ApiExceptionOptions> _options;
    private readonly ILogger<ApiPilotExceptionMiddleware> _logger;
    private readonly ICorrelationIdAccessor? _correlationAccessor;
    private readonly IOptions<CorrelationOptions>? _correlationOptions;

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="mapper">The exception mapper. Must not be null.</param>
    /// <param name="options">The exception options. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="correlationAccessor">
    /// Optional accessor for the current correlation ID. When null, the
    /// middleware falls back to HttpContext.TraceIdentifier.
    /// </param>
    /// <param name="correlationOptions">
    /// Optional correlation options. When null, EchoInResponseBody is
    /// treated as true.
    /// </param>
    public ApiPilotExceptionMiddleware(
        RequestDelegate next,
        IApiExceptionMapper mapper,
        IOptions<ApiExceptionOptions> options,
        ILogger<ApiPilotExceptionMiddleware> logger,
        ICorrelationIdAccessor? correlationAccessor = null,
        IOptions<CorrelationOptions>? correlationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(mapper);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _mapper = mapper;
        _options = options;
        _logger = logger;
        _correlationAccessor = correlationAccessor;
        _correlationOptions = correlationOptions;
    }

    /// <summary>
    /// Executes the middleware. If a downstream exception is thrown and the
    /// response has not yet started, a mapped error response is written.
    /// If the response has already started, the exception is rethrown and
    /// the connection is aborted by the runtime.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected. Do not attempt to write a response.
            throw;
        }
        catch (Exception ex)
        {
            if (httpContext.Response.HasStarted)
            {
                LogResponseStartedDelegate(_logger, ex);
                throw;
            }

            var error = _mapper.Map(ex, httpContext);
            var isUnknown = error.Code.Code == "INTERNAL_ERROR";
            var logLevel = isUnknown ? _options.Value.UnknownExceptionLogLevel : _options.Value.KnownExceptionLogLevel;

            LogSafely(logLevel, ex, error, httpContext);

            var requestId = ResolveRequestId(httpContext);
            var meta = new ResponseMetadata { RequestId = requestId };
            var errorResponse = new ErrorResponse(error, meta);
            await new ErrorResponseResult(errorResponse, _options.Value).ExecuteAsync(httpContext);
        }
    }

    private string ResolveRequestId(HttpContext httpContext)
    {
        return CorrelationEnvelope.ResolveRequestId(httpContext, _correlationAccessor, _correlationOptions);
    }

    private void LogSafely(LogLevel level, Exception ex, ApiError error, HttpContext httpContext)
    {
        var detail = BuildLogDetail(ex);
        var logDelegate = ResolveLogDelegate(level);
        logDelegate(
            _logger,
            error.Code.Code,
            httpContext.Request.Method,
            httpContext.Request.Path.Value ?? string.Empty,
            detail,
            ex);
    }

    private string BuildLogDetail(Exception ex)
    {
        var includeType = _options.Value.IncludeExceptionTypeInLogs;
        var includeMessage = _options.Value.IncludeExceptionMessageInLogs;

        if (!includeType && !includeMessage)
        {
            return string.Empty;
        }

        if (includeType && includeMessage)
        {
            return $" - Type: {ex.GetType().FullName}, Message: {ex.Message}";
        }

        if (includeType)
        {
            return $" - Type: {ex.GetType().FullName}";
        }

        return $" - Message: {ex.Message}";
    }
}

