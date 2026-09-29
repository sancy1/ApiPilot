// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/RateLimitIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Integration tests for the rate-limit rejection over a real loopback HTTP pipeline
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, InProcessHost,
//                ApiPilot.AspNetCore.RateLimiting, Microsoft.AspNetCore.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : RateLimitDiagnosticsHook.cs, InProcessHost.cs, docs/rate-limiting.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.RateLimiting;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Integration tests for the rate-limit rejection. Starts a real
/// ASP.NET Core application on a loopback port, installs the ApiPilot
/// rejection handler, and asserts the standardized response over HTTP.
/// </summary>
[TestClass]
public sealed class RateLimitIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotRateLimitRejection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddRateLimiter(options =>
                {
                    options.RejectionStatusCode = 429;
                    options.OnRejected = RateLimitDiagnosticsHook.HandleAsync;
                    options.AddFixedWindowLimiter("test", o =>
                    {
                        o.PermitLimit = 1;
                        o.Window = TimeSpan.FromMinutes(1);
                        o.QueueLimit = 0;
                    });
                });
            },
            configureApp: app =>
            {
                app.UseRateLimiter();
                app.MapGet("/limited", () => Microsoft.AspNetCore.Http.Results.Ok(new { ok = true }))
                    .RequireRateLimiting("test");
            });
    }

    /// <summary>A request over the limit returns 429 with the rate_limited envelope.</summary>
    [Test]
    public async Task Rejection_Returns429WithEnvelope()
    {
        await using var host = await StartHostAsync();

        // First request consumes the single permit.
        await host.Client.GetAsync(new Uri(host.BaseAddress, "/limited"));
        // Second request is rejected.
        var response = await host.Client.GetAsync(new Uri(host.BaseAddress, "/limited"));

        TestAssert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var code = doc.RootElement.GetProperty("error").GetProperty("code").GetString();
        TestAssert.Equal("RATE_LIMITED", code);
    }

    /// <summary>The rejection envelope carries a non-empty request id.</summary>
    [Test]
    public async Task Rejection_EchoesRequestId()
    {
        await using var host = await StartHostAsync();

        await host.Client.GetAsync(new Uri(host.BaseAddress, "/limited"));
        var response = await host.Client.GetAsync(new Uri(host.BaseAddress, "/limited"));

        TestAssert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.NotNull(requestId);
        TestAssert.True(requestId!.Length > 0);
    }

    /// <summary>A request under the limit succeeds.</summary>
    [Test]
    public async Task SuccessfulRequest_UnderLimit_Passes()
    {
        await using var host = await StartHostAsync();

        var response = await host.Client.GetAsync(new Uri(host.BaseAddress, "/limited"));

        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

