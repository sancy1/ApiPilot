// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTransitionEvents.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Default implementation of the CSRF transition listener
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICsrfTransitionListener
//   Depends on : ICsrfService, ICsrfBindingProvider, ICsrfRotationStore (optional),
//                ICorrelationIdAccessor (optional), ILogger
//   Used by    : application authentication flows
//   See also   : ICsrfTransitionListener.cs, CsrfService.cs, CsrfBindingProvider.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The default CSRF transition listener. Composes over the CSRF service
/// and the binding provider. Login issues a fresh token for the new
/// binding. Logout clears the rotation marker for the previous binding
/// when a store is registered.
/// </summary>
/// <remarks>
/// The listener never authenticates users, never writes cookies, and
/// never issues authentication material. The application is responsible
/// for its own authentication and session lifecycle.
///
/// OnLogoutAsync with a context works when called before session state
/// ends. OnLogoutAsync with an explicit binding works in both orderings.
/// </remarks>
public sealed partial class DefaultCsrfTransitionListener : ICsrfTransitionListener
{
    private readonly ICsrfService _service;
    private readonly ICsrfBindingProvider _bindingProvider;
    private readonly ICsrfRotationStore? _rotationStore;
    private readonly ILogger<DefaultCsrfTransitionListener> _logger;

    /// <summary>Creates the listener.</summary>
    /// <param name="service">The CSRF service. Must not be null.</param>
    /// <param name="bindingProvider">The binding provider. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="rotationStore">
    /// The optional rotation store. When null, logout does not clear a
    /// marker and logs a structured warning.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required argument is null.
    /// </exception>
    public DefaultCsrfTransitionListener(
        ICsrfService service,
        ICsrfBindingProvider bindingProvider,
        ILogger<DefaultCsrfTransitionListener> logger,
        ICsrfRotationStore? rotationStore = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(bindingProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _service = service;
        _bindingProvider = bindingProvider;
        _logger = logger;
        _rotationStore = rotationStore;
    }

    /// <inheritdoc />
    public async Task<CsrfToken?> OnLoginAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The binding is derived from the new authenticated subject.
        // The service resolves the binding and signs a fresh token.
        return await _service
            .IssueAsync(context, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task OnLogoutAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The binding is resolved from the current context. This works
        // only when the context still reflects the authenticated user or
        // the established session. When the binding cannot be resolved,
        // the logout handler should have used the explicit-binding
        // overload instead.
        if (!_bindingProvider.TryGetBinding(context, out var binding, out var failure))
        {
            LogBindingUnresolvedAtLogoutDelegate(_logger, failure.ToString(), null);
            return Task.CompletedTask;
        }

        return ClearMarkerAsync(binding, cancellationToken);
    }

    /// <inheritdoc />
    public Task OnLogoutAsync(
        string previousBinding,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(previousBinding);
        return ClearMarkerAsync(previousBinding, cancellationToken);
    }

    private async Task ClearMarkerAsync(string binding, CancellationToken cancellationToken)
    {
        if (_rotationStore is null)
        {
            LogNoStoreAtLogoutDelegate(_logger, nameof(ICsrfRotationStore), null);
            return;
        }

        await _rotationStore
            .ClearRotationMarkerAsync(binding, cancellationToken)
            .ConfigureAwait(false);
    }
}

