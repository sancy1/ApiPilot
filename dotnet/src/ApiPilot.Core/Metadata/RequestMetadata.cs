// filepath: dotnet/src/ApiPilot.Core/Metadata/RequestMetadata.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable metadata describing an incoming ApiPilot request
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a
//   Used by    : middleware, diagnostics, structured logging
//   See also   : ResponseMetadata.cs, SPEC.md (correlation ID rules)
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Immutable metadata describing an incoming ApiPilot request. Populated
/// by the ASP.NET Core adapter from the HttpContext and carried through
/// the request lifetime for diagnostics and structured logging.
/// </summary>
/// <remarks>
/// This type is transport-neutral. It carries plain strings for the
/// HTTP method and path rather than any ASP.NET Core type. The adapter
/// is responsible for the translation from HttpContext.
/// </remarks>
public sealed record RequestMetadata
{
    /// <summary>
    /// The correlation identifier for this request. Read from the
    /// configured header if present and valid, otherwise generated.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// The UTC instant at which the request was received by the server.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The HTTP method (for example, GET, POST, PUT, PATCH, DELETE).
    /// Uppercase by convention. Empty string until the adapter sets it.
    /// </summary>
    public string Method { get; init; } = string.Empty;

    /// <summary>
    /// The request path without the query string. Empty string until the
    /// adapter sets it.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// A request metadata instance with the current UTC timestamp and
    /// empty string fields. Useful as a starting point in tests and as
    /// an internal default.
    /// </summary>
    public static RequestMetadata Empty => new();

    /// <summary>
    /// Returns a copy of this metadata with the request identifier replaced.
    /// The original instance is unchanged.
    /// </summary>
    /// <param name="requestId">The correlation ID to assign.</param>
    /// <returns>A new <see cref="RequestMetadata"/> with the given request ID.</returns>
    public RequestMetadata WithRequestId(string requestId)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        return this with { RequestId = requestId };
    }
}

