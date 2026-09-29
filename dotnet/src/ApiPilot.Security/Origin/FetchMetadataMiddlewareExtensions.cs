// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataMiddlewareExtensions.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection and application builder extensions for the Fetch Metadata policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : FetchMetadataOptions, FetchMetadataMiddleware, FetchMetadataOptionsValidator
//   Used by    : application setup code in Program.cs
//   See also   : FetchMetadataMiddleware.cs, FetchMetadataOptions.cs, OriginMiddlewareExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Origin;

/// <summary>
/// Service collection and application builder extensions for the Fetch
/// Metadata policy. The service registration wires the options and the
/// startup validator. The pipeline extension adds the middleware.
/// </summary>
/// <remarks>
/// The Fetch Metadata middleware reads IOptions&lt;CsrfOptions&gt; for
/// the ProtectedMethods set. The application must call AddApiPilotCsrf
/// before UseApiPilotFetchMetadata, or the resolve of the middleware
/// fails with a missing-service exception.
///
/// Pipeline placement: after UseApiPilotOriginPolicy and before
/// UseApiPilotCsrfProtection. The Fetch Metadata check runs after the
/// Origin check so the two policies compose.
/// </remarks>
public static class FetchMetadataMiddlewareExtensions
{
    /// <summary>
    /// Registers the Fetch Metadata options and the startup validator.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that adjusts the options. May be null.
    /// </param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotFetchMetadata(
        this IServiceCollection services,
        Action<FetchMetadataOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var optionsBuilder = services.AddOptions<FetchMetadataOptions>();
        if (configure is not null)
        {
            optionsBuilder.Configure(configure);
        }
        optionsBuilder.ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<FetchMetadataOptions>,
                FetchMetadataOptionsValidator>());

        return services;
    }

    /// <summary>
    /// Adds the Fetch Metadata middleware to the pipeline.
    /// </summary>
    /// <param name="app">The application builder. Must not be null.</param>
    /// <returns>The same application builder, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="app"/> is null.
    /// </exception>
    public static IApplicationBuilder UseApiPilotFetchMetadata(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<FetchMetadataMiddleware>();
    }
}

