// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfAttributes.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf] attributes
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IEndpointMetadataProvider
//   Depends on : CsrfEndpointMetadata, CsrfPolicy
//   Used by    : application controllers and minimal API endpoints
//   See also   : CsrfEndpointMetadata.cs, CsrfMiddleware.cs
// -----------------------------------------------------------------------------

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Marks an endpoint as exempt from CSRF protection. The endpoint
/// bypasses the middleware regardless of the request method and the
/// global ProtectedMethods set. The attribute is ignored when
/// [ApiPilotRequireCsrf] is also present; Require wins.
/// </summary>
/// <remarks>
/// Apply to a controller action or pass through WithMetadata on a
/// minimal API endpoint. Use this attribute for endpoints that
/// intentionally accept unauthenticated state-changing requests
/// (for example, a public webhook receiver).
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiPilotSkipCsrfAttribute : Attribute, IEndpointMetadataProvider
{
    /// <inheritdoc />
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.Add(new CsrfEndpointMetadata(CsrfPolicy.Skip));
    }
}

/// <summary>
/// Marks an endpoint as explicitly protected by CSRF validation. The
/// endpoint is enforced regardless of the request method, the global
/// ProtectedMethods set, and any [ApiPilotSkipCsrf] attribute. This
/// makes [ApiPilotRequireCsrf] on a safe method (for example GET)
/// enforce protection.
/// </summary>
/// <remarks>
/// Apply to a controller action or pass through WithMetadata on a
/// minimal API endpoint. Use this attribute for endpoints whose
/// method is safe by HTTP semantics but whose behavior is
/// state-changing or sensitive.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class ApiPilotRequireCsrfAttribute : Attribute, IEndpointMetadataProvider
{
    /// <inheritdoc />
    public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Metadata.Add(new CsrfEndpointMetadata(CsrfPolicy.Require));
    }
}

