// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiPilotRateLimitOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: Startup validation for ApiPilotRateLimitOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ApiPilotRateLimitOptions>
//   Depends on : ApiPilotRateLimitOptions
//   Used by    : AddApiPilotRateLimitRejection via TryAddEnumerable
//   See also   : ApiPilotRateLimitOptions.cs, ApiPilotRateLimitExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the ApiPilotRateLimitOptions configuration at startup. Fails
/// when StatusCode is outside 400-599 or when Message is null, empty, or
/// whitespace.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotRateLimitRejection. To
/// replace or disable it, remove the IValidateOptions service from the
/// container and register your own before the container is built.
/// </remarks>
public sealed class ApiPilotRateLimitOptionsValidator : IValidateOptions<ApiPilotRateLimitOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ApiPilotRateLimitOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.StatusCode < 400 || options.StatusCode > 599)
        {
            failures.Add("ApiPilotRateLimitOptions.StatusCode must be a 4xx or 5xx status code.");
        }

        if (string.IsNullOrWhiteSpace(options.Message))
        {
            failures.Add("ApiPilotRateLimitOptions.Message must not be empty or whitespace.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

