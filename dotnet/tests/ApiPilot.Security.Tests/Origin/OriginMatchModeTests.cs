// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/OriginMatchModeTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the OriginMatchMode enum surface
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Origin
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginMatchMode.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;

namespace ApiPilot.Security.Tests.Origin;

/// <summary>
/// Contract tests for the OriginMatchMode enum. Verifies the three
/// named members and that Exact is the default value.
/// </summary>
[TestClass]
public sealed class OriginMatchModeTests
{
    /// <summary>The enum has exactly three named members.</summary>
    [Test]
    public void Enum_HasThreeMembers()
    {
        var values = Enum.GetValues<OriginMatchMode>();
        TestAssert.Equal(3, values.Length);
        TestAssert.True(Array.IndexOf(values, OriginMatchMode.Exact) >= 0);
        TestAssert.True(Array.IndexOf(values, OriginMatchMode.AnyPortSameHost) >= 0);
        TestAssert.True(Array.IndexOf(values, OriginMatchMode.SameSite) >= 0);
    }

    /// <summary>Exact is the default value.</summary>
    [Test]
    public void Enum_ExactIsDefault()
    {
        TestAssert.Equal(OriginMatchMode.Exact, default(OriginMatchMode));
    }

    /// <summary>Each named member is defined.</summary>
    [Test]
    public void Enum_EveryMember_IsDefined()
    {
        TestAssert.True(Enum.IsDefined(OriginMatchMode.Exact));
        TestAssert.True(Enum.IsDefined(OriginMatchMode.AnyPortSameHost));
        TestAssert.True(Enum.IsDefined(OriginMatchMode.SameSite));
    }
}

