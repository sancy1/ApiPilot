// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/ApiPilotRateLimitExtensionsTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests for the AddApiPilotRateLimitRejection DI extension
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.RateLimiting, Microsoft.Extensions.DependencyInjection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotRateLimitExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Tests for the AddApiPilotRateLimitRejection extension. Verifies the
/// null guard, chaining, options registration, the configure callback,
/// the validator registration, and the fail-closed startup behavior.
/// </summary>
[TestClass]
public sealed class ApiPilotRateLimitExtensionsTests
{
    /// <summary>A null service collection throws ArgumentNullException.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_NullServicesThrows()
    {
        TestAssert.Throws<ArgumentNullException>(
            () => ApiPilotRateLimitExtensions.AddApiPilotRateLimitRejection(null!));
    }

    /// <summary>The extension returns the same service collection.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_ReturnsSameServices()
    {
        var services = new ServiceCollection();
        var returned = services.AddApiPilotRateLimitRejection();
        TestAssert.Equal(services, returned);
    }

    /// <summary>The options type is registered and resolvable.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>();
        TestAssert.NotNull(options);
    }

    /// <summary>The default options resolve to the documented values.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_DefaultOptions_HaveExpectedValues()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value;
        TestAssert.Equal(429, options.StatusCode);
        TestAssert.True(options.EmitRetryAfter);
    }

    /// <summary>The configure callback mutates the registered options.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_ConfigureCallback_IsApplied()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection(o =>
        {
            o.StatusCode = 503;
            o.Message = "custom";
        });
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value;
        TestAssert.Equal(503, options.StatusCode);
        TestAssert.Equal("custom", options.Message);
    }

    /// <summary>The validator is registered.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_RegistersValidator()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection();
        using var provider = services.BuildServiceProvider();
        var validators = provider.GetServices<IValidateOptions<ApiPilotRateLimitOptions>>();
        var count = 0;
        foreach (var _ in validators) { count++; }
        TestAssert.Equal(1, count);
    }

    /// <summary>An invalid StatusCode throws OptionsValidationException on resolution.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_InvalidStatusCode_ThrowsOnResolution()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection(o => o.StatusCode = 200);
        using var provider = services.BuildServiceProvider();
        TestAssert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value);
    }

    /// <summary>An invalid Message throws OptionsValidationException on resolution.</summary>
    [Test]
    public void AddApiPilotRateLimitRejection_InvalidMessage_ThrowsOnResolution()
    {
        var services = new ServiceCollection();
        services.AddApiPilotRateLimitRejection(o => o.Message = string.Empty);
        using var provider = services.BuildServiceProvider();
        TestAssert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value);
    }
}

