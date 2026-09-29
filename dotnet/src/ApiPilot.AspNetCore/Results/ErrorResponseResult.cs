// filepath: dotnet/src/ApiPilot.AspNetCore/Results/ErrorResponseResult.cs
// layer: Results | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: IResult that writes an ErrorResponse envelope with the mapped HTTP status
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.AspNetCore.Http.IResult
//   Depends on : ApiPilot.Core.Errors.ErrorResponse, ApiPilot.Core.Errors.ApiErrorCode,
//                Microsoft.AspNetCore.Http.Json.JsonOptions
//   Used by    : ApiPilotResultsExtensions, exception middleware
//   See also   : SPEC.md (error code table), ApiResponseResult.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Results;

/// <summary>
/// An <see cref="IResult"/> that writes an <see cref="ErrorResponse"/> to the
/// HTTP response. The HTTP status code is derived from the error code using
/// the ApiPilot error code table documented in SPEC.md.
/// </summary>
public sealed class ErrorResponseResult : IResult, IActionResult
{
    private readonly ErrorResponse _response;

    private readonly ApiExceptionOptions? _options;

    /// <summary>
    /// The wrapped error response. Exposed internally for test inspection.
    /// </summary>
    internal ErrorResponse Response => _response;

    /// <summary>Creates a result from the given error response.</summary>
    /// <param name="response">The error response. Must not be null.</param>
    /// <param name="options">
    /// Optional exception options that may include overrides for the
    /// error-code-to-status mapping. When null, the built-in mappings
    /// are used.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="response"/> is null.
    /// </exception>
    public ErrorResponseResult(ErrorResponse response, ApiExceptionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(response);
        _response = response;
        _options = options;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Response.StatusCode = MapErrorCode(_response.Error.Code);
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

    private int MapErrorCode(ApiErrorCode code)
    {
        var wireCode = code.Code;

        if (_options is not null &&
            _options.ErrorCodeToStatusMap.TryGetValue(wireCode, out var customStatus))
        {
            return customStatus;
        }

        return BuiltInMapErrorCode(wireCode);
    }

    private static int BuiltInMapErrorCode(string wireCode)
    {
        return wireCode switch
        {
            "VALIDATION_ERROR"        => StatusCodes.Status400BadRequest,
            "AUTHENTICATION_REQUIRED" => StatusCodes.Status401Unauthorized,
            "FORBIDDEN"               => StatusCodes.Status403Forbidden,
            "CSRF_HEADER_MISSING"     => StatusCodes.Status403Forbidden,
            "CSRF_TOKEN_INVALID"      => StatusCodes.Status403Forbidden,
            "CSRF_TOKEN_EXPIRED"      => StatusCodes.Status403Forbidden,
            "CSRF_ORIGIN_REJECTED"    => StatusCodes.Status403Forbidden,
            "RESOURCE_NOT_FOUND"      => StatusCodes.Status404NotFound,
            "CONFLICT"                => StatusCodes.Status409Conflict,
            "RATE_LIMITED"            => StatusCodes.Status429TooManyRequests,
            "NOT_ACCEPTABLE"          => StatusCodes.Status406NotAcceptable,
            "UNSUPPORTED_MEDIA_TYPE"  => StatusCodes.Status415UnsupportedMediaType,
            "INTERNAL_ERROR"          => StatusCodes.Status500InternalServerError,
            _                         => StatusCodes.Status500InternalServerError,
        };
    }

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        await ExecuteAsync(context.HttpContext);
    }
}

