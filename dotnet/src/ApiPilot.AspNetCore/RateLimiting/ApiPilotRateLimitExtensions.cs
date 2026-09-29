// filepath: dotnet/src/ApiPilot.AspNetCore/RateLimiting/ApiPilotRateLimitExtensions.cs
// layer: RateLimiting | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: Opt-in service collection extension that registers the rate-limit rejection options and validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilotRateLimitOptions, ApiPilotRateLimitOptionsValidator, IServiceCollection
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotRateLimitOptions.cs, RateLimitDiagnosticsHook.cs, docs/rate-limiting.md
// -----------------------------------------------------------------------------
//
// SCOPE
//   This extension registers the options and their startup validation. It
//   does NOT register the platform limiter and it does NOT set
//   RateLimiterOptions.OnRejected. The application owns both: it calls
//   builder.Services.AddRateLimiter(...) and assigns the rejection handler
//   inside that call. ApiPilot cannot call AddRateLimiter on the
//   application behalf without owning the limiter, which the boundary
//   forbids.

using ApiPilot.AspNetCore.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.RateLimiting;

/// <summary>
/// Registers the rate-limit rejection options and their startup validator.
/// Call this to customize <see cref="ApiPilotRateLimitOptions"/>. The
/// registration is fail-closed: an invalid option value prevents the host
/// from starting.
/// </summary>
/// <remarks>
/// This extension does not register the platform limiter and does not set
/// <c>RateLimiterOptions.OnRejected</c>. The application assigns
/// <see cref="RateLimitDiagnosticsHook.HandleAsync"/> to that property
/// inside its own <c>AddRateLimiter</c> call.
/// </remarks>
public static class ApiPilotRateLimitExtensions
{
    /// <summary>
    /// Registers <see cref="ApiPilotRateLimitOptions"/> through the standard
    /// options pipeline, wires startup validation, and returns the service
    /// collection for chaining.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">Optional callback to configure the options. May be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotRateLimitRejection(
        this IServiceCollection services,
        Action<ApiPilotRateLimitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<ApiPilotRateLimitOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ApiPilotRateLimitOptions>,
                ApiPilotRateLimitOptionsValidator>());

        return services;
    }
}

