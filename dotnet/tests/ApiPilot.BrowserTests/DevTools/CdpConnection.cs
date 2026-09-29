// filepath: dotnet/tests/ApiPilot.BrowserTests/DevTools/CdpConnection.cs
// layer: DevTools | package: ApiPilot.BrowserTests | since: v0.6.0
// purpose: Typed helpers over CdpClient for the CDP methods the browser fixtures use
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : CdpClient, System.Text.Json
//   Used by    : BrowserSecurityTests.cs
//   See also   : DevTools/CdpClient.cs, BrowserHost.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   Thin wrappers. Each helper issues one CDP method through the shared
//   client and returns the reply or an extracted value. The set is exactly
//   the methods the fixtures need: Target.getTargets, Page.navigate,
//   Runtime.evaluate, and Network.getCookies.

using System.Text.Json;

namespace ApiPilot.BrowserTests.DevTools;

/// <summary>
/// Typed CDP method helpers over a shared <see cref="CdpClient"/>.
/// </summary>
internal sealed class CdpConnection
{
    private readonly CdpClient _client;

    /// <summary>Creates a connection over an open CDP client.</summary>
    /// <param name="client">The open CDP client. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="client"/> is null.
    /// </exception>
    public CdpConnection(CdpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
    }

    /// <summary>
    /// Calls Target.getTargets and returns only the page targets.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The page targets.</returns>
    public async Task<IReadOnlyList<JsonElement>> GetPageTargetsAsync(CancellationToken cancellationToken)
    {
        var reply = await _client.SendAsync("Target.getTargets", "{}", cancellationToken).ConfigureAwait(false);
        var pages = new List<JsonElement>();
        if (reply.TryGetProperty("result", out var result) &&
            result.TryGetProperty("targetInfos", out var infos) &&
            infos.ValueKind == JsonValueKind.Array)
        {
            foreach (var info in infos.EnumerateArray())
            {
                if (info.TryGetProperty("type", out var type) &&
                    string.Equals(type.GetString(), "page", StringComparison.Ordinal))
                {
                    pages.Add(info.Clone());
                }
            }
        }
        return pages;
    }

    /// <summary>
    /// Calls Page.navigate for the given URL. Throws when the reply carries
    /// an errorText.
    /// </summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The navigate reply.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="url"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the reply carries an errorText.
    /// </exception>
    public async Task<JsonElement> NavigateAsync(string url, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(url);
        var paramsJson = JsonSerializer.Serialize(new { url });
        var reply = await _client.SendAsync("Page.navigate", paramsJson, cancellationToken).ConfigureAwait(false);
        if (reply.TryGetProperty("result", out var result) &&
            result.TryGetProperty("errorText", out var errorText) &&
            errorText.ValueKind == JsonValueKind.String)
        {
            throw new InvalidOperationException("Page.navigate failed: " + errorText.GetString());
        }
        return reply;
    }

    /// <summary>
    /// Calls Runtime.evaluate with returnByValue and returns the value.
    /// </summary>
    /// <param name="expression">The JavaScript expression to evaluate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The evaluated value, or a default element when absent.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="expression"/> is null.
    /// </exception>
    public async Task<JsonElement> EvaluateAsync(string expression, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var paramsJson = JsonSerializer.Serialize(new { expression, returnByValue = true });
        var reply = await _client.SendAsync("Runtime.evaluate", paramsJson, cancellationToken).ConfigureAwait(false);
        if (reply.TryGetProperty("result", out var result) &&
            result.TryGetProperty("result", out var inner) &&
            inner.TryGetProperty("value", out var value))
        {
            return value.Clone();
        }
        return default;
    }

    /// <summary>
    /// Calls Network.getCookies and returns the cookies array.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cookies array, or a default element when absent.</returns>
    public async Task<JsonElement> GetCookiesAsync(CancellationToken cancellationToken)
    {
        var reply = await _client.SendAsync("Network.getCookies", "{}", cancellationToken).ConfigureAwait(false);
        if (reply.TryGetProperty("result", out var result) &&
            result.TryGetProperty("cookies", out var cookies))
        {
            return cookies.Clone();
        }
        return default;
    }
}

