// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/OriginValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the RFC 6454 origin comparison
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Origin
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;

namespace ApiPilot.Security.Tests.Origin;

/// <summary>
/// Contract tests for OriginValidator. Exercises parsing, exact
/// matching, the deferred SameSite mode, and the malformed-input
/// behavior.
/// </summary>
[TestClass]
public sealed class OriginValidatorTests
{
    /// <summary>A well-formed origin parses.</summary>
    [Test]
    public void TryParseOrigin_WellFormed_ReturnsTrue()
    {
        var ok = OriginValidator.TryParseOrigin("https://example.com", out var parts);
        TestAssert.True(ok);
        TestAssert.Equal("https", parts.Scheme);
        TestAssert.Equal("example.com", parts.Host);
        TestAssert.Equal(443, parts.Port);
    }

    /// <summary>An explicit port overrides the scheme default.</summary>
    [Test]
    public void TryParseOrigin_ExplicitPort_UsesIt()
    {
        OriginValidator.TryParseOrigin("https://example.com:8443", out var parts);
        TestAssert.Equal(8443, parts.Port);
    }

    /// <summary>The host is lowercased on parse.</summary>
    [Test]
    public void TryParseOrigin_UppercaseHost_Lowercases()
    {
        OriginValidator.TryParseOrigin("https://EXAMPLE.com", out var parts);
        TestAssert.Equal("example.com", parts.Host);
    }

    /// <summary>The literal null origin is rejected.</summary>
    [Test]
    public void TryParseOrigin_NullLiteral_ReturnsFalse()
    {
        var ok = OriginValidator.TryParseOrigin("null", out _);
        TestAssert.False(ok);
    }

    /// <summary>An empty string is rejected.</summary>
    [Test]
    public void TryParseOrigin_Empty_ReturnsFalse()
    {
        var ok = OriginValidator.TryParseOrigin("", out _);
        TestAssert.False(ok);
    }

    /// <summary>An origin with a path is rejected.</summary>
    [Test]
    public void TryParseOrigin_WithPath_ReturnsFalse()
    {
        var ok = OriginValidator.TryParseOrigin("https://example.com/foo", out _);
        TestAssert.False(ok);
    }

    /// <summary>Exact mode matches on scheme, host, and port.</summary>
    [Test]
    public void OriginMatches_Exact_SameOrigin_ReturnsTrue()
    {
        var ok = OriginValidator.OriginMatches(
            "https://example.com", "https://example.com", OriginMatchMode.Exact);
        TestAssert.True(ok);
    }

    /// <summary>Exact mode rejects a different port.</summary>
    [Test]
    public void OriginMatches_Exact_DifferentPort_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "https://example.com:8443", "https://example.com:9443", OriginMatchMode.Exact);
        TestAssert.False(ok);
    }

    /// <summary>Exact mode rejects a different scheme.</summary>
    [Test]
    public void OriginMatches_Exact_DifferentScheme_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "http://example.com", "https://example.com", OriginMatchMode.Exact);
        TestAssert.False(ok);
    }

    /// <summary>Exact mode rejects a different host.</summary>
    [Test]
    public void OriginMatches_Exact_DifferentHost_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "https://sub.example.com", "https://example.com", OriginMatchMode.Exact);
        TestAssert.False(ok);
    }

    /// <summary>A missing port is normalized to the scheme default.</summary>
    [Test]
    public void OriginMatches_Exact_DefaultPortNormalized_ReturnsTrue()
    {
        var ok = OriginValidator.OriginMatches(
            "https://example.com:443", "https://example.com", OriginMatchMode.Exact);
        TestAssert.True(ok);
    }

    /// <summary>AnyPortSameHost matches a different port on the same host.</summary>
    [Test]
    public void OriginMatches_AnyPortSameHost_DifferentPort_ReturnsTrue()
    {
        var ok = OriginValidator.OriginMatches(
            "https://example.com:8443", "https://example.com:9443", OriginMatchMode.AnyPortSameHost);
        TestAssert.True(ok);
    }

    /// <summary>AnyPortSameHost rejects a different host.</summary>
    [Test]
    public void OriginMatches_AnyPortSameHost_DifferentHost_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "https://a.example.com", "https://b.example.com", OriginMatchMode.AnyPortSameHost);
        TestAssert.False(ok);
    }

    /// <summary>SameSite mode is not implemented and returns false.</summary>
    [Test]
    public void OriginMatches_SameSite_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "https://a.example.com", "https://b.example.com", OriginMatchMode.SameSite);
        TestAssert.False(ok);
    }

    /// <summary>Malformed request origin returns false.</summary>
    [Test]
    public void OriginMatches_MalformedRequest_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "not-an-origin", "https://example.com", OriginMatchMode.Exact);
        TestAssert.False(ok);
    }

    /// <summary>Malformed allowed origin returns false.</summary>
    [Test]
    public void OriginMatches_MalformedAllowed_ReturnsFalse()
    {
        var ok = OriginValidator.OriginMatches(
            "https://example.com", "not-an-origin", OriginMatchMode.Exact);
        TestAssert.False(ok);
    }
}

