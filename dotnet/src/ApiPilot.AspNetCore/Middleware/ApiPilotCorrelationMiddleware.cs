// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotCorrelationMiddleware.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Reads, validates, generates, and echoes the request correlation ID
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed middleware class)
//   Depends on : CorrelationOptions, CorrelationIdValidator, ICorrelationIdGenerator,
//                ErrorResponseResult, ValidationResponseFactory, ILogger
//   Used by    : ApiPilotCorrelationExtensions
//   See also   : CorrelationOptions.cs, SPEC.md (correlation ID rules)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Reads the incoming correlation ID from the configured header,
/// validates it, generates a replacement when required, stores the
/// effective ID on the HttpContext, echoes it in the response header
/// when configured, and calls the next middleware.
/// </summary>
/// <remarks>
/// The middleware consults every Option D override exposed by
/// CorrelationOptions: HeaderName, ValidateIncoming, ValidationPattern,
/// EchoInResponseHeader, and InvalidIncomingIdPolicy. The validator is
/// constructed once in the constructor from the configured pattern;
/// it is not rebuilt per request.
/// </remarks>
public sealed partial class ApiPilotCorrelationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptions<CorrelationOptions> _options;
    private readonly ICorrelationIdGenerator _generator;
    private readonly ILogger<ApiPilotCorrelationMiddleware> _logger;
    private readonly CorrelationIdValidator _validator;

    /// <summary>
    /// The HttpContext.Items key under which the effective correlation
    /// ID is stored for the duration of the request.
    /// </summary>
    internal static readonly object CorrelationIdKey = new();

    /// <summary>Creates the middleware.</summary>
    /// <param name="next">The next middleware in the pipeline. Must not be null.</param>
    /// <param name="options">The correlation options. Must not be null.</param>
    /// <param name="generator">The correlation ID generator. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is null.
    /// </exception>
    public ApiPilotCorrelationMiddleware(
        RequestDelegate next,
        IOptions<CorrelationOptions> options,
        ICorrelationIdGenerator generator,
        ILogger<ApiPilotCorrelationMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _options = options;
        _generator = generator;
        _logger = logger;
        _validator = new CorrelationIdValidator(options.Value.ValidationPattern);
    }

    /// <summary>
    /// Executes the middleware. See the class summary for the exact
    /// behavior at each step.
    /// </summary>
    /// <param name="httpContext">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var incoming = httpContext.Request.Headers[_options.Value.HeaderName].ToString();
        string effectiveId;

        if (string.IsNullOrEmpty(incoming))
        {
            effectiveId = _generator.NewId();
        }
        else if (!_options.Value.ValidateIncoming || _validator.IsValid(incoming))
        {
            effectiveId = incoming;
        }
        else
        {
            switch (_options.Value.InvalidIncomingIdPolicy)
            {
                case CorrelationInvalidIdPolicy.Reject:
                    LogRejectedDelegate(_logger, incoming, httpContext.Request.Method,
                        httpContext.Request.Path.Value ?? string.Empty, null);
                    await WriteRejectionAsync(httpContext, incoming);
                    return;

                case CorrelationInvalidIdPolicy.UseAsIs:
                    LogInvalidIdDelegate(_logger, incoming, httpContext.Request.Method,
                        httpContext.Request.Path.Value ?? string.Empty, "UseAsIs", null);
                    effectiveId = incoming;
                    break;

                default:
                    LogInvalidIdDelegate(_logger, incoming, httpContext.Request.Method,
                        httpContext.Request.Path.Value ?? string.Empty, "Replace", null);
                    effectiveId = _generator.NewId();
                    break;
            }
        }

        httpContext.Items[CorrelationIdKey] = effectiveId;

        if (_options.Value.EchoInResponseHeader)
        {
            httpContext.Response.Headers[_options.Value.HeaderName] = effectiveId;
        }

        await _next(httpContext);
    }

    private async Task WriteRejectionAsync(HttpContext httpContext, string incoming)
    {
        var field = ApiErrorField.WithMessage(
            _options.Value.HeaderName,
            "The incoming correlation ID is not valid.");

        var requestId = _options.Value.EchoInResponseBody ? incoming : string.Empty;
        var meta = new ResponseMetadata { RequestId = requestId };
        var response = ValidationResponseFactory.FromFields(new[] { field }, meta);
        await new ErrorResponseResult(response).ExecuteAsync(httpContext);
    }
}

