// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotContentNegotiationExtensions.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Service and application-builder extensions for the content negotiation middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ContentNegotiationOptions, ApiPilotContentNegotiationMiddleware
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotContentNegotiationMiddleware.cs, ContentNegotiationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Middleware;

/// <summary>
/// Extension methods that register the ApiPilot content negotiation
/// middleware and its options.
/// </summary>
/// <remarks>
/// The ContentNegotiationOptions instance is registered both as a concrete
/// singleton and behind IOptions&lt;ContentNegotiationOptions&gt;, following
/// the same pattern AddApiPilotExceptions uses.
///
/// Recommended pipeline order: UseApiPilotCorrelation first to establish the
/// correlation ID, UseApiPilotExceptions second to catch downstream
/// exceptions, and UseApiPilotContentNegotiation third so it can reject
/// unacceptable requests before the endpoint runs.
/// </remarks>
public static class ApiPilotContentNegotiationExtensions
{
    /// <summary>
    /// Registers the content negotiation options.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to adjust the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotContentNegotiation(
        this IServiceCollection services,
        Action<ContentNegotiationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ContentNegotiationOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<ContentNegotiationOptions>, ContentNegotiationOptionsValidator>());

        return services;
    }

    /// <summary>
    /// Adds the content negotiation middleware to the pipeline. Place this
    /// after the correlation and exception middleware.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotContentNegotiation(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ApiPilotContentNegotiationMiddleware>();
    }
}

