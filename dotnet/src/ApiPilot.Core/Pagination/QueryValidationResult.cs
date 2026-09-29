// filepath: dotnet/src/ApiPilot.Core/Pagination/QueryValidationResult.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Union of a successful query validation or a validation failure
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : PageRequest, SortRequest, FilterRequest, ValidationErrors
//   Used by    : ASP.NET Core pagination endpoint filter (Phase 1.5)
//   See also   : PageRequest.cs, ValidationErrors.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Validation;

namespace ApiPilot.Core.Pagination;

/// <summary>
/// A discriminated result representing either a successful query validation
/// or a validation failure. On success, carries the validated page, sort,
/// and filter requests. On failure, carries the validation errors that
/// prevented the query from being accepted.
/// </summary>
public sealed class QueryValidationResult
{
    /// <summary>True when the query was valid and the request objects are populated.</summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The validated page request. Non-null when <see cref="IsSuccess"/> is
    /// true; null otherwise.
    /// </summary>
    public PageRequest? Page { get; }

    /// <summary>
    /// The validated sort request. May be null even when the query succeeds,
    /// indicating that no sort was requested. Null when <see cref="IsSuccess"/>
    /// is false.
    /// </summary>
    public SortRequest? Sort { get; }

    /// <summary>
    /// The validated filter request. Non-null when <see cref="IsSuccess"/> is
    /// true; null otherwise.
    /// </summary>
    public FilterRequest? Filters { get; }

    /// <summary>
    /// The validation errors when the query was rejected. Non-null when
    /// <see cref="IsSuccess"/> is false; null otherwise.
    /// </summary>
    public ValidationErrors? Errors { get; }

    private QueryValidationResult(
        bool isSuccess,
        PageRequest? page,
        SortRequest? sort,
        FilterRequest? filters,
        ValidationErrors? errors)
    {
        IsSuccess = isSuccess;
        Page = page;
        Sort = sort;
        Filters = filters;
        Errors = errors;
    }

    /// <summary>
    /// Produces a successful result carrying the validated requests.
    /// </summary>
    /// <param name="page">The validated page request. Must not be null.</param>
    /// <param name="sort">The validated sort request, or null when no sort was requested.</param>
    /// <param name="filters">The validated filter request. Must not be null.</param>
    /// <returns>A successful query validation result.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="page"/> or <paramref name="filters"/> is null.
    /// </exception>
    public static QueryValidationResult Success(
        PageRequest page,
        SortRequest? sort,
        FilterRequest filters)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(filters);
        return new QueryValidationResult(true, page, sort, filters, null);
    }

    /// <summary>
    /// Produces a failed result carrying validation errors.
    /// </summary>
    /// <param name="errors">The validation errors. Must not be null or empty.</param>
    /// <returns>A failed query validation result.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="errors"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="errors"/> is empty.
    /// </exception>
    public static QueryValidationResult Failure(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        if (errors.IsEmpty)
        {
            throw new ArgumentException(
                "A query validation failure must carry at least one error.",
                nameof(errors));
        }
        return new QueryValidationResult(false, null, null, null, errors);
    }
}

