// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ApiExceptionOptionsResolutionIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Verifies that a custom ErrorCodeToStatusMap configured via AddApiPilotExceptions reaches both validation paths
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + MVC controllers under test)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.Validation, ApiPilot.AspNetCore.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotExceptionExtensions.cs, ApiPilotValidationFilter.cs,
//                ApiPilotInvalidModelStateResponseFactory.cs, PLANNING.md (A-073)
// -----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that verify a custom ErrorCodeToStatusMap set on
/// ApiExceptionOptions via AddApiPilotExceptions reaches both validation
/// paths: the [ApiPilotValidate] action filter and the [ApiController]
/// InvalidModelStateResponseFactory. This is the test that catches the
/// DI-resolution defect recorded as finding A-073.
/// </summary>
[TestClass]
public sealed class ApiExceptionOptionsResolutionIntegrationTests
{
    private const int CustomValidationStatus = 422;

    private static Action<ApiExceptionOptions> CustomMap()
    {
        return o =>
        {
            o.ErrorCodeToStatusMap = new Dictionary<string, int>
            {
                { "VALIDATION_ERROR", CustomValidationStatus }
            };
        };
    }

    private static HttpContent InvalidJson()
    {
        return new StringContent("{}", Encoding.UTF8, "application/json");
    }

    private static async Task<InProcessHost> StartFilterPathHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotExceptions(CustomMap());
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(CustomMapSimpleController).Assembly);
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    private static async Task<InProcessHost> StartApiControllerPathHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotExceptions(CustomMap());
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(CustomMapApiController).Assembly);
                builder.Services.AddApiPilotControllers();
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapControllers();
            });
    }

    /// <summary>
    /// The [ApiPilotValidate] filter path honors a custom ErrorCodeToStatusMap.
    /// If the DI-resolution defect were present, this test would see 400
    /// (the built-in default) instead of the configured 422.
    /// </summary>
    [Test]
    public async Task Post_InvalidModel_FilterPath_UsesCustomStatus()
    {
        await using var host = await StartFilterPathHostAsync();
        var response = await host.Client.PostAsync("/api/custommap-simple", InvalidJson());
        TestAssert.Equal(CustomValidationStatus, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>
    /// The [ApiController] path honors the same custom ErrorCodeToStatusMap.
    /// Both paths must produce the same HTTP status for the same input.
    /// </summary>
    [Test]
    public async Task Post_InvalidModel_ApiControllerPath_UsesCustomStatus()
    {
        await using var host = await StartApiControllerPathHostAsync();
        var response = await host.Client.PostAsync("/api/custommap-apicontroller", InvalidJson());
        TestAssert.Equal(CustomValidationStatus, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }
}

public sealed class CustomMapSimpleRequest
{
    [Required]
    public string? Email { get; set; }
}

[Route("api/custommap-simple")]
public sealed class CustomMapSimpleController : ControllerBase
{
    [HttpPost]
    [ApiPilotValidate]
    public IActionResult Post([FromBody] CustomMapSimpleRequest request)
    {
        return Ok(new { received = request.Email });
    }
}

public sealed class CustomMapApiControllerRequest
{
    [Required]
    public string? Email { get; set; }
}

[ApiController]
[Route("api/custommap-apicontroller")]
public sealed class CustomMapApiController : ControllerBase
{
    [HttpPost]
    public IActionResult Post([FromBody] CustomMapApiControllerRequest request)
    {
        return Ok(new { received = request.Email });
    }
}

