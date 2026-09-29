// filepath: dotnet/tests/ApiPilot.FuzzTests/OriginFuzzTests.cs
// layer: Fuzz | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Hostile-input tests for the RFC 6454 origin validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, OriginValidator, OriginMatchMode
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginValidator.cs, OriginPolicyOptions.cs
// -----------------------------------------------------------------------------
//
// THE FUZZ SET
//   The validator is a pure function. It never throws for malformed input;
//   it returns false. These tests call it directly with curated hostile
//   shapes. A positive case is included so a validator that rejected
//   everything would fail.

using ApiPilot.Security.Origin;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Fuzz tests for the RFC 6454 origin validator.
/// </summary>
[TestClass]
public sealed class OriginFuzzTests
{
    /// <summary>An empty string is not a valid origin.</summary>
    [Test]
    public void Parse_RejectsEmptyString()
    {
        var ok = OriginValidator.TryParseOrigin(string.Empty, out _);
        TestAssert.False(ok);
    }

    /// <summary>The literal null origin is not usable.</summary>
    [Test]
    public void Parse_RejectsLiteralNull()
    {
        var ok = OriginValidator.TryParseOrigin("null", out _);
        TestAssert.False(ok);
    }

    /// <summary>A non-absolute string is not a valid origin.</summary>
    [Test]
    public void Parse_RejectsNonAbsolute()
    {
        var ok = OriginValidator.TryParseOrigin("not a url", out _);
        TestAssert.False(ok);
    }

    /// <summary>An origin carrying a path or query is rejected.</summary>
    [Test]
    public void Parse_RejectsPathOrQuery()
    {
        var ok = OriginValidator.TryParseOrigin("https://example.com/path?x=1", out _);
        TestAssert.False(ok);
    }

    /// <summary>SameSite match mode is not implemented and returns false.</summary>
    [Test]
    public void Matches_RejectsSameSiteMode()
    {
        var matched = OriginValidator.OriginMatches(
            "https://example.com",
            "https://example.com",
            OriginMatchMode.SameSite);
        TestAssert.False(matched);
    }

    /// <summary>A well-formed origin parses into normalized parts.</summary>
    [Test]
    public void Parse_AcceptsWellFormedOrigin()
    {
        var ok = OriginValidator.TryParseOrigin("https://example.com", out var parts);
        TestAssert.True(ok, "expected the well-formed origin to parse");
        TestAssert.Equal("https", parts.Scheme);
        TestAssert.Equal("example.com", parts.Host);
        TestAssert.Equal(443, parts.Port);
    }
}

