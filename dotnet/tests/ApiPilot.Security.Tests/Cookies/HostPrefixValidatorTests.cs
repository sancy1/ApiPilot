// filepath: dotnet/tests/ApiPilot.Security.Tests/Cookies/HostPrefixValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the RFC 6265bis prefix rules
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Cookies
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : HostPrefixValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Cookies;

namespace ApiPilot.Security.Tests.Cookies;

/// <summary>
/// Contract tests for HostPrefixValidator. Verifies the RFC 6265bis
/// rules for __Host- and __Secure- prefixes.
/// </summary>
[TestClass]
public sealed class HostPrefixValidatorTests
{
    private static CookieProfile Profile(
        bool secure = true,
        string path = "/",
        string? domain = null)
    {
        return new CookieProfile
        {
            Name = "auth",
            Secure = secure,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
            Path = path,
            Domain = domain,
        };
    }

    /// <summary>__Host- with Secure + no Domain + Path=/ passes.</summary>
    [Test]
    public void HostPrefix_ValidCombination_NoFailures()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Host-auth", Profile(), failures);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>__Host- without Secure fails.</summary>
    [Test]
    public void HostPrefix_NoSecure_Fails()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Host-auth", Profile(secure: false), failures);
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>__Host- with a Domain fails.</summary>
    [Test]
    public void HostPrefix_WithDomain_Fails()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Host-auth", Profile(domain: "example.com"), failures);
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>__Host- with a non-root Path fails.</summary>
    [Test]
    public void HostPrefix_NonRootPath_Fails()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Host-auth", Profile(path: "/app"), failures);
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>__Secure- with Secure passes.</summary>
    [Test]
    public void SecurePrefix_WithSecure_NoFailures()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Secure-auth", Profile(), failures);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>__Secure- without Secure fails.</summary>
    [Test]
    public void SecurePrefix_NoSecure_Fails()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("__Secure-auth", Profile(secure: false), failures);
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A name with no prefix has no prefix rules.</summary>
    [Test]
    public void NoPrefix_NoFailures()
    {
        var failures = new List<string>();
        HostPrefixValidator.Validate("auth", Profile(secure: false, path: "/app", domain: "example.com"), failures);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>The empty string and the two prefixes are defined.</summary>
    [Test]
    public void IsDefinedPrefix_RecognizesTheThreeDefinedValues()
    {
        TestAssert.True(HostPrefixValidator.IsDefinedPrefix(""));
        TestAssert.True(HostPrefixValidator.IsDefinedPrefix("__Host-"));
        TestAssert.True(HostPrefixValidator.IsDefinedPrefix("__Secure-"));
    }

    /// <summary>An unknown prefix is not defined.</summary>
    [Test]
    public void IsDefinedPrefix_RejectsUnknownPrefixes()
    {
        TestAssert.False(HostPrefixValidator.IsDefinedPrefix("__host-"));
        TestAssert.False(HostPrefixValidator.IsDefinedPrefix("__Custom-"));
        TestAssert.False(HostPrefixValidator.IsDefinedPrefix("prefix"));
    }
}

