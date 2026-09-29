// filepath: dotnet/src/ApiPilot.Security/Csrf/ICsrfTransitionListener.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Listener interface for authentication transition events that affect CSRF state
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (interface)
//   Depends on : CsrfToken, Microsoft.AspNetCore.Http
//   Used by    : application authentication flows, DefaultCsrfTransitionListener
//   See also   : CsrfTransitionEvents.cs, CsrfService.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// A listener the application calls at authentication transitions to
/// keep the CSRF state consistent. The library never hooks ASP.NET Core
/// Identity, SignInManager, or any authentication framework directly.
/// The application calls OnLoginAsync after its authentication flow
/// completes and OnLogoutAsync before its session state ends.
/// </summary>
/// <remarks>
/// Login semantics: the binding derived from the new authenticated
/// subject differs from the binding derived from the pre-login session
/// or pre-auth source. OnLoginAsync issues a fresh token bound to the
/// new binding and returns it so the login response can deliver it to
/// the client.
///
/// Logout semantics: the CSRF state for the previous binding must be
/// cleared before the session state ends. Two overloads exist:
///
///   - OnLogoutAsync(HttpContext) resolves the binding from the current
///     context. It works when called before the application clears the
///     authentication or session state.
///
///   - OnLogoutAsync(string previousBinding) accepts the binding the
///     application captured before clearing session state. It works in
///     both orderings.
///
/// The recommended pattern is to capture the binding (through
/// ICsrfBindingProvider.TryGetBinding) before logout and pass it to the
/// string overload.
///
/// The listener never authenticates users, never writes cookies, and
/// never issues authentication material.
/// </remarks>
public interface ICsrfTransitionListener
{
    /// <summary>
    /// Called after the application authenticates a user. Issues a fresh
    /// CSRF token bound to the new authenticated subject.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null. The context must
    /// already reflect the authenticated user (the subject claim must be
    /// present).
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>
    /// A freshly signed token bound to the new subject, or null when no
    /// binding can be resolved for the context. The application should
    /// deliver the token to the client in the login response body.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    Task<CsrfToken?> OnLoginAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called before the application clears the authenticated session.
    /// Resolves the binding from the current context and clears the
    /// rotation marker for that binding when a store is registered.
    /// </summary>
    /// <param name="context">
    /// The current HTTP context. Must not be null. The context must still
    /// reflect the authenticated user or the established session.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>A task that completes when the marker is cleared.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="context"/> is null.
    /// </exception>
    Task OnLogoutAsync(
        HttpContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called at logout with the binding captured before the application
    /// cleared session state. Clears the rotation marker for that binding
    /// when a store is registered. Use this overload when the session is
    /// cleared before the logout handler runs.
    /// </summary>
    /// <param name="previousBinding">
    /// The binding captured before session state was cleared. Must not be
    /// null or empty.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to cancel the operation.
    /// </param>
    /// <returns>A task that completes when the marker is cleared.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="previousBinding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="previousBinding"/> is empty.
    /// </exception>
    Task OnLogoutAsync(
        string previousBinding,
        CancellationToken cancellationToken = default);
}

