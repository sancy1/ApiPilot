// filepath: dotnet/src/ApiPilot.Security/Diagnostics/SecurityConfigurationValidator.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.3.0
// purpose: Verifies that the expected security validators are resolvable from the container
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : DiagnosticCodes, Microsoft.Extensions.DependencyInjection,
//                Microsoft.Extensions.Options
//   Used by    : SecurityDiagnosticsHostedService, application code
//   See also   : DiagnosticCodes.cs, SecurityDiagnosticsHostedService.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Cookies;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using ApiPilot.Security.Origin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// Verifies that the expected security validators are resolvable from
/// the container. The class does not run validation; the
/// ValidateOnStart hook does that. This class reports whether the
/// validators are present, so an operator can see a missing validator
/// before a misconfiguration slips through.
/// </summary>
/// <remarks>
/// The class is deliberately not an orchestrator. It does not call
/// IValidateOptions&lt;T&gt;.Validate. It resolves the registered
/// validators and reports what is present. Validation remains the
/// framework's responsibility.
/// 
/// The check runs after the container is built, so it can resolve the
/// IEnumerable&lt;IValidateOptions&lt;T&gt;&gt; registrations directly.
/// </remarks>
public sealed class SecurityConfigurationValidator
{
    private readonly IServiceProvider _provider;

    /// <summary>Creates the checker.</summary>
    /// <param name="provider">
    /// The built service provider. Must not be null. The provider is
    /// queried but not mutated.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> is null.
    /// </exception>
    public SecurityConfigurationValidator(IServiceProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _provider = provider;
    }

    /// <summary>
    /// Returns the diagnostics for the current registration state.
    /// A missing validator produces a SecurityDiagnostic with the
    /// ValidatorMissing code and the Fatal level.
    /// </summary>
    /// <returns>A read-only list of diagnostics. May be empty.</returns>
    public IReadOnlyList<SecurityDiagnostic> GetDiagnostics()
    {
        var result = new List<SecurityDiagnostic>();

        CheckValidator<CsrfOptions, CsrfOptionsValidator>(result, "CsrfOptions");
        CheckValidator<CookieProfileOptions, CookieProfileOptionsValidator>(result, "CookieProfileOptions");
        CheckValidator<OriginPolicyOptions, OriginPolicyOptionsValidator>(result, "OriginPolicyOptions");
        CheckValidator<FetchMetadataOptions, FetchMetadataOptionsValidator>(result, "FetchMetadataOptions");
        CheckValidator<ApiPilotDataProtectionOptions, InstanceSafetyValidator>(result, "ApiPilotDataProtectionOptions");

        return result;
    }

    private void CheckValidator<TOptions, TValidator>(
        List<SecurityDiagnostic> result,
        string optionsName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        var registered = _provider.GetServices<IValidateOptions<TOptions>>();
        var found = false;

        foreach (var validator in registered)
        {
            if (validator is TValidator)
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            result.Add(new SecurityDiagnostic(
                DiagnosticCodes.ValidatorMissing,
                SecurityDiagnosticLevel.Fatal,
                $"The validator {typeof(TValidator).Name} for {optionsName} is not registered. " +
                "The options type is not validated at startup."));
        }
    }
}

