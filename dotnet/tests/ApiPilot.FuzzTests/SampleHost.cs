// filepath: dotnet/tests/ApiPilot.FuzzTests/SampleHost.cs
// layer: TestInfrastructure | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Builds and launches the standalone Samples.Api as a child process for fuzz tests
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IAsyncDisposable
//   Depends on : System.Diagnostics.Process, HttpClient, the Samples.Api project path
//   Used by    : the HTTP fuzz test classes
//   See also   : QueryFuzzTests.cs, HeaderFuzzTests.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This helper builds the standalone Samples.Api with dotnet build, starts
//   it as a child process bound to a chosen loopback port, polls readiness
//   with a bounded timeout, captures stdout and stderr, and kills the
//   process tree on dispose. It does not reference the sample project; it
//   launches the built executable by path.

using System.Diagnostics;
using System.Net.Http;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Builds and launches the standalone Samples.Api as a child process on a
/// chosen loopback port. Readiness is proven by an HTTP probe against a
/// known endpoint. Dispose kills the process tree and disposes the client.
/// </summary>
internal sealed class SampleHost : IAsyncDisposable
{
    private static readonly TimeSpan s_buildTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan s_readyTimeout = TimeSpan.FromSeconds(30);

    private readonly Process _process;
    private readonly HttpClient _client;
    private readonly string _capturedOutput;

    /// <summary>The base address of the running sample.</summary>
    public Uri BaseAddress { get; }

    /// <summary>The HTTP client configured against the running sample.</summary>
    public HttpClient Client => _client;

    /// <summary>The captured stdout and stderr, for diagnostics.</summary>
    public string CapturedOutput => _capturedOutput;

    private SampleHost(Process process, HttpClient client, Uri baseAddress, string capturedOutput)
    {
        _process = process;
        _client = client;
        BaseAddress = baseAddress;
        _capturedOutput = capturedOutput;
    }

    /// <summary>
    /// Resolves the repository root from the test assembly location, builds
    /// the sample, launches it on a free loopback port, and waits for it to
    /// become ready.
    /// </summary>
    /// <returns>A started SampleHost.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the build fails, the process cannot start, or the sample
    /// does not become ready within the timeout.
    /// </exception>
    public static async Task<SampleHost> StartAsync()
    {
        var repoRoot = ResolveRepoRoot();
        var sampleProject = Path.Combine(repoRoot, "dotnet", "samples", "Samples.Api", "Samples.Api.csproj");
        var sampleDll = Path.Combine(repoRoot, "dotnet", "samples", "Samples.Api", "bin", "Release", "net10.0", "Samples.Api.dll");

        await BuildSampleAsync(sampleProject).ConfigureAwait(false);

        var port = ChooseFreePort();
        var baseAddress = new Uri("http://127.0.0.1:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(sampleDll);
        startInfo.Environment["ASPNETCORE_URLS"] = baseAddress.ToString();

        var process = new Process { StartInfo = startInfo };
        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { output.AppendLine(e.Data); } };

        if (!process.Start())
        {
            throw new InvalidOperationException("The sample process did not start.");
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var client = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            await WaitForReadyAsync(client, process, output).ConfigureAwait(false);
        }
        catch
        {
            KillTree(process);
            client.Dispose();
            throw;
        }

        return new SampleHost(process, client, baseAddress, output.ToString());
    }

    private static async Task BuildSampleAsync(string sampleProject)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(sampleProject);
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--nologo");

        using var build = Process.Start(startInfo)!;
        var stdout = await build.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        var stderr = await build.StandardError.ReadToEndAsync().ConfigureAwait(false);
        if (!build.WaitForExit((int)s_buildTimeout.TotalMilliseconds))
        {
            KillTree(build);
            throw new InvalidOperationException("Sample build timed out.");
        }
        if (build.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Sample build failed with exit code " + build.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + Environment.NewLine + stdout + Environment.NewLine + stderr);
        }
    }

    private static async Task WaitForReadyAsync(HttpClient client, Process process, System.Text.StringBuilder output)
    {
        var deadline = DateTime.UtcNow + s_readyTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException("The sample exited before becoming ready." + Environment.NewLine + output);
            }
            try
            {
                using var response = await client.GetAsync("/api/ok").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Not ready yet; probe again.
            }
            catch (TaskCanceledException)
            {
                // Timed out this probe; probe again.
            }
            await Task.Delay(100).ConfigureAwait(false);
        }
        throw new InvalidOperationException("The sample did not become ready within the timeout." + Environment.NewLine + output);
    }

    private static int ChooseFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string ResolveRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(dir);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "SPEC.md")))
            {
                return current.FullName;
            }
            current = current.Parent;
        }
        throw new InvalidOperationException("Could not locate the repository root from " + dir);
    }

    private static void KillTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException)
        {
            // The process already exited; nothing to do.
        }
    }

    /// <summary>Kills the sample process tree and disposes the client.</summary>
    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        KillTree(_process);
        _process.Dispose();
        return ValueTask.CompletedTask;
    }
}

