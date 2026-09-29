// filepath: dotnet/src/ApiPilot.Security/Csrf/ICsrfBindingProvider.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Resolves the stable session binding for a CSRF token
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : CsrfBindingFailureReason, Microsoft.AspNetCore.Http
//   Used by    : CsrfService, applications with a custom binding source
//   See also   : CsrfBindingProvider.cs, CsrfService.cs, CsrfOptions.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Resolves the stable session binding for a CSRF token. The binding is
/// a non-secret identifier that is stable for the same security session
/// across requests and changes when the session changes (login, logout,
/// session teardown).
/// </summary>
/// <remarks>
/// The binding is a security-relevant value. It is included in the
/// protected token payload and compared during validation. It must
/// never be exposed in a client response body and must never be written
/// to a log line.
///
/// The method is synchronous because the default resolution is a
/// cheap per-request lookup on claims and session state. A custom
/// provider that needs asynchronous work can cache its result on the
/// HttpContext items collection on first call and return it on
/// subsequent calls.
///
/// Failure is a normal outcome. A provider that cannot produce a
/// binding returns false and sets a specific reason. The service never
/// silently generates a fallback binding; a missing binding means the
/// protected request cannot be validated.
/// </remarks>
public interface ICsrfBindingProvider
{
    /// <summary>
    /// Attempts to resolve the stable binding for the current request.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null.
    /// </param>
    /// <param name="binding">
    /// When the method returns true, receives the resolved binding. When
    /// the method returns false, receives an empty string.
    /// </param>
    /// <param name="failureReason">
    /// When the method returns false, receives the specific reason the
    /// binding could not be resolved. When the method returns true,
    /// receives <see cref="CsrfBindingFailureReason.None"/>.
    /// </param>
    /// <returns>True when a binding was resolved; false otherwise.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    bool TryGetBinding(
        HttpContext context,
        out string binding,
        out CsrfBindingFailureReason failureReason);
}

