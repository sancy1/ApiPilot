// filepath: dotnet/src/ApiPilot.Security/Cookies/AuthenticationCookieProfile.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: The default authentication and session cookie profiles
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : CookieProfile, CookieSameSite
//   Used by    : CookieProfileOptions
//   See also   : CookieProfile.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// The default authentication and session cookie profiles. Both set
/// Secure, HttpOnly, and a narrow SameSite value. The authentication
/// profile uses SameSite=Lax so top-level navigations carry the
/// cookie. The session profile uses SameSite=Strict for the
/// strongest same-site restriction.
/// </summary>
/// <remarks>
/// These profiles describe the security attributes only. ApiPilot
/// never sets the cookie. The application applies the profile to its
/// own cookie configuration.
/// </remarks>
public static class AuthenticationCookieProfile
{
    /// <summary>
    /// The default authentication cookie profile. Secure, HttpOnly,
    /// SameSite=Lax, Path=/, no Domain (host-only). The base name is
    /// "auth"; the effective name is NamePrefix + "auth".
    /// </summary>
    public static CookieProfile Default { get; } = new()
    {
        Name = "auth",
        Secure = true,
        HttpOnly = true,
        SameSite = CookieSameSite.Lax,
        Path = "/",
        Domain = null,
    };

    /// <summary>
    /// The default session cookie profile. Secure, HttpOnly,
    /// SameSite=Strict, Path=/, no Domain (host-only). The base name
    /// is "session"; the effective name is NamePrefix + "session".
    /// </summary>
    public static CookieProfile SessionDefault { get; } = new()
    {
        Name = "session",
        Secure = true,
        HttpOnly = true,
        SameSite = CookieSameSite.Strict,
        Path = "/",
        Domain = null,
    };
}

