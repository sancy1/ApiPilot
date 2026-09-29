// filepath: dotnet/src/ApiPilot.Security/Cookies/CookieProfileValidator.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: Validates a single cookie profile against the security contract
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : CookieProfile, CookieSameSite, HostPrefixValidator
//   Used by    : CookieProfileOptionsValidator
//   See also   : CookieProfile.cs, HostPrefixValidator.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// Validates a single cookie profile against the security contract in
/// SPEC.md. The validator is a pure function. It adds a failure message
/// for each violated rule to the caller-supplied list and never throws
/// for a validation failure.
/// </summary>
/// <remarks>
/// The validation checks:
///
///   - The effective name (prefix + base name) is non-empty and does
///     not contain any character RFC 6265 forbids in a cookie name.
///   - The Path is non-empty and starts with a slash.
///   - The Domain, when set, is a syntactically valid hostname without
///     a scheme, path, port, or whitespace.
///   - The SameSite value is a defined CookieSameSite member.
///   - SameSite=None requires Secure=true.
///   - The prefix rules from HostPrefixValidator (see RFC 6265bis).
///
/// The checks do not short-circuit. Every violated rule is reported.
/// </remarks>
public static class CookieProfileValidator
{
    /// <summary>
    /// Validates the profile against the effective name. The effective
    /// name is the composed CookieProfileOptions.NamePrefix + the
    /// profile Name.
    /// </summary>
    /// <param name="effectiveName">
    /// The effective cookie name. Must not be null.
    /// </param>
    /// <param name="profile">
    /// The cookie profile. Must not be null.
    /// </param>
    /// <param name="failures">
    /// The list to which failure messages are added. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is null.
    /// </exception>
    public static void Validate(
        string effectiveName,
        CookieProfile profile,
        IList<string> failures)
    {
        ArgumentNullException.ThrowIfNull(effectiveName);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(failures);

        if (string.IsNullOrEmpty(effectiveName))
        {
            failures.Add("Cookie effective name must not be empty.");
        }
        else
        {
            ValidateNameCharacters(effectiveName, failures);
        }

        if (string.IsNullOrEmpty(profile.Path))
        {
            failures.Add("Cookie Path must not be empty.");
        }
        else if (!profile.Path.StartsWith('/'))
        {
            failures.Add("Cookie Path must start with a slash.");
        }

        if (profile.Domain is not null)
        {
            if (profile.Domain.Length == 0)
            {
                failures.Add("Cookie Domain must not be empty when set. Set it to null for a host-only cookie.");
            }
            else if (!IsValidHostname(profile.Domain))
            {
                failures.Add($"Cookie Domain {profile.Domain} is not a valid hostname.");
            }
        }

        if (!Enum.IsDefined(profile.SameSite))
        {
            failures.Add("Cookie SameSite value is not a defined CookieSameSite member.");
        }
        else if (profile.SameSite == CookieSameSite.None && !profile.Secure)
        {
            failures.Add("Cookie SameSite=None requires Secure=true.");
        }

        HostPrefixValidator.Validate(effectiveName, profile, failures);
    }

    private static void ValidateNameCharacters(string name, IList<string> failures)
    {
        // RFC 6265 forbids these characters in a cookie name. Reject
        // them explicitly so an ambiguous name fails closed.
        foreach (var c in name)
        {
            if (c == ',' || c == ';' || c == ' ' || c == '"' || c == '\\'
                || char.IsControl(c))
            {
                failures.Add($"Cookie name {name} contains a character forbidden by RFC 6265.");
                return;
            }
        }
    }

    private static bool IsValidHostname(string hostname)
    {
        if (hostname.StartsWith('.' )
            || hostname.EndsWith('.'))
        {
            return false;
        }
        foreach (var c in hostname)
        {
            if (char.IsWhiteSpace(c) || c == '/' || c == ':' || c == '@')
            {
                return false;
            }
        }
        return true;
    }
}

