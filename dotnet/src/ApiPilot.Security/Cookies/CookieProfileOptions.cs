// filepath: dotnet/src/ApiPilot.Security/Cookies/CookieProfileOptions.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: Options that carry the cookie profiles the host validates at startup
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : CookieProfile, AuthenticationCookieProfile, CsrfCookieProfile
//   Used by    : CookieProfileOptionsValidator, AddApiPilotCookies
//   See also   : AuthenticationCookieProfile.cs, CsrfCookieProfile.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// Options that carry the cookie profiles the host validates at
/// startup. Every profile is validated against the security contract
/// in SPEC.md. An invalid profile fails the host startup.
/// </summary>
/// <remarks>
/// The NamePrefix is composed with each profile Name to produce the
/// effective cookie name. The prefix rules from RFC 6265bis are
/// validated against the effective name, not against the prefix or
/// the name in isolation.
///
/// Path and Domain are on each profile, not on the options. This
/// avoids two sources of truth for the same value.
///
/// The library never sets a cookie. It validates the profiles; the
/// application applies them to its own cookie configuration.
/// </remarks>
public sealed class CookieProfileOptions
{
    /// <summary>
    /// The prefix applied to every cookie name. Defaults to the empty
    /// string (no prefix). The defined values are "", "__Host-", and
    /// "__Secure-". Other values are rejected at startup.
    /// </summary>
    public string NamePrefix { get; set; } = string.Empty;

    /// <summary>
    /// The authentication cookie profile. Defaults to Secure,
    /// HttpOnly, SameSite=Lax, Path=/. An application that needs
    /// different values replaces the profile.
    /// </summary>
    public CookieProfile AuthenticationProfile { get; set; } =
        AuthenticationCookieProfile.Default;

    /// <summary>
    /// The session cookie profile. Defaults to Secure, HttpOnly,
    /// SameSite=Strict, Path=/. An application that needs different
    /// values replaces the profile.
    /// </summary>
    public CookieProfile SessionProfile { get; set; } =
        AuthenticationCookieProfile.SessionDefault;

    /// <summary>
    /// The CSRF double-submit cookie profile. Defaults to Secure,
    /// HttpOnly=false (readable by JavaScript), SameSite=Lax. The
    /// profile is only meaningful when the application uses the
    /// double-submit pattern. Applications that hold the CSRF token
    /// in memory do not set this profile.
    /// </summary>
    public CookieProfile CsrfProfile { get; set; } =
        CsrfCookieProfile.Default;
}

