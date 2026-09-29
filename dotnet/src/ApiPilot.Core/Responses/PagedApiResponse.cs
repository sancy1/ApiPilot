// filepath: dotnet/src/ApiPilot.Core/Responses/PagedApiResponse.cs
// layer: Responses | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Success envelope for a paginated collection, carrying data and pagination metadata
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiResponse
//   Depends on : PaginationMetadata, PagedResult<T>, ResponseMetadata
//   Used by    : PagedResponseResult<T>, PagedResultBuilder consumers, HTTP adapter (Phase 1.5)
//   See also   : SPEC.md (paginated envelope), ApiResponseOfT.cs, PaginationMetadata.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Responses;

/// <summary>
/// A successful ApiPilot response carrying a page of items and the
/// pagination metadata that describes the page's position within the
/// full result set. The wire shape matches the paginated envelope in
/// SPEC.md: a flat data array alongside a pagination object, both as
/// siblings of success and meta.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// The data array is never omitted; an empty page serializes as an empty
/// array. The pagination object is always present. Unlike ApiResponse&lt;T&gt;,
/// this type carries no transport-neutral Status property: a paginated
/// response is always HTTP 200, and the status line conveys it.
/// </remarks>
public sealed class PagedApiResponse<T> : IApiResponse
{
    /// <summary>
    /// Always true for this type. Error responses are represented by a
    /// separate type.
    /// </summary>
    public bool Success => true;

    /// <summary>
    /// The items on this page. Never null; may be empty. The wire shape
    /// is a flat JSON array.
    /// </summary>
    public IReadOnlyList<T> Data { get; }

    /// <summary>
    /// The pagination metadata describing the position of this page
    /// within the full result set.
    /// </summary>
    public PaginationMetadata Pagination { get; }

    /// <summary>
    /// Non-business metadata attached to every response: the correlation
    /// ID, the server timestamp, and optional extras.
    /// </summary>
    public ResponseMetadata Meta { get; }

    /// <summary>
    /// Creates a paginated success response.
    /// </summary>
    /// <param name="data">The items on this page. Must not be null.</param>
    /// <param name="pagination">The pagination metadata. Must not be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any argument is null.
    /// </exception>
    public PagedApiResponse(
        IReadOnlyList<T> data,
        PaginationMetadata pagination,
        ResponseMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(pagination);
        ArgumentNullException.ThrowIfNull(meta);
        Data = data;
        Pagination = pagination;
        Meta = meta;
    }
}

