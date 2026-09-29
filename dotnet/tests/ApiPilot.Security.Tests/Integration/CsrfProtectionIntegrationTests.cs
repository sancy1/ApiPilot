// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/CsrfProtectionIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests for the CSRF protection middleware through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.Security.Csrf,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                Microsoft.AspNetCore.Builder, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfMiddleware.cs, CsrfMiddlewareExtensions.cs
// -----------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests for the CSRF protection middleware through a real
/// in-process ASP.NET Core host. Exercises the middleware, the service,
/// the binding provider, and the signer end to end.
/// </summary>
[TestClass]
public sealed class CsrfProtectionIntegrationTests
{
    private const string PreAuthBinding = "integration-test-binding";

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

    private static async Task<string> FetchTokenAsync(InProcessHost host)
    {
        var json = await host.Client.GetStringAsync("/api/csrf");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    private static HttpRequestMessage PostWithToken(string path, string? token, string headerName = "X-CSRF-TOKEN")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        if (token is not null)
        {
            request.Headers.Add(headerName, token);
        }
        return request;
    }

    /// <summary>A POST with no token returns 403 with CSRF_HEADER_MISSING.</summary>
    [Test]
    public async Task Post_NoToken_Returns403WithCsrfHeaderMissing()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/submit", () => "ok"));

        var response = await host.Client.SendAsync(PostWithToken("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("CSRF_HEADER_MISSING",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A POST with a valid token passes through.</summary>
    [Test]
    public async Task Post_WithValidToken_PassesThrough()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/submit", () => "ok"));

        var token = await FetchTokenAsync(host);
        var response = await host.Client.SendAsync(PostWithToken("/submit", token));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A GET is unaffected by CSRF protection.</summary>
    [Test]
    public async Task Get_UnaffectedByCsrf()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/read", () => "ok"));

        var response = await host.Client.GetAsync("/read");
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A POST on a [Skip] endpoint passes without a token.</summary>
    [Test]
    public async Task Post_WithSkipPolicy_PassesWithoutToken()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/webhook", () => "ok")
               .WithMetadata(new CsrfEndpointMetadata(CsrfPolicy.Skip)));

        var response = await host.Client.SendAsync(PostWithToken("/webhook", null));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A GET on a [Require] endpoint needs a token.</summary>
    [Test]
    public async Task Get_WithRequirePolicy_NeedsToken()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/sensitive", () => "ok")
               .WithMetadata(new CsrfEndpointMetadata(CsrfPolicy.Require)));

        var response = await host.Client.GetAsync("/sensitive");
        TestAssert.Equal(403, (int)response.StatusCode);
    }

    /// <summary>The bootstrap path itself is exempt even on a POST.</summary>
    [Test]
    public async Task Post_BootstrapPath_IsExempt()
    {
        await using var host = await StartHostAsync(app => { });

        // The bootstrap endpoint is a GET. A POST to the same path is
        // not mapped, but the middleware must not enforce CSRF on it.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/csrf");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await host.Client.SendAsync(request);

        // The path is exempt, so the middleware does not reject with 403.
        // The response is 404 or 405 because no POST handler is mapped.
        TestAssert.True((int)response.StatusCode == 404 || (int)response.StatusCode == 405);
    }

    /// <summary>The failure envelope carries the correlation ID from the request header.</summary>
    [Test]
    public async Task Post_FailureEnvelope_CarriesCorrelationId()
    {
        await using var host = await StartHostAsync(app =>
            app.MapPost("/submit", () => "ok"));

        var incomingId = "abcdef12-3456-7890";
        var request = PostWithToken("/submit", null);
        request.Headers.Add("X-Request-Id", incomingId);
        var response = await host.Client.SendAsync(request);

        TestAssert.Equal(403, (int)response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal(incomingId,
            doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>Echo true with an accessor: the rejection carries the accessor id.</summary>
    [Test]
    public async Task FailureEnvelope_EchoTrue_RequestIdPopulated()
    {
        var accessor = new StubCorrelationAccessor("accessor-id-echo-true");
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation(o => o.EchoInResponseBody = true);
                builder.Services.AddSingleton<ApiPilot.Core.Metadata.ICorrelationIdAccessor>(accessor);
                builder.Services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => PreAuthBinding);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapPost("/submit", () => "ok");
            });

        var response = await host.Client.SendAsync(PostWithToken("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("accessor-id-echo-true",
            doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>Echo false: the rejection carries an empty request id.</summary>
    [Test]
    public async Task FailureEnvelope_EchoFalse_RequestIdEmpty()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation(o => o.EchoInResponseBody = false);
                builder.Services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => PreAuthBinding);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapPost("/submit", () => "ok");
            });

        var response = await host.Client.SendAsync(PostWithToken("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal(string.Empty,
            doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>No accessor: the rejection falls back to the trace identifier.</summary>
    [Test]
    public async Task FailureEnvelope_NoAccessor_FallsBackToTraceIdentifier()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => PreAuthBinding);
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapPost("/submit", () => "ok");
            });

        var response = await host.Client.SendAsync(PostWithToken("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.NotNull(requestId);
        TestAssert.True(requestId!.Length > 0);
    }

    /// <summary>An accessor is present: the rejection uses its id.</summary>
    [Test]
    public async Task FailureEnvelope_AccessorPresent_UsesAccessorId()
    {
        var accessor = new StubCorrelationAccessor("fixed-accessor-id");
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddSingleton<ApiPilot.Core.Metadata.ICorrelationIdAccessor>(accessor);
                builder.Services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => PreAuthBinding);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapPost("/submit", () => "ok");
            });

        var response = await host.Client.SendAsync(PostWithToken("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("fixed-accessor-id",
            doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString());
    }
}

/// <summary>A correlation accessor returning a fixed id for tests.</summary>
internal sealed class StubCorrelationAccessor : ApiPilot.Core.Metadata.ICorrelationIdAccessor
{
    /// <summary>Creates the accessor with a fixed id.</summary>
    /// <param name="requestId">The id to return. Must not be null.</param>
    public StubCorrelationAccessor(string requestId)
    {
        RequestId = requestId;
    }

    /// <inheritdoc />
    public string? RequestId { get; }
}

