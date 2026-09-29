// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/HttpStatusMappingIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for HTTP status mapping over real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, InProcessHost,
//                ApiPilot.AspNetCore.Results, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.AspNetCore.Tests.Serialization, ApiPilot.Core.Metadata,
//                ApiPilot.Core.Responses
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiResponseHttpMapper.cs, ApiPilotResultsExtensions.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Tests.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that exercise ApiPilot HTTP status mapping through
/// real minimal API endpoints and real HTTP requests. Each test asserts
/// the numeric status code and, where a body is expected, the shape of
/// the response envelope.
/// </summary>
[TestClass]
public sealed class HttpStatusMappingIntegrationTests
{
    private static ResponseMetadata Meta()
    {
        return new ResponseMetadata { RequestId = "req-status-001" };
    }

    private static SampleOrder Order()
    {
        return new SampleOrder { Id = "ORD-STATUS", Status = "confirmed" };
    }

    /// <summary>An Ok response maps to 200 with a standard envelope.</summary>
    [Test]
    public async Task Get_OkResponse_Returns200WithEnvelope()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapGet("/ok", () =>
                ApiResponseBuilder.Ok(Order(), Meta()).ToResult()));

        var response = await host.Client.GetAsync("/ok");
        TestAssert.Equal(200, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.GetProperty("success").GetBoolean());
        TestAssert.True(root.TryGetProperty("data", out var data));
        TestAssert.Equal("ORD-STATUS", data.GetProperty("id").GetString());
    }

    /// <summary>A Created response maps to 201.</summary>
    [Test]
    public async Task Post_CreatedResponse_Returns201()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapPost("/orders", () =>
                ApiResponseBuilder.Created(Order(), Meta()).ToResult()));

        var response = await host.Client.PostAsync("/orders", null);
        TestAssert.Equal(201, (int)response.StatusCode);
    }

    /// <summary>An Accepted response maps to 202.</summary>
    [Test]
    public async Task Post_AcceptedResponse_Returns202()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapPost("/jobs", () =>
                ApiResponseBuilder.Accepted("job-123", Meta()).ToResult()));

        var response = await host.Client.PostAsync("/jobs", null);
        TestAssert.Equal(202, (int)response.StatusCode);
    }

    /// <summary>A NoContent response maps to 204 with no body.</summary>
    [Test]
    public async Task Delete_NoContentResponse_Returns204WithEmptyBody()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder => builder.Services.AddApiPilotJson(),
            configureApp: app => app.MapDelete("/orders/1", () =>
                ApiResponseBuilder.NoContent(Meta()).ToResult()));

        var response = await host.Client.DeleteAsync("/orders/1");
        TestAssert.Equal(204, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        TestAssert.Equal(string.Empty, body);
    }
}

