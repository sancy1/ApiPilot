// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ApiPilotExceptionMiddlewareIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration test verifying the exception middleware works without correlation registration
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotExceptionMiddleware.cs, A-040
// -----------------------------------------------------------------------------

using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that verify the exception middleware works through
/// the DI pipeline even when AddApiPilotCorrelation has not been called.
/// This proves the "works without registration" contract holds through
/// UseMiddleware, not just through direct construction.
/// </summary>
[TestClass]
public sealed class ApiPilotExceptionMiddlewareIntegrationTests
{
    /// <summary>Exceptions are handled even without correlation registration.</summary>
    [Test]
    public async Task ExceptionMiddleware_WithoutCorrelationRegistration_StillHandlesExceptions()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotExceptions();
            },
            configureApp: app =>
            {
                app.UseApiPilotExceptions();
                app.MapGet("/boom", () => { throw new InvalidOperationException("x"); });
            });

        var response = await host.Client.GetAsync("/boom");
        TestAssert.Equal(409, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.NotNull(requestId);
        TestAssert.True(requestId!.Length > 0);
    }
}

