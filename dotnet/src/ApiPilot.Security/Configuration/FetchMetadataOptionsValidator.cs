// filepath: dotnet/src/ApiPilot.Security/Configuration/FetchMetadataOptionsValidator.cs
// layer: Configuration | package: ApiPilot.Security | since: v0.3.0
// purpose: Startup validation for FetchMetadataOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<FetchMetadataOptions>
//   Depends on : FetchMetadataOptions, FetchMetadataProfile
//   Used by    : AddApiPilotFetchMetadata via TryAddEnumerable
//   See also   : FetchMetadataOptions.cs, FetchMetadataEvaluator.cs, SPEC.md (Fetch Metadata)
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Configuration;

/// <summary>
/// Validates the FetchMetadataOptions configuration at startup. When
/// the profile is Off the inner checks are skipped; the policy is
/// disabled. When the profile is Compat or Strict, the allow-lists
/// must not be empty and the profile must be a defined member. Under
/// Strict, cross-site must not be in AllowedSiteValues.
/// </summary>
public sealed class FetchMetadataOptionsValidator : IValidateOptions<FetchMetadataOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FetchMetadataOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Enum.IsDefined(options.Profile))
        {
            failures.Add("FetchMetadataOptions.Profile is not a defined FetchMetadataProfile value.");
            return ValidateOptionsResult.Fail(failures);
        }

        if (options.Profile == FetchMetadataProfile.Off)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.AllowedSiteValues.Count == 0)
        {
            failures.Add("FetchMetadataOptions.AllowedSiteValues must not be empty when the profile is enabled.");
        }

        if (options.AllowedModeValues.Count == 0)
        {
            failures.Add("FetchMetadataOptions.AllowedModeValues must not be empty when the profile is enabled.");
        }

        if (options.Profile == FetchMetadataProfile.Strict
            && options.AllowedSiteValues.Contains("cross-site"))
        {
            failures.Add(
                "FetchMetadataOptions: cross-site is in AllowedSiteValues under Strict. " +
                "The Strict profile exists to reject cross-site; allowing it makes the policy a no-op.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

