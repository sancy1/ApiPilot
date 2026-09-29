// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ApiPilotInvalidModelStateResponseFactory.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Factory that produces the standard validation envelope for [ApiController] ModelState failures
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with factory method)
//   Depends on : ApiPilotValidationOptions, ValidationKeyTransforms, ModelStateAdapter,
//                ValidationResponseFactory, ErrorResponseResult, CorrelationOptions
//   Used by    : wired into ApiBehaviorOptions.InvalidModelStateResponseFactory by
//                AddApiPilotControllers
//   See also   : ApiPilotValidationFilter.cs, ApiPilotServiceCollectionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// Produces the standard ApiPilot validation envelope when MVC has already
/// determined that ModelState is invalid for a controller marked with
/// [ApiController]. Wired into ApiBehaviorOptions by AddApiPilotControllers.
/// </summary>
/// <remarks>
/// [ApiController] short-circuits invalid ModelState with a ProblemDetails
/// response before any action filter runs. This factory replaces that
/// short-circuit with the ApiPilot wire shape. It shares the same key
/// transform, ModelState adapter, and response factory with the action filter
/// path, so both paths produce identical envelopes for the same ModelState.
/// </remarks>
public static class ApiPilotInvalidModelStateResponseFactory
{
    /// <summary>
    /// Builds the validation envelope for the given ActionContext. This is
    /// the method that is assigned to ApiBehaviorOptions.InvalidModelStateResponseFactory.
    /// </summary>
    /// <param name="context">The action context. Must not be null.</param>
    /// <returns>An IActionResult that writes the standard validation envelope.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    public static IActionResult Create(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<ApiPilotValidationOptions>>()
            .Value;

        var exceptionOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<ApiExceptionOptions>>()
            .Value;
        var keyTransform = ValidationKeyTransforms.Resolve(options);

        var fields = ModelStateAdapter.ToErrors(context.ModelState, keyTransform);

        if (fields.Count == 0)
        {
            fields = new[]
            {
                ApiErrorField.WithMessage(
                    "_request",
                    "The request could not be validated.")
            };
        }

        var correlationOptions = context.HttpContext.RequestServices
            .GetService<IOptions<CorrelationOptions>>()?.Value;
        var echoInBody = correlationOptions?.EchoInResponseBody ?? true;
        var requestId = echoInBody ? context.HttpContext.TraceIdentifier : string.Empty;
        var meta = new ResponseMetadata { RequestId = requestId };
        var response = ValidationResponseFactory.FromFields(fields, meta);

        return new ErrorResponseResult(response, exceptionOptions);
    }
}

