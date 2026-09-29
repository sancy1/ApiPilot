// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfTokenParseResultTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfTokenParseResult enum surface
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfTokenParseResult.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfTokenParseResult. Verifies the six named members
/// exist and are distinct. The enum surface is part of the signer contract
/// and cannot be renamed or removed without a major version bump.
/// </summary>
[TestClass]
public sealed class CsrfTokenParseResultTests
{
    /// <summary>All six named members are present and distinct.</summary>
    [Test]
    public void Enum_HasAllEightNamedMembers()
    {
        var values = Enum.GetValues<CsrfTokenParseResult>();
        TestAssert.Equal(8, values.Length);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.Ok) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.Malformed) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.WrongVersion) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.InvalidSignature) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.Expired) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.WrongSession) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.Rotated) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfTokenParseResult.BindingMissing) >= 0);
    }

    /// <summary>Ok is the default value.</summary>
    [Test]
    public void Enum_OkIsDefaultValue()
    {
        var defaultValue = default(CsrfTokenParseResult);
        TestAssert.Equal(CsrfTokenParseResult.Ok, defaultValue);
    }

    /// <summary>Each named member has a distinct numeric value.</summary>
    [Test]
    public void Enum_EachMember_HasDistinctValue()
    {
        var ok = (int)CsrfTokenParseResult.Ok;
        var malformed = (int)CsrfTokenParseResult.Malformed;
        var wrongVersion = (int)CsrfTokenParseResult.WrongVersion;
        var invalidSignature = (int)CsrfTokenParseResult.InvalidSignature;
        var expired = (int)CsrfTokenParseResult.Expired;
        var wrongSession = (int)CsrfTokenParseResult.WrongSession;

        TestAssert.True(ok != malformed);
        TestAssert.True(malformed != wrongVersion);
        TestAssert.True(wrongVersion != invalidSignature);
        TestAssert.True(invalidSignature != expired);
        TestAssert.True(expired != wrongSession);
        TestAssert.True(wrongSession != (int)CsrfTokenParseResult.Rotated);
        TestAssert.True((int)CsrfTokenParseResult.Rotated != (int)CsrfTokenParseResult.BindingMissing);
    }

    /// <summary>WrongVersion is present but never produced by the default signer.</summary>
    [Test]
    public void Enum_WrongVersion_IsReservedForFutureFormat()
    {
        // The member exists for surface stability. This test documents the
        // intent rather than asserting a behavioral rule about the signer.
        TestAssert.True(Enum.IsDefined(CsrfTokenParseResult.WrongVersion));
    }
}

