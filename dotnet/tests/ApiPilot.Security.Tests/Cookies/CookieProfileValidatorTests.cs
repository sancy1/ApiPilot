// filepath: dotnet/tests/ApiPilot.Security.Tests/Cookies/CookieProfileValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the per-profile cookie validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Cookies
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CookieProfileValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Cookies;

namespace ApiPilot.Security.Tests.Cookies;

/// <summary>
/// Contract tests for CookieProfileValidator. Exercises each failure mode
/// and the happy path.
/// </summary>
[TestClass]
public sealed class CookieProfileValidatorTests
{
    private static CookieProfile Profile(
        string name = "auth",
        bool secure = true,
        bool httpOnly = true,
        CookieSameSite sameSite = CookieSameSite.Lax,
        string path = "/",
        string? domain = null)
    {
        return new CookieProfile
        {
            Name = name,
            Secure = secure,
            HttpOnly = httpOnly,
            SameSite = sameSite,
            Path = path,
            Domain = domain,
        };
    }

    private static List<string> Validate(CookieProfile p, string? effectiveName = null)
    {
        var failures = new List<string>();
        CookieProfileValidator.Validate(effectiveName ?? p.Name, p, failures);
        return failures;
    }

    /// <summary>The auth default passes validation.</summary>
    [Test]
    public void Validate_AuthDefault_Passes()
    {
        var failures = Validate(AuthenticationCookieProfile.Default);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>The session default passes validation.</summary>
    [Test]
    public void Validate_SessionDefault_Passes()
    {
        var failures = Validate(AuthenticationCookieProfile.SessionDefault);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>The csrf default passes validation.</summary>
    [Test]
    public void Validate_CsrfDefault_Passes()
    {
        var failures = Validate(CsrfCookieProfile.Default);
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>An empty effective name fails.</summary>
    [Test]
    public void Validate_EmptyEffectiveName_Fails()
    {
        var failures = Validate(Profile(), effectiveName: "");
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A name with a space fails.</summary>
    [Test]
    public void Validate_NameWithSpace_Fails()
    {
        var failures = Validate(Profile(name: "auth cookie"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A name with a semicolon fails.</summary>
    [Test]
    public void Validate_NameWithSemicolon_Fails()
    {
        var failures = Validate(Profile(name: "auth;session"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A name with a comma fails.</summary>
    [Test]
    public void Validate_NameWithComma_Fails()
    {
        var failures = Validate(Profile(name: "auth,session"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>An empty Path fails.</summary>
    [Test]
    public void Validate_EmptyPath_Fails()
    {
        var failures = Validate(Profile(path: ""));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A Path without a leading slash fails.</summary>
    [Test]
    public void Validate_PathWithoutSlash_Fails()
    {
        var failures = Validate(Profile(path: "app"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>An empty Domain fails.</summary>
    [Test]
    public void Validate_EmptyDomain_Fails()
    {
        var failures = Validate(Profile(domain: ""));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A Domain with a scheme fails.</summary>
    [Test]
    public void Validate_DomainWithScheme_Fails()
    {
        var failures = Validate(Profile(domain: "https://example.com"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A Domain with a path fails.</summary>
    [Test]
    public void Validate_DomainWithPath_Fails()
    {
        var failures = Validate(Profile(domain: "example.com/path"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A Domain with a port fails.</summary>
    [Test]
    public void Validate_DomainWithPort_Fails()
    {
        var failures = Validate(Profile(domain: "example.com:8080"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>A Domain with a leading dot fails.</summary>
    [Test]
    public void Validate_DomainWithLeadingDot_Fails()
    {
        var failures = Validate(Profile(domain: ".example.com"));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>An undefined SameSite value fails.</summary>
    [Test]
    public void Validate_UndefinedSameSite_Fails()
    {
        var failures = Validate(Profile(sameSite: (CookieSameSite)999));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>SameSite=None without Secure fails.</summary>
    [Test]
    public void Validate_SameSiteNoneWithoutSecure_Fails()
    {
        var failures = Validate(Profile(secure: false, sameSite: CookieSameSite.None));
        TestAssert.True(failures.Count >= 1);
    }

    /// <summary>SameSite=None with Secure passes.</summary>
    [Test]
    public void Validate_SameSiteNoneWithSecure_Passes()
    {
        var failures = Validate(Profile(secure: true, sameSite: CookieSameSite.None));
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>HttpOnly=false passes when there is no prefix and no other rule.</summary>
    [Test]
    public void Validate_HttpOnlyFalse_Passes()
    {
        var failures = Validate(Profile(httpOnly: false));
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>A valid custom domain passes.</summary>
    [Test]
    public void Validate_ValidDomain_Passes()
    {
        var failures = Validate(Profile(domain: "example.com"));
        TestAssert.Equal(0, failures.Count);
    }

    /// <summary>A valid subdomain passes.</summary>
    [Test]
    public void Validate_ValidSubdomain_Passes()
    {
        var failures = Validate(Profile(domain: "api.example.com"));
        TestAssert.Equal(0, failures.Count);
    }
}

