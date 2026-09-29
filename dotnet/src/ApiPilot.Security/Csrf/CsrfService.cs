// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfService.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The default CSRF service composing signer, binding provider, and rotation store
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICsrfService
//   Depends on : ICsrfTokenSigner, ICsrfBindingProvider, ICsrfRotationStore (optional),
//                CsrfOptions, CsrfTokenOptions, ILogger
//   Used by    : CsrfProtectionMiddleware (Phase 2.4), CsrfBootstrapEndpoint (Phase 2.3)
//   See also   : ICsrfService.cs, ICsrfTokenSigner.cs, ICsrfBindingProvider.cs, ICsrfRotationStore.cs
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The default CSRF service. Composes the signer, the binding provider,
/// and an optional rotation store. The service is the public entry point
/// for issuance, validation, and rotation.
/// </summary>
/// <remarks>
/// The rotation store is optional. When no store is registered, the
/// service operates in expiry-only mode: RotateAsync issues a fresh
/// token and the previous token remains valid until its natural expiry.
/// The service reports this weaker guarantee on every RotateAsync call
/// by emitting a structured warning. The warning is not throttled.
///
/// The service never generates an anonymous binding. A missing binding
/// is a normal, expected outcome that the caller handles. Issuance
/// returns null; validation returns a failure result with the code
/// selected by CsrfOptions.MissingHeaderCode or CsrfOptions.CodeMapping.
/// </remarks>
public sealed partial class CsrfService : ICsrfService
{
    private readonly ICsrfTokenSigner _signer;
    private readonly ICsrfBindingProvider _bindingProvider;
    private readonly ICsrfRotationStore? _rotationStore;
    private readonly CsrfOptions _options;
    private readonly CsrfTokenOptions _tokenOptions;
    private readonly ILogger<CsrfService> _logger;

    /// <summary>
    /// Creates the service.
    /// </summary>
    /// <param name="signer">The token signer. Must not be null.</param>
    /// <param name="bindingProvider">The binding provider. Must not be null.</param>
    /// <param name="options">The service options. Must not be null.</param>
    /// <param name="tokenOptions">The token signer options. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <param name="rotationStore">
    /// The optional rotation store. May be null. When null, rotation is
    /// expiry-only and RotateAsync reports the weaker guarantee.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required argument is null.
    /// </exception>
    public CsrfService(
        ICsrfTokenSigner signer,
        ICsrfBindingProvider bindingProvider,
        CsrfOptions options,
        CsrfTokenOptions tokenOptions,
        ILogger<CsrfService> logger,
        ICsrfRotationStore? rotationStore = null)
    {
        ArgumentNullException.ThrowIfNull(signer);
        ArgumentNullException.ThrowIfNull(bindingProvider);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(tokenOptions);
        ArgumentNullException.ThrowIfNull(logger);

        _signer = signer;
        _bindingProvider = bindingProvider;
        _options = options;
        _tokenOptions = tokenOptions;
        _logger = logger;
        _rotationStore = rotationStore;
    }

    /// <inheritdoc />
    public Task<CsrfToken?> IssueAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_bindingProvider.TryGetBinding(context, out var binding, out _))
        {
            return Task.FromResult<CsrfToken?>(null);
        }

        var token = _signer.Sign(binding);
        return Task.FromResult<CsrfToken?>(token);
    }

    /// <inheritdoc />
    public async Task<CsrfValidationResult> ValidateAsync(
        HttpContext context,
        string? rawHeaderValue,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // The header being absent is a pre-parse outcome. It maps to the
        // dedicated MissingHeaderCode, not to any parse-result code.
        if (rawHeaderValue is null)
        {
            return CsrfValidationResult.Failure(
                CsrfTokenParseResult.Malformed,
                _options.MissingHeaderCode);
        }

        if (!_bindingProvider.TryGetBinding(context, out var binding, out var bindingFailure))
        {
            var code = LookupCode(CsrfTokenParseResult.BindingMissing);
            return CsrfValidationResult.Failure(
                CsrfTokenParseResult.BindingMissing,
                code);
        }

        var token = CsrfToken.From(rawHeaderValue);
        var outcome = _signer.ValidateWithMetadata(token, binding);

        if (outcome.Reason != CsrfTokenParseResult.Ok)
        {
            return CsrfValidationResult.Failure(outcome.Reason, LookupCode(outcome.Reason));
        }

        // The signature and binding checks passed. Check the rotation
        // marker when a store is registered.
        if (_rotationStore is not null && outcome.IssuedAtUtc is { } issuedAt)
        {
            var marker = await _rotationStore
                .GetRotationMarkerAsync(binding, cancellationToken)
                .ConfigureAwait(false);

            if (marker is { } rotatedAt && issuedAt <= rotatedAt)
            {
                return CsrfValidationResult.Failure(
                    CsrfTokenParseResult.Rotated,
                    LookupCode(CsrfTokenParseResult.Rotated));
            }
        }

        return CsrfValidationResult.Success();
    }

    /// <inheritdoc />
    public async Task<CsrfToken?> RotateAsync(
        HttpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_bindingProvider.TryGetBinding(context, out var binding, out _))
        {
            return null;
        }

        if (_rotationStore is null)
        {
            // No rotation store. The previous token remains valid until
            // natural expiry. Report the weaker guarantee on every call.
            LogRotationWithoutStoreDelegate(_logger, nameof(ICsrfRotationStore), null);
        }

        var token = _signer.Sign(binding);

        if (_rotationStore is not null)
        {
            var now = _tokenOptions.TimeProvider.GetUtcNow();
            await _rotationStore
                .SetRotationMarkerAsync(binding, now, cancellationToken)
                .ConfigureAwait(false);
        }

        return token;
    }

    private string LookupCode(CsrfTokenParseResult reason)
    {
        if (_options.CodeMapping.TryGetValue(reason, out var code) && !string.IsNullOrEmpty(code))
        {
            return code;
        }

        // The mapping did not carry an entry. Fall back to a safe default
        // rather than emitting an empty code.
        return "CSRF_TOKEN_INVALID";
    }
}

