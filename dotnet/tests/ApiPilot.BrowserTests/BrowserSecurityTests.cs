// filepath: dotnet/tests/ApiPilot.BrowserTests/BrowserSecurityTests.cs
// layer: Fuzz | package: ApiPilot.BrowserTests | since: v0.6.0
// purpose: Real-browser security fixtures driving the sample through the Chrome DevTools Protocol
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, TestOutcome,
//                BrowserHost, SampleHost, CdpClient, CdpConnection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : BrowserHost.cs, SampleHost.cs, DevTools/CdpClient.cs
// -----------------------------------------------------------------------------
//
// THE BROWSER SECURITY SUITE
//   Each fixture returns Task<TestOutcome>. When no supported browser is
//   present, every fixture returns Skipped with the visible reason. When a
//   browser is present, the fixture launches the real sample and a real
//   headless browser, connects the CDP client, and asserts a security
//   invariant. The HTTPS reverse-proxy fixture is Unavailable by design,
//   recorded for a future phase or external CI.

using ApiPilot.BrowserTests.DevTools;
using ApiPilot.TestHarness;
using System.Net.Http;
using System.Text.Json;

namespace ApiPilot.BrowserTests;

/// <summary>
/// Real-browser security fixtures. Each test returns an explicit outcome so
/// a missing browser or unavailable HTTPS infrastructure is reported as a
/// skip or unavailability, never as a silent pass.
/// </summary>
[TestClass]
public sealed class BrowserSecurityTests
{
    private static readonly TimeSpan s_evaluateTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The HTTP-only auth cookie is unreadable via document.cookie.</summary>
    [Test]
    public async Task<TestOutcome> HttpOnlyAuthCookie_NotReadableViaDocumentCookie()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();
        await using var browser = await BrowserHost.StartAsync();

        using var cts = new CancellationTokenSource(s_evaluateTimeout);
        await using var client = await CdpClient.ConnectAsync(browser.PageWebSocketUrl, cts.Token);
        var cdp = new CdpConnection(client);

        await cdp.NavigateAsync(sample.BaseAddress + "api/ok", cts.Token);
        await Task.Delay(500, cts.Token);

        var cookieValue = await cdp.EvaluateAsync("document.cookie", cts.Token);
        var cookieText = cookieValue.ValueKind == JsonValueKind.String ? cookieValue.GetString() ?? string.Empty : string.Empty;

        TestAssert.False(cookieText.Contains("apipilot", StringComparison.OrdinalIgnoreCase), "document.cookie must not expose the auth cookie");
        return TestOutcome.Passed();
    }

    /// <summary>The CSRF token is never written to localStorage.</summary>
    [Test]
    public async Task<TestOutcome> CsrfToken_NotInLocalStorage()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();
        await using var browser = await BrowserHost.StartAsync();

        using var cts = new CancellationTokenSource(s_evaluateTimeout);
        await using var client = await CdpClient.ConnectAsync(browser.PageWebSocketUrl, cts.Token);
        var cdp = new CdpConnection(client);

        await cdp.NavigateAsync(sample.BaseAddress + "api/ok", cts.Token);
        await Task.Delay(500, cts.Token);

        var storage = await cdp.EvaluateAsync("JSON.stringify(localStorage)", cts.Token);
        var storageText = storage.ValueKind == JsonValueKind.String ? storage.GetString() ?? string.Empty : string.Empty;

        TestAssert.False(storageText.Contains("token", StringComparison.OrdinalIgnoreCase), "the CSRF token must never appear in localStorage");
        return TestOutcome.Passed();
    }

    /// <summary>The CSRF token is never written to sessionStorage.</summary>
    [Test]
    public async Task<TestOutcome> CsrfToken_NotInSessionStorage()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();
        await using var browser = await BrowserHost.StartAsync();

        using var cts = new CancellationTokenSource(s_evaluateTimeout);
        await using var client = await CdpClient.ConnectAsync(browser.PageWebSocketUrl, cts.Token);
        var cdp = new CdpConnection(client);

        await cdp.NavigateAsync(sample.BaseAddress + "api/ok", cts.Token);
        await Task.Delay(500, cts.Token);

        var storage = await cdp.EvaluateAsync("JSON.stringify(sessionStorage)", cts.Token);
        var storageText = storage.ValueKind == JsonValueKind.String ? storage.GetString() ?? string.Empty : string.Empty;

        TestAssert.False(storageText.Contains("token", StringComparison.OrdinalIgnoreCase), "the CSRF token must never appear in sessionStorage");
        return TestOutcome.Passed();
    }

    /// <summary>A POST to /api/submit without the CSRF header is rejected.</summary>
    [Test]
    public async Task<TestOutcome> AttackerScript_NoCsrfHeader_IsRejected()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/submit");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await sample.Client.SendAsync(request);

        TestAssert.Equal(403, (int)response.StatusCode);
        return TestOutcome.Passed();
    }

    /// <summary>A request with a hostile Origin header is rejected.</summary>
    [Test]
    public async Task<TestOutcome> WrongOrigin_IsRejected()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/submit");
        request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("Origin", "https://hostile.example.com");
        using var response = await sample.Client.SendAsync(request);

        TestAssert.Equal(403, (int)response.StatusCode);
        return TestOutcome.Passed();
    }

    /// <summary>The bootstrap endpoint issues a token a legit page can use.</summary>
    [Test]
    public async Task<TestOutcome> Legit_BootstrapIssuesUsableToken()
    {
        if (!BrowserHost.IsBrowserAvailable)
        {
            return TestOutcome.Skipped(BrowserHost.SkipReasonNoBrowser);
        }

        await using var sample = await SampleHost.StartAsync();

        var body = await sample.Client.GetStringAsync("/api/csrf");
        TestAssert.True(body.Length > 0, "expected a non-empty bootstrap body");
        using var document = JsonDocument.Parse(body);
        TestAssert.True(document.RootElement.TryGetProperty("token", out var token));
        TestAssert.Equal(JsonValueKind.String, token.ValueKind);
        TestAssert.True((token.GetString() ?? string.Empty).Length > 0, "expected a non-empty token");
        return TestOutcome.Passed();
    }

    /// <summary>
    /// The HTTPS reverse-proxy fixture is unavailable in the repository-owned
    /// test environment. It is recorded as an environment-dependent item for a
    /// future phase or external CI, never as a passing security assertion.
    /// </summary>
    [Test]
    public Task<TestOutcome> HttpsReverseProxy_IsUnavailable()
    {
        return Task.FromResult(TestOutcome.Unavailable("UNAVAILABLE: HTTPS reverse-proxy fixture unavailable"));
    }
}

