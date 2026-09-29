// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/OpenApi/ApiPilotOpenApiIntegrationTests.cs
// layer: OpenApi | package: ApiPilot.AspNetCore.Tests | since: v0.6.0
// purpose: End-to-end tests that serve the OpenAPI document over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, InProcessHost, ApiPilotOpenApiExtensions,
//                WebApplication, HttpClient, JsonDocument
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotOpenApiExtensions.cs, docs/openapi.md
// -----------------------------------------------------------------------------
//
// THE INTEGRATION PROOF
//   These tests start a real in-process host, map the OpenAPI document
//   endpoint, and issue a real HTTP GET. They prove the document is
//   served, parses, and contains the error schema. The custom-path
//   test proves the DocumentPath override is consulted.

using ApiPilot.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.OpenApi;

/// <summary>
/// End-to-end tests for the OpenAPI document endpoint.
/// </summary>
[TestClass]
public sealed class ApiPilotOpenApiIntegrationTests
{
    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        ConfigureServices(builder, null);
    }

    private static void ConfigureServices(WebApplicationBuilder builder, string? documentPath)
    {
        builder.Services.AddControllers().AddApplicationPart(typeof(OpenApiSampleController).Assembly);
        builder.Services.AddApiPilotOpenApi(o =>
        {
            if (documentPath is not null)
            {
                o.DocumentPath = documentPath;
            }
        });
    }

    private static void ConfigureApp(WebApplication app)
    {
        app.MapControllers();
        app.MapApiPilotOpenApi();
    }

    /// <summary>The default path serves a parseable OpenAPI 3.0.3 document.</summary>
    [Test]
    public async Task Get_AtDefaultPath_ReturnsOpenApiDocument()
    {
        await using var host = await InProcessHost.StartAsync(ConfigureServices, ConfigureApp);
        var response = await host.Client.GetAsync("/openapi/v1.json");
        TestAssert.Equal(200, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        TestAssert.True(root.TryGetProperty("openapi", out var version));
        TestAssert.Equal("3.0.3", version.GetString());
        TestAssert.True(root.TryGetProperty("paths", out _));
        TestAssert.True(root.TryGetProperty("components", out _));
    }

    /// <summary>The document includes the published error schema.</summary>
    [Test]
    public async Task Get_AtDefaultPath_IncludesErrorSchema()
    {
        await using var host = await InProcessHost.StartAsync(ConfigureServices, ConfigureApp);
        var response = await host.Client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        var schemas = document.RootElement
            .GetProperty("components")
            .GetProperty("schemas");
        TestAssert.True(schemas.TryGetProperty("ApiPilotErrorResponse", out _));
    }

    /// <summary>The custom path serves the document when DocumentPath is overridden.</summary>
    [Test]
    public async Task Get_AtCustomPath_ServesDocument_WhenPathOverridden()
    {
        await using var host = await InProcessHost.StartAsync(
            builder => ConfigureServices(builder, "/custom/openapi.json"),
            ConfigureApp);
        var response = await host.Client.GetAsync("/custom/openapi.json");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        TestAssert.True(document.RootElement.TryGetProperty("openapi", out _));
    }
}

/// <summary>A sample controller that gives the ApiExplorer one endpoint.</summary>
[ApiController]
[Route("openapi-sample")]
public sealed class OpenApiSampleController : ControllerBase
{
    /// <summary>Returns a sample value.</summary>
    /// <returns>A fixed string.</returns>
    [HttpGet]
    public string Get() => "ok";
}

