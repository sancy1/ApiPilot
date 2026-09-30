// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/08_PaginationScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 08. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the pagination
//           options, attach the pagination filter to an endpoint, and observe
//           the documented defaults, the three-layer override chain
//           (global -> fluent -> attribute), the validated PageRequest in the
//           handler, the effective options accessor, the paginated envelope
//           shape, and the SuccessStatusCode override.
// relates:  Uses ApiPilot.AspNetCore.DependencyInjection.ApiPilotServiceCollectionExtensions,
//           ApiPilot.AspNetCore.EndpointMetadata.PaginationEndpointExtensions,
//           PaginationMetadataAttribute,
//           PaginationHttpContextExtensions,
//           ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions,
//           PagedResult,
//           PaginationMetadata,
//           ApiResponseBuilder.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml and
//           lib/net10.0/ApiPilot.Core.xml, and the packaged README. Not the
//           source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using ApiPilot.Core.Pagination;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 08. Pagination end to end over real HTTP. Ten sub-checks.
/// </summary>
public static class PaginationScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "08_Pagination";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckDefaultPageSize(failures);
            await CheckMaxPageSizeRejection(failures);
            await CheckDefaultPageSizeOverride(failures);
            await CheckFluentMaxPageSizeOverride(failures);
            await CheckAttributeDefaultPageSizeOverride(failures);
            await CheckPrecedenceAttributeBeatsFluent(failures);
            await CheckGetPageRequestInHandler(failures);
            await CheckGetEffectivePaginationOptions(failures);
            await CheckEnvelopeShape(failures);
            await CheckSuccessStatusCodeOverride(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "Pagination defaults are on the wire (DefaultPageSize 20, MaxPageSize 100, SuccessStatusCode 200); the three-layer override chain is consumed; the attribute beats the fluent override; GetPageRequest and GetEffectivePaginationOptions return the effective values in the handler; the paginated envelope shape matches the packaged README.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default page size of 20
    // -------------------------------------------------------------------
    private static async Task CheckDefaultPageSize(List<string> failures)
    {
        await using var host = await StartHostAsync(
            configureGlobal: null,
            configureEndpoint: null);

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("DefaultPageSize: expected 200, got " + (int)response.StatusCode + "."); return; }

        var pagination = TryGetPagination(body);
        if (pagination is null) { failures.Add("DefaultPageSize: no pagination object in body. Body=" + body); return; }

        var pageSize = pagination["pageSize"]?.GetValue<int>() ?? -1;
        var page = pagination["page"]?.GetValue<int>() ?? -1;
        if (pageSize != 20) { failures.Add("DefaultPageSize: pageSize was " + pageSize + ", expected 20."); }
        if (page != 1) { failures.Add("DefaultPageSize: page was " + page + ", expected 1."); }

        var data = TryGetDataArray(body);
        if (data is null) { failures.Add("DefaultPageSize: data is not an array."); return; }
        if (data.Count != 20) { failures.Add("DefaultPageSize: data array had " + data.Count + " items, expected 20."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: MaxPageSize rejection with 400 VALIDATION_ERROR
    // -------------------------------------------------------------------
    private static async Task CheckMaxPageSizeRejection(List<string> failures)
    {
        await using var host = await StartHostAsync(
            configureGlobal: null,
            configureEndpoint: null);

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?pageSize=200", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400) { failures.Add("MaxPageSize: expected 400, got " + (int)response.StatusCode + ". Body=" + body); return; }
        AssertErrorCode(failures, "MaxPageSize", body, "VALIDATION_ERROR");
    }

    // -------------------------------------------------------------------
    // Sub-check 3: DefaultPageSize override consumed
    // -------------------------------------------------------------------
    private static async Task CheckDefaultPageSizeOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(
            configureGlobal: o => o.DefaultPageSize = 50,
            configureEndpoint: null);

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("DefaultPageSizeOverride: expected 200, got " + (int)response.StatusCode + "."); return; }

        var pagination = TryGetPagination(body);
        if (pagination is null) { failures.Add("DefaultPageSizeOverride: no pagination object."); return; }
        var pageSize = pagination["pageSize"]?.GetValue<int>() ?? -1;
        if (pageSize != 50) { failures.Add("DefaultPageSizeOverride: pageSize was " + pageSize + ", expected 50."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: fluent MaxPageSize override consumed
    // -------------------------------------------------------------------
    private static async Task CheckFluentMaxPageSizeOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(
            configureGlobal: null,
            configureEndpoint: o => o.MaxPageSize = 50);

        using var client = host.CreateClient();

        // 75 exceeds the endpoint's 50 -> rejected.
        using (var response = await client.GetAsync(new Uri("/?pageSize=75", UriKind.Relative)))
        {
            var body = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 400) { failures.Add("FluentMaxPageSize: 75 expected 400, got " + (int)response.StatusCode + "."); }
            else { AssertErrorCode(failures, "FluentMaxPageSize", body, "VALIDATION_ERROR"); }
        }

        // 40 is within the endpoint's 50 -> accepted.
        using (var response = await client.GetAsync(new Uri("/?pageSize=40", UriKind.Relative)))
        {
            if (response.StatusCode != HttpStatusCode.OK) { failures.Add("FluentMaxPageSize: 40 expected 200, got " + (int)response.StatusCode + "."); }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: [PaginationMetadata] attribute DefaultPageSize override
    // -------------------------------------------------------------------
    private static async Task CheckAttributeDefaultPageSizeOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination();
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) => BuildPagedResult(context))
                   .WithMetadata(new PaginationMetadataAttribute { DefaultPageSize = 5 })
                   .WithApiPilotPagination();
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("AttributeDefaultPageSize: expected 200, got " + (int)response.StatusCode + "."); return; }

        var pagination = TryGetPagination(body);
        if (pagination is null) { failures.Add("AttributeDefaultPageSize: no pagination object."); return; }
        var pageSize = pagination["pageSize"]?.GetValue<int>() ?? -1;
        if (pageSize != 5) { failures.Add("AttributeDefaultPageSize: pageSize was " + pageSize + ", expected 5."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 6: precedence chain (attribute wins over fluent)
    // -------------------------------------------------------------------
    private static async Task CheckPrecedenceAttributeBeatsFluent(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination(o => o.MaxPageSize = 100);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) => BuildPagedResult(context))
                   .WithMetadata(new PaginationMetadataAttribute { MaxPageSize = 30 })
                   .WithApiPilotPagination(o => o.MaxPageSize = 50);
            });

        using var client = host.CreateClient();

        // 40 exceeds the attribute's 30 -> rejected.
        using (var response = await client.GetAsync(new Uri("/?pageSize=40", UriKind.Relative)))
        {
            var body = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 400) { failures.Add("Precedence: 40 expected 400, got " + (int)response.StatusCode + "."); }
            else { AssertErrorCode(failures, "Precedence", body, "VALIDATION_ERROR"); }
        }

        // 25 is within the attribute's 30 -> accepted.
        using (var response = await client.GetAsync(new Uri("/?pageSize=25", UriKind.Relative)))
        {
            if (response.StatusCode != HttpStatusCode.OK) { failures.Add("Precedence: 25 expected 200, got " + (int)response.StatusCode + "."); }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 7: GetPageRequest returns the validated values in the handler
    // -------------------------------------------------------------------
    private static async Task CheckGetPageRequestInHandler(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination();
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) =>
                {
                    var pageRequest = context.GetPageRequest();
                    var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
                    var payload = new
                    {
                        Page = pageRequest?.Page ?? -1,
                        PageSize = pageRequest?.PageSize ?? -1,
                    };
                    return ApiResponseBuilder.Ok(payload, meta, "ok").ToResult();
                })
                .WithApiPilotPagination();
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?page=3&pageSize=15", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("GetPageRequest: expected 200, got " + (int)response.StatusCode + ". Body=" + body); return; }

        if (!body.Contains("\"page\":3", StringComparison.Ordinal))
            failures.Add("GetPageRequest: body does not contain \"page\":3. Body=" + body);
        if (!body.Contains("\"pageSize\":15", StringComparison.Ordinal))
            failures.Add("GetPageRequest: body does not contain \"pageSize\":15. Body=" + body);
    }

    // -------------------------------------------------------------------
    // Sub-check 8: GetEffectivePaginationOptions returns the merged options
    // -------------------------------------------------------------------
    private static async Task CheckGetEffectivePaginationOptions(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination(o => o.MaxPageSize = 100);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) =>
                {
                    var effective = context.GetEffectivePaginationOptions();
                    var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
                    var payload = new
                    {
                        EffectiveMaxPageSize = effective?.MaxPageSize ?? -1,
                    };
                    return ApiResponseBuilder.Ok(payload, meta, "ok").ToResult();
                })
                .WithApiPilotPagination(o => o.MaxPageSize = 50);
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("EffectiveOptions: expected 200, got " + (int)response.StatusCode + "."); return; }

        if (!body.Contains("\"effectiveMaxPageSize\":50", StringComparison.Ordinal))
            failures.Add("EffectiveOptions: body does not contain \"effectiveMaxPageSize\":50. Body=" + body);
    }

    // -------------------------------------------------------------------
    // Sub-check 9: paginated envelope shape
    // -------------------------------------------------------------------
    private static async Task CheckEnvelopeShape(List<string> failures)
    {
        await using var host = await StartHostAsync(null, null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK) { failures.Add("Envelope: expected 200, got " + (int)response.StatusCode + "."); return; }

        var node = JsonNode.Parse(body)?.AsObject();
        if (node is null) { failures.Add("Envelope: body did not parse. Body=" + body); return; }

        if (!node.ContainsKey("success"))    failures.Add("Envelope: missing success.");
        if (!node.ContainsKey("data"))       failures.Add("Envelope: missing data.");
        if (!node.ContainsKey("pagination")) failures.Add("Envelope: missing pagination.");
        if (!node.ContainsKey("meta"))       failures.Add("Envelope: missing meta.");
        if (node.ContainsKey("status"))      failures.Add("Envelope: must not contain status.");

        if (node["success"] is null || node["success"]!.GetValue<bool>() != true)
            failures.Add("Envelope: success was not true.");

        var pagination = node["pagination"]?.AsObject();
        if (pagination is null) { failures.Add("Envelope: pagination was not an object."); return; }
        foreach (var key in new[] { "page", "pageSize", "totalItems", "totalPages", "hasNext", "hasPrevious" })
        {
            if (!pagination.ContainsKey(key)) failures.Add("Envelope: pagination missing " + key + ".");
        }

        var meta = node["meta"]?.AsObject();
        if (meta is null) { failures.Add("Envelope: meta was not an object."); return; }
        if (!meta.ContainsKey("requestId")) failures.Add("Envelope: meta missing requestId.");
    }

    // -------------------------------------------------------------------
    // Sub-check 10: SuccessStatusCode override consumed
    // -------------------------------------------------------------------
    private static async Task CheckSuccessStatusCodeOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(
            configureGlobal: o => o.SuccessStatusCode = 206,
            configureEndpoint: null);

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        if ((int)response.StatusCode != 206)
            failures.Add("SuccessStatusCode: expected 206, got " + (int)response.StatusCode + ".");
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static async Task<InProcessHost> StartHostAsync(
        Action<PaginationOptions>? configureGlobal,
        Action<PaginationOverrides>? configureEndpoint)
    {
        return await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination(configureGlobal);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) => BuildPagedResult(context))
                   .WithApiPilotPagination(configureEndpoint);
            });
    }

    private static IResult BuildPagedResult(HttpContext context)
    {
        var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
        var allItems = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                               21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
                               41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
                               61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
                               81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 };
        var pageRequest = context.GetPageRequest() ?? PageRequest.FromQuery(null, null, new PaginationOptions());
        var skip = (pageRequest.Page - 1) * pageRequest.PageSize;
        var take = pageRequest.PageSize;
        var window = allItems.Skip(skip).Take(take).ToArray();
        var pagination = PaginationMetadata.Create(pageRequest.Page, pageRequest.PageSize, allItems.Length);
        var pagedResult = new PagedResult<int>(window, pagination);
        return pagedResult.ToPagedResult(meta);
    }

    private static JsonObject? TryGetPagination(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            return node?["pagination"]?.AsObject();
        }
        catch
        {
            return null;
        }
    }

    private static JsonArray? TryGetDataArray(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            return node?["data"]?.AsArray();
        }
        catch
        {
            return null;
        }
    }

    private static void AssertErrorCode(List<string> failures, string label, string body, string expectedCode)
    {
        try
        {
            var node = JsonNode.Parse(body);
            var err = node?["error"];
            var code = err?["code"]?.GetValue<string>();
            if (code != expectedCode) { failures.Add(label + ": error.code was '" + (code ?? "<null>") + "', expected '" + expectedCode + "'."); }
        }
        catch (Exception ex)
        {
            failures.Add(label + ": could not parse body (" + ex.GetType().Name + ").");
        }
    }
}