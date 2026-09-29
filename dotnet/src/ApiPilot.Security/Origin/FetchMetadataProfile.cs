// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataProfile.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: The Fetch Metadata enforcement profile
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : FetchMetadataOptions, FetchMetadataEvaluator, FetchMetadataOptionsValidator
//   See also   : FetchMetadataOptions.cs, FetchMetadataEvaluator.cs, SPEC.md (Fetch Metadata)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Origin;

/// <summary>
/// The Fetch Metadata enforcement profile. Off is the default and the
/// compatibility path. Compat allows a missing Sec-Fetch-Site header
/// so older clients continue to work. Strict rejects a disallowed
/// Sec-Fetch-Site value on protected methods.
/// </summary>
public enum FetchMetadataProfile
{
    /// <summary>
    /// The Fetch Metadata policy is disabled. The middleware passes
    /// every request through regardless of the Sec-Fetch-Site header.
    /// This is the default and the compatibility path.
    /// </summary>
    Off,

    /// <summary>
    /// The policy enforces the Sec-Fetch-Site check when the header is
    /// present and well-formed. A missing or malformed header is
    /// permitted (subject to the AllowMissingHeaders option). This
    /// profile is the middle ground: strong clients get the
    /// protection; older clients continue to work.
    /// </summary>
    Compat,

    /// <summary>
    /// The policy enforces the Sec-Fetch-Site check strictly. A
    /// disallowed value is always rejected. A missing or malformed
    /// header is rejected unless AllowMissingHeaders is true. This is
    /// the strongest profile and the one recommended for modern
    /// applications.
    /// </summary>
    Strict,
}

