// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/18_DiagnosticsScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 18. Proves that a real consumer of the published
//           ApiPilot.Security and ApiPilot.AspNetCore 1.0.5 packages can
//           register the security diagnostics surface, observe the
//           documented SECW001 in-memory-key-ring warning, and inspect the
//           ApiPilotLogEvents catalogue. The observability surface.
// relates:  Uses ApiPilot.Security.Diagnostics.ApiPilotSecurityDiagnosticsExtensions,
//           ApiPilot.Security.Diagnostics.ApiPilotSecurityDiagnostics,
//           ApiPilot.Security.DataProtection.ApiPilotDataProtectionExtensions,
//           ApiPilot.AspNetCore.Diagnostics.ApiPilotLogEvents.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml and
//           lib/net10.0/ApiPilot.AspNetCore.xml. Not the source tree.

using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using ApiPilot.AspNetCore.Diagnostics;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Cookies;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.DataProtection;
using ApiPilot.Security.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 18. The security diagnostics surface. Four sub-checks: the
/// registration resolves the service; the default config produces the
/// SECW001 warning; MultiInstance with KeyStorage suppresses it; and
/// ApiPilotLogEvents exposes the shipped constants.
/// </summary>
public static class DiagnosticsScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "18_Diagnostics";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckServiceResolves(failures);
            await CheckDefaultProducesInMemoryWarning(failures, observations);
            await CheckMultiInstanceSuppressesWarning(failures);
            CheckLogEventsCatalogue(failures, observations);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "AddApiPilotSecurityDiagnostics is public and callable; the diagnostics service resolves from the container; the default Data Protection config produces the SECW001 in-memory-key-ring warning; MultiInstance=true with KeyStorage suppresses it; ApiPilotLogEvents exposes the shipped constants, all non-zero and pairwise distinct." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: the diagnostics service resolves
    // -------------------------------------------------------------------
    private static async Task CheckServiceResolves(List<string> failures)
    {
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                    services.AddApiPilotCookies();
                    services.AddApiPilotOriginPolicy();
                    services.AddApiPilotFetchMetadata();
                    services.AddApiPilotDataProtection();
                    services.AddApiPilotSecurityDiagnostics();
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            using var client = host.CreateClient();
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                failures.Add("ServiceResolves: expected 200, got " + (int)response.StatusCode + ".");
            }
        }
        catch (Exception ex)
        {
            failures.Add("ServiceResolves: host failed to start: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: default config produces SECW001
    // -------------------------------------------------------------------
    private static async Task CheckDefaultProducesInMemoryWarning(List<string> failures, List<string> observations)
    {
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                    services.AddApiPilotCookies();
                    services.AddApiPilotOriginPolicy();
                    services.AddApiPilotFetchMetadata();
                    services.AddApiPilotDataProtection();
                    services.AddApiPilotSecurityDiagnostics();
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            var diagnostics = host.Services.GetRequiredService<ApiPilotSecurityDiagnostics>();
            var list = diagnostics.GetDiagnostics();
            if (list is null)
            {
                failures.Add("DefaultWarning: GetDiagnostics returned null.");
                return;
            }
            if (list.Count == 0)
            {
                failures.Add("DefaultWarning: GetDiagnostics returned an empty list; expected the SECW001 in-memory-key-ring warning.");
                return;
            }
            var codes = new List<string>();
            foreach (var d in list)
            {
                var codeProp = d.GetType().GetProperty("Code");
                if (codeProp is not null)
                {
                    var v = codeProp.GetValue(d)?.ToString();
                    if (!string.IsNullOrWhiteSpace(v))
                    {
                        codes.Add(v);
                    }
                }
            }
            observations.Add("DefaultWarning produced " + list.Count + " diagnostic(s): " + string.Join(", ", codes));
            if (!codes.Contains("SECW001"))
            {
                failures.Add("DefaultWarning: expected code SECW001 in the diagnostics; observed codes: " + string.Join(", ", codes));
            }
        }
        catch (Exception ex)
        {
            failures.Add("DefaultWarning: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: MultiInstance with KeyStorage suppresses the warning
    // -------------------------------------------------------------------
    private static async Task CheckMultiInstanceSuppressesWarning(List<string> failures)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "apipilot-verify-diag-" + Guid.NewGuid().ToString("N"));
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddDataProtection();
                    services.AddApiPilotJson();
                    services.AddApiPilotCsrf(o => o.PreAuthBindingSource = _ => "test-binding");
                    services.AddApiPilotCookies();
                    services.AddApiPilotOriginPolicy();
                    services.AddApiPilotFetchMetadata();
                    services.AddApiPilotDataProtection(o =>
                    {
                        o.MultiInstance = true;
                        o.KeyStorage = builder => builder.PersistKeysToFileSystem(new DirectoryInfo(tempDir));
                    });
                    services.AddApiPilotSecurityDiagnostics();
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            var diagnostics = host.Services.GetRequiredService<ApiPilotSecurityDiagnostics>();
            var list = diagnostics.GetDiagnostics();
            if (list is null)
            {
                failures.Add("MultiInstanceSuppresses: GetDiagnostics returned null.");
                return;
            }
            if (list.Count != 0)
            {
                failures.Add("MultiInstanceSuppresses: expected an empty list; got " + list.Count + " diagnostic(s).");
            }
        }
        catch (Exception ex)
        {
            failures.Add("MultiInstanceSuppresses: " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, recursive: true); } catch { }
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: ApiPilotLogEvents catalogue
    // -------------------------------------------------------------------
    private static void CheckLogEventsCatalogue(List<string> failures, List<string> observations)
    {
        var type = typeof(ApiPilotLogEvents);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                         .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(int))
                         .ToList();

        observations.Add("LogEvents: " + fields.Count + " int constants found");

        if (fields.Count < 18)
        {
            failures.Add("LogEvents: expected at least 18 constants; got " + fields.Count + ".");
        }

        var values = new List<int>();
        foreach (var f in fields)
        {
            var v = (int)f.GetRawConstantValue()!;
            if (v == 0)
            {
                failures.Add("LogEvents: constant '" + f.Name + "' has value 0.");
            }
            values.Add(v);
        }

        var duplicates = values.GroupBy(v => v).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            failures.Add("LogEvents: duplicate values found: " + string.Join(", ", duplicates));
        }
    }
}
