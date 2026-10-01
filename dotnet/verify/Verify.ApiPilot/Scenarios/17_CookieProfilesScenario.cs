// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/17_CookieProfilesScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 17. Proves that a real consumer of the published
//           ApiPilot.Security 1.0.5 package can register the cookie profile
//           options and observe the documented startup-validation rules:
//           default profiles start, SameSite=None without Secure fails,
//           __Host- with a non-root Path fails, __Host- with the correct
//           attributes passes, and an invalid NamePrefix fails.
// relates:  Uses ApiPilot.Security.Cookies.ApiPilotCookiesExtensions,
//           ApiPilot.Security.Cookies.CookieProfileOptions,
//           ApiPilot.Security.Cookies.CookieProfile,
//           ApiPilot.Security.Cookies.CookieSameSite.
// authority: the shipped lib/net10.0/ApiPilot.Security.xml. Not the source
//           tree.

using System.Net;
using ApiPilot.Security.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 17. The cookie profile validator. Five sub-checks: defaults
/// start, SameSite=None without Secure fails, __Host- with a non-root Path
/// fails, __Host- with the correct attributes passes, and an invalid
/// NamePrefix fails.
/// </summary>
public static class CookieProfilesScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "17_CookieProfiles";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckDefaultProfilesStart(failures);
            await CheckSameSiteNoneWithoutSecureFails(failures, observations);
            await CheckHostPrefixWithNonRootPathFails(failures, observations);
            await CheckHostPrefixWithCorrectAttributesStarts(failures);
            await CheckInvalidNamePrefixFails(failures, observations);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "AddApiPilotCookies is public and callable; the default profiles start the host; SameSite=None without Secure fails at startup; __Host- with a non-root Path fails at startup; __Host- with Secure, no Domain, and Path=/ passes; an invalid NamePrefix fails at startup." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: default profiles start
    // -------------------------------------------------------------------
    private static async Task CheckDefaultProfilesStart(List<string> failures)
    {
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddApiPilotCookies();
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            using var client = host.CreateClient();
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("Defaults: expected 200, got " + (int)response.StatusCode + ".");
            }
        }
        catch (Exception ex)
        {
            failures.Add("Defaults: host failed to start: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: SameSite=None without Secure fails
    // -------------------------------------------------------------------
    private static async Task CheckSameSiteNoneWithoutSecureFails(List<string> failures, List<string> observations)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddApiPilotCookies(o =>
                    {
                        o.AuthenticationProfile = new CookieProfile
                        {
                            Name = "auth",
                            Secure = false,
                            HttpOnly = true,
                            SameSite = CookieSameSite.None,
                            Path = "/",
                        };
                    });
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }

        if (!threw)
        {
            failures.Add("SameSiteNone: host started with SameSite=None and Secure=false; the documented fail-closed contract was not honored.");
        }
        else
        {
            observations.Add("SameSiteNone threw " + exceptionType);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: __Host- with a non-root Path fails
    // -------------------------------------------------------------------
    private static async Task CheckHostPrefixWithNonRootPathFails(List<string> failures, List<string> observations)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddApiPilotCookies(o =>
                    {
                        o.NamePrefix = "__Host-";
                        o.AuthenticationProfile = new CookieProfile
                        {
                            Name = "auth",
                            Secure = true,
                            HttpOnly = true,
                            SameSite = CookieSameSite.Lax,
                            Path = "/sub",
                        };
                    });
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }

        if (!threw)
        {
            failures.Add("HostPrefixNonRoot: host started with __Host- prefix and Path=/sub; the documented fail-closed contract was not honored.");
        }
        else
        {
            observations.Add("HostPrefixNonRoot threw " + exceptionType);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: __Host- with the correct attributes passes
    // -------------------------------------------------------------------
    private static async Task CheckHostPrefixWithCorrectAttributesStarts(List<string> failures)
    {
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddApiPilotCookies(o =>
                    {
                        o.NamePrefix = "__Host-";
                        o.AuthenticationProfile = new CookieProfile
                        {
                            Name = "auth",
                            Secure = true,
                            HttpOnly = true,
                            SameSite = CookieSameSite.Lax,
                            Path = "/",
                        };
                    });
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });

            using var client = host.CreateClient();
            using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
            if (response.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("HostPrefixValid: expected 200, got " + (int)response.StatusCode + ".");
            }
        }
        catch (Exception ex)
        {
            failures.Add("HostPrefixValid: host failed to start: " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 5: invalid NamePrefix fails
    // -------------------------------------------------------------------
    private static async Task CheckInvalidNamePrefixFails(List<string> failures, List<string> observations)
    {
        var threw = false;
        string? exceptionType = null;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: services =>
                {
                    services.AddApiPilotCookies(o =>
                    {
                        o.NamePrefix = "InvalidPrefix-";
                    });
                },
                configurePipeline: app =>
                {
                    app.MapGet("/", () => Microsoft.AspNetCore.Http.Results.Ok());
                });
        }
        catch (Exception ex)
        {
            threw = true;
            exceptionType = ex.GetType().FullName;
        }

        if (!threw)
        {
            failures.Add("InvalidPrefix: host started with an invalid NamePrefix; the documented fail-closed contract was not honored.");
        }
        else
        {
            observations.Add("InvalidPrefix threw " + exceptionType);
        }
    }
}
