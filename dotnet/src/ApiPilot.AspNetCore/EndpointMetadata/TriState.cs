// filepath: dotnet/src/ApiPilot.AspNetCore/EndpointMetadata/TriState.cs
// layer: EndpointMetadata | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Three-state enum for attribute properties that must distinguish unset from explicit false
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : PaginationMetadataAttribute, PaginationResolver
//   See also   : PaginationMetadataAttribute.cs, PaginationResolver.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.EndpointMetadata;

/// <summary>
/// A three-state value for attribute properties that must distinguish
/// between "not set" and an explicit boolean value. C# attributes cannot
/// carry nullable value types; this enum provides the missing "unset"
/// state so that an unset attribute property does not override a global
/// default, while an explicit false or true does.
/// </summary>
public enum TriState
{
    /// <summary>
    /// The property was not set. The global configuration value applies
    /// unchanged.
    /// </summary>
    Unset = 0,

    /// <summary>
    /// The property was explicitly set to false. Overrides a global true.
    /// </summary>
    False = 1,

    /// <summary>
    /// The property was explicitly set to true. Overrides a global false.
    /// </summary>
    True = 2,
}

