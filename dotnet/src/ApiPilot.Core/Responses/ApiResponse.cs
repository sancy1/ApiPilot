// filepath: dotnet/src/ApiPilot.Core/Responses/ApiResponse.cs
// layer: Responses | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Non-generic success response for envelopes without a payload
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiResponse
//   Depends on : ApiResponseStatus, ResponseMetadata
//   Used by    : ApiResponseBuilder (NoContent and similar), HTTP adapter (Phase 1)
//   See also   : ApiResponseOfT.cs, SPEC.md (success envelope), PLANNING.md
// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;
using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Responses;

/// <summary>
/// A successful ApiPilot response that carries no payload. Used for
/// NoContent (204) and any other success case where the server has
/// nothing to return to the client beyond the standard envelope and
/// metadata.
/// </summary>
/// <remarks>
/// Error responses are a distinct type (ErrorResponse, in the Errors
/// namespace). ApiResponse is the non-generic success side.
/// ApiResponse&lt;T&gt; is the generic success side.
/// </remarks>
public sealed class ApiResponse : IApiResponse
{
    /// <summary>
    /// Always true for this type. Error responses are represented by a
    /// separate type.
    /// </summary>
    public bool Success => true;

    /// <summary>
    /// The transport-neutral category of the response. Used by the
    /// ASP.NET Core adapter to select an HTTP status code.
    /// </summary>
    [JsonIgnore]
    public ApiResponseStatus Status { get; }
    /// <summary>
    /// An optional human-readable message. Null when the application
    /// does not provide one.
    /// </summary>
    public string? Message { get; }

    /// <summary>
    /// Non-business metadata attached to every response: the correlation
    /// ID, the server timestamp, and optional extras.
    /// </summary>
    public ResponseMetadata Meta { get; }

    /// <summary>
    /// Creates a successful response with the given category and metadata.
    /// </summary>
    /// <param name="status">The transport-neutral response category.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="meta"/> is null.
    /// </exception>
    public ApiResponse(
        ApiResponseStatus status,
        ResponseMetadata meta,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(meta);
        Status = status;
        Meta = meta;
        Message = message;
    }
}

