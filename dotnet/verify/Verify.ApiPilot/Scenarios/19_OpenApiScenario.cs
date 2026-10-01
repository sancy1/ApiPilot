// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/19_OpenApiScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 19. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.5 package can register the repository-owned
//           OpenAPI emitter and observe the documented behavior: the document
//           is served at the default path, the DocumentPath, DocumentTitle,
//           DocumentVersion, and IncludeErrorSchemas overrides are consumed,
//           the three error schemas are present by default, and an invalid
//           DocumentPath fails the host at startup.
// relates:  Uses ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiExtensions,
//           ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml. Not the source
//           tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.OpenApi;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 19. The repository-owned OpenAPI emitter. Six sub-checks:
/// default document, DocumentPath override, DocumentTitle and DocumentVersion
/// overrides, default error schemas, IncludeErrorSchemas=false, and an invalid
/// DocumentPath failing at startup.
/// </summary>
public static class OpenApiScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "19_OpenApi";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckDefaultDocument(failures, observations);
            await CheckDocumentPathOverride(failures);
            await CheckTitleAndVersionOverrides(failures);
            await CheckDefaultErrorSchemas(failures);
            await CheckIncludeErrorSchemasFalse(failures);
            await CheckInvalidDocumentPathFailsAtStartup(failures, observations);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "AddApiPilotOpenApi and MapApiPilotOpenApi are public and callable; the document is served at the default path with the documented defaults; the DocumentPath, DocumentTitle, and DocumentVersion overrides are consumed; the three error schemas are present by default; IncludeErrorSchemas=false omits them; an invalid DocumentPath fails the host at startup." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default document at the default path
    // -------------------------------------------------------------------
    private static async Task CheckDefaultDocument(List<string> failures, List<string> observations)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("DefaultDocument: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        JsonNode? node;
        try { node = JsonNode.Parse(body); }
        catch (Exception ex) { failures.Add("DefaultDocument: body did not parse (" + ex.GetType().Name + ")."); return; }
        var obj = node?.AsObject();
        if (obj is null) { failures.Add("DefaultDocument: body was not an object."); return; }
        var openApiVersion = obj["openapi"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(openApiVersion) || !openApiVersion.StartsWith("3.0", StringComparison.Ordinal))
        {
            failures.Add("DefaultDocument: openapi field was '" + (openApiVersion ?? "<null>") + "', expected a 3.0.x value.");
        }
        else
        {
            observations.Add("openapi=" + openApiVersion);
        }
        var info = obj["info"]?.AsObject();
        if (info is null) { failures.Add("DefaultDocument: info object missing."); return; }
        var title = info["title"]?.GetValue<string>();
        var version = info["version"]?.GetValue<string>();
        if (title != "ApiPilot API")
        {
            failures.Add("DefaultDocument: info.title was '" + (title ?? "<null>") + "', expected 'ApiPilot API'.");
        }
        if (version != "1.0.0")
        {
            failures.Add("DefaultDocument: info.version was '" + (version ?? "<null>") + "', expected '1.0.0'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: DocumentPath override
    // -------------------------------------------------------------------
    private static async Task CheckDocumentPathOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.DocumentPath = "/api/docs.json");
        using var client = host.CreateClient();

        using (var response = await client.GetAsync(new Uri("/api/docs.json", UriKind.Relative)))
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add("DocumentPathOverride: /api/docs.json expected 200, got " + (int)response.StatusCode + ".");
            }
        }

        using (var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative)))
        {
            if ((int)response.StatusCode != 404)
            {
                failures.Add("DocumentPathOverride: /openapi/v1.json expected 404, got " + (int)response.StatusCode + ".");
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: DocumentTitle and DocumentVersion overrides
    // -------------------------------------------------------------------
    private static async Task CheckTitleAndVersionOverrides(List<string> failures)
    {
        await using var host = await StartHostAsync(o =>
        {
            o.DocumentTitle = "Orders API";
            o.DocumentVersion = "2.5.0";
        });
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("TitleVersionOverride: expected 200, got " + (int)response.StatusCode + ".");
            return;
        }
        var info = JsonNode.Parse(body)?["info"]?.AsObject();
        if (info is null) { failures.Add("TitleVersionOverride: info missing."); return; }
        var title = info["title"]?.GetValue<string>();
        var version = info["version"]?.GetValue<string>();
        if (title != "Orders API")
        {
            failures.Add("TitleVersionOverride: info.title was '" + (title ?? "<null>") + "', expected 'Orders API'.");
        }
        if (version != "2.5.0")
        {
            failures.Add("TitleVersionOverride: info.version was '" + (version ?? "<null>") + "', expected '2.5.0'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: default error schemas
    // -------------------------------------------------------------------
    private static async Task CheckDefaultErrorSchemas(List<string> failures)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("ErrorSchemas: expected 200, got " + (int)response.StatusCode + ".");
            return;
        }
        var schemas = JsonNode.Parse(body)?["components"]?["schemas"]?.AsObject();
        if (schemas is null) { failures.Add("ErrorSchemas: components.schemas missing."); return; }
        foreach (var name in new[] { "ApiPilotErrorResponse", "ApiPilotError", "ApiPilotErrorFields" })
        {
            if (!schemas.ContainsKey(name))
            {
                failures.Add("ErrorSchemas: " + name + " missing.");
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: IncludeErrorSchemas=false
    // -------------------------------------------------------------------
    private static async Task CheckIncludeErrorSchemasFalse(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.IncludeErrorSchemas = false);
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("IncludeErrorSchemasFalse: expected 200, got " + (int)response.StatusCode + ".");
            return;
        }
        var schemas = JsonNode.Parse(body)?["components"]?["schemas"]?.AsObject();
        if (schemas is null) { return; }
        if (schemas.ContainsKey("ApiPilotErrorResponse"))
        {
            failures.Add("IncludeErrorSchemasFalse: ApiPilotErrorResponse present despite IncludeErrorSchemas=false.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 6: invalid DocumentPath fails at startup
    // -------------------------------------------------------------------
    private static async Task CheckInvalidDocumentPathFailsAtStartup(List<string> failures, List<string> observations)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await StartHostAsync(o => o.DocumentPath = "openapi.json");
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }
        if (!threw)
        {
            failures.Add("InvalidPath: host started with DocumentPath=openapi.json; the documented fail-closed contract was not honored.");
        }
        else
        {
            observations.Add("InvalidPath threw " + exceptionType);
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static Task<InProcessHost> StartHostAsync(Action<ApiPilotOpenApiOptions>? configureOpenApi)
    {
        return InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddEndpointsApiExplorer();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions();
                services.AddApiPilotOpenApi(configureOpenApi);
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapApiPilotOpenApi();
            });
    }
}
