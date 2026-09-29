// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/JsonSerializationIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for ApiPilot JSON conventions over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, InProcessHost,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.AspNetCore.Tests.Serialization,
//                ApiPilot.Core.Metadata, ApiPilot.Core.Responses, System.Text.Json
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : JsonSerializationExtensions.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Tests.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests for ApiPilot JSON conventions. Each test starts an
/// in-process ASP.NET Core application, applies AddApiPilotJson through
/// the service-collection overload, maps a minimal API endpoint that
/// returns an ApiResponse, issues a real HTTP request, and asserts on
/// the parsed response.
/// </summary>
[TestClass]
public sealed class JsonSerializationIntegrationTests
{
    private static ApiResponse<SampleOrder> SampleResponse()
    {
        return new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-42", Status = "confirmed" },
            ApiResponseStatus.Ok,
            new ResponseMetadata { RequestId = "req-http-001" });
    }

    /// <summary>The HTTP response carries a standard ApiPilot envelope.</summary>
    [Test]
    public async Task Get_EndpointReturningApiResponse_ReturnsStandardEnvelope()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapGet("/sample", () => SampleResponse()));

        var response = await host.Client.GetAsync("/sample");
        TestAssert.Equal(200, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.True(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("data", out _));
        TestAssert.True(root.TryGetProperty("meta", out _));
    }

    /// <summary>Metadata keys use camelCase on the wire.</summary>
    [Test]
    public async Task Get_EndpointReturningApiResponse_UsesCamelCase()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapGet("/sample", () => SampleResponse()));

        var json = await host.Client.GetStringAsync("/sample");
        using var doc = JsonDocument.Parse(json);
        var meta = doc.RootElement.GetProperty("meta");
        TestAssert.True(meta.TryGetProperty("requestId", out var requestId));
        TestAssert.Equal("req-http-001", requestId.GetString());
    }

    /// <summary>The data payload preserves its nested shape.</summary>
    [Test]
    public async Task Get_EndpointReturningApiResponse_DataHasNestedShape()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapGet("/sample", () => SampleResponse()));

        var json = await host.Client.GetStringAsync("/sample");
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        TestAssert.Equal("ORD-42", data.GetProperty("id").GetString());
        TestAssert.Equal("confirmed", data.GetProperty("status").GetString());
    }

    /// <summary>The response carries the application/json content type.</summary>
    [Test]
    public async Task Get_EndpointReturningApiResponse_ContentTypeIsApplicationJson()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapGet("/sample", () => SampleResponse()));

        var response = await host.Client.GetAsync("/sample");
        var contentType = response.Content.Headers.ContentType?.MediaType;
        TestAssert.NotNull(contentType);
        TestAssert.Contains("application/json", contentType!);
    }
}

