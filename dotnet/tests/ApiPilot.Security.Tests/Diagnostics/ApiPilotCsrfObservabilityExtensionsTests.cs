// filepath: dotnet/tests/ApiPilot.Security.Tests/Diagnostics/ApiPilotCsrfObservabilityExtensionsTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.Security.Tests | since: v0.5.0
// purpose: Tests for the AddApiPilotCsrfObservability DI extension
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotCsrfObservabilityExtensions, Microsoft.Extensions.DependencyInjection
//   Used by    : the test harness
//   See also   : ApiPilotCsrfObservabilityExtensions.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using ApiPilot.Security.Diagnostics;

namespace ApiPilot.Security.Tests.Diagnostics;

/// <summary>Contract tests for the AddApiPilotCsrfObservability
/// extension. Verifies the null guard, the singleton registration,
/// idempotence, chaining, and the preserve-existing-registration
/// contract.</summary>
[TestClass]
public sealed class ApiPilotCsrfObservabilityExtensionsTests
{
    /// <summary>A null service collection throws ArgumentNullException.</summary>
    [Test]
    public void AddApiPilotCsrfObservability_NullServicesThrows()
    {
        TestAssert.Throws<ArgumentNullException>(
            () => ApiPilotCsrfObservabilityExtensions.AddApiPilotCsrfObservability(null!));
    }

    /// <summary>The counters are registered as a singleton.</summary>
    [Test]
    public void AddApiPilotCsrfObservability_RegistersCountersAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddApiPilotCsrfObservability();
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ApiPilotCsrfCounters>();
        var second = provider.GetRequiredService<ApiPilotCsrfCounters>();

        TestAssert.NotNull(first);
        TestAssert.Equal(first, second);
    }

    /// <summary>The extension returns the same service collection.</summary>
    [Test]
    public void AddApiPilotCsrfObservability_ReturnsSameServices()
    {
        var services = new ServiceCollection();
        var returned = services.AddApiPilotCsrfObservability();
        TestAssert.Equal(services, returned);
    }

    /// <summary>Calling the extension twice registers the counters once.</summary>
    [Test]
    public void AddApiPilotCsrfObservability_IsIdempotent()
    {
        var services = new ServiceCollection();
        services.AddApiPilotCsrfObservability();
        services.AddApiPilotCsrfObservability();
        using var provider = services.BuildServiceProvider();

        var all = provider.GetServices<ApiPilotCsrfCounters>();
        var count = 0;
        foreach (var _ in all) { count++; }

        TestAssert.Equal(1, count);
    }

    /// <summary>An existing registration is not overwritten.</summary>
    [Test]
    public void AddApiPilotCsrfObservability_DoesNotOverwriteExistingRegistration()
    {
        var custom = new ApiPilotCsrfCounters();
        var services = new ServiceCollection();
        services.AddSingleton(custom);
        services.AddApiPilotCsrfObservability();
        using var provider = services.BuildServiceProvider();

        var resolved = provider.GetRequiredService<ApiPilotCsrfCounters>();
        TestAssert.Equal(custom, resolved);
    }
}

