// filepath: dotnet/src/ApiPilot.AspNetCore/Results/ApiResponseResult.cs
// layer: Results | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: IResult that writes a non-generic ApiResponse to the HTTP response
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Http.IResult
//   Depends on : ApiPilot.Core.Responses.ApiResponse, ApiResponseHttpMapper,
//                Microsoft.AspNetCore.Http.Json.JsonOptions
//   Used by    : ApiPilotResultsExtensions
//   See also   : ApiResponseResultOfT.cs, ApiPilotResultsExtensions.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Results;

/// <summary>
/// An <see cref="IResult"/> that writes a non-generic <see cref="ApiResponse"/>
/// envelope to the HTTP response. Used for NoContent and any other success
/// case that carries no payload.
/// </summary>
public sealed class ApiResponseResult : IResult
{
    private readonly ApiResponse _response;

    /// <summary>
    /// The wrapped response. Exposed internally for test inspection.
    /// </summary>
    internal ApiResponse Response => _response;

    /// <summary>Creates a result from the given non-generic response.</summary>
    /// <param name="response">The response. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public ApiResponseResult(ApiResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _response = response;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var statusCode = ApiResponseHttpMapper.Map(_response.Status);
        httpContext.Response.StatusCode = statusCode;

        // 204 No Content must not carry a body.
        if (statusCode == StatusCodes.Status204NoContent)
        {
            return;
        }

        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var jsonOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value;

        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            _response,
            jsonOptions.SerializerOptions,
            httpContext.RequestAborted);
    }
}

