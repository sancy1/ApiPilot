// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfBindingFailureReasonTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfBindingFailureReason enum surface
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfBindingFailureReason.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for the CsrfBindingFailureReason enum. Verifies the five
/// named members are present and distinct, and that None is the default.
/// </summary>
[TestClass]
public sealed class CsrfBindingFailureReasonTests
{
    /// <summary>All five named members are present.</summary>
    [Test]
    public void Enum_HasAllFiveNamedMembers()
    {
        var values = Enum.GetValues<CsrfBindingFailureReason>();
        TestAssert.Equal(5, values.Length);
        TestAssert.True(Array.IndexOf(values, CsrfBindingFailureReason.None) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfBindingFailureReason.NoSubject) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfBindingFailureReason.NoSession) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfBindingFailureReason.NoConfiguredSource) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfBindingFailureReason.ProviderFailure) >= 0);
    }

    /// <summary>None is the default value.</summary>
    [Test]
    public void Enum_NoneIsDefaultValue()
    {
        TestAssert.Equal(CsrfBindingFailureReason.None, default(CsrfBindingFailureReason));
    }

    /// <summary>Each named member has a distinct numeric value.</summary>
    [Test]
    public void Enum_EachMember_HasDistinctValue()
    {
        var none = (int)CsrfBindingFailureReason.None;
        var noSubject = (int)CsrfBindingFailureReason.NoSubject;
        var noSession = (int)CsrfBindingFailureReason.NoSession;
        var noConfiguredSource = (int)CsrfBindingFailureReason.NoConfiguredSource;
        var providerFailure = (int)CsrfBindingFailureReason.ProviderFailure;

        TestAssert.True(none != noSubject);
        TestAssert.True(noSubject != noSession);
        TestAssert.True(noSession != noConfiguredSource);
        TestAssert.True(noConfiguredSource != providerFailure);
    }

    /// <summary>Every named member is defined.</summary>
    [Test]
    public void Enum_EveryMember_IsDefined()
    {
        TestAssert.True(Enum.IsDefined(CsrfBindingFailureReason.None));
        TestAssert.True(Enum.IsDefined(CsrfBindingFailureReason.NoSubject));
        TestAssert.True(Enum.IsDefined(CsrfBindingFailureReason.NoSession));
        TestAssert.True(Enum.IsDefined(CsrfBindingFailureReason.NoConfiguredSource));
        TestAssert.True(Enum.IsDefined(CsrfBindingFailureReason.ProviderFailure));
    }
}

