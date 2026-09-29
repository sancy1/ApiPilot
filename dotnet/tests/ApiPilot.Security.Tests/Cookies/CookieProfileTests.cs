// filepath: dotnet/tests/ApiPilot.Security.Tests/Cookies/CookieProfileTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CookieProfile value object and the three default presets
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Cookies
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CookieProfile.cs, AuthenticationCookieProfile.cs, CsrfCookieProfile.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Cookies;

namespace ApiPilot.Security.Tests.Cookies;

/// <summary>
/// Contract tests for CookieProfile and the three default presets. Verifies
/// the auth, session, and csrf defaults and record structural equality.
/// </summary>
[TestClass]
public sealed class CookieProfileTests
{
    /// <summary>The authentication default is Secure + HttpOnly + SameSite=Lax + Path=/.</summary>
    [Test]
    public void AuthenticationDefault_HasCorrectAttributes()
    {
        var profile = AuthenticationCookieProfile.Default;
        TestAssert.Equal("auth", profile.Name);
        TestAssert.True(profile.Secure);
        TestAssert.True(profile.HttpOnly);
        TestAssert.Equal(CookieSameSite.Lax, profile.SameSite);
        TestAssert.Equal("/", profile.Path);
        TestAssert.Null(profile.Domain);
    }

    /// <summary>The session default is Secure + HttpOnly + SameSite=Strict + Path=/.</summary>
    [Test]
    public void SessionDefault_HasCorrectAttributes()
    {
        var profile = AuthenticationCookieProfile.SessionDefault;
        TestAssert.Equal("session", profile.Name);
        TestAssert.True(profile.Secure);
        TestAssert.True(profile.HttpOnly);
        TestAssert.Equal(CookieSameSite.Strict, profile.SameSite);
        TestAssert.Equal("/", profile.Path);
        TestAssert.Null(profile.Domain);
    }

    /// <summary>The CSRF default is Secure + HttpOnly=false + SameSite=Lax.</summary>
    [Test]
    public void CsrfDefault_HasCorrectAttributes()
    {
        var profile = CsrfCookieProfile.Default;
        TestAssert.Equal("csrf", profile.Name);
        TestAssert.True(profile.Secure);
        TestAssert.False(profile.HttpOnly);
        TestAssert.Equal(CookieSameSite.Lax, profile.SameSite);
        TestAssert.Equal("/", profile.Path);
        TestAssert.Null(profile.Domain);
    }

    /// <summary>Two profiles with the same values are structurally equal.</summary>
    [Test]
    public void Profiles_SameValues_AreEqual()
    {
        var a = new CookieProfile
        {
            Name = "auth",
            Secure = true,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
        };
        var b = new CookieProfile
        {
            Name = "auth",
            Secure = true,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
        };
        TestAssert.True(a == b);
    }

    /// <summary>Two profiles with different names are not equal.</summary>
    [Test]
    public void Profiles_DifferentNames_AreNotEqual()
    {
        var a = new CookieProfile
        {
            Name = "auth",
            Secure = true,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
        };
        var b = new CookieProfile
        {
            Name = "session",
            Secure = true,
            HttpOnly = true,
            SameSite = CookieSameSite.Lax,
        };
        TestAssert.False(a == b);
    }

    /// <summary>CookieSameSite has four named members.</summary>
    [Test]
    public void CookieSameSite_HasFourMembers()
    {
        var values = Enum.GetValues<CookieSameSite>();
        TestAssert.Equal(4, values.Length);
        TestAssert.True(Array.IndexOf(values, CookieSameSite.Unspecified) >= 0);
        TestAssert.True(Array.IndexOf(values, CookieSameSite.Lax) >= 0);
        TestAssert.True(Array.IndexOf(values, CookieSameSite.Strict) >= 0);
        TestAssert.True(Array.IndexOf(values, CookieSameSite.None) >= 0);
    }
}

