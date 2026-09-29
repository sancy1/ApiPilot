// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ContentNegotiationIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the content negotiation middleware through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Core.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotContentNegotiationMiddleware.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests for the content negotiation middleware through real
/// HTTP requests against an in-process host.
/// </summary>
[TestClass]
public sealed class ContentNegotiationIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(
        Action<ContentNegotiationOptions>? configure = null)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotContentNegotiation(configure);
            },
            configureApp: app =>
            {
                app.UseApiPilotContentNegotiation();
                app.MapGet("/hello", () => "hello");
                app.MapPost("/submit", () => "submitted");
            });
    }

    /// <summary>No Accept header returns 200.</summary>
    [Test]
    public async Task Get_NoAcceptHeader_Returns200()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Accept.Clear();
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>Accept: application/json returns 200.</summary>
    [Test]
    public async Task Get_AcceptJson_Returns200()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>Accept: application/xml returns 406.</summary>
    [Test]
    public async Task Get_AcceptXml_Returns406()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(406, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("NOT_ACCEPTABLE",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>
    /// A 406 response carries the correlation ID from the incoming
    /// X-Request-Id header in both the response header and the JSON meta.
    /// </summary>
    [Test]
    public async Task Get_AcceptXml_Returns406_WithCorrelationIdInBody()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotContentNegotiation();
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotContentNegotiation();
                app.MapGet("/hello", () => "hello");
            });

        var incomingId = "abcdef12-3456-7890";
        var request = new HttpRequestMessage(HttpMethod.Get, "/hello");
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/xml"));
        request.Headers.Add("X-Request-Id", incomingId);

        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(406, (int)response.StatusCode);

        var echoed = response.Headers.GetValues("X-Request-Id").First();
        TestAssert.Equal(incomingId, echoed);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal(incomingId, requestId);
    }

    /// <summary>POST with no Content-Type returns 415.</summary>
    [Test]
    public async Task Post_NoContentType_Returns415()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/submit");
        request.Content = new StringContent(string.Empty, Encoding.UTF8);
        request.Content.Headers.ContentType = null;
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(415, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("UNSUPPORTED_MEDIA_TYPE",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>POST with application/json returns 200.</summary>
    [Test]
    public async Task Post_JsonContentType_Returns200()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/submit");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>POST with application/xml returns 415.</summary>
    [Test]
    public async Task Post_XmlContentType_Returns415()
    {
        await using var host = await StartHostAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/submit");
        request.Content = new StringContent("<root/>", Encoding.UTF8, "application/xml");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(415, (int)response.StatusCode);
    }
}

