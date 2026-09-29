// filepath: dotnet/src/ApiPilot.Core/Pagination/PaginationMetadata.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable pagination metadata appearing in the response envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a
//   Used by    : PagedResult<T>, ApiResponse<T> (Phase 0.5 extension), HTTP adapter
//   See also   : SPEC.md (pagination envelope), PagedResult.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// Immutable pagination metadata attached to a paginated response. Page
/// numbers are 1-based. Derived values (TotalPages, HasNext, HasPrevious)
/// are computed from the stored Page, PageSize, and TotalItems.
/// </summary>
public sealed record PaginationMetadata
{
    /// <summary>The current page number, 1-based.</summary>
    public int Page { get; }

    /// <summary>The number of items per page.</summary>
    public int PageSize { get; }

    /// <summary>The total number of items across all pages.</summary>
    public int TotalItems { get; }

    /// <summary>
    /// The total number of pages. Equals ceil(TotalItems / PageSize) with
    /// a minimum of 1 so that an empty collection still has a page 1.
    /// </summary>
    public int TotalPages => TotalItems == 0
        ? 1
        : (TotalItems + PageSize - 1) / PageSize;

    /// <summary>True when there is a page after the current one.</summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>True when there is a page before the current one.</summary>
    public bool HasPrevious => Page > 1;

    private PaginationMetadata(int page, int pageSize, int totalItems)
    {
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }

    /// <summary>
    /// Creates pagination metadata. The arguments must satisfy: Page >= 1,
    /// PageSize >= 1, TotalItems >= 0.
    /// </summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="totalItems">The total number of items.</param>
    /// <returns>A new metadata instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when Page or PageSize is less than 1, or TotalItems is negative.
    /// </exception>
    public static PaginationMetadata Create(int page, int pageSize, int totalItems)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be at least 1.");
        }
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "PageSize must be at least 1.");
        }
        if (totalItems < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalItems), totalItems, "TotalItems must be non-negative.");
        }
        return new PaginationMetadata(page, pageSize, totalItems);
    }
}

