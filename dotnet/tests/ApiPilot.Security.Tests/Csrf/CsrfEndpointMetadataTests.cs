// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfEndpointMetadataTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for CsrfPolicy and CsrfEndpointMetadata
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfEndpointMetadata.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for the CsrfPolicy enum and the CsrfEndpointMetadata
/// record. Verifies the three named members, the default value, and
/// record equality.
/// </summary>
[TestClass]
public sealed class CsrfEndpointMetadataTests
{
    /// <summary>The CsrfPolicy enum has exactly three named members.</summary>
    [Test]
    public void CsrfPolicy_HasThreeMembers()
    {
        var values = Enum.GetValues<CsrfPolicy>();
        TestAssert.Equal(3, values.Length);
        TestAssert.True(Array.IndexOf(values, CsrfPolicy.UseGlobal) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfPolicy.Require) >= 0);
        TestAssert.True(Array.IndexOf(values, CsrfPolicy.Skip) >= 0);
    }

    /// <summary>UseGlobal is the default value.</summary>
    [Test]
    public void CsrfPolicy_UseGlobalIsDefault()
    {
        TestAssert.Equal(CsrfPolicy.UseGlobal, default(CsrfPolicy));
    }

    /// <summary>Metadata carries the policy it was constructed with.</summary>
    [Test]
    public void Metadata_CarriesPolicy()
    {
        var meta = new CsrfEndpointMetadata(CsrfPolicy.Require);
        TestAssert.Equal(CsrfPolicy.Require, meta.Policy);
    }

    /// <summary>Two metadata records with the same policy are equal.</summary>
    [Test]
    public void Metadata_SamePolicy_AreEqual()
    {
        var a = new CsrfEndpointMetadata(CsrfPolicy.Skip);
        var b = new CsrfEndpointMetadata(CsrfPolicy.Skip);
        TestAssert.True(a == b);
    }

    /// <summary>Two metadata records with different policies are not equal.</summary>
    [Test]
    public void Metadata_DifferentPolicies_AreNotEqual()
    {
        var a = new CsrfEndpointMetadata(CsrfPolicy.Skip);
        var b = new CsrfEndpointMetadata(CsrfPolicy.Require);
        TestAssert.False(a == b);
    }
}

