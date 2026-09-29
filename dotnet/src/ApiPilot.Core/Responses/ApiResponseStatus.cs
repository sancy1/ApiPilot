// filepath: dotnet/src/ApiPilot.Core/Responses/ApiResponseStatus.cs
// layer: Responses | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Transport-neutral category of an ApiPilot response for HTTP mapping
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : ApiResponse, ApiResponse<T>, ApiResponseBuilder, HTTP adapter (Phase 1)
//   See also   : SPEC.md (error code table), ApiResponseBuilder.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Core.Responses;

/// <summary>
/// Transport-neutral category of an ApiPilot response. Core assigns a
/// category when it builds a response; the ASP.NET Core adapter (Phase 1)
/// maps the category to the corresponding HTTP status code. Core does
/// not know about HTTP.
/// </summary>
/// <remarks>
/// The enum members are intentionally not assigned numeric values.
/// Numeric values would tie Core to a specific HTTP status code numbering,
/// which is exactly the coupling this type exists to avoid. The mapping
/// from category to HTTP status lives in the adapter.
/// </remarks>
public enum ApiResponseStatus
{
    /// <summary>A successful response carrying a payload or a message.</summary>
    Ok,

    /// <summary>A resource was successfully created. The payload is the created resource.</summary>
    Created,

    /// <summary>The request was accepted for processing but not yet completed.</summary>
    Accepted,

    /// <summary>The request succeeded and there is no payload to return.</summary>
    NoContent,

    /// <summary>The request was rejected because it was malformed or failed validation.</summary>
    BadRequest,

    /// <summary>The request lacked valid authentication credentials.</summary>
    Unauthorized,

    /// <summary>The authenticated caller was not permitted to perform the operation.</summary>
    Forbidden,

    /// <summary>The requested resource was not found.</summary>
    NotFound,

    /// <summary>The request conflicts with the current state of the resource.</summary>
    Conflict,

    /// <summary>The request was well-formed but could not be processed.</summary>
    Unprocessable,

    /// <summary>The caller exceeded an allowed rate of requests.</summary>
    TooManyRequests,

    /// <summary>An unexpected server-side failure occurred.</summary>
    ServerError,
}

