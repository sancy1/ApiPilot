// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/SecurityDiagnosticsIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests for the security diagnostics through a real host
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.Security.Diagnostics,
//                ApiPilot.Security.DataProtection, ApiPilot.Security.Csrf,
//                ApiPilot.Security.Cookies, ApiPilot.Security.Origin,
//                Microsoft.Extensions.DependencyInjection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SecurityDiagnosticsHostedService.cs, ApiPilotSecurityDiagnosticsExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Cookies;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.DataProtection;
using ApiPilot.Security.Diagnostics;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests for the security diagnostics. A fully-configured
/// host starts; a host with a missing validator refuses to start.
/// </summary>
[TestClass]
public sealed class SecurityDiagnosticsIntegrationTests
{
    /// <summary>A fully-configured host starts successfully.</summary>
    [Test]
    public async Task StartHost_AllValidatorsRegistered_StartsSuccessfully()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCsrf();
                builder.Services.AddApiPilotCookies();
                builder.Services.AddApiPilotOriginPolicy();
                builder.Services.AddApiPilotFetchMetadata();
                builder.Services.AddApiPilotDataProtection();
                builder.Services.AddApiPilotSecurityDiagnostics();
            },
            configureApp: app => { });

        TestAssert.NotNull(host);
    }

    /// <summary>
    /// A host with a missing security validator refuses to start.
    /// The diagnostics hosted service throws on the fatal diagnostic.
    /// </summary>
    [Test]
    public async Task StartHost_MissingValidator_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotJson();
                    builder.Services.AddApiPilotDataProtection();
                    builder.Services.AddApiPilotSecurityDiagnostics();
                    // Deliberately do not register the CSRF, cookies,
                    // origin, or fetch metadata options and validators.
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw.");
        TestAssert.True(message.Contains("SEC002", StringComparison.Ordinal),
            "The failure message should carry the ValidatorMissing code SEC002.");
    }

    /// <summary>
    /// The in-memory Data Protection key ring produces the non-fatal
    /// SECW001 diagnostic that the diagnostics surface reports.
    /// </summary>
    [Test]
    public async Task Diagnostics_InMemoryKeyRing_ReportsWarning()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCsrf();
                builder.Services.AddApiPilotCookies();
                builder.Services.AddApiPilotOriginPolicy();
                builder.Services.AddApiPilotFetchMetadata();
                builder.Services.AddApiPilotDataProtection();
                builder.Services.AddApiPilotSecurityDiagnostics();
            },
            configureApp: app => { });

        var diagnostics = host.Services.GetRequiredService<ApiPilotSecurityDiagnostics>();
        var list = diagnostics.GetDiagnostics();

        var found = false;
        foreach (var d in list)
        {
            if (d.Code == DiagnosticCodes.InMemoryKeyRing
                && d.Level == SecurityDiagnosticLevel.Warning)
            {
                found = true;
                break;
            }
        }
        TestAssert.True(found, "Expected SECW001 in the diagnostics list.");
    }
}

