// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/ApiPilotOpenApiExtensions.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: Service collection and endpoint routing extensions for the repository-owned OpenAPI document
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilotOpenApiOptions, ApiPilotOpenApiOptionsValidator,
//                ApiPilotSchemaEmitter, IApiDescriptionGroupCollectionProvider
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotOpenApiOptions.cs, docs/openapi.md
// -----------------------------------------------------------------------------
//
// OPTION D
//   The document path is configured through ApiPilotOpenApiOptions.DocumentPath.
//   MapApiPilotOpenApi takes no path parameter; it reads the configured
//   path. There is one coherent override mechanism.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// Registers the OpenAPI options and maps the OpenAPI document endpoint.
/// </summary>
/// <remarks>
/// <para>
/// The document endpoint requires an ApiExplorer provider in the
/// container. A typical application has one through AddControllers or
/// AddEndpointsApiExplorer. This extension does not register the
/// provider; it consumes it.
/// </para>
/// <para>
/// The document path is read from ApiPilotOpenApiOptions.DocumentPath.
/// The mapping method takes no path parameter, so there is a single
/// override mechanism.
/// </para>
/// </remarks>
public static class ApiPilotOpenApiExtensions
{
    /// <summary>
    /// Registers the OpenAPI options through the standard options
    /// pipeline, wires startup validation, and returns the service
    /// collection for chaining.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to configure the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotOpenApi(
        this IServiceCollection services,
        Action<ApiPilotOpenApiOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiPilotOpenApiOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ApiPilotOpenApiOptions>,
                ApiPilotOpenApiOptionsValidator>());

        return services;
    }

    /// <summary>
    /// Maps a GET endpoint that returns the OpenAPI document at the
    /// configured path. The path is read from
    /// ApiPilotOpenApiOptions.DocumentPath.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder. Must not be null.</param>
    /// <returns>The same endpoint route builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="endpoints"/> is null.
    /// </exception>
    public static IEndpointRouteBuilder MapApiPilotOpenApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider
            .GetService<IOptions<ApiPilotOpenApiOptions>>()?.Value
            ?? new ApiPilotOpenApiOptions();

        var path = options.DocumentPath;

        endpoints.MapGet(path, async (HttpContext context) =>
        {
            var provider = context.RequestServices
                .GetRequiredService<IApiDescriptionGroupCollectionProvider>();
            var resolved = context.RequestServices
                .GetRequiredService<IOptions<ApiPilotOpenApiOptions>>().Value;

            var document = ApiPilotSchemaEmitter.BuildDocument(provider, resolved);

            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(document.ToJsonString()).ConfigureAwait(false);
        });

        return endpoints;
    }
}

