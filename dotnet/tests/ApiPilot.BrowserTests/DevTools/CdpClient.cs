// filepath: dotnet/tests/ApiPilot.BrowserTests/DevTools/CdpClient.cs
// layer: DevTools | package: ApiPilot.BrowserTests | since: v0.6.0
// purpose: A minimal Chrome DevTools Protocol client over ClientWebSocket
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IAsyncDisposable
//   Depends on : System.Net.WebSockets.ClientWebSocket, System.Text.Json
//   Used by    : BrowserSecurityTests.cs
//   See also   : BrowserHost.cs, DevTools/CdpConnection.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This is a minimal CDP transport. It connects a ClientWebSocket to a
//   page-level webSocketDebuggerUrl, sends one command at a time with an
//   incrementing id, and returns the reply whose id matches. Event messages
//   (those with a method and no id) are discarded. There is no package;
//   ClientWebSocket is in the base runtime.

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ApiPilot.BrowserTests.DevTools;

/// <summary>
/// A minimal Chrome DevTools Protocol client. One command at a time; the
/// reply with the matching id is returned; events are discarded.
/// </summary>
internal sealed class CdpClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket;
    private int _nextId;

    private CdpClient(ClientWebSocket socket)
    {
        _socket = socket;
    }

    /// <summary>
    /// Connects a ClientWebSocket to the given debugger URL.
    /// </summary>
    /// <param name="webSocketUrl">The page-level webSocketDebuggerUrl.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A connected CdpClient.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="webSocketUrl"/> is null.
    /// </exception>
    public static async Task<CdpClient> ConnectAsync(string webSocketUrl, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(webSocketUrl);
        var socket = new ClientWebSocket();
        try
        {
            await socket.ConnectAsync(new Uri(webSocketUrl), cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
        return new CdpClient(socket);
    }

    /// <summary>
    /// Sends one CDP command and returns the reply whose id matches.
    /// </summary>
    /// <param name="method">The CDP method name, for example Runtime.evaluate.</param>
    /// <param name="paramsJson">
    /// The parameters as a JSON object string, or null for no parameters.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The reply as a JSON element.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="method"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the socket closes before the matching reply arrives.
    /// </exception>
    public async Task<JsonElement> SendAsync(
        string method,
        string? paramsJson = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(method);

        var id = ++_nextId;
        var command = paramsJson is null
            ? "{\"id\":" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"method\":\"" + method + "\"}"
            : "{\"id\":" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) + ",\"method\":\"" + method + "\",\"params\":" + paramsJson + "}";

        var bytes = Encoding.UTF8.GetBytes(command);
        await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);

        while (true)
        {
            var message = await ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
            if (message is null)
            {
                throw new InvalidOperationException("The CDP socket closed before a reply arrived.");
            }

            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;

            // An event message has a "method" and no "id". Discard it.
            if (!root.TryGetProperty("id", out var replyId))
            {
                continue;
            }

            if (replyId.GetInt32() == id)
            {
                return root.Clone();
            }
            // A reply for a different id; discard it. The fixtures are sequential.
        }
    }

    private async Task<string?> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        using var stream = new System.IO.MemoryStream();

        while (true)
        {
            var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            stream.Write(buffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>Closes the socket and disposes it.</summary>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (WebSocketException)
        {
            // The socket is already broken; dispose will clean up.
        }
        catch (OperationCanceledException)
        {
            // Closing timed out; dispose will clean up.
        }
        _socket.Dispose();
    }
}

