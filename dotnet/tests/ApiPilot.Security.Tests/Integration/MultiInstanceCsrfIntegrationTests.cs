// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/MultiInstanceCsrfIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests proving that two ApiPilot hosts sharing a key ring validate each other
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.Security.DataProtection,
//                ApiPilot.Security.Csrf, ApiPilot.AspNetCore.Serialization,
//                Microsoft.AspNetCore.Builder, Microsoft.AspNetCore.DataProtection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotDataProtectionExtensions.cs, DataProtectionCsrfTokenSigner.cs
// -----------------------------------------------------------------------------

using System.Net.Http;
using System.Text.Json;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using ApiPilot.Security.Tests;
using ApiPilot.Security.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests proving that two ApiPilot hosts sharing a key ring
/// validate each other's CSRF tokens, and that two hosts with separate
/// key rings do not. Uses a temporary directory on disk for the key
/// ring.
/// </summary>
[TestClass]
public sealed class MultiInstanceCsrfIntegrationTests
{
    private static string NewKeyRingDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "apipilot-keys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task<InProcessHost> StartHostAsync(string keyRingDirectory, string applicationName)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotDataProtection(o =>
                {
                    o.ApplicationName = applicationName;
                    o.MultiInstance = true;
                    o.KeyStorage = dp => dp.PersistKeysToFileSystem(new DirectoryInfo(keyRingDirectory));
                });
                builder.Services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => "integration-binding";
                });
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapPost("/submit", () => "ok");
            });
    }

    private static async Task<string> FetchTokenAsync(InProcessHost host)
    {
        var json = await host.Client.GetStringAsync("/api/csrf");
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("token").GetString()!;
    }

    private static HttpRequestMessage PostWithToken(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.Add("X-CSRF-TOKEN", token);
        return request;
    }

    /// <summary>
    /// Two hosts sharing a key ring validate each other's tokens. Host A
    /// issues a token; host B validates it.
    /// </summary>
    [Test]
    public async Task TwoHostsSharedKeyRing_TokenFromAToB_Passes()
    {
        var keyRing = NewKeyRingDirectory();
        var appName = "ApiPilot.MultiInstance.Test";

        try
        {
            await using var hostA = await StartHostAsync(keyRing, appName);
            await using var hostB = await StartHostAsync(keyRing, appName);

            var token = await FetchTokenAsync(hostA);
            var response = await hostB.Client.SendAsync(PostWithToken("/submit", token));

            TestAssert.Equal(200, (int)response.StatusCode);
        }
        finally
        {
            try { Directory.Delete(keyRing, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Two hosts with separate key rings do not validate each other's
    /// tokens. Host A issues a token; host B rejects it.
    /// </summary>
    [Test]
    public async Task TwoHostsSeparateKeyRings_TokenFromAToB_Rejects()
    {
        var keyRingA = NewKeyRingDirectory();
        var keyRingB = NewKeyRingDirectory();
        var appName = "ApiPilot.MultiInstance.Test";

        try
        {
            await using var hostA = await StartHostAsync(keyRingA, appName);
            await using var hostB = await StartHostAsync(keyRingB, appName);

            var token = await FetchTokenAsync(hostA);
            var response = await hostB.Client.SendAsync(PostWithToken("/submit", token));

            TestAssert.Equal(403, (int)response.StatusCode);
        }
        finally
        {
            try { Directory.Delete(keyRingA, recursive: true); } catch { }
            try { Directory.Delete(keyRingB, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Two hosts sharing a key ring validate each other's tokens in the
    /// reverse direction: host B issues a token; host A validates it.
    /// </summary>
    [Test]
    public async Task TwoHostsSharedKeyRing_TokenFromBToA_Passes()
    {
        var keyRing = NewKeyRingDirectory();
        var appName = "ApiPilot.MultiInstance.Test";

        try
        {
            await using var hostA = await StartHostAsync(keyRing, appName);
            await using var hostB = await StartHostAsync(keyRing, appName);

            var token = await FetchTokenAsync(hostB);
            var response = await hostA.Client.SendAsync(PostWithToken("/submit", token));

            TestAssert.Equal(200, (int)response.StatusCode);
        }
        finally
        {
            try { Directory.Delete(keyRing, recursive: true); } catch { }
            TestAssert.False(Directory.Exists(keyRing));
        }
    }

    /// <summary>
    /// Two hosts with separate key rings: host B rejects host A's token
    /// with the exact CSRF_TOKEN_INVALID wire code.
    /// </summary>
    [Test]
    public async Task TwoHostsSeparateKeyRings_TokenFromAToB_RejectsWithCsrfTokenInvalid()
    {
        var keyRingA = NewKeyRingDirectory();
        var keyRingB = NewKeyRingDirectory();
        var appName = "ApiPilot.MultiInstance.Test";

        try
        {
            await using var hostA = await StartHostAsync(keyRingA, appName);
            await using var hostB = await StartHostAsync(keyRingB, appName);

            var token = await FetchTokenAsync(hostA);
            var response = await hostB.Client.SendAsync(PostWithToken("/submit", token));

            TestAssert.Equal(403, (int)response.StatusCode);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            TestAssert.Equal("CSRF_TOKEN_INVALID",
                doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        }
        finally
        {
            try { Directory.Delete(keyRingA, recursive: true); } catch { }
            try { Directory.Delete(keyRingB, recursive: true); } catch { }
            TestAssert.False(Directory.Exists(keyRingA));
            TestAssert.False(Directory.Exists(keyRingB));
        }
    }

    /// <summary>
    /// MultiInstance true without key storage fails closed at host startup.
    /// </summary>
    [Test]
    public async Task MultiInstanceTrue_WithoutKeyStorage_FailsClosedAtStartup()
    {
        var threw = false;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddDataProtection();
                    builder.Services.AddApiPilotJson();
                    builder.Services.AddApiPilotCorrelation();
                    builder.Services.AddApiPilotDataProtection(o =>
                    {
                        o.MultiInstance = true;
                    });
                    builder.Services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "integration-binding");
                });
        }
        catch (Microsoft.Extensions.Options.OptionsValidationException)
        {
            threw = true;
        }
        TestAssert.True(threw);
    }
}

