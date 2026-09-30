// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/SiblingSecurityAttributeIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v1.0.3
// purpose: End-to-end HTTP tests for Origin and Fetch Metadata attribute paths (F-65)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Security.Csrf, ApiPilot.Security.Origin,
//                Microsoft.AspNetCore.Builder, Microsoft.AspNetCore.Http,
//                Microsoft.Extensions.DependencyInjection, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginMiddleware.cs, FetchMetadataMiddleware.cs
// -----------------------------------------------------------------------------
//
// F-65 sibling security end-to-end reproduction.
//
// Pipeline order used by every test in this file:
//     UseApiPilotCorrelation
//     UseRouting
//     UseApiPilotOriginPolicy
//     UseApiPilotFetchMetadata
//     UseApiPilotCsrfProtection
//     MapApiPilotCsrf
//     application endpoints
// Routing runs before the metadata-consuming middleware so the endpoint
// and its metadata are resolved before the middleware inspects them.
//
// ORIGIN cases require a hostile Origin header and the origin policy
// configured so the policy is enabled. A CSRF policy alone does not prove
// the Origin middleware.
//
// FETCH METADATA cases require the Strict profile and the relevant
// Sec-Fetch-Site header. A CSRF policy alone does not prove the Fetch
// Metadata middleware.
//
// SCOPE CLAIM. The CSRF integration results (CsrfAttributeEndToEndTests)
// broaden F-65 from minimal-API only to: the documented attribute-to-policy
// paths are not operational in the tested CSRF integration setup.
// This file establishes whether the same is true for Origin and Fetch
// Metadata in the same host shape. It does NOT claim that all three
// framework paths share exactly the same provider mechanism; that is a
// diagnostic question, recorded separately.
//
// The direct CsrfEndpointMetadata baseline is not repeated here.
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// End-to-end HTTP tests for the Origin and Fetch Metadata attribute paths
/// (F-65). The attribute-based cases are RED until the production fix.
/// </summary>
[TestClass]
public sealed class SiblingSecurityAttributeIntegrationTests
{
    private const string PreAuthBinding = "sibling-attr-e2e-binding";

    private static async Task<InProcessHost> StartHostAsync(
        Action<WebApplication> mapEndpoints)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => PreAuthBinding;
                });
                builder.Services.AddApiPilotOriginPolicy(o =>
                {
                    o.AllowMissingOrigin = false;
                });
                builder.Services.AddApiPilotFetchMetadata(f =>
                {
                    f.Profile = FetchMetadataProfile.Strict;
                });
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.UseApiPilotFetchMetadata();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                mapEndpoints(app);
            });
    }

    private static HttpRequestMessage PostWithHostileOrigin(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.Add("Origin", "https://evil.example");
        return request;
    }

    private static HttpRequestMessage PostCrossSite(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        return request;
    }

    // -----------------------------------------------------------------------
    // ORIGIN — minimal API .WithMetadata(attribute)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Skip attribute via WithMetadata on a minimal-API POST with a hostile
    /// Origin must bypass the Origin check. RED until the fix.
    /// </summary>
    [Test]
    public async Task Origin_MinimalApi_WithMetadata_Skip_HostileOrigin_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook-origin", () => "ok")
               .WithMetadata(new ApiPilotSkipCsrfAttribute()));

        var response = await host.Client.SendAsync(PostWithHostileOrigin("/webhook-origin"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute via WithMetadata on a minimal-API GET with a hostile
    /// Origin must enforce the Origin check. RED until the fix.
    /// </summary>
    [Test]
    public async Task Origin_MinimalApi_WithMetadata_Require_HostileOrigin_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive-origin", () => "ok")
               .WithMetadata(new ApiPilotRequireCsrfAttribute()));

        var request = new HttpRequestMessage(HttpMethod.Get, "/sensitive-origin");
        request.Headers.Add("Origin", "https://evil.example");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // ORIGIN — decorated minimal-API handler
    // -----------------------------------------------------------------------

    /// <summary>
    /// Skip attribute on a static handler with a hostile Origin must bypass.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task Origin_DecoratedHandler_Skip_HostileOrigin_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook-origin-decorated", OriginDecoratedWebhookHandler));

        var response = await host.Client.SendAsync(PostWithHostileOrigin("/webhook-origin-decorated"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute on a static handler with a hostile Origin must
    /// enforce. RED until the fix.
    /// </summary>
    [Test]
    public async Task Origin_DecoratedHandler_Require_HostileOrigin_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive-origin-decorated", OriginDecoratedSensitiveHandler));

        var request = new HttpRequestMessage(HttpMethod.Get, "/sensitive-origin-decorated");
        request.Headers.Add("Origin", "https://evil.example");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    [ApiPilotSkipCsrf]
    private static IResult OriginDecoratedWebhookHandler() => Results.Ok("ok");

    [ApiPilotRequireCsrf]
    private static IResult OriginDecoratedSensitiveHandler() => Results.Ok("ok");

    // -----------------------------------------------------------------------
    // FETCH METADATA — minimal API .WithMetadata(attribute)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Skip attribute via WithMetadata on a minimal-API cross-site POST under
    /// Strict must bypass the Fetch Metadata check. RED until the fix.
    /// </summary>
    [Test]
    public async Task FetchMetadata_MinimalApi_WithMetadata_Skip_CrossSite_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook-fm", () => "ok")
               .WithMetadata(new ApiPilotSkipCsrfAttribute()));

        var response = await host.Client.SendAsync(PostCrossSite("/webhook-fm"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute via WithMetadata on a minimal-API cross-site GET under
    /// Strict must enforce the Fetch Metadata check. RED until the fix.
    /// </summary>
    [Test]
    public async Task FetchMetadata_MinimalApi_WithMetadata_Require_CrossSite_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive-fm", () => "ok")
               .WithMetadata(new ApiPilotRequireCsrfAttribute()));

        var request = new HttpRequestMessage(HttpMethod.Get, "/sensitive-fm");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    // -----------------------------------------------------------------------
    // FETCH METADATA — decorated minimal-API handler
    // -----------------------------------------------------------------------

    /// <summary>
    /// Skip attribute on a static handler with a cross-site POST under Strict
    /// must bypass. RED until the fix.
    /// </summary>
    [Test]
    public async Task FetchMetadata_DecoratedHandler_Skip_CrossSite_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook-fm-decorated", FetchMetadataDecoratedWebhookHandler));

        var response = await host.Client.SendAsync(PostCrossSite("/webhook-fm-decorated"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute on a static handler with a cross-site GET under Strict
    /// must enforce. RED until the fix.
    /// </summary>
    [Test]
    public async Task FetchMetadata_DecoratedHandler_Require_CrossSite_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive-fm-decorated", FetchMetadataDecoratedSensitiveHandler));

        var request = new HttpRequestMessage(HttpMethod.Get, "/sensitive-fm-decorated");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        var response = await host.Client.SendAsync(request);
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    [ApiPilotSkipCsrf]
    private static IResult FetchMetadataDecoratedWebhookHandler() => Results.Ok("ok");

    [ApiPilotRequireCsrf]
    private static IResult FetchMetadataDecoratedSensitiveHandler() => Results.Ok("ok");
}

