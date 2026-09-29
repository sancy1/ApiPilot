// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiPilotValidationOptions.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Mutable configuration for ApiPilot validation behavior
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a
//   Used by    : ApiPilotValidationFilter, ApiPilotValidationEndpointFilter
//   See also   : Validation/ApiPilotValidationFilter.cs, Validation/ApiPilotValidateAttribute.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Mutable configuration for ApiPilot validation behavior. Intended for
/// registration through the AddApiPilotValidation extension method. All
/// properties have library defaults; applications only set what they need
/// to override.
/// </summary>
public sealed class ApiPilotValidationOptions
{
    /// <summary>
    /// Overrides how field keys are normalized before being surfaced in the
    /// validation error response. When null (the default), the library uses
    /// <c>FieldKeyNormalizer.Normalize</c>, which converts PascalCase and
    /// other input key formats into camelCase with bracket notation preserved.
    /// Supply a delegate to replace the normalization entirely; to disable
    /// normalization, supply the identity function <c>key =&gt; key</c>.
    /// </summary>
    /// <remarks>
    /// The transform receives the raw key emitted by the model binder and
    /// returns the key that appears in the response. It is applied once per
    /// field. Exceptions thrown by the transform propagate to the caller;
    /// the library does not swallow them.
    /// </remarks>
    public Func<string, string>? KeyTransform { get; set; }
}

