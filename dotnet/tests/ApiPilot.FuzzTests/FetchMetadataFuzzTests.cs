// filepath: dotnet/tests/ApiPilot.FuzzTests/FetchMetadataFuzzTests.cs
// layer: Fuzz | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Hostile-input tests for the Fetch Metadata evaluator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                FetchMetadataEvaluator, FetchMetadataOptions, DefaultHttpContext
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataEvaluator.cs, FetchMetadataOptions.cs
// -----------------------------------------------------------------------------
//
// THE FUZZ SET
//   The evaluator is a pure function over the request headers and the
//   options. These tests call it directly with curated header values and
//   explicit options. Two positive cases are included so an evaluator that
//   rejected everything would fail.

using ApiPilot.Security.Origin;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Fuzz tests for the Fetch Metadata evaluator.
/// </summary>
[TestClass]
public sealed class FetchMetadataFuzzTests
{
    private static DefaultHttpContext ContextWithSite(string? site)
    {
        var context = new DefaultHttpContext();
        if (site is not null)
        {
            context.Request.Headers["Sec-Fetch-Site"] = site;
        }
        return context;
    }

    /// <summary>The Off profile passes even a cross-site value.</summary>
    [Test]
    public void ProfileOff_PassesEvenCrossSite()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Off };
        var context = ContextWithSite("cross-site");
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(context, options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Compat allows a missing header when AllowMissingHeaders is true.</summary>
    [Test]
    public void Compat_AllowsMissingHeader()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Compat,
            AllowMissingHeaders = true
        };
        var context = ContextWithSite(null);
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(context, options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Strict rejects a present cross-site value.</summary>
    [Test]
    public void Strict_RejectsCrossSiteValue()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var context = ContextWithSite("cross-site");
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(context, options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("DisallowedSiteValue", reason);
    }

    /// <summary>Strict accepts a present same-origin value.</summary>
    [Test]
    public void Strict_AllowsSameOrigin()
    {
        var options = new FetchMetadataOptions { Profile = FetchMetadataProfile.Strict };
        var context = ContextWithSite("same-origin");
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(context, options, out _);
        TestAssert.True(ok);
    }

    /// <summary>Strict rejects a malformed header when missing is not allowed.</summary>
    [Test]
    public void Strict_RejectsMalformedHeaderWhenMissingNotAllowed()
    {
        var options = new FetchMetadataOptions
        {
            Profile = FetchMetadataProfile.Strict,
            AllowMissingHeaders = false
        };
        var context = ContextWithSite("garbage");
        var ok = FetchMetadataEvaluator.IsRequestAcceptable(context, options, out var reason);
        TestAssert.False(ok);
        TestAssert.Equal("MalformedSiteHeader", reason);
    }
}

