// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfServiceExtensions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection extensions that register the CSRF service stack
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : CsrfService, CsrfBindingProvider, DataProtectionCsrfTokenSigner,
//                CsrfOptions, CsrfTokenOptions, CsrfBootstrapOptions, ICsrfRotationStore
//   Used by    : application setup code in Program.cs
//   See also   : CsrfService.cs, CsrfOptions.cs, DataProtectionCsrfTokenSigner.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Service collection extensions that register the CSRF service stack: the
/// token options, the service options, the bootstrap endpoint options, the
/// token signer, the binding provider, the CSRF service, and (optionally)
/// the rotation store.
/// </summary>
/// <remarks>
/// The rotation store is not registered by default. When
/// CsrfOptions.RotationRequirement is Required, an application must
/// register an ICsrfRotationStore before building the provider; otherwise
/// the resolve of ICsrfService throws at first request. To make the
/// failure happen at startup instead, call ValidateCsrfRotationRequirement
/// after building the provider, or rely on ValidateOnStart via the
/// AddOptions pipeline when AddApiPilotServices wires the full stack.
///
/// The Data Protection provider must already be registered. ASP.NET Core
/// registers one by default in a web host. A non-web host must call
/// services.AddDataProtection() before calling AddApiPilotCsrf.
/// </remarks>
public static class CsrfServiceExtensions
{
    /// <summary>
    /// Registers the CSRF service stack.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <param name="configure">
    /// Optional callback that configures the service options. May be null.
    /// </param>
    /// <param name="configureToken">
    /// Optional callback that configures the token options. May be null.
    /// </param>
    /// <param name="configureBootstrap">
    /// Optional callback that configures the bootstrap endpoint options.
    /// May be null.
    /// </param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotCsrf(
        this IServiceCollection services,
        Action<CsrfOptions>? configure = null,
        Action<CsrfTokenOptions>? configureToken = null,
        Action<CsrfBootstrapOptions>? configureBootstrap = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register the service options through the standard pipeline.
        var serviceOptions = services.AddOptions<CsrfOptions>();
        if (configure is not null)
        {
            serviceOptions.Configure(configure);
        }
        serviceOptions.ValidateOnStart();

        // Register the token options through the standard pipeline.
        var tokenOptions = services.AddOptions<CsrfTokenOptions>();
        if (configureToken is not null)
        {
            tokenOptions.Configure(configureToken);
        }
        tokenOptions.ValidateOnStart();

        // Register the startup validator for the service options.
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<CsrfOptions>,
                ApiPilot.Security.Configuration.CsrfOptionsValidator>());

        // Register the bootstrap endpoint options through the standard pipeline.
        var bootstrapOptions = services.AddOptions<CsrfBootstrapOptions>();
        if (configureBootstrap is not null)
        {
            bootstrapOptions.Configure(configureBootstrap);
        }
        bootstrapOptions.ValidateOnStart();

        // Register the default signer.
        services.TryAddSingleton<ICsrfTokenSigner>(sp =>
        {
            var provider = sp.GetRequiredService<IDataProtectionProvider>();
            var opts = sp.GetRequiredService<IOptions<CsrfTokenOptions>>().Value;
            return new DataProtectionCsrfTokenSigner(provider, opts);
        });

        // Register the default binding provider.
        services.TryAddSingleton<ICsrfBindingProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CsrfOptions>>().Value;
            return new CsrfBindingProvider(opts);
        });

        // Register the service. The rotation store is optional.
        services.TryAddSingleton<ICsrfService>(sp =>
        {
            var signer = sp.GetRequiredService<ICsrfTokenSigner>();
            var bindingProvider = sp.GetRequiredService<ICsrfBindingProvider>();
            var serviceOpts = sp.GetRequiredService<IOptions<CsrfOptions>>().Value;
            var tokenOpts = sp.GetRequiredService<IOptions<CsrfTokenOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<CsrfService>>();
            var store = sp.GetService<ICsrfRotationStore>();
            return new CsrfService(signer, bindingProvider, serviceOpts, tokenOpts, logger, store);
        });

        return services;
    }
}

