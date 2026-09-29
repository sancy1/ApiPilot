// filepath: dotnet/src/ApiPilot.Security/Csrf/ICsrfService.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The CSRF issuance, validation, and rotation service
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : CsrfToken, CsrfValidationResult, Microsoft.AspNetCore.Http
//   Used by    : CsrfProtectionMiddleware (Phase 2.4), CsrfBootstrapEndpoint (Phase 2.3), applications
//   See also   : CsrfService.cs, ICsrfBindingProvider.cs, ICsrfTokenSigner.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The CSRF issuance, validation, and rotation service. The service is
/// the public entry point for CSRF operations. It resolves the binding
/// for the current request, calls the signer to produce or verify a
/// token, and consults the rotation store when one is registered.
/// </summary>
/// <remarks>
/// The service is async because the rotation store is async. The store
/// is optional. When no store is registered and rotation is not
/// required, RotateAsync operates in expiry-only mode and reports the
/// weaker guarantee honestly.
///
/// The service never returns a token when the binding cannot be
/// resolved. It never generates an anonymous binding. A missing binding
/// is a normal, expected outcome that the caller handles.
/// </remarks>
public interface ICsrfService
{
    /// <summary>
    /// Issues a fresh CSRF token bound to the current request's binding.
    /// Returns null when no binding can be resolved for the request.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A freshly signed token, or null when the binding could not be
    /// resolved.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    Task<CsrfToken?> IssueAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the given header value against the current request's
    /// binding. Never throws for an invalid token; a failure is a normal
    /// result.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <param name="rawHeaderValue">
    /// The raw CSRF header value as received, or null when the header
    /// was absent. An empty string is a present header with an empty
    /// value and is treated as malformed.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A result carrying the internal reason and the public wire code
    /// the middleware will emit.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    Task<CsrfValidationResult> ValidateAsync(
        HttpContext context,
        string? rawHeaderValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the token for the current request's binding. When a
    /// rotation store is registered, records a rotation marker so that
    /// previous tokens for the same binding are rejected. When no store
    /// is registered, issues a fresh token and the previous token
    /// remains valid until natural expiry.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A freshly signed token, or null when the binding could not be
    /// resolved.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    Task<CsrfToken?> RotateAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);
}

