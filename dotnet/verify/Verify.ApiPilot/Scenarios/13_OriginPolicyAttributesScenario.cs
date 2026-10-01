// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/13_OriginPolicyAttributesScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 13. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.5 package can register the Origin policy
//           middleware, place it in the pipeline, and observe the documented
//           behavior: the [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf]
//           attributes drive per-endpoint Origin enforcement, the default
//           AllowMissingOrigin behavior, and the AllowMissingOrigin=false
//           override. The library author requested this coverage in the 1.0.5
//           fix message.
// relates:  Uses ApiPilot.Security.Origin.OriginMiddlewareExtensions,
//           ApiPilot.Security.Origin.OriginPolicyOptions,
//           ApiPilot.Security.Csrf.ApiPilotSkipCsrfAttribute,
//           ApiPilot.Security.Csrf.ApiPilotRequireCsrfAttribute.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml and the 1.0.5
//           fix message. Not the source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Origin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 13. The Origin policy middleware on minimal-API endpoints. Four
/// sub-checks: Skip bypasses Origin, Require enforces Origin, default
/// AllowMissingOrigin permits a missing header, and AllowMissingOrigin=false
/// rejects a missing header.
/// </summary>
public static class OriginPolicyAttributesScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "13_OriginPolicyAttributes";

    /// <summary>An origin that is not the request own origin and is not allowed.</summary>
    private const string HostileOrigin = "https://evil.example.com";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckSkipBypassesOrigin(failures);
            await CheckRequireEnforcesOrigin(failures);
            await CheckMissingOriginPermittedByDefault(failures);
            await CheckMissingOriginRejectedWhenDisallowed(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "The Origin policy honours [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf] on minimal-API endpoints; the default AllowMissingOrigin=true permits a request with no Origin header and lets the CSRF middleware produce the rejection; the AllowMissingOrigin=false override rejects the missing header with CSRF_ORIGIN_REJECTED.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: Skip bypasses Origin enforcement
    // -------------------------------------------------------------------
    private static async Task CheckSkipBypassesOrigin(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, null),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", SkipHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Origin", HostileOrigin);
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("Skip: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: Require enforces Origin
    // -------------------------------------------------------------------
    private static async Task CheckRequireEnforcesOrigin(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, null),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.UseApiPilotCsrfProtection();
                app.MapGet("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Origin", HostileOrigin);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("Require: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "Require", body, "CSRF_ORIGIN_REJECTED");
    }

    // -------------------------------------------------------------------
    // Sub-check 3: default AllowMissingOrigin=true
    // -------------------------------------------------------------------
    private static async Task CheckMissingOriginPermittedByDefault(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, null),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandlerPost);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("MissingOriginPermitted: expected 403 from CSRF, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "MissingOriginPermitted", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 4: AllowMissingOrigin=false
    // -------------------------------------------------------------------
    private static async Task CheckMissingOriginRejectedWhenDisallowed(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, o => o.AllowMissingOrigin = false),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandlerPost);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("MissingOriginDisallowed: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "MissingOriginDisallowed", body, "CSRF_ORIGIN_REJECTED");
    }

    // -------------------------------------------------------------------
    // Helpers and handlers
    // -------------------------------------------------------------------
    private static void ConfigureAll(IServiceCollection services, Action<OriginPolicyOptions>? configureOrigin)
    {
        services.AddDataProtection();
        services.AddApiPilotJson();
        services.AddApiPilotCsrf(o =>
        {
            o.PreAuthBindingSource = _ => "test-binding";
        });
        services.AddApiPilotOriginPolicy(configureOrigin);
    }

    [ApiPilotSkipCsrf]
    private static IResult SkipHandler() => Microsoft.AspNetCore.Http.Results.Ok();

    [ApiPilotRequireCsrf]
    private static IResult RequireHandler() => Microsoft.AspNetCore.Http.Results.Ok();

    [ApiPilotRequireCsrf]
    private static IResult RequireHandlerPost() => Microsoft.AspNetCore.Http.Results.Ok();

    private static void AssertErrorCode(List<string> failures, string label, string body, string expectedCode)
    {
        try
        {
            var node = JsonNode.Parse(body);
            var code = node?["error"]?["code"]?.GetValue<string>();
            if (code != expectedCode)
            {
                failures.Add(label + ": error.code was '" + (code ?? "<null>") + "', expected '" + expectedCode + "'. Body=" + body);
            }
            if (node?["success"]?.GetValue<bool>() != false)
            {
                failures.Add(label + ": success was not false.");
            }
        }
        catch (Exception ex)
        {
            failures.Add(label + ": could not parse body (" + ex.GetType().Name + "). Body=" + body);
        }
    }
}
