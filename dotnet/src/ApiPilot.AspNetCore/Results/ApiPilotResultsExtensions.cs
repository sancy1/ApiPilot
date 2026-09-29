// filepath: dotnet/src/ApiPilot.AspNetCore/Results/ApiPilotResultsExtensions.cs
// layer: Results | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Extension methods that turn ApiPilot responses into IResult values
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class with extension methods)
//   Depends on : ApiPilot.Core.Responses.ApiResponse, ApiResponse<T>,
//                ApiResponseResult, ApiResponseResult<T>
//   Used by    : application endpoint code
//   See also   : ApiResponseResult.cs, ApiResponseResultOfT.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.Results;

/// <summary>
/// Extension methods that convert ApiPilot responses into <see cref="IResult"/>
/// values suitable for returning from ASP.NET Core minimal API endpoints.
/// </summary>
public static class ApiPilotResultsExtensions
{
    /// <summary>
    /// Converts a non-generic response into an <see cref="IResult"/>.
    /// </summary>
    /// <param name="response">The response. Must not be null.</param>
    /// <returns>An IResult that writes the response to HTTP.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public static IResult ToResult(this ApiResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new ApiResponseResult(response);
    }

    /// <summary>
    /// Converts a generic response into an <see cref="IResult"/>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <param name="response">The response. Must not be null.</param>
    /// <returns>An IResult that writes the response to HTTP.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public static IResult ToResult<T>(this ApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new ApiResponseResult<T>(response);
    }

    /// <summary>
    /// Converts an error response into an <see cref="IResult"/>. The HTTP
    /// status code is derived from the error code using the ApiPilot error
    /// code table.
    /// </summary>
    /// <param name="response">The error response. Must not be null.</param>
    /// <returns>An IResult that writes the error response to HTTP.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public static IResult ToResult(this ErrorResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new ErrorResponseResult(response);
    }
    /// <summary>
    /// Converts a paged result plus response metadata into an <see cref="IResult"/>
    /// carrying the standard paginated envelope.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="result">The paged result. Must not be null.</param>
    /// <param name="meta">The response metadata. Must not be null.</param>
    /// <returns>An IResult that writes the paginated response to HTTP.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> or <paramref name="meta"/> is null.
    /// </exception>
    public static IResult ToPagedResult<T>(
        this PagedResult<T> result,
        ResponseMetadata meta)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(meta);
        return new PagedResponseResult<T>(
            new PagedApiResponse<T>(result.Items, result.Pagination, meta));
    }

    /// <summary>
    /// Converts a paginated response into an <see cref="IResult"/>. This
    /// overload mirrors the ToResult extension on ApiResponse and
    /// ApiResponse&lt;T&gt; for call sites that already hold the envelope.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="response">The paginated response. Must not be null.</param>
    /// <returns>An IResult that writes the paginated response to HTTP.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public static IResult ToResult<T>(this PagedApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return new PagedResponseResult<T>(response);
    }

}

