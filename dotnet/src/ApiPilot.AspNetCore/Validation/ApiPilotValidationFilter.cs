// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ApiPilotValidationFilter.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Internal MVC action filter that validates ModelState and writes the standard envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Mvc.Filters.IActionFilter
//   Depends on : ApiPilotValidationOptions, ModelStateAdapter, ErrorResponseResult,
//                ValidationResponseFactory, IOptions, CorrelationOptions
//   Used by    : ApiPilotValidateAttribute (via TypeFilterAttribute)
//   See also   : ApiPilotValidateAttribute.cs, ApiPilotValidationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Validation;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// MVC action filter that checks ModelState before the action runs. When
/// the model state is invalid, the filter short-circuits the pipeline and
/// writes a standard VALIDATION_ERROR envelope. Field keys are normalized
/// using the transform configured in ApiPilotValidationOptions, or the
/// default FieldKeyNormalizer when no transform was configured.
/// </summary>
/// <remarks>
/// This filter depends on IOptions of ApiPilotValidationOptions. In MVC
/// applications, AddMvcCore registers the options infrastructure, so the
/// dependency is always satisfiable without an explicit AddApiPilotValidation
/// call. Unit tests that construct a ServiceCollection directly must call
/// AddOptions before resolving this filter. Do not change the constructor
/// to accept a nullable IOptions; that hides the real contract.
/// </remarks>
internal sealed class ApiPilotValidationFilter : IActionFilter
{
    private readonly Func<string, string> _keyTransform;

    private readonly ApiExceptionOptions _exceptionOptions;

    private readonly IOptions<CorrelationOptions>? _correlationOptions;

    /// <summary>
    /// Creates the filter, resolving the effective key transform once.
    /// </summary>
    /// <param name="options">
    /// The validation options. Must not be null. The framework always
    /// supplies a non-null IOptions in MVC applications.
    /// </param>
    /// <param name="exceptionOptions">
    /// The exception options that may include error-code-to-status overrides.
    /// Must not be null.
    /// </param>
    /// <param name="correlationOptions">
    /// Optional correlation options. When null, EchoInResponseBody is
    /// treated as true.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public ApiPilotValidationFilter(
        IOptions<ApiPilotValidationOptions> options,
        IOptions<ApiExceptionOptions> exceptionOptions,
        IOptions<CorrelationOptions>? correlationOptions = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _keyTransform = ValidationKeyTransforms.Resolve(options.Value);
        ArgumentNullException.ThrowIfNull(exceptionOptions);
        _exceptionOptions = exceptionOptions.Value;
        _correlationOptions = correlationOptions;
    }

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.ModelState.IsValid)
        {
            return;
        }

        var fields = ModelStateAdapter.ToErrors(context.ModelState, _keyTransform);

        if (fields.Count == 0)
        {
            fields = new[]
            {
                ApiErrorField.WithMessage(
                    "_request",
                    "The request could not be validated.")
            };
        }

        var echoInBody = _correlationOptions?.Value.EchoInResponseBody ?? true;
        var requestId = echoInBody ? context.HttpContext.TraceIdentifier : string.Empty;
        var meta = new ResponseMetadata { RequestId = requestId };
        var response = ValidationResponseFactory.FromFields(fields, meta);

        context.Result = new ErrorResponseResult(response, _exceptionOptions);
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No work; this filter only acts before the action runs.
    }
}

