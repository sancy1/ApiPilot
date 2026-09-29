// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfBootstrapOptionsTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfBootstrapOptions defaults and mutability
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfBootstrapOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for CsrfBootstrapOptions. Verifies the S10 and S11
/// defaults and that both properties are mutable.
/// </summary>
[TestClass]
public sealed class CsrfBootstrapOptionsTests
{
    /// <summary>CacheControl defaults to no-store.</summary>
    [Test]
    public void Defaults_CacheControl_IsNoStore()
    {
        var options = new CsrfBootstrapOptions();
        TestAssert.Equal("no-store", options.CacheControl);
    }

    /// <summary>Enabled defaults to true.</summary>
    [Test]
    public void Defaults_Enabled_IsTrue()
    {
        var options = new CsrfBootstrapOptions();
        TestAssert.True(options.Enabled);
    }

    /// <summary>CacheControl is mutable.</summary>
    [Test]
    public void CacheControl_IsMutable()
    {
        var options = new CsrfBootstrapOptions { CacheControl = "no-cache, private" };
        TestAssert.Equal("no-cache, private", options.CacheControl);
    }

    /// <summary>Enabled is mutable.</summary>
    [Test]
    public void Enabled_IsMutable()
    {
        var options = new CsrfBootstrapOptions { Enabled = false };
        TestAssert.False(options.Enabled);
    }
}

