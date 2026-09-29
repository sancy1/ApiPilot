// filepath: dotnet/src/ApiPilot.Core/Responses/ApiResponseBuilder.cs
// layer: Responses | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Default implementation and static facade for ApiPilot response construction
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : IApiResponseBuilder (DefaultApiResponseBuilder)
//   Depends on : ApiResponse, ApiResponse<T>, ResponseMetadata, ApiResponseStatus
//   Used by    : application code, ASP.NET Core adapter (DI), tests
//   See also   : IApiResponseBuilder.cs, ApiResponse.cs, ApiResponseOfT.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Responses;

/// <summary>
/// Default implementation of <see cref="IApiResponseBuilder"/>. Stateless
/// and thread-safe. One shared instance is sufficient for the lifetime of
/// an application.
/// </summary>
public sealed class DefaultApiResponseBuilder : IApiResponseBuilder
{
    /// <inheritdoc />
    public ApiResponse NoContent(ResponseMetadata meta, string? message = null)
    {
        return new ApiResponse(ApiResponseStatus.NoContent, meta, message);
    }

    /// <inheritdoc />
    public ApiResponse<T> Ok<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return new ApiResponse<T>(data, ApiResponseStatus.Ok, meta, message);
    }

    /// <inheritdoc />
    public ApiResponse<T> Created<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return new ApiResponse<T>(data, ApiResponseStatus.Created, meta, message);
    }

    /// <inheritdoc />
    public ApiResponse<T> Accepted<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return new ApiResponse<T>(data, ApiResponseStatus.Accepted, meta, message);
    }
}

/// <summary>
/// Static facade over the default <see cref="IApiResponseBuilder"/>.
/// Provides an ergonomic, dependency-free way to build ApiPilot responses
/// while keeping the same implementation that is registered for dependency
/// injection. There is exactly one implementation; this facade simply
/// exposes it statically for call sites that do not need to inject a
/// builder instance.
/// </summary>
public static class ApiResponseBuilder
{
    /// <summary>
    /// The shared default builder instance. Stateless and safe to reuse
    /// across threads and requests.
    /// </summary>
    public static readonly IApiResponseBuilder Default = new DefaultApiResponseBuilder();

    /// <summary>
    /// Builds a successful response without a payload. Delegates to
    /// <see cref="Default"/>.<see cref="IApiResponseBuilder.NoContent"/>.
    /// </summary>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A non-generic successful response.</returns>
    public static ApiResponse NoContent(ResponseMetadata meta, string? message = null)
    {
        return Default.NoContent(meta, message);
    }

    /// <summary>
    /// Builds a successful response carrying a payload. Delegates to
    /// <see cref="Default"/>.<see cref="IApiResponseBuilder.Ok{T}"/>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The application payload. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response.</returns>
    public static ApiResponse<T> Ok<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return Default.Ok(data, meta, message);
    }

    /// <summary>
    /// Builds a successful response representing a newly created resource.
    /// Delegates to <see cref="Default"/>.<see cref="IApiResponseBuilder.Created{T}"/>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The created resource. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response with the Created category.</returns>
    public static ApiResponse<T> Created<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return Default.Created(data, meta, message);
    }

    /// <summary>
    /// Builds a successful response representing work accepted for processing.
    /// Delegates to <see cref="Default"/>.<see cref="IApiResponseBuilder.Accepted{T}"/>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The accepted payload. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response with the Accepted category.</returns>
    public static ApiResponse<T> Accepted<T>(T? data, ResponseMetadata meta, string? message = null)
    {
        return Default.Accepted(data, meta, message);
    }
}

