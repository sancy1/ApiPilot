// filepath: dotnet/src/ApiPilot.Core/Pagination/PageRequest.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Validated value object carrying a 1-based page and page size
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : PaginationOptions
//   Used by    : ASP.NET Core pagination endpoint filter (Phase 1.5)
//   See also   : PaginationOptions.cs, PaginationMetadata.cs, SPEC.md
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// A validated request for a page of results. Page numbers are 1-based.
/// Instances can only be created through the factories, which enforce
/// that the page and page size are within the configured bounds.
/// </summary>
public sealed class PageRequest
{
    /// <summary>The requested 1-based page number. Always at least 1.</summary>
    public int Page { get; }

    /// <summary>
    /// The requested number of items per page. Always at least 1 and no
    /// greater than the configured <see cref="PaginationOptions.MaxPageSize"/>.
    /// </summary>
    public int PageSize { get; }

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Creates a page request from explicit page and page size values,
    /// validated against the given options.
    /// </summary>
    /// <param name="page">The 1-based page number. Must be at least 1.</param>
    /// <param name="pageSize">The page size. Must be at least 1 and no greater than the maximum.</param>
    /// <param name="options">The pagination options. Must not be null.</param>
    /// <returns>A validated page request.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the page is less than 1, the page size is less than 1,
    /// or the page size exceeds the configured maximum.
    /// </exception>
    public static PageRequest Create(int page, int pageSize, PaginationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), page, "Page must be at least 1.");
        }
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, "PageSize must be at least 1.");
        }
        if (pageSize > options.MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                pageSize,
                $"PageSize must not exceed the configured maximum of {options.MaxPageSize}.");
        }

        return new PageRequest(page, pageSize);
    }

    /// <summary>
    /// Creates a page request from optional query values. Missing values are
    /// filled from the configured defaults: page 1 and
    /// <see cref="PaginationOptions.DefaultPageSize"/>.
    /// </summary>
    /// <param name="page">The optional page number. Null defaults to 1.</param>
    /// <param name="pageSize">The optional page size. Null defaults to the configured default.</param>
    /// <param name="options">The pagination options. Must not be null.</param>
    /// <returns>A validated page request.</returns>
    public static PageRequest FromQuery(int? page, int? pageSize, PaginationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var effectivePage = page ?? 1;
        var effectiveSize = pageSize ?? options.DefaultPageSize;
        return Create(effectivePage, effectiveSize, options);
    }
}

