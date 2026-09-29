// filepath: dotnet/tests/ApiPilot.Core.Tests/TestHarnessContractTests.cs
// layer: TestInfrastructure | package: ApiPilot.Core.Tests | since: v0.6.0
// purpose: Self-test proving the shared outcome classifier and exit policy
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                TestOutcomeKind, TestOutcome, TestOutcomeClassifier, TestExitPolicy
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : TestHarness.cs, TestRunner.cs
// -----------------------------------------------------------------------------
//
// THE SELF-TEST
//   These tests call the pure classifier and the pure exit policy directly.
//   They prove that Skipped and Unavailable are distinct from Passed, that a
//   void-method normal return is Passed, and that the exit policy returns 1
//   on failure, 2 on unaccepted skip, and 0 on a clean or accepted-skip run.
//   No skip-producing [Test] exists in this harness, so the harness run
//   itself stays at zero skips and exit 0.

using ApiPilot.TestHarness;

namespace ApiPilot.Core.Tests;

/// <summary>
/// Contract tests for the shared outcome classifier and exit policy.
/// </summary>
[TestClass]
public sealed class TestHarnessContractTests
{
    /// <summary>A Passed outcome classifies as Passed.</summary>
    [Test]
    public void Classify_PassedOutcome_ReturnsPassed()
    {
        var kind = TestOutcomeClassifier.Classify(TestOutcome.Passed());
        TestAssert.Equal(TestOutcomeKind.Passed, kind);
    }

    /// <summary>A Skipped outcome classifies as Skipped, not Passed.</summary>
    [Test]
    public void Classify_SkippedOutcome_ReturnsSkipped()
    {
        var outcome = TestOutcome.Skipped("SKIPPED: self-test");
        var kind = TestOutcomeClassifier.Classify(outcome);
        TestAssert.Equal(TestOutcomeKind.Skipped, kind);
        TestAssert.Equal("SKIPPED: self-test", TestOutcomeClassifier.MessageOf(outcome));
    }

    /// <summary>An Unavailable outcome classifies as Unavailable, not Passed.</summary>
    [Test]
    public void Classify_UnavailableOutcome_ReturnsUnavailable()
    {
        var outcome = TestOutcome.Unavailable("UNAVAILABLE: self-test");
        var kind = TestOutcomeClassifier.Classify(outcome);
        TestAssert.Equal(TestOutcomeKind.Unavailable, kind);
        TestAssert.Equal("UNAVAILABLE: self-test", TestOutcomeClassifier.MessageOf(outcome));
    }

    /// <summary>A null return value classifies as Passed.</summary>
    [Test]
    public void Classify_NullValue_ReturnsPassed()
    {
        var kind = TestOutcomeClassifier.Classify(null);
        TestAssert.Equal(TestOutcomeKind.Passed, kind);
    }

    /// <summary>An unknown return value classifies as Passed.</summary>
    [Test]
    public void Classify_UnknownValue_ReturnsPassed()
    {
        var kind = TestOutcomeClassifier.Classify("not an outcome");
        TestAssert.Equal(TestOutcomeKind.Passed, kind);
    }

    /// <summary>A failure forces exit code 1 regardless of skips.</summary>
    [Test]
    public void ExitCode_Failed_Returns1()
    {
        TestAssert.Equal(1, TestExitPolicy.ExitCode(failed: 1, skipped: 0, unavailable: 0, allowSkip: false));
        TestAssert.Equal(1, TestExitPolicy.ExitCode(failed: 1, skipped: 2, unavailable: 0, allowSkip: true));
    }

    /// <summary>An unaccepted skip or unavailable forces exit code 2.</summary>
    [Test]
    public void ExitCode_SkippedNotAccepted_Returns2()
    {
        TestAssert.Equal(2, TestExitPolicy.ExitCode(failed: 0, skipped: 1, unavailable: 0, allowSkip: false));
        TestAssert.Equal(2, TestExitPolicy.ExitCode(failed: 0, skipped: 0, unavailable: 1, allowSkip: false));
    }

    /// <summary>A clean run or an accepted skip returns exit code 0.</summary>
    [Test]
    public void ExitCode_AllAcceptedOrClean_Returns0()
    {
        TestAssert.Equal(0, TestExitPolicy.ExitCode(failed: 0, skipped: 0, unavailable: 0, allowSkip: false));
        TestAssert.Equal(0, TestExitPolicy.ExitCode(failed: 0, skipped: 1, unavailable: 1, allowSkip: true));
    }
}

