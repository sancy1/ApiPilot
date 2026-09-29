// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ApiPilotControllersIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the [ApiController] validation path over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + MVC controller under test)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotInvalidModelStateResponseFactory.cs, ApiPilotServiceCollectionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

[TestClass]
public sealed class ApiPilotControllersIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(ApiControllerSampleController).Assembly);
                builder.Services.AddApiPilotControllers();
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    private static HttpContent JsonContent(string body)
    {
        return new StringContent(body, Encoding.UTF8, "application/json");
    }

    [Test]
    public async Task Post_InvalidModel_WithApiController_Returns400WithValidationError()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.PostAsync("/api/apipilot-sample", JsonContent("{}"));
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Test]
    public async Task Post_InvalidModel_WithApiController_FieldKeyIsCamelCased()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.PostAsync("/api/apipilot-sample", JsonContent("{}"));
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("email", out var messages));
        TestAssert.Equal(JsonValueKind.Array, messages.ValueKind);
    }

    [Test]
    public async Task Post_ValidModel_WithApiController_ExecutesAction()
    {
        await using var host = await StartHostAsync();
        var body = "{\"email\":\"user@example.com\"}";
        var response = await host.Client.PostAsync("/api/apipilot-sample", JsonContent(body));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    [Test]
    public async Task AddApiPilotControllers_AfterAddControllers_OverridesDefaultProblemDetailsResponse()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.PostAsync("/api/apipilot-sample", JsonContent("{}"));
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        TestAssert.True(doc.RootElement.TryGetProperty("success", out _));
        TestAssert.True(doc.RootElement.TryGetProperty("error", out var error));
        TestAssert.Equal("VALIDATION_ERROR", error.GetProperty("code").GetString());
    }
}

public sealed class ApiControllerSampleRequest
{
    [Required]
    public string? Email { get; set; }
}

[ApiController]
[Route("api/apipilot-sample")]
public sealed class ApiControllerSampleController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] ApiControllerSampleRequest request)
    {
        return Ok(new { received = request.Email });
    }
}

