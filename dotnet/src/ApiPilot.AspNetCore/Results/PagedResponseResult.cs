// filepath: dotnet/src/ApiPilot.AspNetCore/Results/PagedResponseResult.cs
// layer: Results | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: IResult and IActionResult that writes a PagedApiResponse<T> to the HTTP response
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Http.IResult, Microsoft.AspNetCore.Mvc.IActionResult
//   Depends on : PagedApiResponse<T>, PaginationHttpContextExtensions,
//                PaginationOptions, Microsoft.AspNetCore.Http.Json.JsonOptions
//   Used by    : ApiPilotResultsExtensions
//   See also   : PagedApiResponse.cs, ApiResponseResultOfT.cs, ErrorResponseResult.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Results;

/// <summary>
/// An <see cref="IResult"/> and <see cref="IActionResult"/> that writes a
/// <see cref="PagedApiResponse{T}"/> envelope to the HTTP response. The
/// HTTP status is read from the effective pagination options resolved for
/// the request (global plus per-endpoint overrides), falling back to the
/// globally configured
/// <see cref="ApiPilot.Core.Pagination.PaginationOptions.SuccessStatusCode"/>
/// (default 200) when the pagination filter did not run. The envelope is
/// serialized with the shared Microsoft.AspNetCore.Http.Json.JsonOptions
/// configuration applied by AddApiPilotJson, for both the IResult and
/// IActionResult entry points.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public sealed class PagedResponseResult<T> : IResult, IActionResult
{
    private readonly PagedApiResponse<T> _response;

    /// <summary>
    /// The wrapped response. Exposed internally for test inspection.
    /// </summary>
    internal PagedApiResponse<T> Response => _response;

    /// <summary>
    /// Creates a result from the given paginated response.
    /// </summary>
    /// <param name="response">The paginated response. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public PagedResponseResult(PagedApiResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);
        _response = response;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var effective = httpContext.GetEffectivePaginationOptions()
            ?? httpContext.RequestServices
                .GetRequiredService<IOptions<PaginationOptions>>()
                .Value;
        httpContext.Response.StatusCode = effective.SuccessStatusCode;
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

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await ExecuteAsync(context.HttpContext);
    }
}

