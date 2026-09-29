// filepath: dotnet/src/ApiPilot.Security/Origin/OriginPolicyOptions.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Configuration for the defense-in-depth Origin policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : OriginMatchMode
//   Used by    : OriginMiddleware, OriginValidator, OriginPolicyOptionsValidator
//   See also   : OriginMatchMode.cs, OriginValidator.cs, SPEC.md (Origin policy)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Origin;

/// <summary>
/// Configuration for the defense-in-depth Origin policy. The policy
/// runs on protected methods and rejects a request whose Origin does
/// not match an allowed origin. The policy is not a replacement for
/// CSRF protection. It is a supplementary check.
/// </summary>
/// <remarks>
/// AllowSameOrigin and AllowMissingOrigin are independent. The first
/// permits a present Origin that matches the request's own origin.
/// The second permits an absent Origin. Neither implies the other.
///
/// The Referer fallback is opt-in. When enabled and the Origin header
/// is absent, the Referer is parsed to extract its origin and the
/// comparison runs against that. A Referer that cannot be parsed is
/// treated as a non-match.
///
/// The RejectionCode defaults to CSRF_ORIGIN_REJECTED, matching the
/// wire contract in SPEC.md. Applications that use a different code
/// for this rejection override it.
/// </remarks>
public sealed class OriginPolicyOptions
{
    /// <summary>
    /// Whether the Origin policy is enabled. Defaults to true. When
    /// false, the middleware passes every request through and the
    /// startup validator does not check the configuration.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The set of allowed origins. Each entry is an origin string of
    /// the form scheme://host[:port]. Comparison is case-insensitive
    /// on the scheme and host, and normalizes the port to the scheme
    /// default when absent. The set is case-sensitive by structure
    /// because the origin syntax uses lowercase by convention.
    /// </summary>
    public ISet<string> AllowedOrigins { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether a present Origin that matches the request's own origin
    /// is permitted without being in AllowedOrigins. Defaults to true.
    /// This is independent of AllowMissingOrigin.
    /// </summary>
    public bool AllowSameOrigin { get; set; } = true;

    /// <summary>
    /// Whether a missing Origin header is permitted. Defaults to true.
    /// The default matches the practice of modern browsers that omit
    /// the header on same-origin requests. An application that wants
    /// the Origin policy to reject a missing Origin on protected
    /// methods sets this to false.
    /// </summary>
    public bool AllowMissingOrigin { get; set; } = true;

    /// <summary>
    /// Whether the Referer fallback is enabled. Defaults to false. When
    /// true and the Origin header is absent, the Referer is parsed and
    /// its origin is compared against AllowedOrigins. The fallback is
    /// opt-in because the Referer header is less reliable than Origin.
    /// </summary>
    public bool AllowRefererFallback { get; set; }

    /// <summary>
    /// The origin comparison mode. Defaults to Exact (scheme, host,
    /// and normalized port must match). See OriginMatchMode for the
    /// defined values.
    /// </summary>
    public OriginMatchMode MatchMode { get; set; } = OriginMatchMode.Exact;

    /// <summary>
    /// The public wire code for a rejected origin. Defaults to
    /// CSRF_ORIGIN_REJECTED, matching SPEC.md. Applications that use
    /// a different code override it.
    /// </summary>
    public string RejectionCode { get; set; } = "CSRF_ORIGIN_REJECTED";
}

