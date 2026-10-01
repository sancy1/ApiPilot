// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/15_DataProtectionAndMultiInstanceScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 15. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.5 package can register the Data Protection
//           configuration, observe the documented MultiInstance startup-failure
//           rule, and confirm that two hosts sharing a key ring validate each
//           other CSRF tokens. The highest-priority remaining feature.
// relates:  Uses ApiPilot.Security.DataProtection.ApiPilotDataProtectionExtensions,
//           ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions,
//           ApiPilot.Security.Csrf.CsrfServiceExtensions,
//           ApiPilot.Security.Csrf.CsrfBootstrapExtensions,
//           ApiPilot.Security.Csrf.CsrfMiddlewareExtensions.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml. Not the source
//           tree.

using System.IO;
using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 15. Data Protection and multi-instance. Four sub-checks:
/// single-instance default, MultiInstance without KeyStorage fails at startup,
/// MultiInstance with KeyStorage starts, and two hosts sharing a key ring
/// validate each other CSRF tokens.
/// </summary>
public static class DataProtectionAndMultiInstanceScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "15_DataProtectionAndMultiInstance";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckSingleInstanceStarts(failures);
            await CheckMultiInstanceWithoutKeyStorageFails(failures);
            await CheckMultiInstanceWithKeyStorageStarts(failures);
            await CheckSharedKeyRingValidatesAcrossHosts(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "AddApiPilotDataProtection is public and callable; the single-instance default starts; MultiInstance=true without KeyStorage fails at startup; MultiInstance=true with KeyStorage starts; two hosts sharing a key ring validate each other CSRF tokens.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: single-instance default starts
    // -------------------------------------------------------------------
    private static async Task CheckSingleInstanceStarts(List<string> failures)
    {
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotDataProtection();
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            using var client = host.CreateClient();
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("SingleInstance: expected 200, got " + (int)response.StatusCode + ".");
            }
        }
        catch (Exception ex)
        {
            failures.Add("SingleInstance: host failed to start: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: MultiInstance without KeyStorage fails at startup
    // -------------------------------------------------------------------
    private static async Task CheckMultiInstanceWithoutKeyStorageFails(List<string> failures)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotDataProtection(o => o.MultiInstance = true);
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });
            // If we get here, the host started. That is a failure of the documented contract.
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }

        if (!threw)
        {
            failures.Add("MultiInstanceFail: host started with MultiInstance=true and no KeyStorage; the documented fail-closed contract was not honored.");
        }
        else
        {
            System.Console.WriteLine("Observation: MultiInstanceFail threw " + exceptionType);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: MultiInstance with KeyStorage starts
    // -------------------------------------------------------------------
    private static async Task CheckMultiInstanceWithKeyStorageStarts(List<string> failures)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "apipilot-verify-keys-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotDataProtection(o =>
                    {
                        o.ApplicationName = "apipilot-verify-shared";
                        o.MultiInstance = true;
                        o.KeyStorage = builder => builder.PersistKeysToFileSystem(new DirectoryInfo(tempDir));
                    });
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            using var client = host.CreateClient();
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("MultiInstanceStart: expected 200, got " + (int)response.StatusCode + ".");
            }
        }
        catch (Exception ex)
        {
            failures.Add("MultiInstanceStart: host failed to start: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort cleanup */ }
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: shared key ring validates across hosts
    // -------------------------------------------------------------------
    private static async Task CheckSharedKeyRingValidatesAcrossHosts(List<string> failures)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "apipilot-verify-keys-shared-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var hostA = await InProcessHost.StartAsync(
                configureServices: services => ConfigureShared(services, tempDir),
                configurePipeline: app =>
                {
                    app.MapApiPilotCsrf();
                });

            await using var hostB = await InProcessHost.StartAsync(
                configureServices: services => ConfigureShared(services, tempDir),
                configurePipeline: app =>
                {
                    app.UseRouting();
                    app.UseApiPilotCsrfProtection();
                    app.MapPost("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            // Obtain a token from host A.
            using var clientA = hostA.CreateClient();
            using var tokenResponse = await clientA.GetAsync(new Uri("/api/csrf", UriKind.Relative));
            var tokenBody = await tokenResponse.Content.ReadAsStringAsync();
            if (tokenResponse.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("SharedKeyRing: host A bootstrap expected 200, got " + (int)tokenResponse.StatusCode + ". Body=" + tokenBody);
                return;
            }
            var token = JsonNode.Parse(tokenBody)?["token"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(token))
            {
                failures.Add("SharedKeyRing: host A bootstrap did not return a token. Body=" + tokenBody);
                return;
            }

            // Send the token to host B.
            using var clientB = hostB.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", token);
            using var response = await clientB.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                var body = await response.Content.ReadAsStringAsync();
                failures.Add("SharedKeyRing: host B expected 200, got " + (int)response.StatusCode + ". Body=" + body);
            }
        }
        catch (Exception ex)
        {
            failures.Add("SharedKeyRing: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { /* best effort cleanup */ }
            }
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static void ConfigureShared(IServiceCollection services, string tempDir)
    {
        services.AddDataProtection();
        services.AddApiPilotDataProtection(o =>
        {
            o.ApplicationName = "apipilot-verify-shared";
            o.MultiInstance = true;
            o.KeyStorage = builder => builder.PersistKeysToFileSystem(new DirectoryInfo(tempDir));
        });
        services.AddApiPilotJson();
        services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
    }
}
