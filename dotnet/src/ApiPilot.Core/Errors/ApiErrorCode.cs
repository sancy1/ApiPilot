// filepath: dotnet/src/ApiPilot.Core/Errors/ApiErrorCode.cs
// layer: Errors | package: ApiPilot.Core | since: v0.1.0-alpha.0
// purpose: Immutable value object identifying an ApiPilot error code on the wire
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a
//   Used by    : ApiError, ErrorResponse, StandardErrorCodes, HTTP adapter (Phase 1)
//   See also   : SPEC.md (error code table), StandardErrorCodes.cs
// -----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace ApiPilot.Core.Errors;

/// <summary>
/// An immutable, extensible identifier for an error condition. The string
/// value carried by <see cref="Code"/> is the exact value that appears in
/// the error envelope on the wire. Standard ApiPilot codes are exposed as
/// static instances; applications may define additional codes through
/// <see cref="From"/> for domain-specific conditions.
/// </summary>
/// <remarks>
/// Codes are normalized to uppercase. This matches the wire contract in
/// SPEC.md, which uses uppercase codes such as VALIDATION_ERROR and
/// CSRF_TOKEN_INVALID. Structural equality is by the Code string, so two
/// instances constructed from the same value are equal.
/// </remarks>
[JsonConverter(typeof(ApiErrorCodeJsonConverter))]
public sealed record ApiErrorCode
{
    /// <summary>
    /// The wire value of the code. Always non-null and non-empty. Uppercase
    /// by construction.
    /// </summary>
    public string Code { get; }

    private ApiErrorCode(string code)
    {
        Code = code;
    }

    /// <summary>
    /// Creates a code from the given string. Null, empty, and whitespace
    /// values are rejected. The value is trimmed and uppercased so that
    /// "validation_error" and "VALIDATION_ERROR" produce the same code.
    /// </summary>
    /// <param name="code">The wire value of the code. Must be non-null and non-empty.</param>
    /// <returns>A new code instance carrying the normalized value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="code"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="code"/> is empty or whitespace.
    /// </exception>
    public static ApiErrorCode From(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var trimmed = code.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Error code must not be empty or whitespace.", nameof(code));
        }
        return new ApiErrorCode(trimmed.ToUpperInvariant());
    }

    /// <summary>Returns the wire value of the code.</summary>
    public override string ToString() => Code;

    /// <summary>Validation failure. HTTP 400 or 422 on the wire.</summary>
    public static ApiErrorCode ValidationError { get; } = new("VALIDATION_ERROR");

    /// <summary>Authentication is required but missing. HTTP 401 on the wire.</summary>
    public static ApiErrorCode AuthenticationRequired { get; } = new("AUTHENTICATION_REQUIRED");

    /// <summary>Authorization failure. HTTP 403 on the wire.</summary>
    public static ApiErrorCode Forbidden { get; } = new("FORBIDDEN");

    /// <summary>Missing CSRF header. HTTP 403 on the wire.</summary>
    public static ApiErrorCode CsrfHeaderMissing { get; } = new("CSRF_HEADER_MISSING");

    /// <summary>Invalid CSRF token. HTTP 403 on the wire.</summary>
    public static ApiErrorCode CsrfTokenInvalid { get; } = new("CSRF_TOKEN_INVALID");

    /// <summary>Expired CSRF token. HTTP 403 on the wire.</summary>
    public static ApiErrorCode CsrfTokenExpired { get; } = new("CSRF_TOKEN_EXPIRED");

    /// <summary>Origin rejected by policy. HTTP 403 on the wire.</summary>
    public static ApiErrorCode CsrfOriginRejected { get; } = new("CSRF_ORIGIN_REJECTED");

    /// <summary>Resource not found. HTTP 404 on the wire.</summary>
    public static ApiErrorCode ResourceNotFound { get; } = new("RESOURCE_NOT_FOUND");

    /// <summary>Conflict with current resource state. HTTP 409 on the wire.</summary>
    public static ApiErrorCode Conflict { get; } = new("CONFLICT");

    /// <summary>Rate limit exceeded. HTTP 429 on the wire.</summary>
    public static ApiErrorCode RateLimited { get; } = new("RATE_LIMITED");

    /// <summary>Unknown server-side failure. HTTP 500 on the wire.</summary>
    public static ApiErrorCode InternalError { get; } = new("INTERNAL_ERROR");

    /// <summary>Startup configuration error. Not a response code; fails closed at boot.</summary>
    public static ApiErrorCode ConfigurationError { get; } = new("CONFIGURATION_ERROR");

    /// <summary>The Accept header did not include any acceptable media type. HTTP 406 on the wire.</summary>
    public static ApiErrorCode NotAcceptable { get; } = new("NOT_ACCEPTABLE");

    /// <summary>The request Content-Type on a body-carrying method was not acceptable. HTTP 415 on the wire.</summary>
    public static ApiErrorCode UnsupportedMediaType { get; } = new("UNSUPPORTED_MEDIA_TYPE");
}

