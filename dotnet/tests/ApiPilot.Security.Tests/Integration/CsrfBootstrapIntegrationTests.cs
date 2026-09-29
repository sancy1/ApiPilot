// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/CsrfBootstrapIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests for the CSRF bootstrap endpoint through real HTTP
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.Security.Csrf,
//                Microsoft.AspNetCore.Builder, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfBootstrapEndpoint.cs, CsrfBootstrapExtensions.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests for the CSRF bootstrap endpoint. Uses a real
/// in-process ASP.NET Core host so the response is what an HTTP client
/// actually receives, including headers, cache-control, and status code.
/// </summary>
[TestClass]
public sealed class CsrfBootstrapIntegrationTests
{
    private static async Task<InProcessHost> StartHostAsync(
        Action<CsrfOptions>? configureCsrf = null,
        Action<CsrfBootstrapOptions>? configureBootstrap = null)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotCsrf(
                    configure: o =>
                    {
                        // The integration test is anonymous. Provide a
                        // stable pre-auth binding so IssueAsync succeeds.
                        o.PreAuthBindingSource = _ => "integration-test-binding";
                        configureCsrf?.Invoke(o);
                    },
                    configureBootstrap: configureBootstrap);
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapApiPilotCsrf();
            });
    }

    /// <summary>The endpoint returns 200 with a token in the body.</summary>
    [Test]
    public async Task Get_Bootstrap_ReturnsToken()
    {
        await using var host = await StartHostAsync();

        var response = await host.Client.GetAsync("/api/csrf");
        TestAssert.Equal(200, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("token").GetString();
        TestAssert.NotNull(token);
        TestAssert.True(token!.Length > 0);
    }

    /// <summary>The response object has exactly one property: token.</summary>
    [Test]
    public async Task Get_Bootstrap_ResponseContainsOnlyToken()
    {
        await using var host = await StartHostAsync();

        var json = await host.Client.GetStringAsync("/api/csrf");
        using var doc = JsonDocument.Parse(json);

        var propertyCount = 0;
        foreach (var _ in doc.RootElement.EnumerateObject())
        {
            propertyCount++;
        }
        TestAssert.Equal(1, propertyCount);
        TestAssert.True(doc.RootElement.TryGetProperty("token", out _));
    }

    /// <summary>
    /// The raw response does not leak authentication material. This is the
    /// SPEC.md 6.13 boundary test: the bootstrap response carries only the
    /// token, never a cookie, a session id, or a credential.
    /// </summary>
    [Test]
    public async Task Get_Bootstrap_NoAuthMaterialLeaked()
    {
        await using var host = await StartHostAsync();

        var json = await host.Client.GetStringAsync("/api/csrf");
        // Case-insensitive check for known leak terms.
        var lower = json.ToLowerInvariant();
        TestAssert.False(lower.Contains("cookie", StringComparison.Ordinal));
        TestAssert.False(lower.Contains("session", StringComparison.Ordinal));
        TestAssert.False(lower.Contains("password", StringComparison.Ordinal));
        TestAssert.False(lower.Contains("secret", StringComparison.Ordinal));
        TestAssert.False(lower.Contains("auth", StringComparison.Ordinal));
    }

    /// <summary>
    /// Without a pre-auth source and without authentication, the endpoint
    /// returns 403 with the standard error envelope and CSRF_TOKEN_INVALID.
    /// </summary>
    [Test]
    public async Task Get_Bootstrap_UnauthenticatedNoBinding_Returns403()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                // Deliberately do NOT configure a pre-auth source.
                builder.Services.AddApiPilotCsrf();
            },
            configureApp: app =>
            {
                app.UseRouting();
                app.MapApiPilotCsrf();
            });

        var response = await host.Client.GetAsync("/api/csrf");
        TestAssert.Equal(403, (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        TestAssert.False(doc.RootElement.GetProperty("success").GetBoolean());
        TestAssert.Equal("CSRF_TOKEN_INVALID",
            doc.RootElement.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A custom bootstrap path is honored.</summary>
    [Test]
    public async Task Get_Bootstrap_CustomPath_HonorsOverride()
    {
        await using var host = await StartHostAsync(
            configureCsrf: o => o.BootstrapPath = "/custom/csrf");

        var response = await host.Client.GetAsync("/custom/csrf");
        TestAssert.Equal(200, (int)response.StatusCode);

        // The default path is not mapped when the custom path is used.
        var missing = await host.Client.GetAsync("/api/csrf");
        TestAssert.Equal(404, (int)missing.StatusCode);
    }

    /// <summary>The response has Cache-Control: no-store by default.</summary>
    [Test]
    public async Task Get_Bootstrap_CacheControlHeader_IsNoStore()
    {
        await using var host = await StartHostAsync();

        var response = await host.Client.GetAsync("/api/csrf");
        TestAssert.Equal(200, (int)response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        TestAssert.NotNull(cacheControl);
        TestAssert.True(cacheControl!.NoStore);
    }

    /// <summary>A custom CacheControl value is honored.</summary>
    [Test]
    public async Task Get_Bootstrap_CustomCacheControl_HonorsOverride()
    {
        await using var host = await StartHostAsync(
            configureBootstrap: o => o.CacheControl = "no-cache");

        var response = await host.Client.GetAsync("/api/csrf");
        TestAssert.Equal(200, (int)response.StatusCode);

        var cacheControl = response.Headers.CacheControl;
        TestAssert.NotNull(cacheControl);
        TestAssert.True(cacheControl!.NoCache);
    }
}

