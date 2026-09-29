// filepath: dotnet/src/ApiPilot.Security/Cookies/CsrfCookieProfile.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: The default CSRF double-submit cookie profile
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : CookieProfile, CookieSameSite
//   Used by    : CookieProfileOptions
//   See also   : CookieProfile.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// The default CSRF double-submit cookie profile. The profile is
/// provided for applications whose legacy clients cannot hold a CSRF
/// request token in memory and must rely on the double-submit
/// pattern. Applications that use the in-memory CSRF token (the
/// primary API in this library) do not need a CSRF cookie at all.
/// </summary>
/// <remarks>
/// The profile sets HttpOnly=false. This is necessary only because
/// the double-submit pattern requires browser JavaScript to read the
/// cookie value and echo it in the request header.
///
/// HttpOnly=false is not inherently safer than HttpOnly=true. It
/// increases exposure to XSS: any script on the origin can read the
/// cookie value. The double-submit cookie should only be enabled
/// when the trade-off is deliberate. The recommended pattern is to
/// hold the CSRF token in memory and skip the cookie entirely.
///
/// The profile never carries a credential. Its value is a CSRF
/// token, not an authentication credential. The authentication and
/// session cookies remain HttpOnly.
/// </remarks>
public static class CsrfCookieProfile
{
    /// <summary>
    /// The default CSRF double-submit cookie profile. Secure,
    /// HttpOnly=false (readable by JavaScript), SameSite=Lax,
    /// Path=/, no Domain (host-only). The base name is "csrf"; the
    /// effective name is NamePrefix + "csrf".
    /// </summary>
    public static CookieProfile Default { get; } = new()
    {
        Name = "csrf",
        Secure = true,
        HttpOnly = false,
        SameSite = CookieSameSite.Lax,
        Path = "/",
        Domain = null,
    };
}

