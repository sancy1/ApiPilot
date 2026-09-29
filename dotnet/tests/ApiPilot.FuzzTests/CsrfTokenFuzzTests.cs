// filepath: dotnet/tests/ApiPilot.FuzzTests/CsrfTokenFuzzTests.cs
// layer: Fuzz | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Hostile-input tests for the CSRF token plaintext codec
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, CsrfTokenFormat
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfTokenFormat.cs, CsrfTokenParseResult.cs
// -----------------------------------------------------------------------------
//
// THE FUZZ SET
//   The codec is a pure function. These tests call it directly, with no
//   host. Each hostile shape is deterministic and curated, not random. The
//   suite includes a positive case, so a codec that rejected everything
//   would fail.

using ApiPilot.Security.Csrf;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Fuzz tests for the CSRF token plaintext codec.
/// </summary>
[TestClass]
public sealed class CsrfTokenFuzzTests
{
    /// <summary>An empty payload is rejected.</summary>
    [Test]
    public void Decode_RejectsEmpty()
    {
        var ok = CsrfTokenFormat.TryDecode(string.Empty, out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A payload with no delimiter is rejected.</summary>
    [Test]
    public void Decode_RejectsNoDelimiter()
    {
        var ok = CsrfTokenFormat.TryDecode("1234567890", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A payload with adjacent delimiters is rejected.</summary>
    [Test]
    public void Decode_RejectsAdjacentDelimiters()
    {
        var ok = CsrfTokenFormat.TryDecode("123||binding", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A payload with a third delimiter is rejected.</summary>
    [Test]
    public void Decode_RejectsThirdDelimiter()
    {
        var ok = CsrfTokenFormat.TryDecode("123|random|bind|extra", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A payload whose issued-at part is not numeric is rejected.</summary>
    [Test]
    public void Decode_RejectsNonNumericIssuedAt()
    {
        var ok = CsrfTokenFormat.TryDecode("abc|random|binding", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A payload whose issued-at part is out of range is rejected.</summary>
    [Test]
    public void Decode_RejectsOutOfRangeIssuedAt()
    {
        var ok = CsrfTokenFormat.TryDecode("9999999999999|random|binding", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>A valid payload round-trips through Encode and TryDecode.</summary>
    [Test]
    public void Decode_AcceptsValidPayload()
    {
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(1700000000);
        var payload = CsrfTokenFormat.Encode(issuedAt, "cmFuZG9t", "binding-fp");
        TestAssert.True(payload.Length > 0, "expected a non-empty payload");

        var ok = CsrfTokenFormat.TryDecode(payload, out var decodedAt, out var random, out var binding);
        TestAssert.True(ok, "expected the valid payload to decode");
        TestAssert.Equal(issuedAt.ToUnixTimeSeconds(), decodedAt.ToUnixTimeSeconds());
        TestAssert.Equal("cmFuZG9t", random);
        TestAssert.Equal("binding-fp", binding);
    }
}

