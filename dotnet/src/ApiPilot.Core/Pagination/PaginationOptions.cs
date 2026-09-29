// filepath: dotnet/src/ApiPilot.Core/Pagination/PaginationOptions.cs
// layer: Pagination | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Mutable configuration for pagination defaults and limits
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a
//   Used by    : ASP.NET Core options binding (Phase 1), PageRequest factory
//   See also   : PageRequest.cs, SPEC.md (query validation), PLANNING.md
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Pagination;

/// <summary>
/// Mutable configuration for pagination defaults and limits. This is a
/// plain data holder intended for binding from application configuration
/// (for example appsettings.json). Validation of the configured values
/// happens in the ASP.NET Core options validation step, not here.
/// </summary>
public sealed class PaginationOptions
{
    /// <summary>
    /// The page size used when a caller does not specify one. Defaults to 20.
    /// Must be at least 1 and no greater than <see cref="MaxPageSize"/> when
    /// validated by the application.
    /// </summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>
    /// The maximum allowed page size. Requests for a larger page size are
    /// rejected by the application endpoint filter. Defaults to 100. Must be
    /// at least 1.
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// When true, the ASP.NET Core pagination endpoint filter rejects
    /// requests that contain query parameters it does not recognize.
    /// Defaults to false, which passes unknown parameters through.
    /// This is the global default; individual endpoints can override it
    /// via PaginationMetadataAttribute.
    /// </summary>
    public bool StrictQueryValidation { get; set; }

    /// <summary>
    /// The HTTP status code used for a successful paginated response.
    /// Defaults to 200 (OK). Applications that follow RFC 7233 range
    /// semantics may set this to 206 (Partial Content) globally, or
    /// override it per endpoint via PaginationOverrides.
    /// </summary>
    public int SuccessStatusCode { get; set; } = 200;

    /// <summary>
    /// The names of the four control query parameters (page, page size,
    /// sort, direction). Defaults to the ApiPilot convention documented
    /// in SPEC.md. Applications that front different client conventions
    /// assign a custom QueryParameterNames instance here.
    /// </summary>
    public QueryParameterNames ParameterNames { get; set; } = new();

    /// <summary>
    /// The base for page numbering. 1 means the first page is page 1.
    /// 0 means the first page is page 0. Defaults to 1.
    /// </summary>
    public int PageNumberBase { get; set; } = 1;

    /// <summary>
    /// Optional parser for sort direction values. When null, the
    /// library accepts "asc" and "desc" case-insensitively (see
    /// SortDirectionExtensions.TryParseWireValue). Applications
    /// whose frontends use other direction tokens (for example,
    /// "ascending" or "+"/"-") supply their own parser here.
    /// </summary>
    public Func<string, SortDirection?>? SortDirectionParser { get; set; }

    /// <summary>
    /// Optional parser for the entire sort expression. Receives the sort
    /// field and the direction value separately and returns a SortRequest,
    /// or null when the expression cannot be parsed. When null, the
    /// library uses the default parser which requires a separate direction
    /// parameter. Applications whose frontends encode the direction in
    /// the field (for example, "-createdAt" for descending) supply their
    /// own parser here.
    /// </summary>
    public Func<string, string, SortRequest?>? SortParser { get; set; }

    /// <summary>
    /// Optional predicate that decides whether a query parameter is a
    /// filter. When null, the library treats any parameter not named
    /// by ParameterNames as a filter in lenient mode, and rejects it
    /// in strict mode. Applications that restrict filter keys to a
    /// known set supply their own predicate here.
    /// </summary>
    public Func<string, bool>? IsFilterParameter { get; set; }

    /// <summary>
    /// Optional parser for integer query values (page and page size).
    /// When null, the library uses int.TryParse with NumberStyles.Integer
    /// and the invariant culture. Applications that accept other integer
    /// formats supply their own parser here.
    /// </summary>
    public Func<string, int?>? IntegerParser { get; set; }
}

