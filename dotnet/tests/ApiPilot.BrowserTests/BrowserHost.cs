// filepath: dotnet/tests/ApiPilot.BrowserTests/BrowserHost.cs
// layer: TestInfrastructure | package: ApiPilot.BrowserTests | since: v0.6.0
// purpose: Detects, launches, and connects to a headless Chromium/Edge browser via the Chrome DevTools Protocol
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IAsyncDisposable
//   Depends on : System.Diagnostics.Process, HttpClient, JsonDocument, TcpListener
//   Used by    : BrowserSecurityTests.cs, DevTools/CdpClient.cs
//   See also   : SampleHost.cs, DevTools/CdpConnection.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This helper detects a Chromium or Edge executable, launches it headless
//   with a temporary profile and a free remote-debugging port, polls the
//   debugging endpoint until it answers, and exposes the page-level
//   webSocketDebuggerUrl. When no executable is found, IsBrowserAvailable
//   is false and SkipReasonNoBrowser is exposed, so tests can return the
//   visible Skip outcome instead of a silent pass.

using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace ApiPilot.BrowserTests;

/// <summary>
/// Detects, launches, and exposes the Chrome DevTools Protocol endpoint of a
/// headless Chromium or Edge browser. When no browser is present, the host is
/// unavailable and the caller returns a visible Skip.
/// </summary>
internal sealed class BrowserHost : IAsyncDisposable
{
    /// <summary>The visible skip reason used when no browser is found.</summary>
    public const string SkipReasonNoBrowser = "SKIPPED: no supported Chromium/Edge executable found";

    private static readonly TimeSpan s_readyTimeout = TimeSpan.FromSeconds(30);

    /// <summary>The detected browser executable path, or null when none.</summary>
    public static string? DetectedExecutable { get; } = DetectExecutable();

    /// <summary>Whether a supported browser executable is present.</summary>
    public static bool IsBrowserAvailable => DetectedExecutable is not null;

    private readonly Process _process;
    private readonly HttpClient _client;
    private readonly string _profileDirectory;
    private readonly string _capturedOutput;

    /// <summary>The remote-debugging port the browser is listening on.</summary>
    public int DebuggingPort { get; }

    /// <summary>The browser-level webSocketDebuggerUrl.</summary>
    public string BrowserWebSocketUrl { get; }

    /// <summary>The page-level webSocketDebuggerUrl (the attached page target).</summary>
    public string PageWebSocketUrl { get; }

    /// <summary>The captured browser stdout and stderr, for diagnostics.</summary>
    public string CapturedOutput => _capturedOutput;

    private BrowserHost(
        Process process,
        HttpClient client,
        string profileDirectory,
        int debuggingPort,
        string browserWebSocketUrl,
        string pageWebSocketUrl,
        string capturedOutput)
    {
        _process = process;
        _client = client;
        _profileDirectory = profileDirectory;
        DebuggingPort = debuggingPort;
        BrowserWebSocketUrl = browserWebSocketUrl;
        PageWebSocketUrl = pageWebSocketUrl;
        _capturedOutput = capturedOutput;
    }

