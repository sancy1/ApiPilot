// filepath: dotnet/src/ApiPilot.AspNetCore/Validation/ApiPilotValidateAttribute.cs
// layer: Validation | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Attribute that applies the ApiPilot validation filter to MVC actions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Mvc.TypeFilterAttribute (IFilterFactory)
//   Depends on : ApiPilotValidationFilter
//   Used by    : MVC controllers and actions via [ApiPilotValidate]
//   See also   : ApiPilotValidationFilter.cs, ApiPilotValidationOptions.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace ApiPilot.AspNetCore.Validation;

/// <summary>
/// Applies the ApiPilot validation filter to a controller action or to every
/// action in a controller. The filter checks ModelState before the action
/// runs and, when the model state is invalid, writes a standard
/// VALIDATION_ERROR envelope without invoking the action.
/// </summary>
/// <remarks>
/// The attribute is a TypeFilterAttribute: it does not contain any logic
/// itself, and MVC resolves the actual filter through the application's
/// service provider. Key transformation is configured through
/// ApiPilotValidationOptions, not through the attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiPilotValidateAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Creates the attribute. The framework resolves the underlying filter
    /// from DI on each request.
    /// </summary>
    public ApiPilotValidateAttribute()
        : base(typeof(ApiPilotValidationFilter))
    {
    }
}

