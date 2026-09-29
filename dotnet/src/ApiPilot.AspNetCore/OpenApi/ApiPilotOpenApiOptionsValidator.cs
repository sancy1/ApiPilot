// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/ApiPilotOpenApiOptionsValidator.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: Startup validation for ApiPilotOpenApiOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<ApiPilotOpenApiOptions>
//   Depends on : ApiPilotOpenApiOptions
//   Used by    : ApiPilotOpenApiExtensions via TryAddEnumerable
//   See also   : ApiPilotOpenApiOptions.cs, ApiPilotOpenApiExtensions.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// Validates the ApiPilotOpenApiOptions configuration at startup. Fails
/// when the document title or version is null, empty, or whitespace, or
/// when the document path is not an absolute route path.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotOpenApi. To replace or
/// disable it, remove the IValidateOptions service from the container
/// and register your own before the container is built.
/// </remarks>
public sealed class ApiPilotOpenApiOptionsValidator : IValidateOptions<ApiPilotOpenApiOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ApiPilotOpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DocumentTitle))
        {
            failures.Add("ApiPilotOpenApiOptions.DocumentTitle must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.DocumentVersion))
        {
            failures.Add("ApiPilotOpenApiOptions.DocumentVersion must not be empty or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(options.DocumentPath))
        {
            failures.Add("ApiPilotOpenApiOptions.DocumentPath must not be empty or whitespace.");
        }
        else if (!options.DocumentPath.StartsWith('/'))
        {
            failures.Add("ApiPilotOpenApiOptions.DocumentPath must be an absolute route path starting with a forward slash.");
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

