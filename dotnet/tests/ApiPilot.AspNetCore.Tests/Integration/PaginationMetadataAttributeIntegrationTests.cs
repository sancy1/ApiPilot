// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Integration/PaginationMetadataAttributeIntegrationTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Integration tests for the PaginationMetadataAttribute on minimal API endpoints
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.DependencyInjection,
//                ApiPilot.AspNetCore.EndpointMetadata, ApiPilot.AspNetCore.Results,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationMetadataAttribute.cs, PaginationIntegrationTests.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.AspNetCore.Tests.Integration;

/// <summary>
/// Integration tests that verify the PaginationMetadataAttribute is
/// read by the pagination endpoint filter through real HTTP requests.
/// </summary>
[TestClass]
public sealed class PaginationMetadataAttributeIntegrationTests
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

    /// <summary>The attribute overrides MaxPageSize on the endpoint.</summary>
    [Test]
    public async Task Get_EndpointWithMetadataAttribute_MaxPageSizeOverride()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler)
               .WithApiPilotPagination()
               .WithMetadata(new PaginationMetadataAttribute { MaxPageSize = 50 }));

        var response = await host.Client.GetAsync("/items?pageSize=75");
        TestAssert.Equal(400, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("VALIDATION_ERROR",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>Fluent ParameterNames and attribute MaxPageSize compose.</summary>
    [Test]
    public async Task Get_EndpointWithMetadataAttribute_ParameterNamesViaFluent()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/items", PaginatedHandler)
               .WithApiPilotPagination(o => o.ParameterNames = new QueryParameterNames { Page = "p" })
               .WithMetadata(new PaginationMetadataAttribute { MaxPageSize = 50 }));

        var json = await host.Client.GetStringAsync("/items?p=2");
        using var doc = JsonDocument.Parse(json);
        var pagination = doc.RootElement.GetProperty("pagination");
        TestAssert.Equal(2, pagination.GetProperty("page").GetInt32());
        TestAssert.Equal(20, pagination.GetProperty("pageSize").GetInt32());
    }

    /// <summary>An endpoint without the filter is unaffected by pagination.</summary>
    [Test]
    public async Task Get_EndpointWithoutPaginationFilter_NoValidationRuns()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/plain", () => "ok"));

        var response = await host.Client.GetAsync("/plain?page=abc");
        TestAssert.Equal(200, (int)response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        TestAssert.Equal("ok", body);
    }
}

