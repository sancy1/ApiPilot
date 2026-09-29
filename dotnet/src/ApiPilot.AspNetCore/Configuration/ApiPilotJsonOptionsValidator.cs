// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiPilotJsonOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Startup validation for ApiPilotJsonOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ApiPilotJsonOptions>
//   Depends on : ApiPilotJsonOptions
//   Used by    : AddApiPilotJson via TryAddEnumerable
//   See also   : ApiPilotJsonOptions.cs, JsonSerializationExtensions.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the ApiPilotJsonOptions configuration at startup. Fails
/// when MaxDepth is less than 1 or when any enum-typed property carries
/// a value that is not a defined member of its enum.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotJson. To replace or disable
/// it, remove the IValidateOptions service from the container and register
/// your own before the container is built.
/// </remarks>
public sealed class ApiPilotJsonOptionsValidator : IValidateOptions<ApiPilotJsonOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ApiPilotJsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.MaxDepth < 1)
        {
            failures.Add("ApiPilotJsonOptions.MaxDepth must be at least 1.");
        }

        if (!Enum.IsDefined(options.EnumMode))
        {
            failures.Add("ApiPilotJsonOptions.EnumMode is not a recognized value.");
        }

        if (!Enum.IsDefined(options.DateMode))
        {
            failures.Add("ApiPilotJsonOptions.DateMode is not a recognized value.");
        }

        if (!Enum.IsDefined(options.ReadCommentHandling))
        {
            failures.Add("ApiPilotJsonOptions.ReadCommentHandling is not a recognized value.");
        }

        if (!Enum.IsDefined(options.DefaultIgnoreCondition))
        {
            failures.Add("ApiPilotJsonOptions.DefaultIgnoreCondition is not a recognized value.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

