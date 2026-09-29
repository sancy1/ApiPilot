// filepath: dotnet/src/ApiPilot.Core/Pagination/PagedResult.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable wrapper pairing a page of items with its pagination metadata
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : PaginationMetadata
//   Used by    : application services, ApiResponse<T> pagination extension (Phase 0.5)
//   See also   : PaginationMetadata.cs, SPEC.md (paginated envelope)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// An immutable wrapper pairing a page of items with its pagination
/// metadata. Applications return a PagedResult from their query services;
/// the response builder attaches the metadata to the response envelope.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// The items on this page. Never null; may be empty. An immutable
    /// snapshot taken at construction.
    /// </summary>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    /// The pagination metadata describing the position of this page within
    /// the full result set.
    /// </summary>
    public PaginationMetadata Pagination { get; }

    /// <summary>
    /// Creates a paginated result from a set of items and its pagination
    /// metadata. The item collection is defensively copied.
    /// </summary>
    /// <param name="items">The items on this page. Must not be null.</param>
    /// <param name="pagination">The pagination metadata. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="items"/> or <paramref name="pagination"/> is null.
    /// </exception>
    public PagedResult(IReadOnlyList<T> items, PaginationMetadata pagination)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(pagination);

        var snapshot = new T[items.Count];
        for (var i = 0; i < items.Count; i++)
        {
            snapshot[i] = items[i];
        }

        Items = snapshot;
        Pagination = pagination;
    }
}

