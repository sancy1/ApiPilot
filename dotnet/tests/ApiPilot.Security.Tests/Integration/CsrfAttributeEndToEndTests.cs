// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/CsrfAttributeEndToEndTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v1.0.3
// purpose: End-to-end HTTP tests for the CSRF attribute path (F-65)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + controller + handler)
//   Depends on : InProcessHost, TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.Builder,
//                Microsoft.AspNetCore.Http, Microsoft.AspNetCore.Mvc,
//                Microsoft.Extensions.DependencyInjection, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfProtectionIntegrationTests.cs (direct-record baseline)
// -----------------------------------------------------------------------------
//
// F-65 end-to-end reproduction.
//
// Pipeline order used by every test in this file:
//     UseApiPilotCorrelation
//     UseRouting
//     UseApiPilotCsrfProtection
//     MapApiPilotCsrf
//     application endpoints
// Routing runs before the metadata-consuming middleware so the endpoint
// and its metadata are resolved before the middleware inspects them.
//
// Two metadata forms are tested as separate concerns:
//   A. .WithMetadata(new ApiPilotSkipCsrfAttribute()) on a minimal-API endpoint.
//   B. [ApiPilotSkipCsrf] on a static handler method (decorated handler).
// The repair may fix one path while leaving the other broken; they are not
// collapsed into one test.
//
// The direct CsrfEndpointMetadata baseline is NOT duplicated here. It is
// already covered by CsrfProtectionIntegrationTests:
//   - Post_WithSkipPolicy_PassesWithoutToken (direct record Skip)
//   - Get_WithRequirePolicy_NeedsToken (direct record Require)
// This file references that coverage and does not repeat it.
//
// Controller coverage: two tests attach the attributes to an MVC controller
// action. The shipped XML promises controller support. If the controller path
// cannot be exercised by this harness, the test reports the path as
// unavailable rather than inferring behavior.
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// End-to-end HTTP tests for the CSRF attribute path (F-65).
/// The attribute-based cases are RED until the production fix. The
/// direct-record baseline is covered elsewhere and is not repeated.
/// </summary>
[TestClass]
public sealed class CsrfAttributeEndToEndTests
{
    private const string PreAuthBinding = "csrf-attr-e2e-binding";

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
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                mapEndpoints(app);
            });
    }

    private static HttpRequestMessage PostWithoutToken(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return request;
    }

    /// <summary>
    /// Skip attribute via WithMetadata on a minimal-API POST must bypass.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task MinimalApi_WithMetadata_Skip_Post_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook", () => "ok")
               .WithMetadata(new ApiPilotSkipCsrfAttribute()));

        var response = await host.Client.SendAsync(PostWithoutToken("/webhook"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute via WithMetadata on a minimal-API GET must enforce.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task MinimalApi_WithMetadata_Require_Get_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive", () => "ok")
               .WithMetadata(new ApiPilotRequireCsrfAttribute()));

        var response = await host.Client.GetAsync("/sensitive");
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    /// <summary>
    /// Skip attribute on a static handler method must bypass.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task DecoratedHandler_Skip_Post_Passes()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook-decorated", DecoratedWebhookHandler));

        var response = await host.Client.SendAsync(PostWithoutToken("/webhook-decorated"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute on a static handler method must enforce.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task DecoratedHandler_Require_Get_Returns403()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive-decorated", DecoratedSensitiveHandler));

        var response = await host.Client.GetAsync("/sensitive-decorated");
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    [ApiPilotSkipCsrf]
    private static IResult DecoratedWebhookHandler() => Results.Ok("ok");

    [ApiPilotRequireCsrf]
    private static IResult DecoratedSensitiveHandler() => Results.Ok("ok");

    /// <summary>
    /// Skip attribute on a controller action must bypass. The shipped XML
    /// documents controller support. RED until the fix.
    /// </summary>
    [Test]
    public async Task Controller_Skip_Post_Passes()
    {
        await using var host = await StartControllerHostAsync();

        var response = await host.Client.SendAsync(PostWithoutToken("/api/webhook"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>
    /// Require attribute on a controller GET action must enforce. The shipped
    /// XML documents controller support. RED until the fix.
    /// </summary>
    [Test]
    public async Task Controller_Require_Get_Returns403()
    {
        await using var host = await StartControllerHostAsync();

        var response = await host.Client.GetAsync("/api/sensitive");
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    private static async Task<InProcessHost> StartControllerHostAsync()
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
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(CsrfAttributeEndToEndTests).Assembly);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapControllers();
            });
    }
}

/// <summary>A controller whose actions carry the CSRF attributes.</summary>
[ApiController]
[Route("api")]
public sealed class CsrfAttributeSampleController : ControllerBase
{
    /// <summary>Skip attribute on a POST action.</summary>
    [HttpPost("webhook")]
    [ApiPilotSkipCsrf]
    public IActionResult Webhook() => Ok(new { ok = true });

    /// <summary>Require attribute on a GET action.</summary>
    [HttpGet("sensitive")]
    [ApiPilotRequireCsrf]
    public IActionResult Sensitive() => Ok(new { ok = true });
}

