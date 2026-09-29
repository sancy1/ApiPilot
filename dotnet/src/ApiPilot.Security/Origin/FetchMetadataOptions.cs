// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataOptions.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Configuration for the Fetch Metadata enforcement profile
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : FetchMetadataProfile
//   Used by    : FetchMetadataEvaluator, FetchMetadataMiddleware, FetchMetadataOptionsValidator
//   See also   : FetchMetadataProfile.cs, FetchMetadataEvaluator.cs, SPEC.md (Fetch Metadata)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Origin;

/// <summary>
/// Configuration for the Fetch Metadata policy. The policy is
/// defense-in-depth. It supplements the Origin policy and the CSRF
/// check. It is not a replacement for either.
/// </summary>
/// <remarks>
/// The policy is disabled by default. The default profile is Off.
/// Applications that want the enforcement set the profile to Compat
/// or Strict.
///
/// AllowMissingHeaders governs the missing-or-malformed case only.
/// It never overrides an explicitly present disallowed value. Under
/// Strict with AllowMissingHeaders=true, a cross-site value is still
/// rejected because cross-site is not in AllowedSiteValues.
/// </remarks>
public sealed class FetchMetadataOptions
{
    /// <summary>
    /// The enforcement profile. Defaults to Off. See
    /// FetchMetadataProfile for the three values and their behavior.
    /// </summary>
    public FetchMetadataProfile Profile { get; set; } = FetchMetadataProfile.Off;

    /// <summary>
    /// The Sec-Fetch-Site values that are permitted on protected
    /// methods. Defaults to same-origin, same-site, and none. The value
    /// cross-site is not in the default set because it is the value the
    /// policy exists to reject. An application that has a legitimate
    /// cross-site flow adds the value explicitly.
    /// </summary>
    public ISet<string> AllowedSiteValues { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "same-origin",
            "same-site",
            "none",
        };

    /// <summary>
    /// The Sec-Fetch-Mode values that are permitted on protected
    /// methods when the header is present. Defaults to cors,
    /// same-origin, and navigate. The values no-cors and websocket are
    /// not in the default set.
    /// </summary>
    public ISet<string> AllowedModeValues { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cors",
            "same-origin",
            "navigate",
        };

    /// <summary>
    /// Whether a missing or malformed Sec-Fetch-Site header is
    /// permitted. Defaults to true. The setting is only consulted when
    /// the header is absent or malformed. It never overrides an
    /// explicitly present disallowed value.
    /// </summary>
    public bool AllowMissingHeaders { get; set; } = true;
}

