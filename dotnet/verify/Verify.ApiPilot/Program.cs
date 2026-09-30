// filepath: dotnet/verify/Verify.ApiPilot/Program.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  The executable entry point. It reads the resolved package versions
//           from project.assets.json, prints the banner, runs the scenario
//           catalog in order, renders the results and the summary, and returns
//           the process exit code. It orchestrates. It does not contain feature
//           logic.
// relates:  Reads obj/project.assets.json. Invokes each Scenarios/*.cs file.
//           Uses Infrastructure/ConsoleRenderer for output.

using System.Text.Json;
using Verify.ApiPilot.Infrastructure;
using Verify.ApiPilot.Scenarios;

var renderer = new ConsoleRenderer(Console.Out);

var assetsPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "obj", "project.assets.json");
assetsPath = Path.GetFullPath(assetsPath);

var resolvedVersions = new Dictionary<string, string>(StringComparer.Ordinal);
if (File.Exists(assetsPath))
{
    using var stream = File.OpenRead(assetsPath);
    using var doc = JsonDocument.Parse(stream);
    if (doc.RootElement.TryGetProperty("targets", out var targets))
    {
        foreach (var target in targets.EnumerateObject())
        {
            foreach (var pkg in target.Value.EnumerateObject())
            {
                var name = pkg.Name;
                var slash = name.IndexOf('/');
                if (slash > 0)
                {
                    var id = name.Substring(0, slash);
                    var version = name.Substring(slash + 1);
                    if (id.StartsWith("ApiPilot", StringComparison.Ordinal))
                    {
                        resolvedVersions[id] = version;
                    }
                }
            }
            break;
        }
    }
}

renderer.RenderBanner("Verify.ApiPilot", resolvedVersions);

// Run the scenario catalog in explicit order.
var results = new List<ScenarioResult>
{
    CoreEnvelopeSuccessScenario.Run(),
    NoContentScenario.Run(),
    ErrorEnvelopeScenario.Run(),
    await CorrelationScenario.RunAsync(),
    await ExceptionMappingScenario.RunAsync(),
    await ContentNegotiationScenario.RunAsync(),
    await JsonSerializationScenario.RunAsync(),
    await PaginationScenario.RunAsync(),
    await ValidationScenario.RunAsync(),
    await CsrfBootstrapScenario.RunAsync(),
    await CsrfProtectionScenario.RunAsync(),
};

var index = 0;
foreach (var r in results)
{
    index++;
    renderer.RenderResult(index, r);
}

var passed      = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
var failed      = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
var unavailable = results.Count(r => r.Outcome == ScenarioOutcome.Unavailable);
var total       = results.Count;

var exitCode = total == 0 ? 2 : (failed > 0 ? 1 : 0);

renderer.RenderSummary(passed, failed, unavailable, total, exitCode);

return exitCode;