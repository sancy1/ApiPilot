// filepath: dotnet/src/ApiPilot.Core/Responses/ApiResponseOfT.cs
// layer: Responses | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Generic success response that carries an application payload
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiResponse
//   Depends on : ApiResponseStatus, ResponseMetadata
//   Used by    : ApiResponseBuilder (Ok, Created, Accepted), HTTP adapter (Phase 1)
//   See also   : ApiResponse.cs, SPEC.md (success envelope), PaginationMetadata (Phase 0.5)
// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;
using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Responses;

/// <summary>
/// A successful ApiPilot response that carries an application payload of
/// type <typeparamref name="T"/>. This is the primary success type used by
/// applications returning orders, products, customers, reports, search
/// results, and any other JSON-serializable DTO.
/// </summary>
/// <typeparam name="T">
/// The type of the payload. May be a value type, a reference type, a
/// collection, or a nested DTO. The data property is nullable even for
/// value types when the caller uses Nullable&lt;T&gt; as the type argument.
/// </typeparam>
/// <remarks>
/// The data property is always present in the serialized envelope; its
/// value may be null. This matches the wire contract in SPEC.md, which
/// states that data is never omitted.
/// </remarks>
public sealed class ApiResponse<T> : IApiResponse
{
    /// <summary>
    /// Always true for this type. Error responses are represented by a
    /// separate type.
    /// </summary>
    public bool Success => true;

    /// <summary>
    /// The application payload. May be null when the caller explicitly
    /// passes null or when the type argument itself is nullable.
    /// </summary>
    public T? Data { get; }

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
    /// Creates a successful response carrying the given payload.
    /// </summary>
    /// <param name="data">The application payload. May be null.</param>
    /// <param name="status">The transport-neutral response category.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="meta"/> is null.
    /// </exception>
    public ApiResponse(
        T? data,
        ApiResponseStatus status,
        ResponseMetadata meta,
        string? message = null)
    {
        ArgumentNullException.ThrowIfNull(meta);
        Data = data;
        Status = status;
        Meta = meta;
        Message = message;
    }
}

