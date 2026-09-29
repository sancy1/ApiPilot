// filepath: dotnet/src/ApiPilot.Core/Abstractions/IApiResponseBuilder.cs
// layer: Abstractions | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Contract for building ApiPilot success responses from application data
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : ApiResponse, ApiResponse<T>, ResponseMetadata
//   Used by    : DefaultApiResponseBuilder, ApiResponseBuilder facade, application code, tests
//   See also   : ApiResponse.cs, ApiResponseOfT.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;

namespace ApiPilot.Core.Abstractions;

/// <summary>
/// Contract for building ApiPilot success responses from application-owned
/// data. The builder attaches the standard envelope, response category,
/// optional message, and metadata to the caller-supplied payload. It does
/// not create or calculate business data, and it does not decide HTTP
/// status codes; that is the ASP.NET Core adapter&#39;s responsibility.
/// </summary>
/// <remarks>
/// The builder is intentionally stateless and pure: given a payload and
/// a metadata instance, it produces a response object. Correlation and
/// timestamping are performed by the middleware that constructs the
/// metadata; the builder trusts the metadata it is given.
/// </remarks>
public interface IApiResponseBuilder
{
    /// <summary>
    /// Builds a successful response without a payload. The typical use
    /// is a 204 No Content outcome after a successful state change.
    /// </summary>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A non-generic successful response.</returns>
    ApiResponse NoContent(ResponseMetadata meta, string? message = null);

    /// <summary>
    /// Builds a successful response carrying a payload. The category
    /// defaults to Ok (HTTP 200).
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The application payload. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response.</returns>
    ApiResponse<T> Ok<T>(T? data, ResponseMetadata meta, string? message = null);

    /// <summary>
    /// Builds a successful response representing a newly created resource.
    /// The category is Created (HTTP 201).
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The created resource. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response with the Created category.</returns>
    ApiResponse<T> Created<T>(T? data, ResponseMetadata meta, string? message = null);

    /// <summary>
    /// Builds a successful response representing work accepted for processing.
    /// The category is Accepted (HTTP 202).
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="data">The accepted payload. May be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <param name="message">An optional human-readable message.</param>
    /// <returns>A generic successful response with the Accepted category.</returns>
    ApiResponse<T> Accepted<T>(T? data, ResponseMetadata meta, string? message = null);
}

