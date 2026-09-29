// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfBootstrapExtensions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Endpoint routing extensions that register the CSRF bootstrap endpoint
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : CsrfBootstrapEndpoint, CsrfBootstrapOptions, CsrfOptions,
//                ICsrfService, ICorrelationIdAccessor, Microsoft.AspNetCore.Routing
//   Used by    : application setup code in Program.cs
//   See also   : CsrfBootstrapEndpoint.cs, CsrfBootstrapOptions.cs, SPEC.md (CSRF bootstrap response)
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Endpoint routing extensions that register the CSRF bootstrap endpoint.
/// The endpoint is a GET that responds with a bare { "token": "..." }
/// body. The path is taken from CsrfOptions.BootstrapPath.
/// </summary>
/// <remarks>
/// The endpoint is disabled when CsrfBootstrapOptions.Enabled is false.
/// When disabled, this extension registers nothing; a request to the
/// path falls through to whatever middleware follows.
///
/// The endpoint does not require authentication. It is safe to expose
/// to any origin under the configured CORS policy. It sets no cookie
/// and echoes no binding.
/// </remarks>
public static class CsrfBootstrapExtensions
{
    /// <summary>
    /// Maps the CSRF bootstrap endpoint on the configured path.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder. Must not be null.</param>
    /// <returns>The same endpoint route builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="endpoints"/> is null.
    /// </exception>
    public static IEndpointRouteBuilder MapApiPilotCsrf(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var bootstrapOptions = endpoints.ServiceProvider
            .GetService<IOptions<CsrfBootstrapOptions>>()?.Value
            ?? new CsrfBootstrapOptions();

        if (!bootstrapOptions.Enabled)
        {
            return endpoints;
        }

        var csrfOptions = endpoints.ServiceProvider
            .GetService<IOptions<CsrfOptions>>()?.Value
            ?? new CsrfOptions();

        var path = csrfOptions.BootstrapPath;

        endpoints.MapGet(path, async (HttpContext context) =>
        {
            var service = context.RequestServices.GetRequiredService<ICsrfService>();
            var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
            await CsrfBootstrapEndpoint.HandleAsync(
                context,
                service,
                bootstrapOptions,
                accessor).ConfigureAwait(false);
        });

        return endpoints;
    }
}

