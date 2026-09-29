// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/FetchMetadataEvaluatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the Fetch Metadata evaluator matrix
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Origin, Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataEvaluator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Tests.Origin;

/// <summary>
/// Contract tests for FetchMetadataEvaluator. The matrix covers the
/// three profiles, the four site values, header presence, and the
/// AllowMissingHeaders correction.
/// </summary>
[TestClass]
public sealed class FetchMetadataEvaluatorTests
{
    private static DefaultHttpContext Context(string? site, string? mode = null)
    {
        var ctx = new DefaultHttpContext();
        if (site is not null) { ctx.Request.Headers["Sec-Fetch-Site"] = site; }
        if (mode is not null) { ctx.Request.Headers["Sec-Fetch-Mode"] = mode; }
        return ctx;
    }

    /// <summary>Off passes even with a cross-site value.</summary>
    [Test]
    public void Off_CrossSite_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Off };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("cross-site"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Compat with a same-origin value passes.</summary>
    [Test]
    public void Compat_SameOrigin_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Compat };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("same-origin"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Compat with a missing header passes (AllowMissingHeaders default true).</summary>
    [Test]
    public void Compat_MissingHeader_PassesByDefault()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Compat };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context(null), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Compat with cross-site is rejected.</summary>
    [Test]
    public void Compat_CrossSite_Rejected()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Compat };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("cross-site"), options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("DisallowedSiteValue", reason);
    }

    /// <summary>
    /// Strict with cross-site present is rejected even when
    /// AllowMissingHeaders is true. This is the Q8.2 correction.
    /// </summary>
    [Test]
    public void Strict_CrossSiteWithAllowMissingHeadersTrue_Rejected()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = true,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("cross-site"), options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("DisallowedSiteValue", reason);
    }

    /// <summary>Strict with cross-site and AllowMissingHeaders false is rejected.</summary>
    [Test]
    public void Strict_CrossSiteWithAllowMissingHeadersFalse_Rejected()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = false,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("cross-site"), options, out _);
        TestAssert.False(ok);
    }

    /// <summary>Strict with a missing header and AllowMissingHeaders true passes.</summary>
    [Test]
    public void Strict_MissingHeader_AllowTrue_Passes()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = true,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context(null), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Strict with a missing header and AllowMissingHeaders false is rejected.</summary>
    [Test]
    public void Strict_MissingHeader_AllowFalse_Rejected()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = false,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context(null), options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("MissingSiteHeader", reason);
    }

    /// <summary>Strict with a malformed value and AllowMissingHeaders true passes.</summary>
    [Test]
    public void Strict_MalformedHeader_AllowTrue_Passes()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = true,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("not-a-site-value"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Strict with a malformed value and AllowMissingHeaders false is rejected.</summary>
    [Test]
    public void Strict_MalformedHeader_AllowFalse_Rejected()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = false,
        };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("not-a-site-value"), options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("MalformedSiteHeader", reason);
    }

    /// <summary>Strict with same-site passes.</summary>
    [Test]
    public void Strict_SameSite_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("same-site"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Strict with none passes.</summary>
    [Test]
    public void Strict_NoneValue_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("none"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>An allowed mode passes.</summary>
    [Test]
    public void Strict_AllowedMode_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(
            Context("same-origin", "navigate"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>A disallowed mode is rejected.</summary>
    [Test]
    public void Strict_DisallowedMode_Rejected()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(
            Context("same-origin", "no-cors"), options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("DisallowedModeValue", reason);
    }

    /// <summary>A missing mode header is allowed when the site value passes.</summary>
    [Test]
    public void Strict_MissingMode_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("same-origin"), options, out _);
        TestAssert.True(ok);
    }

    /// <summary>An application that adds cross-site to the allow-list passes.</summary>
    [Test]
    public void Strict_CrossSiteExplicitlyAllowed_Passes()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        options.AllowedSiteValues.Add("cross-site");
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(Context("cross-site"), options, out _);
        TestAssert.True(ok);
    }
}

