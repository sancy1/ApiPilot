// filepath: dotnet/src/ApiPilot.Security/Cookies/__HostPrefixValidator.cs
// layer: Cookies | package: ApiPilot.Security | since: v0.3.0
// purpose: Validates the RFC 6265bis __Host- and __Secure- prefix rules on a cookie name
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : CookieProfile
//   Used by    : CookieProfileValidator
//   See also   : CookieProfile.cs, CookieProfileValidator.cs, RFC 6265bis
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Cookies;

/// <summary>
/// Validates the RFC 6265bis prefix rules on a cookie name. The
/// effective name is the NamePrefix from CookieProfileOptions composed
/// with the base Name from the profile. This class validates the
/// effective name and the profile attributes together, not the prefix
/// in isolation.
/// </summary>
/// <remarks>
/// The defined prefixes are:
///
///   - __Host- requires Secure=true, Domain unset, Path=/
///   - __Secure- requires Secure=true
///
/// A cookie name that does not start with either prefix has no
/// additional prefix rules. This class adds a failure message for
/// each violated rule to the caller-supplied list. It does not
/// short-circuit; every violated rule is reported.
/// </remarks>
public static class HostPrefixValidator
{
    /// <summary>The RFC 6265bis __Host- prefix.</summary>
    public const string HostPrefix = "__Host-";

    /// <summary>The RFC 6265bis __Secure- prefix.</summary>
    public const string SecurePrefix = "__Secure-";

    /// <summary>
    /// Validates the prefix rules on the effective name and the
    /// profile attributes. Adds a failure message for each violated
    /// rule to the failures list.
    /// </summary>
    /// <param name="effectiveName">
    /// The effective cookie name: NamePrefix + Name. Must not be null.
    /// </param>
    /// <param name="profile">
    /// The cookie profile whose attributes are checked. Must not be null.
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

        if (effectiveName.StartsWith(HostPrefix, StringComparison.Ordinal))
        {
            if (!profile.Secure)
            {
                failures.Add(
                    "Cookies whose effective name starts with __Host- must set Secure=true.");
            }
            if (profile.Domain is not null)
            {
                failures.Add(
                    "Cookies whose effective name starts with __Host- must not set a Domain.");
            }
            if (!string.Equals(profile.Path, "/", StringComparison.Ordinal))
            {
                failures.Add(
                    "Cookies whose effective name starts with __Host- must set Path=/.");
            }
            return;
        }

        if (effectiveName.StartsWith(SecurePrefix, StringComparison.Ordinal))
        {
            if (!profile.Secure)
            {
                failures.Add(
                    "Cookies whose effective name starts with __Secure- must set Secure=true.");
            }
            return;
        }

        // No prefix. No additional rules.
    }

    /// <summary>
    /// Returns true when the given prefix is one of the defined
    /// prefixes or the empty string. Used by the options validator to
    /// reject unknown prefixes.
    /// </summary>
    /// <param name="prefix">The prefix to check. Must not be null.</param>
    /// <returns>True when the prefix is defined or empty.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="prefix"/> is null.
    /// </exception>
    public static bool IsDefinedPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return prefix.Length == 0
            || string.Equals(prefix, HostPrefix, StringComparison.Ordinal)
            || string.Equals(prefix, SecurePrefix, StringComparison.Ordinal);
    }
}

