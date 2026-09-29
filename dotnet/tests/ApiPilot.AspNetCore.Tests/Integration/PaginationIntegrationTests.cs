// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/PaginationIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for pagination through real HTTP requests
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.EndpointMetadata, ApiPilot.AspNetCore.Results,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationEndpointFilter.cs, SPEC.md (paginated envelope)
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests for the pagination endpoint filter through real
/// HTTP requests against an in-process ASP.NET Core host.
/// </summary>
[TestClass]
public sealed class PaginationIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(Action<WebApplication> mapEndpoints)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotPagination();
            },
            configureApp: app =>
            {
                mapEndpoints(app);
            });
    }

    private static IResult PaginatedHandler(HttpContext context)
    {
        var page = context.GetPageRequest() ?? PageRequest.Create(1, 20, new PaginationOptions());
        var items = new[] { "a", "b", "c" };
        var meta = new ResponseMetadata { RequestId = "req-integration" };
        var paged = PagedResultBuilder.From(items, page.Page, page.PageSize, 100);
        return paged.ToPagedResult(meta);
    }

    /// <summary>An endpoint with the pagination filter returns the standard envelope.</summary>
    [Test]
    public async Task Get_EndpointWithPaginationFilter_ReturnsPaginatedEnvelope()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler).WithApiPilotPagination());

        var response = await host.Client.GetAsync("/items");
        TestAssert.Equal(200, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.GetProperty("success").GetBoolean());
        TestAssert.Equal(JsonValueKind.Array, root.GetProperty("data").ValueKind);
        TestAssert.Equal(3, root.GetProperty("data").GetArrayLength());

        var pagination = root.GetProperty("pagination");
        TestAssert.True(pagination.TryGetProperty("page", out _));
        TestAssert.True(pagination.TryGetProperty("pageSize", out _));
        TestAssert.True(pagination.TryGetProperty("totalItems", out _));
        TestAssert.True(pagination.TryGetProperty("totalPages", out _));
        TestAssert.True(pagination.TryGetProperty("hasNext", out _));
        TestAssert.True(pagination.TryGetProperty("hasPrevious", out _));

        var meta = root.GetProperty("meta");
        TestAssert.Equal("req-integration", meta.GetProperty("requestId").GetString());
    }

    /// <summary>No query parameters uses the global default page size.</summary>
    [Test]
    public async Task Get_NoQueryParameters_UsesGlobalDefaults()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler).WithApiPilotPagination());

        var json = await host.Client.GetStringAsync("/items");
        using var doc = JsonDocument.Parse(json);
        var pagination = doc.RootElement.GetProperty("pagination");
        TestAssert.Equal(1, pagination.GetProperty("page").GetInt32());
        TestAssert.Equal(20, pagination.GetProperty("pageSize").GetInt32());
    }

    /// <summary>Custom parameter names set via the fluent extension are honored.</summary>
    [Test]
    public async Task Get_CustomParameterNames_HonorsOverride()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler)
               .WithApiPilotPagination(o => o.ParameterNames = new QueryParameterNames { Page = "pageNumber", PageSize = "perPage" }));

        var json = await host.Client.GetStringAsync("/items?pageNumber=2&perPage=5");
        using var doc = JsonDocument.Parse(json);
        var pagination = doc.RootElement.GetProperty("pagination");
        TestAssert.Equal(2, pagination.GetProperty("page").GetInt32());
        TestAssert.Equal(5, pagination.GetProperty("pageSize").GetInt32());
    }

    /// <summary>Custom success status code is honored.</summary>
    [Test]
    public async Task Get_CustomSuccessStatusCode_ReturnsConfiguredStatus()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler)
               .WithApiPilotPagination(o => o.SuccessStatusCode = 206));

        var response = await host.Client.GetAsync("/items");
        TestAssert.Equal(206, (int)response.StatusCode);
    }

    /// <summary>An invalid page returns a 400 validation envelope.</summary>
    [Test]
    public async Task Get_InvalidPage_Returns400ValidationEnvelope()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler).WithApiPilotPagination());

        var response = await host.Client.GetAsync("/items?page=abc");
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        var fields = doc.RootElement.GetProperty("error").GetProperty("fields");
        TestAssert.True(fields.TryGetProperty("page", out _));
    }

    /// <summary>A page size above the maximum returns 400.</summary>
    [Test]
    public async Task Get_PageSizeAboveMax_Returns400()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler).WithApiPilotPagination());

        var response = await host.Client.GetAsync("/items?pageSize=999");
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A per-endpoint MaxPageSize override is honored.</summary>
    [Test]
    public async Task Get_PerEndpointMaxPageSize_OverridesGlobal()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler)
               .WithApiPilotPagination(o => o.MaxPageSize = 50));

        var response = await host.Client.GetAsync("/items?pageSize=75");
        TestAssert.Equal(400, (int)response.StatusCode);
    }

    /// <summary>The endpoint handler reads the parsed page request from context.</summary>
    [Test]
    public async Task Get_EndpointHandlerReadsPageRequestFromContext()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/echo", (HttpContext context) =>
            {
                var p = context.GetPageRequest();
                var response = new ApiResponse<string>(
                    $"page={p!.Page};pageSize={p.PageSize}",
                    ApiResponseStatus.Ok,
                    new ResponseMetadata { RequestId = "req-echo" });
                return response.ToResult();
            }).WithApiPilotPagination());

        var json = await host.Client.GetStringAsync("/echo?page=4&pageSize=7");
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("page=4;pageSize=7", doc.RootElement.GetProperty("data").GetString());
    }
}

