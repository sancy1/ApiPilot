// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfMiddlewareExtensions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Application builder extensions that add the CSRF protection middleware to the pipeline
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : CsrfMiddleware, Microsoft.AspNetCore.Builder
//   Used by    : application setup code in Program.cs
//   See also   : CsrfMiddleware.cs, CsrfServiceExtensions.cs (AddApiPilotCsrf)
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Application builder extensions that add the CSRF protection
/// middleware to the pipeline.
/// </summary>
/// <remarks>
/// Call AddApiPilotCsrf on the service collection before calling
/// UseApiPilotCsrfProtection. The service registration registers
/// ICsrfService; the pipeline extension adds the middleware that
/// uses the service.
///
/// Placement: after UseApiPilotCorrelation, after UseApiPilotExceptions,
/// and after authentication. The CSRF binding is the authenticated
/// subject when the request is authenticated. Place before
/// UseApiPilotContentNegotiation and before the endpoint middleware.
/// </remarks>
public static class CsrfMiddlewareExtensions
{
    /// <summary>
    /// Adds the CSRF protection middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotCsrfProtection(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<CsrfMiddleware>();
    }
}

