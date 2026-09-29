// filepath: dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotCsrfObservabilityExtensions.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.5.0
// purpose: Opt-in service collection extension that registers the CSRF metrics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilotCsrfCounters, IServiceCollection
//   Used by    : application setup code that wants CSRF metrics
//   See also   : ApiPilotCsrfCounters.cs, docs/observability.md (Phase 4.3)
// -----------------------------------------------------------------------------
//
// SCOPE
//   The registration is opt-in and idempotent. Calling this extension
//   twice registers ApiPilotCsrfCounters exactly once. An application
//   that registers its own ApiPilotCsrfCounters before this extension
//   runs keeps its own registration because TryAddSingleton does not
//   overwrite.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// Registers the CSRF metrics surface. Call this extension to opt in to
/// the counters and histograms emitted by <see cref="ApiPilotCsrfCounters"/>.
/// </summary>
/// <remarks>
/// <para>
/// The registration is idempotent. <c>TryAddSingleton</c> registers
/// <see cref="ApiPilotCsrfCounters"/> exactly once, even if this
/// extension is called more than once.
/// </para>
/// <para>
/// An application that wants to supply its own counters implementation
/// registers a subclass or a compatible service before this extension
/// runs. Because <c>TryAddSingleton</c> does not overwrite an existing
/// registration, the application-provided registration wins.
/// </para>
/// </remarks>
public static class ApiPilotCsrfObservabilityExtensions
{
    /// <summary>
    /// Registers <see cref="ApiPilotCsrfCounters"/> as a singleton.
    /// </summary>
    /// <param name="services">The service collection. Must not be null.</param>
    /// <returns>The same service collection, for method chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is null.
    /// </exception>
    public static IServiceCollection AddApiPilotCsrfObservability(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ApiPilotCsrfCounters>();
        return services;
    }
}