    /// <summary>
    /// Detects a supported browser executable. The APIPILOT_BROWSER_PATH
    /// environment variable, when set and pointing at an existing file,
    /// overrides the standard locations.
    /// </summary>
    /// <returns>The executable path, or null when no supported browser is found.</returns>
    private static string? DetectExecutable()
    {
        var overridePath = Environment.GetEnvironmentVariable("APIPILOT_BROWSER_PATH");
        if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
        {
            return overridePath;
        }

        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Chromium", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Chromium", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        foreach (var name in new[] { "chrome.exe", "chromium.exe", "msedge.exe" })
        {
            var resolved = ResolveFromPath(name);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }

    private static string? ResolveFromPath(string name)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }
        foreach (var dir in pathVariable.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }
            try
            {
                var full = Path.Combine(dir.Trim(), name);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch (ArgumentException)
            {
                // An invalid PATH segment; skip it.
            }
        }
        return null;
    }

    /// <summary>
    /// Launches the browser headless on a free debugging port and returns a
    /// started host with the page-attached WebSocket URL.
    /// </summary>
    /// <returns>A started BrowserHost.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no browser is available, the process cannot start, or the
    /// debugging endpoint does not answer within the timeout.
    /// </exception>
    public static async Task<BrowserHost> StartAsync()
    {
        var executable = DetectedExecutable;
        if (executable is null)
        {
            throw new InvalidOperationException(SkipReasonNoBrowser);
        }

        var profileDirectory = Path.Combine(Path.GetTempPath(), "apipilot-cdp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profileDirectory);

        var port = ChooseFreePort();

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("--headless=new");
        startInfo.ArgumentList.Add("--disable-gpu");
        startInfo.ArgumentList.Add("--no-first-run");
        startInfo.ArgumentList.Add("--no-default-browser-check");
        startInfo.ArgumentList.Add("--user-data-dir=" + profileDirectory);
        startInfo.ArgumentList.Add("--remote-debugging-port=" + port.ToString(System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("about:blank");

        var process = new Process { StartInfo = startInfo };
        var output = new System.Text.StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { output.AppendLine(e.Data); } };

        if (!process.Start())
        {
            throw new InvalidOperationException("The browser process did not start.");
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        try
        {
            var (browserUrl, pageUrl) = await WaitForDebuggerAsync(client, port, process, output).ConfigureAwait(false);
            return new BrowserHost(process, client, profileDirectory, port, browserUrl, pageUrl, output.ToString());
        }
        catch
        {
            KillTree(process);
            client.Dispose();
            TryDeleteDirectory(profileDirectory);
            throw;
        }
    }

    private static async Task<(string BrowserUrl, string PageUrl)> WaitForDebuggerAsync(
        HttpClient client,
        int port,
        Process process,
        System.Text.StringBuilder output)
    {
        var deadline = DateTime.UtcNow + s_readyTimeout;
        var versionUri = "http://127.0.0.1:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/json/version";
        var listUri = "http://127.0.0.1:" + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/json/list";

        while (DateTime.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException("The browser exited before the debugging endpoint was ready." + Environment.NewLine + output);
            }
            try
            {
                using var versionResponse = await client.GetAsync(versionUri).ConfigureAwait(false);
                if (versionResponse.IsSuccessStatusCode)
                {
                    var versionBody = await versionResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var versionDoc = JsonDocument.Parse(versionBody);
                    var browserUrl = versionDoc.RootElement.TryGetProperty("webSocketDebuggerUrl", out var bws)
                        ? bws.GetString() ?? string.Empty
                        : string.Empty;

                    var pageUrl = await FindPageTargetAsync(client, listUri).ConfigureAwait(false);
                    if (pageUrl.Length > 0)
                    {
                        return (browserUrl, pageUrl);
                    }
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
            catch (JsonException)
            {
                // Partial body; probe again.
            }
            await Task.Delay(100).ConfigureAwait(false);
        }
        throw new InvalidOperationException("The browser debugging endpoint did not become ready within the timeout." + Environment.NewLine + output);
    }

    private static async Task<string> FindPageTargetAsync(HttpClient client, string listUri)
    {
        try
        {
            using var listResponse = await client.GetAsync(listUri).ConfigureAwait(false);
            if (!listResponse.IsSuccessStatusCode)
            {
                return string.Empty;
            }
            var listBody = await listResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var listDoc = JsonDocument.Parse(listBody);
            foreach (var target in listDoc.RootElement.EnumerateArray())
            {
                if (target.TryGetProperty("type", out var type) &&
                    string.Equals(type.GetString(), "page", StringComparison.Ordinal) &&
                    target.TryGetProperty("webSocketDebuggerUrl", out var ws))
                {
                    return ws.GetString() ?? string.Empty;
                }
            }
        }
        catch (HttpRequestException)
        {
        }
        catch (TaskCanceledException)
        {
        }
        catch (JsonException)
        {
        }
        return string.Empty;
    }

    private static int ChooseFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
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

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // The browser may still hold a file handle; leave the temp dir.
        }
    }

    /// <summary>Kills the browser process tree, disposes the client, and removes the profile.</summary>
    public ValueTask DisposeAsync()
    {
        _client.Dispose();
        KillTree(_process);
        _process.Dispose();
        TryDeleteDirectory(_profileDirectory);
        return ValueTask.CompletedTask;
    }
}

