// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTokenValidateOutcome.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The outcome of a signer validation, including the issue time on success
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : CsrfTokenParseResult
//   Used by    : ICsrfTokenSigner.ValidateWithMetadata, DataProtectionCsrfTokenSigner, CsrfService
//   See also   : ICsrfTokenSigner.cs, CsrfService.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The outcome of a signer validation. Carries the internal parse reason
/// and, on success, the token issue time. The issue time is needed by
/// the service to compare against a rotation marker.
/// </summary>
/// <remarks>
/// The issue time is null when the reason is not Ok. On Ok, it is
/// always populated.
/// </remarks>
public sealed record CsrfTokenValidateOutcome
{
    /// <summary>
    /// The internal parse reason.
    /// </summary>
    public CsrfTokenParseResult Reason { get; }

    /// <summary>
    /// The token issue time, or null when the reason is not Ok.
    /// </summary>
    public DateTimeOffset? IssuedAtUtc { get; }

    private CsrfTokenValidateOutcome(CsrfTokenParseResult reason, DateTimeOffset? issuedAtUtc)
    {
        Reason = reason;
        IssuedAtUtc = issuedAtUtc;
    }

    /// <summary>
    /// Creates a success outcome with the given issue time.
    /// </summary>
    /// <param name="issuedAtUtc">
    /// The token issue time. Must be a UTC value.
    /// </param>
    /// <returns>An outcome with Reason Ok.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="issuedAtUtc"/> is not a UTC value.
    /// </exception>
    public static CsrfTokenValidateOutcome Success(DateTimeOffset issuedAtUtc)
    {
        if (issuedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "issuedAtUtc must be a UTC DateTimeOffset (offset 0).",
                nameof(issuedAtUtc));
        }
        return new CsrfTokenValidateOutcome(CsrfTokenParseResult.Ok, issuedAtUtc);
    }

    /// <summary>
    /// Creates a failure outcome with the given reason and no issue time.
    /// </summary>
    /// <param name="reason">
    /// The internal parse reason. Must not be Ok.
    /// </param>
    /// <returns>An outcome with IssuedAtUtc null.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="reason"/> is Ok.
    /// </exception>
    public static CsrfTokenValidateOutcome Failure(CsrfTokenParseResult reason)
    {
        if (reason == CsrfTokenParseResult.Ok)
        {
            throw new ArgumentException(
                "A failure outcome must carry a specific reason, not Ok.",
                nameof(reason));
        }
        return new CsrfTokenValidateOutcome(reason, null);
    }
}

