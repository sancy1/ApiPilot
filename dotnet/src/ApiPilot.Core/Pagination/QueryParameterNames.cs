// filepath: dotnet/src/ApiPilot.Core/Pagination/QueryParameterNames.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Configurable names for the four control query parameters used by pagination
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a
//   Used by    : PaginationOptions (ParameterNames), PaginationEndpointFilter
//   See also   : PaginationOptions.cs, SPEC.md (query parameters)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// Configurable names for the four control query parameters used by the
/// pagination endpoint filter: the page number, the page size, the sort
/// field, and the sort direction. Different frontends use different
/// names for these parameters (for example, pageNumber and perPage for
/// a legacy client, or offset and limit for a Rails-style API). This
/// type is the Option D override point for query parameter names.
/// </summary>
/// <remarks>
/// The default names match the ApiPilot convention documented in SPEC.md:
/// page, pageSize, sort, and direction. Applications that front different
/// client conventions set the corresponding property on the ParameterNames
/// instance assigned to PaginationOptions.
/// </remarks>
public sealed class QueryParameterNames
{
    /// <summary>
    /// The default parameter names. This instance is shared and should not
    /// be mutated; applications that need different names create their own
    /// instance and assign it to PaginationOptions.ParameterNames.
    /// </summary>
    public static QueryParameterNames Default { get; } = new();

    /// <summary>
    /// The name of the page number parameter. Defaults to "page".
    /// </summary>
    public string Page { get; set; } = "page";

    /// <summary>
    /// The name of the page size parameter. Defaults to "pageSize".
    /// </summary>
    public string PageSize { get; set; } = "pageSize";

    /// <summary>
    /// The name of the sort field parameter. Defaults to "sort".
    /// </summary>
    public string Sort { get; set; } = "sort";

    /// <summary>
    /// The name of the sort direction parameter. Defaults to "direction".
    /// </summary>
    public string Direction { get; set; } = "direction";
}

