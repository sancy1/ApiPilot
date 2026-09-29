// filepath: dotnet/src/ApiPilot.Security/Csrf/ICsrfTokenSigner.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The CSRF token signing and validation abstraction
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : CsrfToken, CsrfTokenParseResult
//   Used by    : DataProtectionCsrfTokenSigner, CsrfService (Phase 2.2)
//   See also   : DataProtectionCsrfTokenSigner.cs, CsrfTokenParseResult.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The CSRF token signing and validation abstraction. The default
/// implementation uses ASP.NET Core Data Protection. An application
/// with its own cryptographic infrastructure (an HSM, a KMS, a custom
/// Data Protection purpose layout) replaces this service through
/// dependency injection.
/// </summary>
/// <remarks>
/// The interface is intentionally narrow. It knows nothing about HTTP,
/// headers, middleware, or the response envelope. Those concerns belong
/// to the middleware and the service that compose on top of the signer.
/// The signer only produces and validates a token against a binding.
///
/// The binding is a string that identifies the session the token was
/// issued for. The default binding source is the authenticated session
/// identifier, supplied by the caller. The signer does not resolve the
/// binding itself; it only includes the binding in the protected
/// payload and checks it on validation.
/// </remarks>
public interface ICsrfTokenSigner
{
    /// <summary>
    /// Issues a new CSRF token bound to the given session binding.
    /// </summary>
    /// <param name="binding">
    /// The session binding the token is issued for. Must not be null or
    /// empty. Must not contain the pipe delimiter.
    /// </param>
    /// <returns>A freshly signed token.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="binding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty or contains the
    /// pipe delimiter.
    /// </exception>
    CsrfToken Sign(string binding);

    /// <summary>
    /// Validates a token against the given session binding. Returns a
    /// result that identifies the outcome. Never throws for an invalid
    /// token; a malformed or invalid token is a normal result, not an
    /// exception.
    /// </summary>
    /// <param name="token">
    /// The token to validate. Must not be null.
    /// </param>
    /// <param name="binding">
    /// The session binding the token must match. Must not be null or
    /// empty.
    /// </param>
    /// <returns>
    /// One of the <see cref="CsrfTokenParseResult"/> values. Ok on
    /// success; a specific reason otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="token"/> or <paramref name="binding"/>
    /// is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty.
    /// </exception>
    CsrfTokenParseResult TryValidate(CsrfToken token, string binding);

    /// <summary>
    /// Validates a token and returns both the reason and, on success,
    /// the token issue time. The issue time is needed by services that
    /// consult a rotation marker; the plain TryValidate method discards
    /// it. Implementations that produce Ok must populate IssuedAtUtc.
    /// </summary>
    /// <param name="token">
    /// The token to validate. Must not be null.
    /// </param>
    /// <param name="binding">
    /// The session binding the token must match. Must not be null or
    /// empty.
    /// </param>
    /// <returns>
    /// An outcome carrying the reason and, on success, the issue time.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="token"/> or <paramref name="binding"/>
    /// is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty.
    /// </exception>
    CsrfTokenValidateOutcome ValidateWithMetadata(CsrfToken token, string binding);
}

