// filepath: dotnet/src/ApiPilot.AspNetCore/Results/ApiResponseHttpMapper.cs
// layer: Results | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Maps transport-neutral response statuses to HTTP status codes
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilot.Core.Responses.ApiResponseStatus, Microsoft.AspNetCore.Http.StatusCodes
//   Used by    : ApiResponseResult, ApiResponseResult<T>
//   See also   : SPEC.md (error code table), ApiResponseResult.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.Results;

/// <summary>
/// Maps the transport-neutral <see cref="ApiResponseStatus"/> enum from
/// ApiPilot.Core to the corresponding HTTP status code. This is the single
/// place in ApiPilot where the core categories are translated to HTTP.
/// </summary>
public static class ApiResponseHttpMapper
{
    /// <summary>
    /// Returns the HTTP status code for the given response category.
    /// </summary>
    /// <param name="status">The transport-neutral response category.</param>
    /// <returns>The HTTP status code.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the status is not a known member of <see cref="ApiResponseStatus"/>.
    /// This is a defensive check; the switch is exhaustive for the current enum.
    /// </exception>
    public static int Map(ApiResponseStatus status)
    {
        return status switch
        {
            ApiResponseStatus.Ok              => StatusCodes.Status200OK,
            ApiResponseStatus.Created         => StatusCodes.Status201Created,
            ApiResponseStatus.Accepted        => StatusCodes.Status202Accepted,
            ApiResponseStatus.NoContent       => StatusCodes.Status204NoContent,
            ApiResponseStatus.BadRequest      => StatusCodes.Status400BadRequest,
            ApiResponseStatus.Unauthorized    => StatusCodes.Status401Unauthorized,
            ApiResponseStatus.Forbidden       => StatusCodes.Status403Forbidden,
            ApiResponseStatus.NotFound        => StatusCodes.Status404NotFound,
            ApiResponseStatus.Conflict        => StatusCodes.Status409Conflict,
            ApiResponseStatus.Unprocessable   => StatusCodes.Status422UnprocessableEntity,
            ApiResponseStatus.TooManyRequests => StatusCodes.Status429TooManyRequests,
            ApiResponseStatus.ServerError     => StatusCodes.Status500InternalServerError,
            _ => throw new ArgumentOutOfRangeException(
                nameof(status), status,
                $"No HTTP status mapping is defined for {status}."),
        };
    }
}

