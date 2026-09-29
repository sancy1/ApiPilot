// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/PaginationOptionsValidator.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Startup validation for PaginationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Options.IValidateOptions<PaginationOptions>
//   Depends on : ApiPilot.Core.Pagination.PaginationOptions
//   Used by    : AddApiPilotPagination via TryAddEnumerable
//   See also   : PaginationOptions.cs, ApiPilotServiceCollectionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Validates the PaginationOptions configuration at startup. Fails when
/// DefaultPageSize is less than 1, when MaxPageSize is less than 1, when
/// DefaultPageSize exceeds MaxPageSize, when PageNumberBase is negative,
/// when SuccessStatusCode is not a 2xx code, or when ParameterNames is
/// null or contains an empty entry.
/// </summary>
/// <remarks>
/// This validator is registered by AddApiPilotPagination. To replace or
/// disable it, remove the IValidateOptions&lt;PaginationOptions&gt; service
/// from the container and register your own before the container is built.
/// </remarks>
public sealed class PaginationOptionsValidator : IValidateOptions<PaginationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PaginationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.DefaultPageSize < 1)
        {
            failures.Add("PaginationOptions.DefaultPageSize must be at least 1.");
        }

        if (options.MaxPageSize < 1)
        {
            failures.Add("PaginationOptions.MaxPageSize must be at least 1.");
        }

        if (options.DefaultPageSize > options.MaxPageSize)
        {
            failures.Add("PaginationOptions.DefaultPageSize must not exceed MaxPageSize.");
        }

        if (options.PageNumberBase < 0)
        {
            failures.Add("PaginationOptions.PageNumberBase must be non-negative.");
        }

        if (options.SuccessStatusCode < 200 || options.SuccessStatusCode > 299)
        {
            failures.Add("PaginationOptions.SuccessStatusCode must be a 2xx status code.");
        }

        if (options.ParameterNames is null)
        {
            failures.Add("PaginationOptions.ParameterNames must not be null.");
        }
        else
        {
            if (string.IsNullOrEmpty(options.ParameterNames.Page))
            {
                failures.Add("PaginationOptions.ParameterNames.Page must not be empty.");
            }
            if (string.IsNullOrEmpty(options.ParameterNames.PageSize))
            {
                failures.Add("PaginationOptions.ParameterNames.PageSize must not be empty.");
            }
            if (string.IsNullOrEmpty(options.ParameterNames.Sort))
            {
                failures.Add("PaginationOptions.ParameterNames.Sort must not be empty.");
            }
            if (string.IsNullOrEmpty(options.ParameterNames.Direction))
            {
                failures.Add("PaginationOptions.ParameterNames.Direction must not be empty.");
            }
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }
}

