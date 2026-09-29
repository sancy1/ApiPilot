// filepath: dotnet/src/ApiPilot.Security/Cookies/CookieSameSite.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: The SameSite attribute value for a cookie profile
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : CookieProfile, CookieProfileValidator
//   See also   : CookieProfile.cs, AuthenticationCookieProfile.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// The SameSite attribute value for a cookie profile. Mirrors the
/// three values defined by RFC 6265bis. ApiPilot defines its own
/// enum so the profile value object does not depend on the ASP.NET
/// Core SameSiteMode type. The application converts at apply time.
/// </summary>
public enum CookieSameSite
{
    /// <summary>
    /// The SameSite attribute is not set. Some older clients default
    /// to Lax when the attribute is absent; others treat the cookie as
    /// cross-site. The setting is not recommended and is not the
    /// default for any ApiPilot profile.
    /// </summary>
    Unspecified,

    /// <summary>
    /// The browser sends the cookie on same-site requests and on
    /// top-level cross-site navigations. The default for the
    /// authentication cookie profile.
    /// </summary>
    Lax,

    /// <summary>
    /// The browser sends the cookie only on same-site requests. The
    /// default for the session cookie profile. The strongest of the
    /// three values, but it can break cross-site redirect flows.
    /// </summary>
    Strict,

    /// <summary>
    /// The browser sends the cookie on same-site and cross-site
    /// requests. The value requires Secure=true. The weakest of the
    /// three values and the reason the validator exists.
    /// </summary>
    None,
}

