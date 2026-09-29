// filepath: dotnet/src/ApiPilot.AspNetCore/EndpointMetadata/PaginationHttpContextExtensions.cs
// layer: EndpointMetadata | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: HttpContext extension methods that retrieve the validated pagination requests
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : PaginationEndpointFilter (context keys), PageRequest, SortRequest,
//                FilterRequest, PaginationOptions
//   Used by    : application endpoint handlers after the pagination filter runs,
//                PagedResponseResult when reading the effective SuccessStatusCode
//   See also   : PaginationEndpointFilter.cs, SPEC.md (query parameters)
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.EndpointMetadata;

/// <summary>
/// Extension methods on <see cref="HttpContext"/> that retrieve the
/// validated pagination requests stored by <see cref="PaginationEndpointFilter"/>.
/// Each method returns null when the filter did not run for the current
/// request (for example, when pagination is not applied to the endpoint).
/// </summary>
public static class PaginationHttpContextExtensions
{
    /// <summary>
    /// Returns the validated PageRequest for the current request, or null
    /// when the pagination filter did not run.
    /// </summary>
    /// <param name="context">The HTTP context. Must not be null.</param>
    /// <returns>The validated page request, or null.</returns>
    public static PageRequest? GetPageRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(PaginationEndpointFilter.PageRequestKey, out var value)
            ? value as PageRequest
            : null;
    }

    /// <summary>
    /// Returns the validated SortRequest for the current request, or null
    /// when no sort was requested or the pagination filter did not run.
    /// </summary>
    /// <param name="context">The HTTP context. Must not be null.</param>
    /// <returns>The validated sort request, or null.</returns>
    public static SortRequest? GetSortRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(PaginationEndpointFilter.SortRequestKey, out var value)
            ? value as SortRequest
            : null;
    }

    /// <summary>
    /// Returns the validated FilterRequest for the current request, or null
    /// when the pagination filter did not run.
    /// </summary>
    /// <param name="context">The HTTP context. Must not be null.</param>
    /// <returns>The validated filter request, or null.</returns>
    public static FilterRequest? GetFilterRequest(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(PaginationEndpointFilter.FilterRequestKey, out var value)
            ? value as FilterRequest
            : null;
    }

    /// <summary>
    /// Returns the effective PaginationOptions resolved for the current
    /// request (global defaults plus any endpoint overrides), or null when
    /// the pagination filter did not run.
    /// </summary>
    /// <param name="context">The HTTP context. Must not be null.</param>
    /// <returns>The effective options, or null.</returns>
    public static PaginationOptions? GetEffectivePaginationOptions(this HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Items.TryGetValue(PaginationEndpointFilter.EffectiveOptionsKey, out var value)
            ? value as PaginationOptions
            : null;
    }
}

