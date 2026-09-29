// filepath: dotnet/src/ApiPilot.Security/Cookies/CookieProfile.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: A value object describing a cookie profile for validation
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : CookieSameSite
//   Used by    : CookieProfileValidator, CookieProfileOptions, AuthenticationCookieProfile, CsrfCookieProfile
//   See also   : CookieSameSite.cs, CookieProfileValidator.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// A value object describing a cookie profile. The profile carries the
/// base name without a prefix, the two security flags, the SameSite
/// value, the path, and the optional domain. It does not carry the
/// cookie value and is never used to set a cookie. ApiPilot validates
/// the profile; the application applies it to its own cookie
/// configuration.
/// </summary>
/// <remarks>
/// The Name property is the base cookie name. The effective cookie
/// name is computed as CookieProfileOptions.NamePrefix + Name. The
/// prefix rules from RFC 6265bis apply to the effective name, not to
/// the base name.
///
/// The Path property is the configured cookie path. ApiPilot does not
/// claim a specific path is "narrow". The path must be syntactically
/// valid: non-empty and starting with a slash. A value of "/" is the
/// widest possible scope. It is required by RFC 6265bis for cookies
/// whose effective name starts with the __Host- prefix; for all other
/// cookies the application should choose the narrowest path that
/// serves its needs.
///
/// The Domain property is optional. When null, the cookie is host-only
/// (the strongest setting). When set, the value must be a valid
/// hostname without scheme, path, port, or whitespace.
/// </remarks>
public sealed record CookieProfile
{
    /// <summary>
    /// The base cookie name, without any prefix. Must be non-empty and
    /// must not contain any of the characters that RFC 6265 forbids in
    /// a cookie name (",", ";", space, double quote, backslash, or a
    /// control character).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Whether the cookie is marked Secure. When true, the browser sends
    /// the cookie only over HTTPS. Required for cookies whose effective
    /// name starts with __Host- or __Secure-, and required when SameSite
    /// is None.
    /// </summary>
    public required bool Secure { get; init; }

    /// <summary>
    /// Whether the cookie is marked HttpOnly. When true, browser
    /// JavaScript cannot read the cookie value. Required for cookies
    /// that carry a credential. When false, the value is readable by
    /// any script on the origin and is exposed to XSS.
    /// </summary>
    public required bool HttpOnly { get; init; }

    /// <summary>
    /// The SameSite attribute value. See CookieSameSite for the defined
    /// values and their trade-offs.
    /// </summary>
    public required CookieSameSite SameSite { get; init; }

    /// <summary>
    /// The cookie path. Must be non-empty and must start with a slash.
    /// Defaults to "/" which is the widest possible scope. Choose the
    /// narrowest path that serves the cookie purpose.
    /// </summary>
    public string Path { get; init; } = "/";

    /// <summary>
    /// The cookie domain. When null, the cookie is host-only, which is
    /// the strongest setting. When set, the value must be a valid
    /// hostname without a scheme, path, port, or whitespace.
    /// </summary>
    public string? Domain { get; init; }
}

