// filepath: dotnet/src/ApiPilot.AspNetCore/EndpointMetadata/PaginationMetadataAttribute.cs
// layer: EndpointMetadata | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Attribute that applies per-endpoint pagination overrides
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.Attribute
//   Depends on : n/a
//   Used by    : PaginationResolver, PaginationEndpointFilter, application code
//   See also   : PaginationOverrides.cs, PaginationResolver.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.AspNetCore.EndpointMetadata;

/// <summary>
/// Applies per-endpoint pagination overrides on top of the global
/// PaginationOptions. Apply to a controller action, a controller class,
/// or a minimal API endpoint (via WithMetadata). Unset properties fall
/// through to the global configuration.
/// </summary>
/// <remarks>
/// The DefaultPageSize, MaxPageSize, and SuccessStatusCode
/// properties use -1 as a sentinel for "not set" because C#
/// attributes cannot carry nullable value types. The
/// StrictQueryValidation property uses TriState so that the
/// attribute can distinguish "not set" from an explicit false or
/// true; an unset property falls through to the global
/// configuration, and an explicit value overrides it.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class PaginationMetadataAttribute : Attribute
{
    /// <summary>
    /// The page size to use when the caller does not specify one. -1 means
    /// the global DefaultPageSize applies.
    /// </summary>
    public int DefaultPageSize { get; set; } = -1;

    /// <summary>
    /// The maximum allowed page size. -1 means the global MaxPageSize applies.
    /// </summary>
    public int MaxPageSize { get; set; } = -1;

    /// <summary>
    /// Whether the pagination endpoint filter rejects unknown query
    /// parameters. False (the default) leaves the global StrictQueryValidation
    /// unchanged. Setting true overrides the global value to true.
    /// </summary>
    public TriState StrictQueryValidation { get; set; }

    /// <summary>
    /// The HTTP status code used for a successful paginated response.
    /// -1 means the global SuccessStatusCode applies.
    /// </summary>
    public int SuccessStatusCode { get; set; } = -1;

    /// <summary>
    /// The base for page numbering. 1 means the first page is page 1;
    /// 0 means the first page is page 0. -1 means the global
    /// PageNumberBase applies.
    /// </summary>
    public int PageNumberBase { get; set; } = -1;
}

