// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfBootstrapEndpoint.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The handler logic for the CSRF bootstrap endpoint
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ICsrfService, CsrfBootstrapOptions, CsrfOptions,
//                ErrorResponseResult, ApiError, ApiErrorCode, ErrorResponse,
//                ResponseMetadata, ICorrelationIdAccessor
//   Used by    : CsrfBootstrapExtensions
//   See also   : CsrfBootstrapExtensions.cs, SPEC.md (CSRF bootstrap response)
// -----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The handler logic for the CSRF bootstrap endpoint. The endpoint is a
/// GET that returns { "token": "..." } in the response body. It issues a
/// fresh token bound to the current request's binding. When no binding
/// can be resolved, the endpoint responds with the standard ApiPilot
/// error envelope and HTTP 403.
/// </summary>
/// <remarks>
/// This is the one place in ApiPilot where the successful response is
/// not the standard success envelope. The wire shape is mandated by
/// SPEC.md. Do not wrap the token in the envelope.
///
/// The endpoint sets a Cache-Control header on every response. The
/// default is no-store so that intermediaries do not cache the token.
///
/// The endpoint never sets a cookie and never exposes authentication
/// material. The token in the response body is the only data.
/// </remarks>
public static class CsrfBootstrapEndpoint
{
    /// <summary>
    /// Handles a GET request to the bootstrap path.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <param name="service">
    /// The CSRF service. Must not be null.
    /// </param>
    /// <param name="bootstrapOptions">
    /// The bootstrap endpoint options. Must not be null.
    /// </param>
    /// <param name="correlationAccessor">
    /// Optional accessor for the current correlation ID. When null, the
    /// endpoint falls back to HttpContext.TraceIdentifier on error
    /// responses.
    /// </param>
    /// <returns>A task that completes when the response is written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    public static async Task HandleAsync(
        HttpContext context,
        ICsrfService service,
        CsrfBootstrapOptions bootstrapOptions,
        ICorrelationIdAccessor? correlationAccessor = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(bootstrapOptions);

        context.Response.Headers.CacheControl = bootstrapOptions.CacheControl;

        var token = await service.IssueAsync(context).ConfigureAwait(false);

        if (token is null)
        {
            await WriteErrorAsync(
                context,
                correlationAccessor,
                ApiErrorCode.CsrfTokenInvalid,
                "The bootstrap endpoint could not issue a token because no session binding is available.")
                .ConfigureAwait(false);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json; charset=utf-8";

        // The wire shape is fixed by SPEC.md: a bare object with the
        // token string and no other field. Do not add fields.
        var payload = new CsrfBootstrapResponse(token.Value);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            payload,
            CsrfBootstrapJsonContext.Default.CsrfBootstrapResponse)
            .ConfigureAwait(false);
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        ICorrelationIdAccessor? correlationAccessor,
        ApiErrorCode code,
        string message)
    {
        var accessorId = correlationAccessor?.RequestId;
        var requestId = string.IsNullOrEmpty(accessorId)
            ? context.TraceIdentifier
            : accessorId;

        var meta = new ResponseMetadata { RequestId = requestId };
        var error = ApiError.Create(code, message);
        var response = new ErrorResponse(error, meta);
        await new ErrorResponseResult(response).ExecuteAsync(context).ConfigureAwait(false);
    }
}

/// <summary>
/// The wire shape of the CSRF bootstrap response. A bare object with a
/// single token string. The shape is fixed by SPEC.md. The property name
/// is pinned with JsonPropertyName so the wire output does not depend on
/// any naming policy the serializer happens to apply.
/// </summary>
public sealed record CsrfBootstrapResponse
{
    /// <summary>
    /// Creates a bootstrap response.
    /// </summary>
    /// <param name="token">The CSRF request token.</param>
    public CsrfBootstrapResponse(string token)
    {
        Token = token;
    }

    /// <summary>
    /// The CSRF request token. Pinned to the lowercase wire key "token".
    /// </summary>
    [JsonPropertyName("token")]
    public string Token { get; }
}

/// <summary>
/// Source-generated JSON context for the bootstrap response. Avoids
/// reflection-based serialization on the hot path.
/// </summary>
[JsonSerializable(typeof(CsrfBootstrapResponse))]
internal sealed partial class CsrfBootstrapJsonContext : JsonSerializerContext
{
}

