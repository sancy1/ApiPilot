// filepath: dotnet/src/ApiPilot.Core/Pagination/PagedResultBuilder.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Static factory that assembles a PagedResult<T> from items and page dimensions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : PagedResult<T>, PaginationMetadata
//   Used by    : application query services, ASP.NET Core pagination filter (Phase 1.5)
//   See also   : PagedResult.cs, PaginationMetadata.cs, SPEC.md (paginated envelope)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// Builds a <see cref="PagedResult{T}"/> from a page of items and the
/// dimensions that describe where the page sits in the full result set.
/// The total item count is supplied by the caller; this builder does not
/// query data sources.
/// </summary>
public static class PagedResultBuilder
{
    /// <summary>
    /// Creates a paged result. The items are copied into an immutable
    /// snapshot by the <see cref="PagedResult{T}"/> constructor.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="items">The items on this page. Must not be null.</param>
    /// <param name="page">The 1-based page number. Must be at least 1.</param>
    /// <param name="pageSize">The page size. Must be at least 1.</param>
    /// <param name="totalItems">The total number of items across all pages. Must be non-negative.</param>
    /// <returns>A paged result carrying the items and pagination metadata.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="page"/> or <paramref name="pageSize"/>
    /// is less than 1, or when <paramref name="totalItems"/> is negative.
    /// </exception>
    public static PagedResult<T> From<T>(
        IEnumerable<T> items,
        int page,
        int pageSize,
        int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);

        var snapshot = new List<T>(items);
        var metadata = PaginationMetadata.Create(page, pageSize, totalItems);
        return new PagedResult<T>(snapshot, metadata);
    }
}

