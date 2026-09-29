// filepath: dotnet/tests/ApiPilot.Core.Tests/SanityTests.cs
// layer: TestInfrastructure | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Sanity tests that prove the test harness runs end to end
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : TestAssert.cs, TestRunner.cs
// -----------------------------------------------------------------------------

using System.Threading.Tasks;

namespace ApiPilot.Core.Tests;

/// <summary>
/// Sanity tests that verify the repository-local test harness discovers and
/// runs [TestClass] types and [Test] methods correctly. These tests do not
/// exercise any ApiPilot feature; they exercise the test infrastructure itself.
/// </summary>
[TestClass]
public sealed class SanityTests
{
    /// <summary>Verifies a passing True assertion does not throw.</summary>
    [Test]
    public void True_WithTrueCondition_Passes()
    {
        TestAssert.True(true);
    }

    /// <summary>Verifies Equal assertion on two equal values does not throw.</summary>
    [Test]
    public void Equal_WithEqualValues_Passes()
    {
        TestAssert.Equal(1, 1);
        TestAssert.Equal("abc", "abc");
    }

    /// <summary>
    /// Verifies the runner awaits and completes async tests correctly.
    /// </summary>
    [Test]
    public async Task AsyncTest_AwaitsAndPasses()
    {
        await Task.Delay(1);
        TestAssert.NotNull(new object());
    }
}

