// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfValidationResult.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The outcome of a CSRF validation attempt, pairing the internal reason with the public wire code
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : CsrfTokenParseResult
//   Used by    : ICsrfService, CsrfService
//   See also   : CsrfTokenParseResult.cs, CsrfService.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The outcome of a CSRF validation attempt. Pairs the internal parse
/// reason with the public wire code the middleware will emit. The two
/// are separate so that the internal reason can be logged and the
/// public code can be configured independently.
/// </summary>
/// <remarks>
/// A successful validation has Reason Ok and no public code. A failed
/// validation has a specific reason and a non-empty public code that
/// the middleware writes to the response error envelope.
///
/// The public code is chosen from CsrfOptions.CodeMapping or from
/// CsrfOptions.MissingHeaderCode, depending on whether a header was
/// present. The code is the exact string the client sees in the error
/// envelope under error.code.
/// </remarks>
public sealed record CsrfValidationResult
{
    /// <summary>
    /// The internal parse reason. Ok on success; a specific reason
    /// otherwise. The reason is logged server-side and is never returned
    /// to the client directly.
    /// </summary>
    public CsrfTokenParseResult Reason { get; }

    /// <summary>
    /// The public wire code the middleware will emit. Empty when the
    /// result is Ok. A non-empty code is one of the values chosen by
    /// CsrfOptions.CodeMapping or CsrfOptions.MissingHeaderCode.
    /// </summary>
    public string PublicCode { get; }

    /// <summary>
    /// True when the result represents a successful validation.
    /// </summary>
    public bool IsSuccess => Reason == CsrfTokenParseResult.Ok;

    private CsrfValidationResult(CsrfTokenParseResult reason, string publicCode)
    {
        Reason = reason;
        PublicCode = publicCode;
    }

    /// <summary>
    /// Creates a successful result. The public code is empty.
    /// </summary>
    /// <returns>A result with Reason Ok.</returns>
    public static CsrfValidationResult Success()
    {
        return new CsrfValidationResult(CsrfTokenParseResult.Ok, string.Empty);
    }

    /// <summary>
    /// Creates a failure result with the given internal reason and public
    /// code.
    /// </summary>
    /// <param name="reason">
    /// The internal parse reason. Must not be Ok.
    /// </param>
    /// <param name="publicCode">
    /// The public wire code. Must be non-null and non-empty.
    /// </param>
    /// <returns>A failure result.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="reason"/> is Ok, or when
    /// <paramref name="publicCode"/> is null, empty, or whitespace.
    /// </exception>
    public static CsrfValidationResult Failure(CsrfTokenParseResult reason, string publicCode)
    {
        if (reason == CsrfTokenParseResult.Ok)
        {
            throw new ArgumentException(
                "A failure result must carry a specific reason, not Ok.",
                nameof(reason));
        }
        ArgumentException.ThrowIfNullOrEmpty(publicCode);
        return new CsrfValidationResult(reason, publicCode);
    }
}

