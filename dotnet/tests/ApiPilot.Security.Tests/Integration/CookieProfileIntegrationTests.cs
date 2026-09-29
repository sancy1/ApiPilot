// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/CookieProfileIntegrationTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Integration tests that prove cookie profile validation fails closed at startup
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : InProcessHost, TestAssert, ApiPilot.Security.Cookies
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CookieProfileOptionsValidator.cs, ApiPilotCookiesExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Cookies;
using ApiPilot.Security.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Integration tests for the cookie profile startup validation. A valid
/// configuration starts the host. Each insecure configuration fails the
/// host at startup with a message that names the offending property.
/// </summary>
[TestClass]
public sealed class CookieProfileIntegrationTests
{
    /// <summary>A valid configuration starts the host.</summary>
    [Test]
    public async Task StartHost_ValidConfiguration_StartsSuccessfully()
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddApiPilotCookies();
            },
            configureApp: app => { });

        TestAssert.NotNull(host);
    }

    /// <summary>SameSite=None without Secure fails startup.</summary>
    [Test]
    public async Task StartHost_SameSiteNoneWithoutSecure_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotCookies(o =>
                    {
                        o.AuthenticationProfile = new CookieProfile
                        {
                            Name = "auth",
                            Secure = false,
                            HttpOnly = true,
                            SameSite = CookieSameSite.None,
                        };
                    });
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw.");
        TestAssert.True(message.Contains("SameSite", StringComparison.Ordinal),
            "The failure message should name SameSite.");
    }

    /// <summary>An __Host- prefix with a non-root Path fails startup.</summary>
    [Test]
    public async Task StartHost_HostPrefixWithNonRootPath_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotCookies(o =>
                    {
                        o.NamePrefix = HostPrefixValidator.HostPrefix;
                        o.AuthenticationProfile = new CookieProfile
                        {
                            Name = "auth",
                            Secure = true,
                            HttpOnly = true,
                            SameSite = CookieSameSite.Lax,
                            Path = "/app",
                        };
                    });
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw.");
        TestAssert.True(message.Contains("__Host-", StringComparison.Ordinal),
            "The failure message should mention the __Host- prefix rule.");
    }

    /// <summary>An unknown prefix fails startup.</summary>
    [Test]
    public async Task StartHost_UnknownPrefix_ThrowsOnStartup()
    {
        var threw = false;
        var message = string.Empty;
        try
        {
            await using var host = await InProcessHost.StartAsync(
                configureServices: builder =>
                {
                    builder.Services.AddApiPilotCookies(o =>
                    {
                        o.NamePrefix = "__Custom-";
                    });
                },
                configureApp: app => { });
        }
        catch (Exception ex)
        {
            message = ex.ToString();
            threw = true;
        }
        TestAssert.True(threw, "Expected the host start to throw.");
        TestAssert.True(message.Contains("NamePrefix", StringComparison.Ordinal),
            "The failure message should name NamePrefix.");
    }
}

