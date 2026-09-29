// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfTokenTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfToken value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfToken.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfToken. Verifies construction, rejection of
/// invalid values, structural equality, and the ToString redaction that
/// prevents accidental logging of the wire value.
/// </summary>
[TestClass]
public sealed class CsrfTokenTests
{
    /// <summary>From preserves the wire value.</summary>
    [Test]
    public void From_ValidValue_PreservesWireValue()
    {
        var token = CsrfToken.From("abc.def.ghi");
        TestAssert.Equal("abc.def.ghi", token.Value);
    }

    /// <summary>From rejects null.</summary>
    [Test]
    public void From_Null_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() => CsrfToken.From(null!));
    }

    /// <summary>From rejects empty.</summary>
    [Test]
    public void From_Empty_Throws()
    {
        TestAssert.Throws<ArgumentException>(() => CsrfToken.From(""));
    }

    /// <summary>ToString does not expose the wire value.</summary>
    [Test]
    public void ToString_ReturnsRedactedPlaceholder()
    {
        var token = CsrfToken.From("super-secret-wire-value");
        var rendered = token.ToString();
        TestAssert.Equal("[csrf-token]", rendered);
        TestAssert.False(rendered.Contains("super-secret-wire-value", StringComparison.Ordinal));
    }

    /// <summary>Two tokens with the same value are structurally equal.</summary>
    [Test]
    public void Equality_SameValue_AreEqual()
    {
        var a = CsrfToken.From("abc.def.ghi");
        var b = CsrfToken.From("abc.def.ghi");
        TestAssert.True(a == b);
        TestAssert.True(a.Equals(b));
    }
}

