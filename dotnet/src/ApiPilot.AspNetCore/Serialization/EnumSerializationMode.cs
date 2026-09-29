// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/EnumSerializationMode.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: How enum values are serialized in ApiPilot JSON responses
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : ApiPilotJsonOptions, JsonSerializerConfigurator
//   See also   : ApiPilotJsonOptions.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// How enum values are serialized in ApiPilot JSON responses.
/// </summary>
public enum EnumSerializationMode
{
    /// <summary>
    /// Enums are serialized as their names, for example "Confirmed".
    /// This is the default. It is more readable and it is stable if
    /// the underlying enum ordinals change over time.
    /// </summary>
    AsString,

    /// <summary>
    /// Enums are serialized as their underlying integer values, for
    /// example 0. Use this when a strict numeric wire contract is
    /// required.
    /// </summary>
    Number,
}

