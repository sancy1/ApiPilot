// filepath: dotnet/src/ApiPilot.Security/Origin/OriginMiddlewareExtensions.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection and application builder extensions for the Origin policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : OriginPolicyOptions, OriginMiddleware, OriginPolicyOptionsValidator
//   Used by    : application setup code in Program.cs
//   See also   : OriginMiddleware.cs, OriginPolicyOptions.cs, CsrfServiceExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Origin;

/// <summary>
/// Service collection and application builder extensions for the
/// Origin policy. The service registration wires the options and the
/// startup validator. The pipeline extension adds the middleware.
/// </summary>
/// <remarks>
/// The Origin middleware reads IOptions&lt;CsrfOptions&gt; for the
/// ProtectedMethods set. The application must call AddApiPilotCsrf
/// before UseApiPilotOriginPolicy, or the resolve of the middleware
/// fails with a missing-service exception.
///
/// Pipeline placement: after UseApiPilotCorrelation, after
/// UseApiPilotExceptions, after UseRouting, after authentication, and
/// before UseApiPilotCsrfProtection. The Origin check runs before the
/// CSRF token check so a rejected origin does not require reading the
/// token.
/// </remarks>
public static class OriginMiddlewareExtensions
{
    /// <summary>
    /// Registers the Origin policy options and the startup validator.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that adjusts the options. May be null.
    /// </param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotOriginPolicy(
        this IServiceCollection services,
        Action<OriginPolicyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<OriginPolicyOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<OriginPolicyOptions>,
                OriginPolicyOptionsValidator>());

        return services;
    }

    /// <summary>
    /// Adds the Origin policy middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotOriginPolicy(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<OriginMiddleware>();
    }
}

