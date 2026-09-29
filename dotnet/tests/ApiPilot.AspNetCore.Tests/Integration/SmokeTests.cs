// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/SmokeTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Smoke tests that prove the in-process ASP.NET Core host works end to end
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, InProcessHost
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : InProcessHost.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Smoke tests for the integration test infrastructure. These tests do not
/// exercise ApiPilot code; they exercise the in-process ASP.NET Core host
/// and the loopback HTTP client that every later integration test will
/// depend on.
/// </summary>
[TestClass]
public sealed class SmokeTests
{
    /// <summary>
    /// Starts an in-process host with a trivial endpoint, issues a real
    /// HTTP GET, and asserts on the response.
    /// </summary>
    [Test]
    public async Task Get_HealthEndpoint_Returns200WithOkBody()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: null,
            configureApp: app =>
            {
                app.MapGet("/health", () => "ok");
            });

        var response = await host.Client.GetAsync("/health");
        TestAssert.Equal(200, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        TestAssert.Equal("ok", body);
    }
}

