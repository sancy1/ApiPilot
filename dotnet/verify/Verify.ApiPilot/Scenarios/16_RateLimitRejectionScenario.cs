// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/16_RateLimitRejectionScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 16. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.5 package can register the rate-limit
//           rejection handler, integrate it with the ASP.NET Core rate
//           limiter through OnRejected, and observe the documented
//           RATE_LIMITED envelope plus the three Option D overrides and
//           the fail-closed startup validator.
// relates:  Uses ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitExtensions,
//           ApiPilot.AspNetCore.RateLimiting.RateLimitDiagnosticsHook,
//           ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitOptions.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml. Not the source
//           tree.

using System.Net;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.RateLimiting;
using ApiPilot.AspNetCore.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 16. Rate-limit rejection. Five sub-checks: default envelope,
/// StatusCode override, Message override, EmitRetryAfter override, and the
/// fail-closed startup validator for an invalid StatusCode.
/// </summary>
public static class RateLimitRejectionScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "16_RateLimitRejection";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckDefaultRejection(failures, observations);
            await CheckStatusCodeOverride(failures);
            await CheckMessageOverride(failures);
            await CheckEmitRetryAfterFalse(failures);
            await CheckInvalidStatusCodeFailsAtStartup(failures, observations);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "AddApiPilotRateLimitRejection is public and callable; the default rejection produces HTTP 429 with error.code RATE_LIMITED; the StatusCode, Message, and EmitRetryAfter overrides are consumed; an invalid StatusCode fails the host at startup (fail-closed)." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default rejection envelope
    // -------------------------------------------------------------------
    private static async Task CheckDefaultRejection(List<string> failures, List<string> observations)
    {
        await using var host = await StartHostAsync(null);
        using var client = host.CreateClient();

        // Permit limit 1 in a 10-second window. The second request should be rejected.
        HttpResponseMessage? rejected = null;
        string? rejectedBody = null;
        for (var i = 0; i < 4; i++)
        {
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if ((int)response.StatusCode == 429)
            {
                rejected = response;
                rejectedBody = await response.Content.ReadAsStringAsync();
                break;
            }
        }

        if (rejected is null)
        {
            failures.Add("DefaultRejection: no 429 response after 4 requests; the rate limiter did not reject.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rejectedBody))
        {
            failures.Add("DefaultRejection: rejection body was empty.");
            return;
        }

        JsonNode? node;
        try { node = JsonNode.Parse(rejectedBody); }
        catch (Exception ex) { failures.Add("DefaultRejection: body did not parse (" + ex.GetType().Name + "). Body=" + rejectedBody); return; }

        var obj = node?.AsObject();
        if (obj is null) { failures.Add("DefaultRejection: body was not a JSON object."); return; }
        if (obj["success"]?.GetValue<bool>() != false) { failures.Add("DefaultRejection: success was not false."); }
        var err = obj["error"]?.AsObject();
        if (err is null) { failures.Add("DefaultRejection: error object missing."); return; }
        var code = err["code"]?.GetValue<string>();
        if (code != "RATE_LIMITED") { failures.Add("DefaultRejection: error.code was '" + (code ?? "<null>") + "', expected 'RATE_LIMITED'."); }
        var message = err["message"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(message)) { failures.Add("DefaultRejection: error.message was empty."); }
        else { observations.Add("DefaultRejection: default error.message = '" + message + "'"); }
        if (obj["meta"]?["requestId"] is null) { failures.Add("DefaultRejection: meta.requestId missing."); }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: StatusCode override
    // -------------------------------------------------------------------
    private static async Task CheckStatusCodeOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.StatusCode = 503);
        using var client = host.CreateClient();

        var rejected = false;
        for (var i = 0; i < 4; i++)
        {
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if ((int)response.StatusCode == 503) { rejected = true; break; }
        }

        if (!rejected)
        {
            failures.Add("StatusCodeOverride: no 503 response after 4 requests.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: Message override
    // -------------------------------------------------------------------
    private static async Task CheckMessageOverride(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.Message = "Slow down, please.");
        using var client = host.CreateClient();

        string? body = null;
        for (var i = 0; i < 4; i++)
        {
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if ((int)response.StatusCode == 429) { body = await response.Content.ReadAsStringAsync(); break; }
        }

        if (body is null) { failures.Add("MessageOverride: no rejection response observed."); return; }

        var node = JsonNode.Parse(body);
        var message = node?["error"]?["message"]?.GetValue<string>();
        if (message != "Slow down, please.")
        {
            failures.Add("MessageOverride: error.message was '" + (message ?? "<null>") + "', expected 'Slow down, please.'.");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: EmitRetryAfter=false
    // -------------------------------------------------------------------
    private static async Task CheckEmitRetryAfterFalse(List<string> failures)
    {
        await using var host = await StartHostAsync(o => o.EmitRetryAfter = false);
        using var client = host.CreateClient();

        HttpResponseMessage? rejected = null;
        for (var i = 0; i < 4; i++)
        {
            var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if ((int)response.StatusCode == 429) { rejected = response; break; }
            response.Dispose();
        }

        if (rejected is null) { failures.Add("EmitRetryAfterFalse: no rejection response observed."); return; }

        using (rejected)
        {
            if (rejected.Headers.Contains("Retry-After"))
            {
                failures.Add("EmitRetryAfterFalse: Retry-After header was present but EmitRetryAfter=false.");
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: invalid StatusCode fails at startup
    // -------------------------------------------------------------------
    private static async Task CheckInvalidStatusCodeFailsAtStartup(List<string> failures, List<string> observations)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await StartHostAsync(o => o.StatusCode = 200);
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }

        if (!threw)
        {
            failures.Add("InvalidStatusCode: host started with StatusCode=200; the documented fail-closed contract was not honored.");
        }
        else
        {
            observations.Add("InvalidStatusCode threw " + exceptionType);
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static Task<InProcessHost> StartHostAsync(Action<ApiPilotRateLimitOptions>? configureRateLimit)
    {
        return InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions();
                services.AddApiPilotRateLimitRejection(configureRateLimit);
                services.AddRateLimiter(o =>
                {
                    o.OnRejected = RateLimitDiagnosticsHook.HandleAsync;
                    o.AddFixedWindowLimiter("api", w =>
                    {
                        w.PermitLimit = 1;
                        w.Window = TimeSpan.FromSeconds(10);
                        w.QueueLimit = 0;
                    });
                });
            },
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.UseRateLimiter();
                app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok()).RequireRateLimiting("api");
            });
    }
}
