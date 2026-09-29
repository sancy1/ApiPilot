// filepath: dotnet/src/ApiPilot.Core/Metadata/ResponseMetadata.cs
// layer: Metadata | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable non-business metadata attached to every ApiPilot response
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a (System.Collections.Generic via implicit usings)
//   Used by    : IApiResponse, ApiResponse, ApiResponse<T>, ErrorResponse
//   See also   : SPEC.md (meta.requestId), RequestMetadata.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Metadata;

/// <summary>
/// Non-business metadata attached to every ApiPilot response envelope.
/// Carries the request correlation ID, the server timestamp of response
/// production, and an optional dictionary of extra values the application
/// has chosen to expose.
/// </summary>
/// <remarks>
/// Instances are immutable. Every property is init-only. The record
/// provides value equality, which makes metadata assertions in tests
/// straightforward and deterministic.
/// </remarks>
public sealed record ResponseMetadata
{
    /// <summary>
    /// The correlation identifier for the request that produced this
    /// response. Always present on real responses; the empty string is
    /// used only by the <see cref="Empty"/> factory until the middleware
    /// assigns a real value.
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// The UTC instant at which the response was produced. Captured by
    /// the middleware or builder that constructed the metadata.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Optional application-defined metadata. Never null; the empty
    /// dictionary is returned when no extra values are provided so that
    /// consumers can iterate without null checks.
    /// </summary>
    public IReadOnlyDictionary<string, string> Extra { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// A metadata instance with the current UTC timestamp, an empty
    /// request ID, and no extra values. Useful as a starting point in
    /// tests and as an internal default before correlation is applied.
    /// </summary>
    public static ResponseMetadata Empty => new();

    /// <summary>
    /// Returns a copy of this metadata with the request identifier replaced.
    /// The original instance is unchanged.
    /// </summary>
    /// <param name="requestId">The correlation ID to assign.</param>
    /// <returns>A new <see cref="ResponseMetadata"/> with the given request ID.</returns>
    public ResponseMetadata WithRequestId(string requestId)
    {
        ArgumentNullException.ThrowIfNull(requestId);
        return this with { RequestId = requestId };
    }
}

