// filepath: dotnet/src/ApiPilot.Core/Errors/ErrorResponse.cs
// layer: Errors | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: The ApiPilot error response envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiResponse
//   Depends on : ApiError, ResponseMetadata
//   Used by    : HTTP adapter (Phase 1), exception mapping middleware
//   See also   : SPEC.md (error envelope), ApiResponse.cs, ApiError.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Errors;

/// <summary>
/// The ApiPilot error response envelope. Success is always false. The
/// Error property carries a stable, machine-readable code and a safe
/// human-readable message. Optional field-level errors are carried inside
/// the Error object. Meta always carries correlation and timestamp.
/// </summary>
public sealed class ErrorResponse : IApiResponse
{
    /// <summary>
    /// Always false. Error responses are the failure side of the envelope.
    /// </summary>
    public bool Success => false;

    /// <summary>
    /// The error object: code, safe message, and optional field errors.
    /// </summary>
    public ApiError Error { get; }

    /// <summary>
    /// Non-business metadata attached to every response: the correlation
    /// ID, the server timestamp, and optional extras.
    /// </summary>
    public ResponseMetadata Meta { get; }

    /// <summary>
    /// Creates an error response envelope.
    /// </summary>
    /// <param name="error">The error object. Must not be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="error"/> or <paramref name="meta"/> is null.
    /// </exception>
    public ErrorResponse(ApiError error, ResponseMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(meta);
        Error = error;
        Meta = meta;
    }
}

