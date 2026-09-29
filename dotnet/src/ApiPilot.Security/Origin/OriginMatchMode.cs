// filepath: dotnet/src/ApiPilot.Security/Origin/OriginMatchMode.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: The origin comparison mode for the Origin policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : OriginPolicyOptions, OriginValidator
//   See also   : OriginPolicyOptions.cs, OriginValidator.cs, SPEC.md (Origin policy)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Origin;

/// <summary>
/// The origin comparison mode for the Origin policy. The default is
/// Exact. AnyPortSameHost relaxes the port check. SameSite is not
/// implemented in this phase and is treated as a non-match.
/// </summary>
public enum OriginMatchMode
{
    /// <summary>
    /// The request Origin must match an allowed origin by scheme,
    /// host (case-insensitive), and port. Missing ports are
    /// normalized to the scheme default (443 for https, 80 for http).
    /// This is the default and the strictest mode.
    /// </summary>
    Exact,

    /// <summary>
    /// The request Origin must match an allowed origin by scheme and
    /// host (case-insensitive). The port is ignored. Applications
    /// whose clients access the same host on multiple ports use this
    /// mode.
    /// </summary>
    AnyPortSameHost,

    /// <summary>
    /// The request Origin must share a registrable domain (eTLD+1)
    /// with an allowed origin. This mode is NOT implemented in this
    /// phase. The validator treats it as a non-match. The mode exists
    /// in the enum so the surface is stable; a future phase that
    /// bundles a public suffix mechanism can implement it without
    /// changing the enum.
    /// </summary>
    SameSite,
}

