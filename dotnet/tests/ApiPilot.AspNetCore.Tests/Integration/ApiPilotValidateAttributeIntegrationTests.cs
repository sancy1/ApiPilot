// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ApiPilotValidateAttributeIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the [ApiPilotValidate] action filter over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + MVC controller under test)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Validation, ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotValidateAttribute.cs, ApiPilotValidationFilter.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

[TestClass]
public sealed class ApiPilotValidateAttributeIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(SimpleController).Assembly);
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
    public async Task Post_InvalidModel_Returns400WithValidationError()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.PostAsync("/api/simple", JsonContent("{}"));
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    [Test]
    public async Task Post_InvalidModel_FieldKeyIsCamelCased()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.PostAsync("/api/simple", JsonContent("{}"));
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("email", out var messages));
        TestAssert.Equal(JsonValueKind.Array, messages.ValueKind);
    }

    [Test]
    public async Task Post_ValidModel_ExecutesAction()
    {
        await using var host = await StartHostAsync();
        var body = "{\"email\":\"user@example.com\"}";
        var response = await host.Client.PostAsync("/api/simple", JsonContent(body));
        TestAssert.Equal(200, (int)response.StatusCode);
    }
}

public sealed class SimpleRequest
{
    [Required]
    public string? Email { get; set; }
}

[Route("api/simple")]
public sealed class SimpleController : ControllerBase
{
    [HttpPost]
    [ApiPilotValidate]
    public IActionResult Post([FromBody] SimpleRequest request)
    {
        return Ok(new { received = request.Email });
    }
}

