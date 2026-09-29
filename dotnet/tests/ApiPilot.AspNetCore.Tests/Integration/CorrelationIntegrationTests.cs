// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/CorrelationIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the correlation middleware through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotCorrelationMiddleware.cs, ApiPilotCorrelationExtensions.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests for the correlation middleware through real HTTP
/// requests against an in-process host.
/// </summary>
[TestClass]
public sealed class CorrelationIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(
        Action<CorrelationOptions>? configureCorrelation = null)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation(configureCorrelation);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.MapGet("/hello", (HttpContext ctx) =>
                {
                    var id = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
                    return id ?? "none";
                });
            });
    }

    /// <summary>No incoming header generates and echoes an ID.</summary>
    [Test]
    public async Task Get_NoIncomingHeader_ResponseIncludesGeneratedRequestIdHeader()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.GetAsync("/hello");
        TestAssert.True(response.Headers.Contains("X-Request-Id"));
    }

    /// <summary>A valid incoming header is echoed.</summary>
    [Test]
    public async Task Get_ValidIncomingHeader_EchoedInResponseHeader()
    {
        var incoming = "abcdef12-3456-7890";
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Add("X-Request-Id", incoming);
        var response = await host.Client.SendAsync(request);
        TestAssert.True(response.Headers.Contains("X-Request-Id"));
        var echoed = response.Headers.GetValues("X-Request-Id").First();
        TestAssert.Equal(incoming, echoed);
    }

    /// <summary>An invalid header with the default policy generates a new ID.</summary>
    [Test]
    public async Task Get_InvalidIncomingHeader_DefaultPolicy_GeneratesNewId()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Add("X-Request-Id", "short");
        var response = await host.Client.SendAsync(request);
        var echoed = response.Headers.GetValues("X-Request-Id").First();
        TestAssert.NotEqual("short", echoed);
    }

    /// <summary>An invalid header with the Reject policy returns 400.</summary>
    [Test]
    public async Task Get_InvalidIncomingHeader_RejectPolicy_Returns400()
    {
        await using var host = await StartHostAsync(
            o => o.InvalidIncomingIdPolicy = CorrelationInvalidIdPolicy.Reject);
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Add("X-Request-Id", "short");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(400, (int)response.StatusCode);
    }

    /// <summary>The response body carries the meta.requestId when EchoInResponseBody is true.</summary>
    [Test]
    public async Task Get_ResponseBodyMeta_ContainsRequestId()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotExceptions();
                builder.Services.AddApiPilotCorrelation();
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/boom", () => { throw new InvalidOperationException("x"); });
            });

        var response = await host.Client.GetAsync("/boom");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.NotNull(requestId);
        TestAssert.True(requestId!.Length > 0);
    }

    /// <summary>A custom header name is read from the request.</summary>
    [Test]
    public async Task Get_CustomHeaderName_ReadsFromCustomHeader()
    {
        var incoming = "custom-correlation-id";
        await using var host = await StartHostAsync(o => o.HeaderName = "X-Correlation-Id");
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Add("X-Correlation-Id", incoming);
        var response = await host.Client.SendAsync(request);
        TestAssert.True(response.Headers.Contains("X-Correlation-Id"));
        var echoed = response.Headers.GetValues("X-Correlation-Id").First();
        TestAssert.Equal(incoming, echoed);
    }
}

