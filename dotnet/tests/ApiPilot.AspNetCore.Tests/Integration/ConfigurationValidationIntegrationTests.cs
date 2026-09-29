// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ConfigurationValidationIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests that prove ApiPilot options validation fails closed at startup
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationOptionsValidator.cs, CorrelationOptionsValidator.cs,
//                A-122, A-120
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that verify the fail-closed startup property. An
/// application that configures an invalid value through the public
/// AddApiPilot* delegate fails to start; the failure message names the
/// specific misconfigured property. This exercises the full DI chain:
/// AddOptions&lt;T&gt;().Configure(...).ValidateOnStart() plus the
/// TryAddEnumerable-registered IValidateOptions implementations.
/// </summary>
[TestClass]
public sealed class ConfigurationValidationIntegrationTests
{
    /// <summary>A valid configuration starts the host successfully.</summary>
    [Test]
    public async Task StartHost_ValidConfiguration_StartsSuccessfully()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotPagination(o => o.DefaultPageSize = 25);
            },
            configureApp: app => app.MapGet("/hello", () => "hello"));

        var response = await host.Client.GetAsync("/hello");
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// An invalid pagination configuration fails at startup with a message
    /// that names the specific misconfigured property. The configuration is
    /// applied through the public AddApiPilotPagination delegate.
    /// </summary>
    [Test]
    public async Task StartHost_InvalidPaginationConfiguration_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotJson();
                    builder.Services.AddApiPilotPagination(o =>
                    {
                        o.DefaultPageSize = 500;
                        o.MaxPageSize = 100;
                    });
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw for invalid configuration.");
        TestAssert.True(message.Contains("DefaultPageSize"),
            "Expected the validation failure to name DefaultPageSize.");
    }

    /// <summary>
    /// An invalid correlation configuration fails at startup. The regex
    /// pattern is invalid, so the CorrelationOptionsValidator reports it.
    /// </summary>
    [Test]
    public async Task StartHost_InvalidCorrelationConfiguration_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotJson();
                    builder.Services.AddApiPilotCorrelation(o =>
                    {
                        o.ValidationPattern = "([unclosed";
                    });
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw for an invalid regex.");
        TestAssert.True(message.Contains("ValidationPattern"),
            "Expected the validation failure to name ValidationPattern.");
    }
}

