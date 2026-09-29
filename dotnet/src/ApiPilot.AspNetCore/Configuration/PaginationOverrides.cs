// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/PaginationOverrides.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Partial per-endpoint overrides for the global PaginationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : ApiPilot.Core.Pagination.QueryParameterNames
//   Used by    : PaginationResolver, PaginationEndpointFilter, PaginationMetadataAttribute
//   See also   : PaginationOptions.cs (Core), PaginationResolver.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Partial overrides for the global PaginationOptions. Each property is
/// nullable; null means the global value is used unchanged. Non-null
/// values replace the corresponding global value for the endpoint the
/// overrides apply to. This is the Option D override layer for pagination.
/// </summary>
/// <remarks>
/// The nullable shape is deliberate: an endpoint might want a higher
/// MaxPageSize without changing the global DefaultPageSize, or vice versa.
/// Only the fields that differ from the global configuration need to be set.
/// Properties use settable accessors so the fluent API can populate them
/// through a callback: WithApiPilotPagination(o =&gt; o.MaxPageSize = 50).
/// </remarks>
public sealed class PaginationOverrides
{
    /// <summary>
    /// The page size to use when the caller does not specify one. Null means
    /// the global DefaultPageSize applies.
    /// </summary>
    public int? DefaultPageSize { get; set; }

    /// <summary>
    /// The maximum allowed page size. Null means the global MaxPageSize applies.
    /// </summary>
    public int? MaxPageSize { get; set; }

    /// <summary>
    /// Whether the pagination endpoint filter rejects unknown query parameters.
    /// Null means the global StrictQueryValidation applies.
    /// </summary>
    public bool? StrictQueryValidation { get; set; }

    /// <summary>
    /// The HTTP status code used for a successful paginated response.
    /// Null means the global SuccessStatusCode applies.
    /// </summary>
    public int? SuccessStatusCode { get; set; }

    /// <summary>
    /// The names of the four control query parameters. Null means
    /// the global ParameterNames applies.
    /// </summary>
    public QueryParameterNames? ParameterNames { get; set; }

    /// <summary>
    /// The base for page numbering. Null means the global PageNumberBase
    /// applies.
    /// </summary>
    public int? PageNumberBase { get; set; }

    /// <summary>
    /// Optional parser for sort direction values. Null means the global
    /// SortDirectionParser applies (which may itself be null, in which
    /// case the library default is used).
    /// </summary>
    public Func<string, SortDirection?>? SortDirectionParser { get; set; }

    /// <summary>
    /// Optional parser for the whole sort expression. Null means the
    /// global SortParser applies.
    /// </summary>
    public Func<string, string, SortRequest?>? SortParser { get; set; }

    /// <summary>
    /// Optional predicate for filter parameter recognition. Null means
    /// the global IsFilterParameter applies.
    /// </summary>
    public Func<string, bool>? IsFilterParameter { get; set; }

    /// <summary>
    /// Optional parser for integer query values. Null means the global
    /// IntegerParser applies.
    /// </summary>
    public Func<string, int?>? IntegerParser { get; set; }
}

