// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfValidationContext.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Per-request inputs for CSRF token validation
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : CsrfBindingFailureReason
//   Used by    : ICsrfService, CsrfService
//   See also   : ICsrfService.cs, CsrfService.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Per-request inputs for CSRF token validation. The context is
/// constructed by the caller (the middleware in Phase 2.4, or an
/// application that composes the service directly). The context is
/// immutable.
/// </summary>
/// <remarks>
/// The context carries the binding resolution result and the raw
/// header value. The binding is either resolved (with the binding
/// string) or unresolved (with a failure reason). When the binding is
/// unresolved, the service cannot validate any token, and the result
/// reflects the specific reason.
///
/// The binding string is a security-relevant value. It is never
/// exposed in a response body and never written to a log line.
/// </remarks>
public sealed record CsrfValidationContext
{
    /// <summary>
    /// The resolved binding for the current request, or null when the
    /// binding could not be resolved. Exactly one of Binding and
    /// BindingFailure is non-null on a well-formed context.
    /// </summary>
    public string? Binding { get; }

    /// <summary>
    /// The reason the binding could not be resolved, or None when the
    /// binding was resolved. Exactly one of Binding and BindingFailure
    /// is non-null on a well-formed context.
    /// </summary>
    public CsrfBindingFailureReason BindingFailure { get; }

    /// <summary>
    /// The raw value of the CSRF header as received from the client, or
    /// null when the header was absent. An empty string is a present
    /// header with an empty value and is treated as malformed; null is
    /// treated as a missing header by the middleware.
    /// </summary>
    public string? RawHeaderValue { get; }

    private CsrfValidationContext(
        string? binding,
        CsrfBindingFailureReason bindingFailure,
        string? rawHeaderValue)
    {
        Binding = binding;
        BindingFailure = bindingFailure;
        RawHeaderValue = rawHeaderValue;
    }

    /// <summary>
    /// Creates a context with a resolved binding.
    /// </summary>
    /// <param name="binding">
    /// The resolved binding. Must not be null or empty.
    /// </param>
    /// <param name="rawHeaderValue">
    /// The raw CSRF header value, or null when the header was absent.
    /// </param>
    /// <returns>A context with Binding set and BindingFailure None.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="binding"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="binding"/> is empty or whitespace.
    /// </exception>
    public static CsrfValidationContext WithBinding(string binding, string? rawHeaderValue)
    {
        ArgumentException.ThrowIfNullOrEmpty(binding);
        return new CsrfValidationContext(binding, CsrfBindingFailureReason.None, rawHeaderValue);
    }

    /// <summary>
    /// Creates a context with an unresolved binding.
    /// </summary>
    /// <param name="failure">
    /// The reason the binding could not be resolved. Must not be None.
    /// </param>
    /// <param name="rawHeaderValue">
    /// The raw CSRF header value, or null when the header was absent.
    /// </param>
    /// <returns>A context with Binding null and BindingFailure set.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="failure"/> is None.
    /// </exception>
    public static CsrfValidationContext WithoutBinding(
        CsrfBindingFailureReason failure,
        string? rawHeaderValue)
    {
        if (failure == CsrfBindingFailureReason.None)
        {
            throw new ArgumentException(
                "An unresolved binding must carry a specific failure reason, not None.",
                nameof(failure));
        }
        return new CsrfValidationContext(null, failure, rawHeaderValue);
    }
}

