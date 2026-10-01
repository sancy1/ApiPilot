// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/12_CsrfAttributesOnControllersScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 12. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.5 package can apply [ApiPilotSkipCsrf] and
//           [ApiPilotRequireCsrf] on controller actions and observe the
//           documented behavior. The library author requested this coverage
//           in the 1.0.5 fix message; scenario 11 covered the minimal-API
//           path only.
// relates:  Uses ApiPilot.Security.Csrf.CsrfServiceExtensions,
//           ApiPilot.Security.Csrf.CsrfMiddlewareExtensions,
//           ApiPilot.Security.Csrf.ApiPilotSkipCsrfAttribute,
//           ApiPilot.Security.Csrf.ApiPilotRequireCsrfAttribute.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml, the packaged
//           README, and the 1.0.5 fix message. Not the source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 12. The CSRF attributes on controller actions. Three sub-checks:
/// Skip on a POST, Require on a GET, and the precedence chain when both are
/// attached.
/// </summary>
public static class CsrfAttributesOnControllersScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "12_CsrfAttributesOnControllers";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckSkipAttributeOnController(failures);
            await CheckRequireAttributeOnController(failures);
            await CheckPrecedenceOnController(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "The [ApiPilotSkipCsrf] attribute bypasses the middleware on a controller action; the [ApiPilotRequireCsrf] attribute enforces protection on a GET controller action; the precedence chain holds when both attributes are on the same action (Require wins).");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: [ApiPilotSkipCsrf] on a POST controller action
    // -------------------------------------------------------------------
    private static async Task CheckSkipAttributeOnController(List<string> failures)
    {
        await using var host = await StartHostAsync();
        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/api/skip", UriKind.Relative), content);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("ControllerSkip: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: [ApiPilotRequireCsrf] on a GET controller action
    // -------------------------------------------------------------------
    private static async Task CheckRequireAttributeOnController(List<string> failures)
    {
        await using var host = await StartHostAsync();
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/api/require", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("ControllerRequire: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "ControllerRequire", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 3: precedence on a controller action (both attributes)
    // -------------------------------------------------------------------
    private static async Task CheckPrecedenceOnController(List<string> failures)
    {
        await using var host = await StartHostAsync();
        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/api/both", UriKind.Relative), content);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("ControllerPrecedence: expected 403 (Require wins), got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "ControllerPrecedence", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static Task<InProcessHost> StartHostAsync()
    {
        return InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "test-binding";
                });
                services.AddControllers();
            },
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapControllers();
            });
    }

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

/// <summary>
/// Controller for the [ApiPilotSkipCsrf] path. The attribute bypasses the
/// CSRF middleware regardless of method.
/// </summary>
[Route("api/skip")]
public sealed class SkipController : ControllerBase
{
    /// <summary>Returns 200.</summary>
    [ApiPilotSkipCsrf]
    [HttpPost]
    public IActionResult Post() => Ok();
}

/// <summary>
/// Controller for the [ApiPilotRequireCsrf] path. The attribute enforces
/// CSRF protection on a GET endpoint.
/// </summary>
[Route("api/require")]
public sealed class RequireController : ControllerBase
{
    /// <summary>Returns 200 if CSRF validation passes.</summary>
    [ApiPilotRequireCsrf]
    [HttpGet]
    public IActionResult Get() => Ok();
}

/// <summary>
/// Controller for the precedence test. Both attributes are applied; Require
/// wins per the shipped XML.
/// </summary>
[Route("api/both")]
public sealed class BothController : ControllerBase
{
    /// <summary>Returns 200 only if the request bypasses CSRF; the Require
    /// attribute forces CSRF validation, so the request is rejected.</summary>
    [ApiPilotSkipCsrf]
    [ApiPilotRequireCsrf]
    [HttpPost]
    public IActionResult Post() => Ok();
}
