// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiExceptionOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Startup validation for ApiExceptionOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ApiExceptionOptions>
//   Depends on : ApiExceptionOptions, KnownExceptionType
//   Used by    : AddApiPilotExceptions via TryAddEnumerable
//   See also   : ApiExceptionOptions.cs, ApiPilotExceptionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.ExceptionHandling;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the ApiExceptionOptions configuration at startup. Fails
/// when the mapping list is empty, when any mapping carries a null
/// exception type, a null error code, or an empty safe message, when
/// any ErrorCodeToStatusMap value is outside 100..599, or when any
/// ErrorCodeToStatusMap key is empty.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotExceptions. To replace or
/// disable it, remove the IValidateOptions service from the container
/// and register your own before the container is built.
/// </remarks>
public sealed class ApiExceptionOptionsValidator : IValidateOptions<ApiExceptionOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ApiExceptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Mappings.Count == 0)
        {
            failures.Add("ApiExceptionOptions.Mappings must contain at least one entry.");
        }
        else
        {
            foreach (var mapping in options.Mappings)
            {
                if (mapping is null)
                {
                    failures.Add("ApiExceptionOptions.Mappings must not contain null entries.");
                    continue;
                }
                if (mapping.ExceptionType is null)
                {
                    failures.Add("ApiExceptionOptions.Mappings entry has a null ExceptionType.");
                }
                if (mapping.Code is null)
                {
                    failures.Add("ApiExceptionOptions.Mappings entry has a null Code.");
                }
                if (string.IsNullOrEmpty(mapping.SafeMessage))
                {
                    failures.Add("ApiExceptionOptions.Mappings entry has an empty SafeMessage.");
                }
            }
        }

        if (options.ErrorCodeToStatusMap is null)
        {
            failures.Add("ApiExceptionOptions.ErrorCodeToStatusMap must not be null.");
        }
        else
        {
            foreach (var kvp in options.ErrorCodeToStatusMap)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    failures.Add("ApiExceptionOptions.ErrorCodeToStatusMap keys must not be empty.");
                }
                if (kvp.Value < 100 || kvp.Value > 599)
                {
                    failures.Add($"ApiExceptionOptions.ErrorCodeToStatusMap entry '{kvp.Key}' has an invalid HTTP status {kvp.Value}.");
                }
            }
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

