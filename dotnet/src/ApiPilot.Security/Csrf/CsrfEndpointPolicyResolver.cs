// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfEndpointPolicyResolver.cs
// layer: Csrf | package: ApiPilot.Security | since: v1.0.4
// purpose: Shared endpoint CSRF policy resolution for the CSRF, Origin, and Fetch Metadata middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (internal static class)
//   Depends on : CsrfPolicy, CsrfEndpointMetadata, ApiPilotSkipCsrfAttribute,
//                ApiPilotRequireCsrfAttribute, HttpContext, EndpointMetadataCollection
//   Used by    : CsrfMiddleware, OriginMiddleware, FetchMetadataMiddleware
//   See also   : CsrfAttributes.cs, CsrfEndpointMetadata.cs, CsrfMiddleware.cs
// -----------------------------------------------------------------------------
//
// F-65 fix. The three security middleware share one endpoint policy
// resolution. Before this resolver, each middleware had a private
// ResolvePolicy that read only CsrfEndpointMetadata. The framework attaches
// the ApiPilotSkipCsrfAttribute and ApiPilotRequireCsrfAttribute instances
// to the endpoint metadata in all documented paths (minimal-API
// WithMetadata, decorated handler, controller action) but does not invoke
// their IEndpointMetadataProvider.PopulateMetadata, so the canonical
// CsrfEndpointMetadata record is never produced for those paths. The
// middleware therefore found no record and fell back to the global policy,
// and the attributes were inert.
//
// This resolver reads BOTH the canonical record AND the attribute
// instances, combines them, and applies the documented precedence:
//
//     Require > Skip > UseGlobal
//
// The resolver is internal. It does not change the public API. It returns
// the existing CsrfPolicy. It preserves the aggregate ordering semantics of
// the previous record-only loop: a later Skip does not override an earlier
// Require.
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Resolves the effective <see cref="CsrfPolicy"/> for an endpoint by
/// reading both the canonical <see cref="CsrfEndpointMetadata"/> record and
/// the attribute instances the framework attaches to endpoint metadata.
/// Applied consistently by the CSRF, Origin, and Fetch Metadata middleware.
/// </summary>
/// <remarks>
/// Precedence: Require beats Skip beats UseGlobal. The resolver aggregates
/// every matching entry rather than taking the first match, so collection
/// order cannot change the result. A null endpoint yields UseGlobal.
/// </remarks>
internal static class CsrfEndpointPolicyResolver
{
    /// <summary>
    /// Resolves the effective CSRF policy for the endpoint on the given
    /// HttpContext. Returns <see cref="CsrfPolicy.UseGlobal"/> when there is
    /// no endpoint or no matching metadata.
    /// </summary>
    /// <param name="httpContext">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <returns>The effective CSRF policy for the endpoint.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpContext"/> is null.
    /// </exception>
    public static CsrfPolicy Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            return CsrfPolicy.UseGlobal;
        }

        var resolved = CsrfPolicy.UseGlobal;

        // Pass 1: the canonical record, when the framework produced it.
        foreach (var metadata in endpoint.Metadata.GetOrderedMetadata<CsrfEndpointMetadata>())
        {
            if (metadata.Policy == CsrfPolicy.Require)
            {
                resolved = CsrfPolicy.Require;
            }
            else if (metadata.Policy == CsrfPolicy.Skip && resolved != CsrfPolicy.Require)
            {
                resolved = CsrfPolicy.Skip;
            }
        }

        // Pass 2: the Require attribute instance, applied before the Skip
        // attribute so Require always beats Skip regardless of order.
        foreach (var _ in endpoint.Metadata.GetOrderedMetadata<ApiPilotRequireCsrfAttribute>())
        {
            resolved = CsrfPolicy.Require;
        }

        // Pass 3: the Skip attribute instance, applied only when Require has
        // not been resolved.
        if (resolved != CsrfPolicy.Require)
        {
            foreach (var _ in endpoint.Metadata.GetOrderedMetadata<ApiPilotSkipCsrfAttribute>())
            {
                resolved = CsrfPolicy.Skip;
            }
        }

        return resolved;
    }
}

