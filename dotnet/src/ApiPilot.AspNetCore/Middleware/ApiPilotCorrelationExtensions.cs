// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotCorrelationExtensions.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Service and application-builder extensions for the correlation middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : CorrelationOptions, ICorrelationIdGenerator, ICorrelationIdAccessor,
//                ApiPilotCorrelationMiddleware, HttpContextCorrelationIdAccessor
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotCorrelationMiddleware.cs, CorrelationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Extension methods that register the ApiPilot correlation middleware
/// and its supporting services.
/// </summary>
/// <remarks>
/// The CorrelationOptions instance is registered both as a concrete
/// singleton and behind IOptions&lt;CorrelationOptions&gt;, following
/// the same pattern AddApiPilotExceptions uses. IHttpContextAccessor is
/// registered only when not already present.
/// </remarks>
public static class ApiPilotCorrelationExtensions
{
    /// <summary>
    /// Registers the correlation options, the default generator, the
    /// HttpContext-based accessor, and the HttpContextAccessor if needed.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to adjust the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotCorrelation(
        this IServiceCollection services,
        Action<CorrelationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<CorrelationOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<CorrelationOptions>, CorrelationOptionsValidator>());
        services.AddSingleton<ICorrelationIdGenerator, DefaultCorrelationIdGenerator>();
        services.AddSingleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }

    /// <summary>
    /// Adds the correlation middleware to the pipeline. Place this early,
    /// before any middleware that needs the correlation ID.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotCorrelation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ApiPilotCorrelationMiddleware>();
    }
}

