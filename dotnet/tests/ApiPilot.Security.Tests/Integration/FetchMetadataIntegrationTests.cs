// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/FetchMetadataIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests for the Fetch Metadata policy through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.AspNetCore.Serialization, ApiPilot.Security.Csrf,
//                ApiPilot.Security.Origin, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataMiddleware.cs, FetchMetadataMiddlewareExtensions.cs
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
/// Integration tests for the Fetch Metadata policy through a real
/// in-process ASP.NET Core host. Exercises the profile matrix through
/// HTTP and proves the compatibility path.
/// </summary>
[TestClass]
public sealed class FetchMetadataIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(
        Action<FetchMetadataOptions>? configureFetch = null)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotCsrf();
                builder.Services.AddApiPilotFetchMetadata(configureFetch);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotFetchMetadata();
                app.MapPost("/submit", () => "ok");
                app.MapGet("/read", () => "ok");
            });
    }

    private static HttpRequestMessage PostWithFetch(string path, string? site, string? mode = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        if (site is not null) { request.Headers.Add("Sec-Fetch-Site", site); }
        if (mode is not null) { request.Headers.Add("Sec-Fetch-Mode", mode); }
        return request;
    }

    /// <summary>
    /// The default Off profile passes through, proving the compatibility
    /// path required by SPEC.md 6.17.
    /// </summary>
    [Test]
    public async Task Post_OffProfile_PassesThrough()
    {
        await using var host = await StartHostAsync();
        var response = await host.Client.SendAsync(PostWithFetch("/submit", "cross-site"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>A GET is unaffected by the policy.</summary>
    [Test]
    public async Task Get_UnaffectedByPolicy()
    {
        await using var host = await StartHostAsync(f => f.Profile = FetchMetadataProfile.Strict);
        var response = await host.Client.GetAsync("/read");
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>Strict with a cross-site value returns 403 FORBIDDEN.</summary>
    [Test]
    public async Task Post_StrictCrossSite_Returns403()
    {
        await using var host = await StartHostAsync(f => f.Profile = FetchMetadataProfile.Strict);
        var response = await host.Client.SendAsync(PostWithFetch("/submit", "cross-site"));
        TestAssert.Equal(403, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.Equal("FORBIDDEN",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>Strict with a same-origin value passes.</summary>
    [Test]
    public async Task Post_StrictSameOrigin_Passes()
    {
        await using var host = await StartHostAsync(f => f.Profile = FetchMetadataProfile.Strict);
        var response = await host.Client.SendAsync(PostWithFetch("/submit", "same-origin"));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>Compat with no headers passes.</summary>
    [Test]
    public async Task Post_CompatMissingHeader_Passes()
    {
        await using var host = await StartHostAsync(f => f.Profile = FetchMetadataProfile.Compat);
        var response = await host.Client.SendAsync(PostWithFetch("/submit", null));
        TestAssert.Equal(200, (int)response.StatusCode);
    }

    /// <summary>Strict with a disallowed mode returns 403.</summary>
    [Test]
    public async Task Post_StrictDisallowedMode_Returns403()
    {
        await using var host = await StartHostAsync(f => f.Profile = FetchMetadataProfile.Strict);
        var response = await host.Client.SendAsync(PostWithFetch("/submit", "same-origin", "no-cors"));
        TestAssert.Equal(403, (int)response.StatusCode);
    }
}

