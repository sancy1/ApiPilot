// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfTokenFormatTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfTokenFormat payload codec
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfTokenFormat.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfTokenFormat. Verifies the payload codec's
/// round-trip, its rejection of pipe-bearing inputs, and its
/// structural validation on decode.
/// </summary>
[TestClass]
public sealed class CsrfTokenFormatTests
{
    /// <summary>Encode then TryDecode preserves all three parts.</summary>
    [Test]
    public void Encode_Decode_RoundTripsAllParts()
    {
        var issuedAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
        var random = "abcdefgh";
        var binding = "session-42";

        var payload = CsrfTokenFormat.Encode(issuedAt, random, binding);
        var ok = CsrfTokenFormat.TryDecode(payload, out var decodedAt, out var decodedRandom, out var decodedBinding);

        TestAssert.True(ok);
        TestAssert.Equal(issuedAt, decodedAt);
        TestAssert.Equal(random, decodedRandom);
        TestAssert.Equal(binding, decodedBinding);
    }

    /// <summary>Encode rejects a non-UTC DateTimeOffset.</summary>
    [Test]
    public void Encode_NonUtcOffset_Throws()
    {
        var issuedAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.FromHours(2));
        TestAssert.Throws<ArgumentException>(() =>
            CsrfTokenFormat.Encode(issuedAt, "abc", "session-42"));
    }

    /// <summary>Encode rejects a random value containing the delimiter.</summary>
    [Test]
    public void Encode_RandomWithPipe_Throws()
    {
        var issuedAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
        TestAssert.Throws<ArgumentException>(() =>
            CsrfTokenFormat.Encode(issuedAt, "ab|cd", "session-42"));
    }

    /// <summary>Encode rejects a binding containing the delimiter.</summary>
    [Test]
    public void Encode_BindingWithPipe_Throws()
    {
        var issuedAt = new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero);
        TestAssert.Throws<ArgumentException>(() =>
            CsrfTokenFormat.Encode(issuedAt, "abcd", "sess|ion"));
    }

    /// <summary>TryDecode returns false for an empty payload.</summary>
    [Test]
    public void TryDecode_Empty_ReturnsFalse()
    {
        var ok = CsrfTokenFormat.TryDecode("", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>TryDecode returns false when a part is missing.</summary>
    [Test]
    public void TryDecode_MissingPart_ReturnsFalse()
    {
        var ok = CsrfTokenFormat.TryDecode("123|abc", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>TryDecode returns false when the issued-at is not an integer.</summary>
    [Test]
    public void TryDecode_NonNumericIssuedAt_ReturnsFalse()
    {
        var ok = CsrfTokenFormat.TryDecode("not-a-number|abc|session-42", out _, out _, out _);
        TestAssert.False(ok);
    }

    /// <summary>TryDecode returns false when a third pipe is present.</summary>
    [Test]
    public void TryDecode_ExtraPipe_ReturnsFalse()
    {
        var ok = CsrfTokenFormat.TryDecode("123|abc|session|extra", out _, out _, out _);
        TestAssert.False(ok);
    }
}

