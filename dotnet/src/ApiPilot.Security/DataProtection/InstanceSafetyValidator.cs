// filepath: dotnet/src/ApiPilot.Security/DataProtection/InstanceSafetyValidator.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: Startup validation for multi-instance Data Protection configuration
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ApiPilotDataProtectionOptions>
//   Depends on : ApiPilotDataProtectionOptions
//   Used by    : ApiPilotDataProtectionExtensions via TryAddEnumerable
//   See also   : ApiPilotDataProtectionOptions.cs, ApiPilotDataProtectionExtensions.cs, SPEC.md (Data Protection)
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Validates the ApiPilotDataProtectionOptions configuration at startup. Fails
/// closed when MultiInstance is true and no KeyStorage delegate was
/// configured through the approved ApiPilot registration path.
/// </summary>
/// <remarks>
/// The validator enforces the documented contract:
///
///     MultiInstance = true requires the application to explicitly
///     configure shared or persistent key storage through the approved
///     ApiPilot registration path.
///
/// The validator cannot verify that the KeyStorage delegate configured
/// a persistent store. It records only that the delegate ran. An
/// application that supplies a delegate that does nothing has violated
/// the contract; the library trusts the declaration.
///
/// The validator is deliberately free of reflection and framework
/// internals. The check is on the ApiPilotDataProtectionOptions state.
/// </remarks>
public sealed class InstanceSafetyValidator : IValidateOptions<ApiPilotDataProtectionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ApiPilotDataProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.MultiInstance)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.KeyStorageConfigured)
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(
            new[]
            {
                "ApiPilotDataProtectionOptions.MultiInstance is true but no KeyStorage delegate " +
                "was supplied through the approved ApiPilot registration path. " +
                "MultiInstance = true requires the application to explicitly configure " +
                "shared or persistent key storage through AddApiPilotDataProtection.",
            });
    }
}

