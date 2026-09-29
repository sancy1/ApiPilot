// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfBindingFailureReason.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The reason a CSRF binding could not be resolved for the current request
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : ICsrfBindingProvider, CsrfBindingProvider, CsrfService
//   See also   : ICsrfBindingProvider.cs, CsrfService.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The reason a CSRF binding could not be resolved for the current request.
/// A binding failure is a normal, expected outcome. The library never
/// silently generates an anonymous binding; a failure here means the
/// protected request cannot be validated, so the request is rejected
/// with the appropriate error code.
/// </summary>
/// <remarks>
/// The reason is used server-side. It is never returned to the client in
/// the response body. The client sees the general error code chosen by
/// CsrfOptions.CodeMapping or CsrfOptions.MissingHeaderCode, depending
/// on whether a header was present at all.
/// </remarks>
public enum CsrfBindingFailureReason
{
    /// <summary>
    /// No binding failure occurred. This value is not returned by a
    /// provider; it exists so that the enum has an explicit success value
    /// and so that default-constructed reason values are meaningful.
    /// </summary>
    None,

    /// <summary>
    /// The current request is not authenticated and has no stable subject
    /// claim. Pre-auth issuance is only possible when the application
    /// configured a pre-auth binding source; without one, no binding can
    /// be produced for an unauthenticated request.
    /// </summary>
    NoSubject,

    /// <summary>
    /// The current request has no established session. A server-managed
    /// session is a valid binding source only after the session has been
    /// established; a request-scoped placeholder would not survive across
    /// the issuance request and the validation request, so it is not used.
    /// </summary>
    NoSession,

    /// <summary>
    /// No binding source is available and no pre-auth source is configured.
    /// This is the fail-closed default for an unauthenticated request when
    /// the application has not opted in to pre-auth issuance.
    /// </summary>
    NoConfiguredSource,

    /// <summary>
    /// A configured binding source ran but failed to produce a value. This
    /// includes a custom ICsrfBindingProvider that returned false and a
    /// pre-auth source delegate that returned null or an empty value.
    /// </summary>
    ProviderFailure,
}

