// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/DateSerializationMode.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: How date and time values are serialized in ApiPilot JSON responses
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : ApiPilotJsonOptions, JsonSerializerConfigurator
//   See also   : ApiPilotJsonOptions.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// How date and time values are serialized in ApiPilot JSON responses.
/// </summary>
public enum DateSerializationMode
{
    /// <summary>
    /// Dates are serialized as ISO 8601 strings, for example
    /// "2026-09-21T14:30:00Z" for DateTimeOffset, or
    /// "2026-09-21T14:30:00" for DateTime. This is the default. It
    /// is unambiguous, timezone-aware, and readable.
    /// </summary>
    Iso8601,

    /// <summary>
    /// Dates are serialized as Unix epoch seconds (integer). Some clients
    /// and some legacy systems prefer this form. It is compact but less
    /// readable and it loses sub-second precision.
    /// </summary>
    UnixTimeSeconds,
}

