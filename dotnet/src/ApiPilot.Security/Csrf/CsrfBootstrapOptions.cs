// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfBootstrapOptions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Bootstrap endpoint configuration for the CSRF service
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a (BCL only)
//   Used by    : CsrfBootstrapEndpoint, CsrfBootstrapExtensions
//   See also   : CsrfBootstrapEndpoint.cs, CsrfBootstrapExtensions.cs, SPEC.md (CSRF bootstrap response)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Bootstrap endpoint configuration. Two Option D points: the cache
/// control header value and whether the endpoint is enabled. The
/// response property name and the response shape are fixed by SPEC.md
/// and are not overridable.
/// </summary>
/// <remarks>
/// The bootstrap endpoint responds with a bare JSON object
/// { "token": "..." } and nothing else. This is the one place in
/// ApiPilot where a response is not the standard success envelope.
/// The wire shape is mandated by SPEC.md. Do not add fields.
///
/// When the endpoint is disabled (Enabled = false), MapApiPilotCsrf
/// registers nothing. The path is not routed and a request to the
/// path falls through to whatever middleware follows.
/// </remarks>
public sealed class CsrfBootstrapOptions
{
    /// <summary>
    /// The Cache-Control header value set on the bootstrap response.
    /// Defaults to "no-store" so that intermediaries do not cache the
    /// token. An application that has a specific cache policy for the
    /// endpoint sets a different value.
    /// </summary>
    public string CacheControl { get; set; } = "no-store";

    /// <summary>
    /// Whether the bootstrap endpoint is enabled. Defaults to true.
    /// When false, MapApiPilotCsrf registers nothing.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

