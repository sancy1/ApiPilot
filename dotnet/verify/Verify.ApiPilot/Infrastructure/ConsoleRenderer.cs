// filepath: dotnet/verify/Verify.ApiPilot/Infrastructure/ConsoleRenderer.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Renders scenario results and the final summary to a TextWriter.
//           The writer is injected so the renderer can be tested without
//           depending on the console. Output is plain ASCII and stable across
//           Windows PowerShell, Windows Terminal, and GitHub Actions.
// relates:  Consumes ScenarioResult. Invoked by Program.cs for each scenario
//           result and once for the final summary.

using System.Globalization;
using System.Text;

namespace Verify.ApiPilot.Infrastructure;

/// <summary>
/// Renders scenario results and the final summary.
/// </summary>
public sealed class ConsoleRenderer
{
    private readonly TextWriter _writer;

    /// <summary>
    /// Initialises a new renderer over the given writer.
    /// </summary>
    /// <param name="writer">The writer to render to. Must not be null.</param>
    public ConsoleRenderer(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _writer = writer;
    }

    /// <summary>
    /// Renders the run banner, including the resolved package versions.
    /// </summary>
    public void RenderBanner(string applicationName, IReadOnlyDictionary<string, string> resolvedVersions)
    {
        _writer.WriteLine("================================================================================");
        _writer.WriteLine("  ApiPilot Consumer Verification Application");
        _writer.WriteLine("  " + applicationName);
        _writer.WriteLine("================================================================================");
        _writer.WriteLine("  Resolved package versions:");
        foreach (var kv in resolvedVersions.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            _writer.WriteLine("    " + kv.Key + " -> " + kv.Value);
        }
        _writer.WriteLine("================================================================================");
        _writer.WriteLine();
    }

    /// <summary>
    /// Renders one scenario result.
    /// </summary>
    public void RenderResult(int index, ScenarioResult result)
    {
        var tag = result.Outcome switch
        {
            ScenarioOutcome.Passed      => "[PASS]",
            ScenarioOutcome.Failed      => "[FAIL]",
            ScenarioOutcome.Unavailable => "[UNAVAIL]",
            _                           => "[????]"
        };

        _writer.WriteLine(tag + " " + index.ToString("D2", CultureInfo.InvariantCulture) + " - " + result.Name);
        _writer.WriteLine("       " + result.Message);

        if (result.Exception is not null)
        {
            _writer.WriteLine("       Exception: " + result.Exception.GetType().FullName);
            _writer.WriteLine("       Message:   " + result.Exception.Message);
        }

        _writer.WriteLine();
    }

    /// <summary>
    /// Renders the final summary and the exit code.
    /// </summary>
    public void RenderSummary(int passed, int failed, int unavailable, int total, int exitCode)
    {
        _writer.WriteLine("Summary:");
        _writer.WriteLine("  Passed:      " + passed);
        _writer.WriteLine("  Failed:      " + failed);
        _writer.WriteLine("  Unavailable: " + unavailable);
        _writer.WriteLine("  Total:       " + total);
        _writer.WriteLine("Exit: " + exitCode);
    }
}