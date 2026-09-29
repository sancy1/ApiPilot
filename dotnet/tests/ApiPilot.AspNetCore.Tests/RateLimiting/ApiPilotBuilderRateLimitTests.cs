// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/ApiPilotBuilderRateLimitTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests that ApiPilotBuilder.ConfigureRateLimitRejection delegates and propagates
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.DependencyInjection, ApiPilot.AspNetCore.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotBuilder.cs, ApiPilotRateLimitExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Tests that ApiPilotBuilder.ConfigureRateLimitRejection delegates to the
/// per-concern extension and propagates the configuration.
/// </summary>
[TestClass]
public sealed class ApiPilotBuilderRateLimitTests
{
    /// <summary>The builder method returns the same builder for chaining.</summary>
    [Test]
    public void ConfigureRateLimitRejection_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddApiPilot();
        var returned = builder.ConfigureRateLimitRejection();
        TestAssert.Equal(builder, returned);
    }

    /// <summary>The builder method registers the options via the extension.</summary>
    [Test]
    public void ConfigureRateLimitRejection_DelegatesToExtension()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureRateLimitRejection();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>();
        TestAssert.NotNull(options);
    }

    /// <summary>The configure callback propagates to the resolved options.</summary>
    [Test]
    public void ConfigureRateLimitRejection_ConfigureCallback_Propagates()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureRateLimitRejection(o =>
        {
            o.StatusCode = 503;
            o.Message = "from builder";
        });
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value;
        TestAssert.Equal(503, options.StatusCode);
        TestAssert.Equal("from builder", options.Message);
    }

    /// <summary>A null callback registers the defaults.</summary>
    [Test]
    public void ConfigureRateLimitRejection_NullCallback_RegistersDefaults()
    {
        var services = new ServiceCollection();
        services.AddApiPilot().ConfigureRateLimitRejection(null);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ApiPilotRateLimitOptions>>().Value;
        TestAssert.Equal(429, options.StatusCode);
        TestAssert.True(options.EmitRetryAfter);
    }
}

