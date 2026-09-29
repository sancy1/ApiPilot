// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/OriginPolicyIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests for the Origin policy through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Security.Csrf,
//                ApiPilot.Security.Origin, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginMiddleware.cs, OriginMiddlewareExtensions.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text;
using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests for the Origin policy through a real in-process
/// ASP.NET Core host. Exercises the middleware, the validator, and the
/// failure envelope end to end.
/// </summary>
[TestClass]
public sealed class OriginPolicyIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(
        Action<OriginPolicyOptions>? configureOrigin = null)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotCsrf();
                builder.Services.AddApiPilotOriginPolicy(configureOrigin);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotOriginPolicy();
                app.MapPost("/submit", () => "ok");
                app.MapGet("/read", () => "ok");
            });
    }

    private static HttpRequestMessage PostWithOrigin(string path, string? origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }
        return request;
    }

    /// <summary>A GET is unaffected by the Origin policy.</summary>
    [Test]
    public async Task Get_UnaffectedByOriginPolicy()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.GetAsync("/read");
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A POST with a same-origin Origin passes by default.</summary>
    [Test]
    public async Task Post_SameOrigin_PassesByDefault()
    {
        await using var host = await StartHostAsync();
        var sameOrigin = host.BaseAddress.GetLeftPart(UriPartial.Authority);
        var response = await host.Client.SendAsync(PostWithOrigin("/submit", sameOrigin));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A POST with no Origin passes with AllowMissingOrigin=true (default).</summary>
    [Test]
    public async Task Post_NoOrigin_PassesByDefault()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.SendAsync(PostWithOrigin("/submit", null));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A POST with a hostile Origin returns 403 CSRF_ORIGIN_REJECTED.</summary>
    [Test]
    public async Task Post_HostileOrigin_Returns403()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.SendAsync(PostWithOrigin("/submit", "https://evil.example"));
        TestAssert.Equal(403, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("CSRF_ORIGIN_REJECTED",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>An allowed origin in the allow-list passes.</summary>
    [Test]
    public async Task Post_AllowedOrigin_PassesThrough()
    {
        await using var host = await StartHostAsync(o =>
        {
            o.AllowSameOrigin = false;
            o.AllowedOrigins.Add("https://trusted.example");
        });
        var response = await host.Client.SendAsync(PostWithOrigin("/submit", "https://trusted.example"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A missing Origin returns 403 when AllowMissingOrigin is false.</summary>
    [Test]
    public async Task Post_NoOrigin_WithDisallowedMissing_Returns403()
    {
        await using var host = await StartHostAsync(o => o.AllowMissingOrigin = false);
        var response = await host.Client.SendAsync(PostWithOrigin("/submit", null));
        TestAssert.Equal(403, (int)response.StatusCode);
    }
}

