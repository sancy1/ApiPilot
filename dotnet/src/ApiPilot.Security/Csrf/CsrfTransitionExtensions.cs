// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTransitionExtensions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection extensions that register the default CSRF transition listener
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : DefaultCsrfTransitionListener, ICsrfTransitionListener,
//                ICsrfService, ICsrfBindingProvider, ICsrfRotationStore (optional), ILogger
//   Used by    : application setup code in Program.cs
//   See also   : ICsrfTransitionListener.cs, CsrfTransitionEvents.cs, CsrfServiceExtensions.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Service collection extensions that register the default CSRF
/// transition listener. Use this extension when the application wants
/// the default listener without replacing it.
/// </summary>
/// <remarks>
/// The listener is registered with TryAddSingleton. An application
/// that wants a custom listener registers its own implementation before
/// calling this method, or replaces the registration after.
///
/// The lifetime follows the dependencies. ICsrfService and
/// ICsrfRotationStore are both singletons in the Phase 2.2 and 2.3
/// registrations, so the listener is a singleton. If a future phase
/// changes the store to scoped, the listener registration here changes
/// to scoped in the same edit.
///
/// The listener is not auto-registered by AddApiPilotCsrf. Applications
/// that do not need the transition hooks do not pay for them.
/// </remarks>
public static class CsrfTransitionExtensions
{
    /// <summary>
    /// Registers the default CSRF transition listener if one is not
    /// already registered.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotCsrfTransitionListener(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ICsrfTransitionListener>(sp =>
        {
            var service = sp.GetRequiredService<ICsrfService>();
            var bindingProvider = sp.GetRequiredService<ICsrfBindingProvider>();
            var logger = sp.GetRequiredService<ILogger<DefaultCsrfTransitionListener>>();
            var store = sp.GetService<ICsrfRotationStore>();
            return new DefaultCsrfTransitionListener(service, bindingProvider, logger, store);
        });

        return services;
    }
}

