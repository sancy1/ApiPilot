// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/ExceptionMappingIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the exception middleware over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, InProcessHost,
//                ApiPilot.AspNetCore.ExceptionHandling, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Results, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.Tests.Serialization, ApiPilot.Core.Metadata,
//                ApiPilot.Core.Responses
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotExceptionMiddleware.cs, ApiPilotExceptionExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Tests.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that exercise the exception middleware through real
/// minimal API endpoints and real HTTP requests. Each test asserts that
/// exceptions map to the correct HTTP status, the correct error code, and
/// a safe response body that does not leak internal details.
/// </summary>
[TestClass]
public sealed class ExceptionMappingIntegrationTests
{
    private static async Task<InProcessHost> StartThrowingHost(Action<WebApplication> extraEndpoints)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotExceptions();
            },
            configureApp: app =>
            {
                app.UseApiPilotExceptions();
                extraEndpoints(app);
            });
    }

    /// <summary>An ArgumentException maps to 400 VALIDATION_ERROR.</summary>
    [Test]
    public async Task Get_ArgumentException_Returns400WithValidationError()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/throw-argument", () => { throw new ArgumentException("bad"); }));

        var response = await host.Client.GetAsync("/throw-argument");
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A KeyNotFoundException maps to 404 RESOURCE_NOT_FOUND.</summary>
    [Test]
    public async Task Get_KeyNotFoundException_Returns404()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/throw-notfound", () => { throw new KeyNotFoundException(); }));

        var response = await host.Client.GetAsync("/throw-notfound");
        TestAssert.Equal(404, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("RESOURCE_NOT_FOUND",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>An unknown exception maps to 500 with a generic message.</summary>
    [Test]
    public async Task Get_UnknownException_Returns500WithGenericMessage()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/throw-unknown", () => { throw new InvalidCastException("secret-detail"); }));

        var response = await host.Client.GetAsync("/throw-unknown");
        TestAssert.Equal(500, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("INTERNAL_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        TestAssert.False(json.Contains("secret-detail"));
    }

    /// <summary>The unknown-exception response does not leak the type name.</summary>
    [Test]
    public async Task Get_UnknownException_DoesNotLeakExceptionType()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/throw-unknown2", () => { throw new InvalidCastException(); }));

        var response = await host.Client.GetAsync("/throw-unknown2");
        var json = await response.Content.ReadAsStringAsync();
        TestAssert.False(json.Contains("InvalidCastException"));
    }

    /// <summary>The middleware does not interfere with successful responses.</summary>
    [Test]
    public async Task Get_SuccessfulEndpoint_IsNotAffected()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/ok", () => ApiResponseBuilder.Ok(
                new SampleOrder { Id = "ORD-OK", Status = "confirmed" },
                new ResponseMetadata { RequestId = "req-exc-ok" }).ToResult()));

        var response = await host.Client.GetAsync("/ok");
        TestAssert.Equal(200, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    /// <summary>The middleware does not change the status of a normal response.</summary>
    [Test]
    public async Task Get_NoException_DoesNotChangeStatus()
    {
        await using var host = await StartThrowingHost(app =>
            app.MapGet("/ping", () => ApiResponseBuilder.Ok("pong",
                new ResponseMetadata { RequestId = "req-ping" }).ToResult()));

        var response = await host.Client.GetAsync("/ping");
        TestAssert.Equal(200, (int)response.StatusCode);
    }
}

