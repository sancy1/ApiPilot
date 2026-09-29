// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfBindingProvider.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The default CSRF binding provider resolving from subject claim, session, or pre-auth source
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : ICsrfBindingProvider
//   Depends on : CsrfOptions, CsrfBindingFailureReason, Microsoft.AspNetCore.Http
//   Used by    : CsrfService
//   See also   : ICsrfBindingProvider.cs, CsrfOptions.cs, CsrfService.cs
// -----------------------------------------------------------------------------

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The default CSRF binding provider. Resolves a stable, non-secret
/// binding from the current request using a fixed resolution order:
/// subject claim, session, then the configured pre-authentication
/// source. When no source produces a binding, the provider returns
/// false with a specific failure reason. It never generates a binding
/// silently.
/// </summary>
/// <remarks>
/// The binding is always derived through a SHA-256 fingerprint of the
/// source value, prefixed by a purpose string. The original value is
/// never used directly. The fingerprint is deterministic and
/// non-reversible. A SHA-256 hash is the right tool here because the
/// binding is a stable identifier, not a secret; the token payload
/// that contains the binding is protected by Data Protection, so a
/// tampered binding would fail signature verification before the
/// binding comparison runs.
///
/// The fingerprint is prefixed so that two different purposes do not
/// produce the same binding from the same source value. This keeps the
/// binding stable for the same session and stable across restarts.
///
/// The binding is never written to a log line. The fingerprint is
/// itself not a secret, but treating the binding as opaque keeps the
/// API contract clean.
/// </remarks>
public sealed class CsrfBindingProvider : ICsrfBindingProvider
{
    private const string FingerprintPurpose = "ApiPilot.Csrf.Binding.v1";
    private const string SubjectClaimPrimary = "sub";

    private readonly CsrfOptions _options;

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="options">The CSRF service options. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public CsrfBindingProvider(CsrfOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <inheritdoc />
    public bool TryGetBinding(
        HttpContext context,
        out string binding,
        out CsrfBindingFailureReason failureReason)
    {
        ArgumentNullException.ThrowIfNull(context);

        binding = string.Empty;
        failureReason = CsrfBindingFailureReason.None;

        // Source 1: a stable authenticated subject claim.
        var subject = ReadSubjectClaim(context);
        if (subject is not null)
        {
            binding = Fingerprint(subject);
            return true;
        }

        // Source 2: an established server-managed session. The whole
        // session access is wrapped because HttpContext.Session throws
        // when session middleware is not registered, and Session.Id
        // throws when the session has not been established yet.
        var sessionId = TryGetSessionId(context);
        if (sessionId is not null)
        {
            binding = Fingerprint("session:" + sessionId);
            return true;
        }

        // Source 3: an explicitly configured pre-authentication source.
        if (_options.PreAuthBindingSource is not null)
        {
            var preAuth = _options.PreAuthBindingSource(context);
            if (!string.IsNullOrEmpty(preAuth))
            {
                binding = Fingerprint("preauth:" + preAuth);
                return true;
            }

            // The source was configured but returned no binding.
            failureReason = CsrfBindingFailureReason.ProviderFailure;
            return false;
        }

        // Fail closed. No source produced a binding.
        failureReason = DecideFailClosedReason(context);
        return false;
    }

    private static string? ReadSubjectClaim(HttpContext context)
    {
        var user = context.User;
        if (user is null || user.Identity is null || !user.Identity.IsAuthenticated)
        {
            return null;
        }

        foreach (var claim in user.Claims)
        {
            if (string.Equals(claim.Type, SubjectClaimPrimary, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(claim.Value))
                {
                    return claim.Value;
                }
            }
        }

        var nameId = user.FindFirst(ClaimTypes.NameIdentifier);
        if (nameId is not null && !string.IsNullOrEmpty(nameId.Value))
        {
            return nameId.Value;
        }

        return null;
    }

    private static string? TryGetSessionId(HttpContext context)
    {
        try
        {
            var session = context.Session;
            if (session is null || !session.IsAvailable)
            {
                return null;
            }

            var id = session.Id;
            return string.IsNullOrEmpty(id) ? null : id;
        }
        catch (InvalidOperationException)
        {
            // Session middleware is not registered for this application,
            // or the session has not been established for this request.
            // Either way, no session binding is available.
            return null;
        }
    }

    private static CsrfBindingFailureReason DecideFailClosedReason(HttpContext context)
    {
        var hasAuthenticatedUser = context.User is not null
            && context.User.Identity is not null
            && context.User.Identity.IsAuthenticated;

        if (hasAuthenticatedUser)
        {
            // The user is authenticated but has no stable subject claim.
            return CsrfBindingFailureReason.NoSubject;
        }

        if (TryGetSessionId(context) is null)
        {
            // No usable session and no authenticated user.
            return CsrfBindingFailureReason.NoSession;
        }

        // Session is available but had no id, and no subject was present.
        // This is the fail-closed default when the application did not
        // configure a pre-auth source.
        return CsrfBindingFailureReason.NoConfiguredSource;
    }

    private static string Fingerprint(string source)
    {
        // The purpose prefix ensures two different purposes do not
        // produce the same binding from the same source value.
        var payload = FingerprintPurpose + "|" + source;
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('/', '_')
            .Replace('+', '-');
    }
}

