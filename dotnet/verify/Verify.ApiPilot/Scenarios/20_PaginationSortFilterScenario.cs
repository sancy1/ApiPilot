// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/20_PaginationSortFilterScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 20. Proves that a real consumer of the published
//           ApiPilot.Core and ApiPilot.AspNetCore 1.0.5 packages can observe
//           the documented sort and filter behavior: sort parsing for asc and
//           desc, filter parsing in lenient mode, StrictQueryValidation=true
//           rejecting an unknown parameter, and the default lenient mode
//           passing it through.
// relates:  Uses ApiPilot.AspNetCore.DependencyInjection.AddApiPilotPagination,
//           ApiPilot.AspNetCore.EndpointMetadata.WithApiPilotPagination,
//           ApiPilot.AspNetCore.EndpointMetadata.PaginationHttpContextExtensions,
//           ApiPilot.Core.Pagination.SortRequest,
//           ApiPilot.Core.Pagination.FilterRequest.
// authority: the shipped lib/net10.0/ApiPilot.Core.xml and
//           lib/net10.0/ApiPilot.AspNetCore.xml. Not the source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 20. Pagination sort and filter. Five sub-checks: sort asc, sort
/// desc, filter in lenient mode, StrictQueryValidation=true, and the default
/// lenient mode.
/// </summary>
public static class PaginationSortFilterScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "20_PaginationSortFilter";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckSortAscending(failures);
            await CheckSortDescending(failures);
            await CheckFilterLenient(failures, observations);
            await CheckStrictValidationRejectsUnknown(failures);
            await CheckDefaultLenientPassesUnknown(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "Sort parsing produces the documented SortRequest for asc and desc; filter parsing in lenient mode produces the documented FilterRequest with the passed-through pair; StrictQueryValidation=true rejects an unknown query parameter with VALIDATION_ERROR; the default lenient mode passes the unknown parameter through." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: sort ascending
    // -------------------------------------------------------------------
    private static async Task CheckSortAscending(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?sort=name&direction=asc", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("SortAsc: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        var data = JsonNode.Parse(body)?["data"]?.AsObject();
        if (data is null) { failures.Add("SortAsc: data missing."); return; }
        var field = data["field"]?.GetValue<string>();
        var direction = data["direction"]?.GetValue<string>();
        if (field != "name") { failures.Add("SortAsc: field was '" + (field ?? "<null>") + "', expected 'name'."); }
        if (direction is null || !direction.Contains("Ascending", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("SortAsc: direction was '" + (direction ?? "<null>") + "', expected Ascending.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: sort descending
    // -------------------------------------------------------------------
    private static async Task CheckSortDescending(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?sort=name&direction=desc", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("SortDesc: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        var data = JsonNode.Parse(body)?["data"]?.AsObject();
        if (data is null) { failures.Add("SortDesc: data missing."); return; }
        var direction = data["direction"]?.GetValue<string>();
        if (direction is null || !direction.Contains("Descending", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("SortDesc: direction was '" + (direction ?? "<null>") + "', expected Descending.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: filter in lenient mode
    // -------------------------------------------------------------------
    private static async Task CheckFilterLenient(List<string> failures, List<string> observations)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?status=active", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("Filter: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        var data = JsonNode.Parse(body)?["data"]?.AsObject();
        if (data is null) { failures.Add("Filter: data missing."); return; }
        var pairs = data["pairs"]?.AsObject();
        if (pairs is null) { failures.Add("Filter: data.pairs missing."); return; }
        observations.Add("pairs=" + pairs.ToJsonString());
        if (!pairs.ContainsKey("status"))
        {
            failures.Add("Filter: data.pairs did not contain 'status'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: StrictQueryValidation=true
    // -------------------------------------------------------------------
    private static async Task CheckStrictValidationRejectsUnknown(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.StrictQueryValidation = true);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?unknown=value", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400)
        {
            failures.Add("StrictRejects: expected 400, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        var code = JsonNode.Parse(body)?["error"]?["code"]?.GetValue<string>();
        if (code != "VALIDATION_ERROR")
        {
            failures.Add("StrictRejects: error.code was '" + (code ?? "<null>") + "', expected 'VALIDATION_ERROR'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: default lenient passes unknown
    // -------------------------------------------------------------------
    private static async Task CheckDefaultLenientPassesUnknown(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/?unknown=value", UriKind.Relative));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("LenientPasses: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static Task<InProcessHost> StartHostAsync(Action<PaginationOptions>? configurePagination)
    {
        return InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotPagination(configurePagination);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) =>
                {
                    var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
                    var sort = context.GetSortRequest();
                    var filter = context.GetFilterRequest();
                    var payload = new
                    {
                        Field = sort?.Field,
                        Direction = sort?.Direction.ToString(),
                        Pairs = filter?.Pairs,
                    };
                    return ApiResponseBuilder.Ok(payload, meta, "ok").ToResult();
                })
                .WithApiPilotPagination();
            });
    }
}
