// filepath: dotnet/src/ApiPilot.Security/Configuration/OriginPolicyOptionsValidator.cs
// layer: Configuration | package: ApiPilot.Security | since: v0.3.0
// purpose: Startup validation for OriginPolicyOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<OriginPolicyOptions>
//   Depends on : OriginPolicyOptions, OriginMatchMode, OriginValidator
//   Used by    : AddApiPilotOriginPolicy via TryAddEnumerable
//   See also   : OriginPolicyOptions.cs, OriginValidator.cs, SPEC.md (Origin policy)
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Configuration;

/// <summary>
/// Validates the OriginPolicyOptions configuration at startup. Fails
/// when the policy is enabled and no request could ever pass, when a
/// configured origin is malformed, when the match mode is SameSite
/// (not implemented), or when the rejection code is empty.
/// </summary>
/// <remarks>
/// The fail-closed rule: when Enabled is true and AllowSameOrigin is
/// false, AllowedOrigins must contain at least one entry. Otherwise
/// no request could pass, and the policy is a silent deny-all.
/// </remarks>
public sealed class OriginPolicyOptionsValidator : IValidateOptions<OriginPolicyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OriginPolicyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.AllowedOrigins.Count == 0 && !options.AllowSameOrigin)
        {
            failures.Add(
                "OriginPolicyOptions.AllowedOrigins must not be empty when AllowSameOrigin is false " +
                "and the policy is enabled, because no request would ever pass.");
        }

        foreach (var origin in options.AllowedOrigins)
        {
            if (!OriginValidator.TryParseOrigin(origin, out _))
            {
                failures.Add($"OriginPolicyOptions.AllowedOrigins entry {origin} is not a valid origin.");
            }
        }

        if (!Enum.IsDefined(options.MatchMode))
        {
            failures.Add("OriginPolicyOptions.MatchMode is not a defined OriginMatchMode value.");
        }
        else if (options.MatchMode == OriginMatchMode.SameSite)
        {
            failures.Add(
                "OriginPolicyOptions.MatchMode SameSite is not implemented in this version. " +
                "Use Exact or AnyPortSameHost, or list the specific origins in AllowedOrigins.");
        }

        if (string.IsNullOrEmpty(options.RejectionCode))
        {
            failures.Add("OriginPolicyOptions.RejectionCode must not be empty.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

