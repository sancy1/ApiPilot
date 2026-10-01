// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/14_FetchMetadataAttributesScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 14. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.5 package can register the Fetch Metadata
//           middleware and observe the documented behavior of the Off and
//           Strict profiles, plus the AllowMissingHeaders default and
//           override. The library author requested this coverage in the 1.0.5
//           fix message.
// relates:  Uses ApiPilot.Security.Origin.FetchMetadataMiddlewareExtensions,
//           ApiPilot.Security.Origin.FetchMetadataOptions,
//           ApiPilot.Security.Origin.FetchMetadataProfile,
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
/// Scenario 14. The Fetch Metadata middleware on minimal-API endpoints. Four
/// sub-checks: Off is inert, Strict rejects a cross-site Sec-Fetch-Site,
/// Strict with AllowMissingHeaders=true permits the missing header and lets
/// CSRF reject, and Strict with AllowMissingHeaders=false rejects the missing
/// header.
/// </summary>
public static class FetchMetadataAttributesScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "14_FetchMetadataAttributes";

    /// <summary>The Sec-Fetch-Site header name.</summary>
    private const string SecFetchSite = "Sec-Fetch-Site";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckOffProfileIsInert(failures);
            await CheckStrictRejectsCrossSite(failures);
            await CheckStrictMissingHeaderPermitted(failures);
            await CheckStrictMissingHeaderRejected(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "The Fetch Metadata Off profile (the default) is inert and lets the CSRF middleware produce the rejection; the Strict profile rejects a cross-site Sec-Fetch-Site with FORBIDDEN; Strict with AllowMissingHeaders=true permits a missing header and lets CSRF reject; Strict with AllowMissingHeaders=false rejects the missing header with FORBIDDEN.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: Off profile is inert
    // -------------------------------------------------------------------
    private static async Task CheckOffProfileIsInert(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, null),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotFetchMetadata();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation(SecFetchSite, "cross-site");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("OffProfile: expected 403 from CSRF, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "OffProfile", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 2: Strict rejects cross-site
    // -------------------------------------------------------------------
    private static async Task CheckStrictRejectsCrossSite(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, o => o.Profile = FetchMetadataProfile.Strict),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotFetchMetadata();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation(SecFetchSite, "cross-site");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("StrictCrossSite: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "StrictCrossSite", body, "FORBIDDEN");
    }

    // -------------------------------------------------------------------
    // Sub-check 3: Strict + missing header permitted by default
    // -------------------------------------------------------------------
    private static async Task CheckStrictMissingHeaderPermitted(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, o => o.Profile = FetchMetadataProfile.Strict),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotFetchMetadata();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        // No Sec-Fetch-Site header.
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("StrictMissingPermitted: expected 403 from CSRF, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "StrictMissingPermitted", body, "CSRF_HEADER_MISSING");
    }

    // -------------------------------------------------------------------
    // Sub-check 4: Strict + missing header rejected when disallowed
    // -------------------------------------------------------------------
    private static async Task CheckStrictMissingHeaderRejected(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => ConfigureAll(services, o =>
            {
                o.Profile = FetchMetadataProfile.Strict;
                o.AllowMissingHeaders = false;
            }),
            configurePipeline: app =>
            {
                app.UseRouting();
                app.UseApiPilotFetchMetadata();
                app.UseApiPilotCsrfProtection();
                app.MapPost("/", RequireHandler);
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        // No Sec-Fetch-Site header.
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 403)
        {
            failures.Add("StrictMissingDisallowed: expected 403, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertErrorCode(failures, "StrictMissingDisallowed", body, "FORBIDDEN");
    }

    // -------------------------------------------------------------------
    // Helpers and handlers
    // -------------------------------------------------------------------
    private static void ConfigureAll(IServiceCollection services, Action<FetchMetadataOptions>? configureFetch)
    {
        services.AddDataProtection();
        services.AddApiPilotJson();
        services.AddApiPilotCsrf(o =>
        {
            o.PreAuthBindingSource = _ => "test-binding";
        });
        services.AddApiPilotFetchMetadata(configureFetch);
    }

    [ApiPilotRequireCsrf]
    private static IResult RequireHandler() => Microsoft.AspNetCore.Http.Results.Ok();

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
