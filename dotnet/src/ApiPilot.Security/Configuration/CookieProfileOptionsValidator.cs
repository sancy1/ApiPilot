// filepath: dotnet/src/ApiPilot.Security/Configuration/CookieProfileOptionsValidator.cs
// layer: Configuration | package: ApiPilot.Security | since: v0.3.0
// purpose: Startup validation for CookieProfileOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<CookieProfileOptions>
//   Depends on : CookieProfileOptions, CookieProfileValidator, HostPrefixValidator
//   Used by    : AddApiPilotCookies via TryAddEnumerable
//   See also   : CookieProfileOptions.cs, CookieProfileValidator.cs, SPEC.md (Cookie baseline)
// -----------------------------------------------------------------------------

using ApiPilot.Security.Cookies;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Configuration;

/// <summary>
/// Validates the CookieProfileOptions configuration at startup. Fails
/// when the NamePrefix is not one of the defined prefixes, when the
/// effective name of any profile is empty or contains a character
/// forbidden by RFC 6265, when a Path is empty or does not start with
/// a slash, when a Domain is set but not a valid hostname, when a
/// SameSite value is not a defined member, when SameSite=None is set
/// without Secure, or when the RFC 6265bis prefix rules are violated.
/// </summary>
/// <remarks>
/// The effective cookie name is NamePrefix + profile.Name. The
/// prefix rules are validated against the effective name, not against
/// the prefix or the name in isolation.
///
/// This validator is registered by AddApiPilotCookies. To replace or
/// disable it, remove the IValidateOptions service from the container
/// and register your own before the container is built.
/// </remarks>
public sealed class CookieProfileOptionsValidator : IValidateOptions<CookieProfileOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CookieProfileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.NamePrefix is null)
        {
            failures.Add("CookieProfileOptions.NamePrefix must not be null.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (!HostPrefixValidator.IsDefinedPrefix(options.NamePrefix))
        {
            failures.Add(
                $"CookieProfileOptions.NamePrefix {options.NamePrefix} is not one of the defined prefixes. " +
                "Use the empty string, __Host-, or __Secure-.");
            return ValidateOptionsResult.Fail(failures);
        }

        ValidateProfile("AuthenticationProfile", options.AuthenticationProfile, options.NamePrefix, failures);
        ValidateProfile("SessionProfile", options.SessionProfile, options.NamePrefix, failures);
        ValidateProfile("CsrfProfile", options.CsrfProfile, options.NamePrefix, failures);

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateProfile(
        string label,
        CookieProfile? profile,
        string namePrefix,
        List<string> failures)
    {
        if (profile is null)
        {
            failures.Add($"CookieProfileOptions.{label} must not be null.");
            return;
        }

        var effectiveName = namePrefix + profile.Name;

        // The per-profile validator adds its own failure messages. The
        // label prefix identifies which profile the message belongs to.
        var profileFailures = new List<string>();
        CookieProfileValidator.Validate(effectiveName, profile, profileFailures);

        foreach (var f in profileFailures)
        {
            failures.Add($"{label}: {f}");
        }
    }
}

