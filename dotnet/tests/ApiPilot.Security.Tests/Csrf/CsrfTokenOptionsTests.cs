// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfTokenOptionsTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfTokenOptions defaults and mutability
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfTokenOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfTokenOptions. Verifies the S2 through S5 defaults
/// and that each property is mutable through its public setter.
/// </summary>
[TestClass]
public sealed class CsrfTokenOptionsTests
{
    /// <summary>ProtectionPurpose defaults to the v1 purpose.</summary>
    [Test]
    public void Defaults_ProtectionPurpose_IsV1()
    {
        var options = new CsrfTokenOptions();
        TestAssert.Equal("ApiPilot.Csrf.v1", options.ProtectionPurpose);
    }

    /// <summary>TokenEntropyBytes defaults to 32.</summary>
    [Test]
    public void Defaults_TokenEntropyBytes_Is32()
    {
        var options = new CsrfTokenOptions();
        TestAssert.Equal(32, options.TokenEntropyBytes);
    }

    /// <summary>TokenLifetime defaults to two hours.</summary>
    [Test]
    public void Defaults_TokenLifetime_IsTwoHours()
    {
        var options = new CsrfTokenOptions();
        TestAssert.Equal(TimeSpan.FromHours(2), options.TokenLifetime);
    }

    /// <summary>TimeProvider defaults to TimeProvider.System.</summary>
    [Test]
    public void Defaults_TimeProvider_IsSystem()
    {
        var options = new CsrfTokenOptions();
        TestAssert.True(ReferenceEquals(TimeProvider.System, options.TimeProvider));
    }

    /// <summary>Every property is mutable through its setter.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new CsrfTokenOptions
        {
            ProtectionPurpose = "ApiPilot.Csrf.v2",
            TokenEntropyBytes = 64,
            TokenLifetime = TimeSpan.FromMinutes(30),
        };

        TestAssert.Equal("ApiPilot.Csrf.v2", options.ProtectionPurpose);
        TestAssert.Equal(64, options.TokenEntropyBytes);
        TestAssert.Equal(TimeSpan.FromMinutes(30), options.TokenLifetime);
    }
}

