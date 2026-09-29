// filepath: dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotSecurityDiagnosticsExtensions.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.3.0
// purpose: Service collection extension that registers the security diagnostics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : SecurityConfigurationValidator, ApiPilotSecurityDiagnostics,
//                SecurityDiagnosticsHostedService, IServiceCollection
//   Used by    : application setup code in Program.cs
//   See also   : ApiPilotSecurityDiagnostics.cs, SecurityDiagnosticsHostedService.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// Registers the ApiPilot security diagnostics. Call this after the
/// other AddApiPilot* security extensions so the diagnostics can
/// verify their registrations.
/// </summary>
public static class ApiPilotSecurityDiagnosticsExtensions
{
    /// <summary>
    /// Registers the diagnostics surface, the registration checker, and
    /// the startup hosted service.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotSecurityDiagnostics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<SecurityConfigurationValidator>();

        services.TryAddSingleton<ApiPilotSecurityDiagnostics>(sp =>
        {
            var options = sp.GetRequiredService<ApiPilotDataProtectionOptions>();
            return new ApiPilotSecurityDiagnostics(options);
        });

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, SecurityDiagnosticsHostedService>());

        return services;
    }
}

