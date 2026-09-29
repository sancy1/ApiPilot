// filepath: dotnet/tests/ApiPilot.Security.Tests/Configuration/CookieProfileOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the cookie profile startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Configuration, ApiPilot.Security.Cookies
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CookieProfileOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Cookies;

namespace ApiPilot.Security.Tests.Configuration;

/// <summary>
/// Contract tests for CookieProfileOptionsValidator. Verifies the
/// default profiles pass, and each failure mode is detected.
/// </summary>
[TestClass]
public sealed class CookieProfileOptionsValidatorTests
{
    private static CookieProfileOptionsValidator Validator() => new();

    private static CookieProfileOptions Valid() => new();

    /// <summary>The default configuration passes.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A null prefix fails.</summary>
    [Test]
    public void Validate_NullPrefix_Fails()
    {
        var options = Valid();
        options.NamePrefix = null!;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined prefix fails.</summary>
    [Test]
    public void Validate_UndefinedPrefix_Fails()
    {
        var options = Valid();
        options.NamePrefix = "__Custom-";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A null authentication profile fails.</summary>
    [Test]
    public void Validate_NullAuthProfile_Fails()
    {
        var options = Valid();
        options.AuthenticationProfile = null!;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>SameSite=None without Secure fails.</summary>
    [Test]
    public void Validate_SameSiteNoneWithoutSecure_Fails()
    {
        var options = Valid();
        options.AuthenticationProfile = new CookieProfile
        {
            Name = "auth",
            Secure = false,
            HttpOnly = true,
            SameSite = CookieSameSite.None,
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>__Host- prefix without Path=/ fails.</summary>
    [Test]
    public void Validate_HostPrefixWithNonRootPath_Fails()
    {
        var options = Valid();
        options.NamePrefix = HostPrefixValidator.HostPrefix;
        options.AuthenticationProfile = new CookieProfile
        {
            Name = "auth",
            Secure = true,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
            Path = "/app",
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A valid custom profile passes.</summary>
    [Test]
    public void Validate_CustomValidProfile_ReturnsSuccess()
    {
        var options = Valid();
        options.NamePrefix = HostPrefixValidator.HostPrefix;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }
}

