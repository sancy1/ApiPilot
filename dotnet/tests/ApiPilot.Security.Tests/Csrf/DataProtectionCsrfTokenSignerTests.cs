// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/DataProtectionCsrfTokenSignerTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the default Data Protection CSRF token signer
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test-local FakeTimeProvider)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.DataProtection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : DataProtectionCsrfTokenSigner.cs, ICsrfTokenSigner.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.DataProtection;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for DataProtectionCsrfTokenSigner. Exercises the real
/// signer through real Data Protection with a controlled clock. Verifies
/// the Phase 2.1 exit criteria: round-trip, tamper rejection, cross-session
/// rejection, expiry, purpose isolation, and the input contracts.
/// </summary>
[TestClass]
public sealed class DataProtectionCsrfTokenSignerTests
{
    private static DataProtectionCsrfTokenSigner BuildSigner(
        CsrfTokenOptions? options = null)
    {
        var provider = DataProtectionProvider.Create("ApiPilot.Security.Tests");
        var opts = options ?? new CsrfTokenOptions();
        return new DataProtectionCsrfTokenSigner(provider, opts);
    }

    /// <summary>Sign then validate with the same binding returns Ok.</summary>
    [Test]
    public void Sign_ThenValidate_ReturnsOk()
    {
        var signer = BuildSigner();
        var token = signer.Sign("session-42");
        var result = signer.TryValidate(token, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.Ok, result);
    }

    /// <summary>The wire value is not the plaintext payload.</summary>
    [Test]
    public void Sign_ProducesOpaqueWireValue()
    {
        var signer = BuildSigner();
        var token = signer.Sign("session-42");
        // The wire value must not contain the delimiter that separates the
        // three payload parts, because the delimiter only exists inside the
        // protection boundary.
        TestAssert.False(token.Value.Contains('|', StringComparison.Ordinal));
    }

    /// <summary>Two signs with the same binding produce different tokens.</summary>
    [Test]
    public void Sign_ProducesUniqueTokens()
    {
        var signer = BuildSigner();
        var a = signer.Sign("session-42");
        var b = signer.Sign("session-42");
        TestAssert.NotEqual(a.Value, b.Value);
    }

    /// <summary>A tampered token is rejected with InvalidSignature.</summary>
    [Test]
    public void TryValidate_TamperedToken_ReturnsInvalidSignature()
    {
        var signer = BuildSigner();
        var token = signer.Sign("session-42");
        // Flip one character in the middle of the wire value.
        var chars = token.Value.ToCharArray();
        var mid = chars.Length / 2;
        chars[mid] = chars[mid] == 'A' ? 'B' : 'A';
        var tampered = CsrfToken.From(new string(chars));
        var result = signer.TryValidate(tampered, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.InvalidSignature, result);
    }

    /// <summary>A token from a different key ring is rejected.</summary>
    [Test]
    public void TryValidate_TokenFromDifferentKeyRing_ReturnsInvalidSignature()
    {
        var signerA = new DataProtectionCsrfTokenSigner(
            DataProtectionProvider.Create("ring-A"),
            new CsrfTokenOptions());
        var signerB = new DataProtectionCsrfTokenSigner(
            DataProtectionProvider.Create("ring-B"),
            new CsrfTokenOptions());

        var tokenFromA = signerA.Sign("session-42");
        var result = signerB.TryValidate(tokenFromA, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.InvalidSignature, result);
    }

    /// <summary>A valid token with a different binding is rejected with WrongSession.</summary>
    [Test]
    public void TryValidate_WrongBinding_ReturnsWrongSession()
    {
        var signer = BuildSigner();
        var token = signer.Sign("session-42");
        var result = signer.TryValidate(token, "session-99");
        TestAssert.Equal(CsrfTokenParseResult.WrongSession, result);
    }

    /// <summary>An expired token is rejected with Expired.</summary>
    [Test]
    public void TryValidate_ExpiredToken_ReturnsExpired()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));
        var options = new CsrfTokenOptions
        {
            TimeProvider = clock,
            TokenLifetime = TimeSpan.FromMinutes(30),
        };
        var signer = BuildSigner(options);
        var token = signer.Sign("session-42");

        // Advance the clock past the lifetime.
        clock.Advance(TimeSpan.FromMinutes(31));

        var result = signer.TryValidate(token, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.Expired, result);
    }

    /// <summary>A token within its lifetime is accepted.</summary>
    [Test]
    public void TryValidate_NotYetExpired_ReturnsOk()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 25, 12, 0, 0, TimeSpan.Zero));
        var options = new CsrfTokenOptions
        {
            TimeProvider = clock,
            TokenLifetime = TimeSpan.FromMinutes(30),
        };
        var signer = BuildSigner(options);
        var token = signer.Sign("session-42");

        clock.Advance(TimeSpan.FromMinutes(29));

        var result = signer.TryValidate(token, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.Ok, result);
    }

    /// <summary>Two different purposes cannot validate each other's tokens.</summary>
    [Test]
    public void DifferentPurposes_CannotValidateEachOthersTokens()
    {
        var provider = DataProtectionProvider.Create("ApiPilot.Security.Tests");
        var optionsA = new CsrfTokenOptions { ProtectionPurpose = "ApiPilot.Csrf.v1" };
        var optionsB = new CsrfTokenOptions { ProtectionPurpose = "ApiPilot.Csrf.v2" };

        var signerA = new DataProtectionCsrfTokenSigner(provider, optionsA);
        var signerB = new DataProtectionCsrfTokenSigner(provider, optionsB);

        var tokenA = signerA.Sign("session-42");
        var result = signerB.TryValidate(tokenA, "session-42");
        TestAssert.Equal(CsrfTokenParseResult.InvalidSignature, result);
    }

    /// <summary>TryValidate rejects a null token.</summary>
    [Test]
    public void TryValidate_NullToken_Throws()
    {
        var signer = BuildSigner();
        TestAssert.Throws<ArgumentNullException>(() =>
            signer.TryValidate(null!, "session-42"));
    }

    /// <summary>TryValidate rejects an empty binding.</summary>
    [Test]
    public void TryValidate_EmptyBinding_Throws()
    {
        var signer = BuildSigner();
        var token = signer.Sign("session-42");
        TestAssert.Throws<ArgumentException>(() =>
            signer.TryValidate(token, ""));
    }

    /// <summary>Sign rejects a null binding.</summary>
    [Test]
    public void Sign_NullBinding_Throws()
    {
        var signer = BuildSigner();
        TestAssert.Throws<ArgumentNullException>(() =>
            signer.Sign(null!));
    }

    /// <summary>
    /// A test-only TimeProvider with a mutable clock. The current time is
    /// advanced by calling Advance. The class is nested so that only this
    /// test file uses it; a later phase can promote it to a shared helper
    /// when more than one test class needs it.
    /// </summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private DateTimeOffset _now;

        public FakeTimeProvider(DateTimeOffset start)
        {
            _now = start;
        }

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta)
        {
            _now = _now.Add(delta);
        }
    }
}

