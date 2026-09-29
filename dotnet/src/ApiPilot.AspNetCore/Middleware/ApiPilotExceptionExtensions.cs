// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotExceptionExtensions.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Service and application-builder extensions that register the exception middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiExceptionOptions, DefaultApiExceptionMapper, IApiExceptionMapper,
//                ApiPilotExceptionMiddleware, IServiceCollection, IApplicationBuilder
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotExceptionMiddleware.cs, ApiExceptionOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Extension methods that register the ApiPilot exception middleware and
/// its supporting services.
/// </summary>
/// <remarks>
/// Two methods are provided. Use AddApiPilotExceptions in the service
/// configuration phase (before the application is built). Use
/// UseApiPilotExceptions in the pipeline configuration phase, early in the
/// pipeline so it catches exceptions from downstream middleware and
/// endpoints.
/// </remarks>
public static class ApiPilotExceptionExtensions
{
    /// <summary>
    /// Registers the exception mapper, the options object, and everything
    /// needed by the exception middleware. When <paramref name="configure"/>
    /// is provided, it runs against a fresh options instance before
    /// registration.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to adjust the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotExceptions(
        this IServiceCollection services,
        Action<ApiExceptionOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiExceptionOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ApiExceptionOptions>, ApiExceptionOptionsValidator>());
        services.AddSingleton<IApiExceptionMapper, DefaultApiExceptionMapper>();

        return services;
    }

    /// <summary>
    /// Adds the exception middleware to the pipeline. Place this early in
    /// the pipeline, immediately after any security middleware that must run
    /// before business logic.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotExceptions(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ApiPilotExceptionMiddleware>();
    }
}

