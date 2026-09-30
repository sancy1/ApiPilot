// filepath: dotnet/verify/Verify.ApiPilot/Infrastructure/ScenarioResult.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  The outcome of one verification scenario. A scenario returns
//           exactly one ScenarioResult. The result carries the scenario name,
//           an outcome (Passed, Failed, Unavailable), a concise message, and
//           an optional exception for diagnostics.
// relates:  Returned by every Scenarios/*.cs scenario. Consumed by Program.cs,
//           rendered by ConsoleRenderer.

namespace Verify.ApiPilot.Infrastructure;

/// <summary>
/// The outcome of one verification scenario.
/// </summary>
public enum ScenarioOutcome
{
    /// <summary>
    /// The scenario ran and the published artifact matched the documented contract.
    /// </summary>
    Passed,

    /// <summary>
    /// The scenario ran and the published artifact did not match the documented contract.
    /// The verifier fails when any required scenario returns this outcome.
    /// </summary>
    Failed,

    /// <summary>
    /// The scenario could not run because a prerequisite was unavailable.
    /// An Unavailable outcome is visible and is never folded into the pass count.
    /// </summary>
    Unavailable
}

/// <summary>
/// The result of one verification scenario.
/// </summary>
/// <param name="Name">The scenario name, matching the file name without the .cs extension.</param>
/// <param name="Outcome">The outcome of the scenario.</param>
/// <param name="Message">A concise message explaining what was verified or why the scenario failed or was unavailable.</param>
/// <param name="Exception">An optional exception captured during the scenario, for diagnostics.</param>
public sealed record ScenarioResult(
    string Name,
    ScenarioOutcome Outcome,
    string Message,
    Exception? Exception = null)
{
    /// <summary>
    /// Creates a Passed result.
    /// </summary>
    public static ScenarioResult Passed(string name, string message)
        => new(name, ScenarioOutcome.Passed, message);

    /// <summary>
    /// Creates a Failed result.
    /// </summary>
    public static ScenarioResult Failed(string name, string message, Exception? exception = null)
        => new(name, ScenarioOutcome.Failed, message, exception);

    /// <summary>
    /// Creates an Unavailable result.
    /// </summary>
    public static ScenarioResult Unavailable(string name, string message)
        => new(name, ScenarioOutcome.Unavailable, message);
}