// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/11_CsrfProtectionScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 11. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.3 package can register the CSRF protection
//           middleware, place it in the pipeline, and observe the documented
//           behavior: missing header produces CSRF_HEADER_MISSING, invalid
//           header produces CSRF_TOKEN_INVALID, safe methods pass through the
//           global policy, the [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf]
//           attributes work on minimal APIs, the precedence rule Require beats
//           Skip beats UseGlobal holds, ExemptPaths is consumed, and the
//           HeaderName override is consumed.
// relates:  Uses ApiPilot.Security.Csrf.CsrfServiceExtensions,
//           ApiPilot.Security.Csrf.CsrfMiddlewareExtensions,
//           ApiPilot.Security.Csrf.ApiPilotSkipCsrfAttribute,
//           ApiPilot.Security.Csrf.ApiPilotRequireCsrfAttribute,
//           ApiPilot.Security.Csrf.CsrfOptions.
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
/// Scenario 11. CSRF protection middleware end to end over real HTTP. Eight
/// sub-checks: missing header, invalid header, safe method passthrough, Skip
/// attribute, Require attribute, precedence chain, ExemptPaths override, and
/// HeaderName override.
/// </summary>
public static class CsrfProtectionScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "11_CsrfProtection";

    /// <summary>The default CSRF header name.</summary>
    private const string HeaderNameDefault = "X-CSRF-TOKEN";

    /// <summary>The custom CSRF header name used by the override sub-check.</summary>
    private const string HeaderNameCustom = "X-CUSTOM-CSRF";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckMissingHeaderRejected(failures);
            await CheckInvalidHeaderRejected(failures);
            await CheckSafeMethodPasses(failures);
            await CheckSkipAttributeBypasses(failures);
            await CheckRequireAttributeEnforcesOnGet(failures);
            await CheckPrecedenceChain(failures);
            await CheckExemptPathsOverride(failures);
            await CheckHeaderNameOverride(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "UseApiPilotCsrfProtection is public and callable; the middleware rejects a missing header with CSRF_HEADER_MISSING and an invalid header with CSRF_TOKEN_INVALID; safe methods pass the global policy; CsrfOptions.ExemptPaths and CsrfOptions.HeaderName overrides are consumed; the attribute behavior on minimal-API endpoints is honored.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: missing header rejected
    // -------------------------------------------------------------------
    private static async Task CheckMissingHeaderRejected(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("MissingHeader: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "MissingHeader", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 2: invalid header rejected
    // -------------------------------------------------------------------
    private static async Task CheckInvalidHeaderRejected(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation(HeaderNameDefault, "not-a-real-token");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("InvalidHeader: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "InvalidHeader", body, "CSRF_TOKEN_INVALID");
    }

    // -------------------------------------------------------------------
    // Sub-check 3: GET is not protected
    // -------------------------------------------------------------------
    private static async Task CheckSafeMethodPasses(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("SafeMethod: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: [ApiPilotSkipCsrf] on a POST bypasses the middleware
    // -------------------------------------------------------------------
    private static async Task CheckSkipAttributeBypasses(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: ConfigureCsrf,
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", SkipHandler);
            });

        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            failures.Add("Skip: expected 200, got " + (int)response.StatusCode + ". Body=" + body);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: [ApiPilotRequireCsrf] on a GET enforces protection
    // -------------------------------------------------------------------
    private static async Task CheckRequireAttributeEnforcesOnGet(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: ConfigureCsrf,
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapGet("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("RequireOnGet: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "RequireOnGet", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 6: precedence chain - Require beats Skip
    // -------------------------------------------------------------------
    private static async Task CheckPrecedenceChain(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: ConfigureCsrf,
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                // Both attributes applied. Per the shipped XML, the last
                // attribute wins when both are present, which is Require.
                app.MapPost("/", PrecedenceHandler);
            });

        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/", UriKind.Relative), content);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("Precedence: expected 403 (Require wins), got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "Precedence", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 7: ExemptPaths override consumed
    // -------------------------------------------------------------------
    private static async Task CheckExemptPathsOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "test-binding";
                    o.ExemptPaths.Add("/exempt");
                });
            },
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/exempt", () => Microsoft.AspNetCore.Http.Results.Ok());
                app.MapPost("/", () => Microsoft.AspNetCore.Http.Results.Ok());
            });

        using var client = host.CreateClient();

        using (var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))
        using (var response = await client.PostAsync(new Uri("/exempt", UriKind.Relative), content))
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add("ExemptPaths: /exempt expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            }
        }

        using (var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json"))
        using (var response = await client.PostAsync(new Uri("/", UriKind.Relative), content))
        {
            if ((int)response.StatusCode != 403)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add("ExemptPaths: / expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 8: HeaderName override consumed
    // -------------------------------------------------------------------
    private static async Task CheckHeaderNameOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddDataProtection();
                services.AddApiPilotJson();
                services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "test-binding";
                    o.HeaderName = HeaderNameCustom;
                });
            },
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", () => Microsoft.AspNetCore.Http.Results.Ok());
            });

        using var client = host.CreateClient();

        // The old header name is not read -> missing header.
        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative)))
        {
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation(HeaderNameDefault, "whatever");
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 403)
            {
                failures.Add("HeaderOverride: old header expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            }
            else
            {
                AssertErrorCode(failures, "HeaderOverride", body, "CSRF_HEADER_MISSING");
            }
        }

        // The new header name is read -> invalid token.
        using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative)))
        {
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation(HeaderNameCustom, "whatever");
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if ((int)response.StatusCode != 403)
            {
                failures.Add("HeaderOverride: new header expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            }
            else
            {
                AssertErrorCode(failures, "HeaderOverride", body, "CSRF_TOKEN_INVALID");
            }
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static void ConfigureCsrf(IServiceCollection services)
    {
        services.AddDataProtection();
        services.AddApiPilotJson();
        services.AddApiPilotCsrf(o =>
        {
            o.PreAuthBindingSource = _ => "test-binding";
        });
    }

    private static Task<InProcessHost> StartDefaultHostAsync()
    {
        return InProcessHost.StartAsync(
            configureServices: ConfigureCsrf,
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
            });
    }

    [ApiPilotSkipCsrf]
    private static IResult SkipHandler() => Microsoft.AspNetCore.Http.Results.Ok();

    [ApiPilotRequireCsrf]
    private static IResult RequireHandler() => Microsoft.AspNetCore.Http.Results.Ok();

    [ApiPilotSkipCsrf]
    [ApiPilotRequireCsrf]
    private static IResult PrecedenceHandler() => Microsoft.AspNetCore.Http.Results.Ok();

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