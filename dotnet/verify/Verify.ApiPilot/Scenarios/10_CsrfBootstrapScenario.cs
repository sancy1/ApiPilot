// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/10_CsrfBootstrapScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 10. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.3 package can register the CSRF stack,
//           expose the bootstrap endpoint, and observe the documented bare
//           { "token": "..." } shape (the one documented exception to the
//           standard success envelope), the documented Cache-Control, the
//           documented Enabled override, and the documented BootstrapPath
//           override.
// relates:  Uses ApiPilot.Security.Csrf.CsrfServiceExtensions,
//           ApiPilot.Security.Csrf.CsrfBootstrapExtensions,
//           ApiPilot.Security.Csrf.CsrfOptions,
//           ApiPilot.Security.Csrf.CsrfBootstrapOptions.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml and the
//           packaged README. Not the source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 10. CSRF bootstrap endpoint end to end over real HTTP. Three
/// sub-checks: default shape, disabled endpoint, and BootstrapPath override.
/// </summary>
public static class CsrfBootstrapScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "10_CsrfBootstrap";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckDefaultBootstrapShape(failures);
            await CheckDisabledBootstrap(failures);
            await CheckBootstrapPathOverride(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "AddApiPilotCsrf and MapApiPilotCsrf are public and callable; the bootstrap endpoint returns the documented bare { \"token\": \"...\" } shape with Cache-Control no-store; CsrfBootstrapOptions.Enabled=false suppresses the endpoint; the CsrfOptions.BootstrapPath override is consumed.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default bootstrap endpoint shape
    // -------------------------------------------------------------------
    private static async Task CheckDefaultBootstrapShape(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "test-binding";
                });
            },
            configurePipeline: app =>
            {
                app.MapApiPilotCsrf();
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/csrf", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("Default: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }

        JsonNode? node;
        try { node = JsonNode.Parse(body); }
        catch (Exception ex) { failures.Add("Default: body did not parse (" + ex.GetType().Name + "). Body=" + body); return; }
        if (node is null) { failures.Add("Default: JsonNode.Parse returned null."); return; }

        var obj = node.AsObject();
        if (obj.Count != 1)
        {
            failures.Add("Default: bootstrap body has " + obj.Count + " properties; expected exactly 1. Body=" + body);
        }
        if (!obj.ContainsKey("token"))
        {
            failures.Add("Default: bootstrap body missing 'token' key. Body=" + body);
            return;
        }
        if (obj.ContainsKey("success"))
        {
            failures.Add("Default: bootstrap body contains 'success'; the shape is not the standard envelope. Body=" + body);
        }
        var token = obj["token"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(token))
        {
            failures.Add("Default: bootstrap token was empty.");
        }

        // Cache-Control header.
        var cacheControl = response.Headers.CacheControl?.ToString() ?? string.Empty;
        if (!cacheControl.Contains("no-store", StringComparison.Ordinal))
        {
            failures.Add("Default: Cache-Control was '" + cacheControl + "'; expected to contain 'no-store'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: disabled bootstrap endpoint
    // -------------------------------------------------------------------
    private static async Task CheckDisabledBootstrap(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(
                    configure: o => { o.PreAuthBindingSource = _ => "test-binding"; },
                    configureToken: null,
                    configureBootstrap: o => { o.Enabled = false; });
            },
            configurePipeline: app =>
            {
                app.MapApiPilotCsrf();
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/csrf", UriKind.Relative));
        if ((int)response.StatusCode != 404)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("Disabled: expected 404, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: BootstrapPath override
    // -------------------------------------------------------------------
    private static async Task CheckBootstrapPathOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "test-binding";
                    o.BootstrapPath = "/api/token";
                });
            },
            configurePipeline: app =>
            {
                app.MapApiPilotCsrf();
            });

        using var client = host.CreateClient();

        // The new path serves the bootstrap shape.
        using (var response = await client.GetAsync(new Uri("/api/token", UriKind.Relative)))
        {
            var body = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("Override: /api/token expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            }
            else
            {
                var obj = JsonNode.Parse(body)?.AsObject();
                if (obj is null || !obj.ContainsKey("token") || obj.Count != 1)
                {
                    failures.Add("Override: /api/token body shape was not the bare token. Body=" + body);
                }
            }
        }

        // The default path is no longer routed.
        using (var response = await client.GetAsync(new Uri("/api/csrf", UriKind.Relative)))
        {
            if ((int)response.StatusCode != 404)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add("Override: /api/csrf expected 404 after override, got " + (int)response.StatusCode + ". Body=" + body);
            }
        }
    }
}