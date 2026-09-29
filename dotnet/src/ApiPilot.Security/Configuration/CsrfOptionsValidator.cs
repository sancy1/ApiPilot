// filepath: dotnet/src/ApiPilot.Security/Configuration/CsrfOptionsValidator.cs
// layer: Configuration | package: ApiPilot.Security | since: v0.3.0
// purpose: Startup validation for CsrfOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<CsrfOptions>
//   Depends on : ApiPilot.Security.Csrf.CsrfOptions, CsrfTokenParseResult
//   Used by    : AddApiPilotCsrf via TryAddEnumerable
//   See also   : CsrfOptions.cs, CsrfServiceExtensions.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Configuration;

/// <summary>
/// Validates the CsrfOptions configuration at startup. Fails when the
/// header name is empty, when the bootstrap path is empty or does not
/// start with a slash, when the missing-header code is empty, when the
/// protected-methods set is empty, or when the CodeMapping does not
/// cover every non-Ok CsrfTokenParseResult member.
/// </summary>
/// <remarks>
/// The CodeMapping check is the important one: an unmapped failure
/// reason would mean a rejection has no public wire code, which is a
/// silent deny with an empty error envelope. The validator fails closed
/// when the mapping is incomplete.
/// </remarks>
public sealed class CsrfOptionsValidator : IValidateOptions<CsrfOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CsrfOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.HeaderName))
        {
            failures.Add("CsrfOptions.HeaderName must not be empty.");
        }

        if (string.IsNullOrEmpty(options.BootstrapPath))
        {
            failures.Add("CsrfOptions.BootstrapPath must not be empty.");
        }
        else if (!options.BootstrapPath.StartsWith('/'))
        {
            failures.Add("CsrfOptions.BootstrapPath must start with a slash.");
        }

        if (string.IsNullOrEmpty(options.MissingHeaderCode))
        {
            failures.Add("CsrfOptions.MissingHeaderCode must not be empty.");
        }

        if (options.ProtectedMethods.Count == 0)
        {
            failures.Add(
                "CsrfOptions.ProtectedMethods must not be empty. " +
                "A CSRF policy with no protected methods protects nothing.");
        }

        foreach (var reason in Enum.GetValues<CsrfTokenParseResult>())
        {
            if (reason == CsrfTokenParseResult.Ok)
            {
                continue;
            }

            if (!options.CodeMapping.TryGetValue(reason, out var code)
                || string.IsNullOrEmpty(code))
            {
                failures.Add(
                    $"CsrfOptions.CodeMapping must carry a non-empty public code for {reason}.");
            }
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

