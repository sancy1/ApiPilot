// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/FetchMetadataProfileTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the FetchMetadataProfile enum surface
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Origin
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataProfile.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Origin;

namespace ApiPilot.Security.Tests.Origin;

/// <summary>
/// Contract tests for FetchMetadataProfile. Verifies the three members
/// and that Off is the default value.
/// </summary>
[TestClass]
public sealed class FetchMetadataProfileTests
{
    /// <summary>The enum has three named members.</summary>
    [Test]
    public void Enum_HasThreeMembers()
    {
        var values = Enum.GetValues<FetchMetadataProfile>();
        TestAssert.Equal(3, values.Length);
        TestAssert.True(Array.IndexOf(values, FetchMetadataProfile.Off) >= 0);
        TestAssert.True(Array.IndexOf(values, FetchMetadataProfile.Compat) >= 0);
        TestAssert.True(Array.IndexOf(values, FetchMetadataProfile.Strict) >= 0);
    }

    /// <summary>Off is the default value.</summary>
    [Test]
    public void Enum_OffIsDefault()
    {
        TestAssert.Equal(FetchMetadataProfile.Off, default(FetchMetadataProfile));
    }

    /// <summary>Each named member is defined.</summary>
    [Test]
    public void Enum_EveryMember_IsDefined()
    {
        TestAssert.True(Enum.IsDefined(FetchMetadataProfile.Off));
        TestAssert.True(Enum.IsDefined(FetchMetadataProfile.Compat));
        TestAssert.True(Enum.IsDefined(FetchMetadataProfile.Strict));
    }

    /// <summary>Each named member has a distinct numeric value.</summary>
    [Test]
    public void Enum_EachMember_HasDistinctValue()
    {
        TestAssert.True((int)FetchMetadataProfile.Off != (int)FetchMetadataProfile.Compat);
        TestAssert.True((int)FetchMetadataProfile.Compat != (int)FetchMetadataProfile.Strict);
        TestAssert.True((int)FetchMetadataProfile.Off != (int)FetchMetadataProfile.Strict);
    }
}

