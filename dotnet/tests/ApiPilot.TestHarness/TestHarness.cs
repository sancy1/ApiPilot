// filepath: dotnet/tests/ApiPilot.TestHarness/TestHarness.cs
// layer: TestInfrastructure | package: ApiPilot.TestHarness | since: v0.6.0
// purpose: The shared, pure test outcome model linked into every repository test harness
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum, sealed record, static classes)
//   Depends on : n/a (BCL only)
//   Used by    : every TestRunner.cs in the six test projects; TestHarnessContractTests
//   See also   : TestRunner.cs in each test project, TestHarnessContractTests.cs
// -----------------------------------------------------------------------------
//
// THE SHARED OUTCOME CONTRACT
//   Four outcomes: Passed, Failed, Skipped, Unavailable. A [Test] method
//   that returns void or Task is Passed when it completes normally and
//   Failed when it throws (the existing behavior, unchanged). A [Test]
//   method may instead return a TestOutcome, which reports its own kind
//   and an optional message. Skipped and Unavailable never increment the
//   Passed count.
//
//   The classifier and the exit policy are pure functions. They carry no
//   state, no thread-static storage, and no side effects. The self-test
//   calls them directly.

namespace ApiPilot.TestHarness;

/// <summary>
/// The classification of a single test result.
/// </summary>
public enum TestOutcomeKind
{
    /// <summary>The test executed and its assertions held.</summary>
    Passed,

    /// <summary>The test executed and its assertions failed.</summary>
    Failed,

    /// <summary>
    /// The test could not execute because a prerequisite is absent at
    /// run time (for example, no browser is installed). This is not a
    /// pass.
    /// </summary>
    Skipped,

    /// <summary>
    /// The test could not execute because the infrastructure it needs
    /// is structurally unavailable in this environment (for example,
    /// the HTTPS reverse-proxy fixture). This is not a pass.
    /// </summary>
    Unavailable,
}

/// <summary>
/// An explicit test outcome: a kind and an optional visible message. A
/// test method returns a TestOutcome to report its own classification.
/// </summary>
/// <param name="Kind">The outcome kind.</param>
/// <param name="Message">An optional visible message. Null when none.</param>
public sealed record TestOutcome(TestOutcomeKind Kind, string? Message = null)
{
    /// <summary>Creates a Passed outcome.</summary>
    /// <returns>A Passed outcome with no message.</returns>
    public static TestOutcome Passed() => new(TestOutcomeKind.Passed);

    /// <summary>Creates a Skipped outcome with a visible reason.</summary>
    /// <param name="reason">The visible skip reason. Must not be null.</param>
    /// <returns>A Skipped outcome.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reason"/> is null.
    /// </exception>
    public static TestOutcome Skipped(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return new TestOutcome(TestOutcomeKind.Skipped, reason);
    }

    /// <summary>Creates an Unavailable outcome with a visible reason.</summary>
    /// <param name="reason">The visible unavailability reason. Must not be null.</param>
    /// <returns>An Unavailable outcome.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reason"/> is null.
    /// </exception>
    public static TestOutcome Unavailable(string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);
        return new TestOutcome(TestOutcomeKind.Unavailable, reason);
    }
}

/// <summary>
/// The pure classifier that maps a test method return value to an
/// outcome kind. It carries no state and has no side effects.
/// </summary>
public static class TestOutcomeClassifier
{
    /// <summary>
    /// Classifies a test method return value. A TestOutcome yields its
    /// own Kind. A null or any other value yields Passed, because a
    /// void or Task method that completes normally is a pass.
    /// </summary>
    /// <param name="value">The return value of the test method. May be null.</param>
    /// <returns>The outcome kind.</returns>
    public static TestOutcomeKind Classify(object? value)
    {
        if (value is TestOutcome outcome)
        {
            return outcome.Kind;
        }
        return TestOutcomeKind.Passed;
    }

    /// <summary>
    /// Extracts the visible message from a test method return value, or
    /// an empty string when none is present.
    /// </summary>
    /// <param name="value">The return value of the test method. May be null.</param>
    /// <returns>The message, or an empty string.</returns>
    public static string MessageOf(object? value)
    {
        if (value is TestOutcome outcome && outcome.Message is not null)
        {
            return outcome.Message;
        }
        return string.Empty;
    }
}

/// <summary>
/// The pure exit-code policy. It carries no state and has no side
/// effects. The runner calls it; the self-test calls it directly.
/// </summary>
public static class TestExitPolicy
{
    /// <summary>
    /// Computes the process exit code from the outcome counts and the
    /// allow-skip flag.
    /// </summary>
    /// <param name="failed">The number of failed tests.</param>
    /// <param name="skipped">The number of skipped tests.</param>
    /// <param name="unavailable">The number of unavailable tests.</param>
    /// <param name="allowSkip">
    /// Whether the invocation explicitly accepted skips and
    /// unavailability through --allow-skip.
    /// </param>
    /// <returns>
    /// 1 when any test failed; 2 when no test failed but a skip or
    /// unavailability is present and allowSkip is false; 0 otherwise.
    /// </returns>
    public static int ExitCode(int failed, int skipped, int unavailable, bool allowSkip)
    {
        if (failed > 0)
        {
            return 1;
        }
        if ((skipped + unavailable) > 0 && !allowSkip)
        {
            return 2;
        }
        return 0;
    }
}

