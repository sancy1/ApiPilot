// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/CorrelationOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Startup validation for CorrelationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<CorrelationOptions>
//   Depends on : ApiPilot.Core.Metadata.CorrelationOptions
//   Used by    : AddApiPilotCorrelation via TryAddEnumerable
//   See also   : CorrelationOptions.cs, ApiPilotCorrelationExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the CorrelationOptions configuration at startup. Fails when
/// HeaderName is empty, when ValidationPattern is not a valid regular
/// expression, or when InvalidIncomingIdPolicy is not a defined enum value.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotCorrelation. To replace or
/// disable it, remove the IValidateOptions&lt;CorrelationOptions&gt; service
/// from the container and register your own before the container is built.
/// </remarks>
public sealed class CorrelationOptionsValidator : IValidateOptions<CorrelationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CorrelationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrEmpty(options.HeaderName))
        {
            failures.Add("CorrelationOptions.HeaderName must not be empty.");
        }

        if (string.IsNullOrEmpty(options.ValidationPattern))
        {
            failures.Add("CorrelationOptions.ValidationPattern must not be empty.");
        }
        else
        {
            try
            {
                _ = new Regex(options.ValidationPattern);
            }
            catch (ArgumentException)
            {
                failures.Add("CorrelationOptions.ValidationPattern is not a valid regular expression.");
            }
        }

        if (!Enum.IsDefined(options.InvalidIncomingIdPolicy))
        {
            failures.Add("CorrelationOptions.InvalidIncomingIdPolicy is not a recognized value.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

