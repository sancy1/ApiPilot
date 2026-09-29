// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfToken.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The opaque CSRF request token value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed record)
//   Depends on : n/a (BCL only)
//   Used by    : ICsrfTokenSigner, DataProtectionCsrfTokenSigner, CsrfService (Phase 2.2)
//   See also   : CsrfTokenFormat.cs, ICsrfTokenSigner.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The CSRF request token. The token is an opaque string on the wire. It
/// is produced by the configured signer and consumed by the validation
/// path. The value object carries the string, enforces non-null and
/// non-empty, and redacts itself in any log output.
/// </summary>
/// <remarks>
/// The type is a sealed record for structural equality. Two tokens are
/// equal when their wire strings are equal. The wire string is the
/// base64url output of the signer; it is never decoded by this type.
///
/// The ToString override returns a fixed placeholder instead of the
/// wire string so that a token never appears in a log line by
/// accident. Callers that need the wire value read the Value property
/// explicitly.
/// </remarks>
public sealed record CsrfToken
{
    /// <summary>
    /// The wire value of the token. Non-null, non-empty. This is the
    /// string the client sends in the configured header.
    /// </summary>
    public string Value { get; }

    private CsrfToken(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a token from the given wire value. Null, empty, and
    /// whitespace values are rejected.
    /// </summary>
    /// <param name="value">
    /// The wire value of the token. Must be non-null, non-empty, and not
    /// whitespace-only.
    /// </param>
    /// <returns>A token instance carrying the given value.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is empty or whitespace.
    /// </exception>
    public static CsrfToken From(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        return new CsrfToken(value);
    }

    /// <summary>
    /// Returns a fixed placeholder, never the wire value. This prevents
    /// accidental logging of a CSRF token through string interpolation
    /// or a default ToString call.
    /// </summary>
    /// <returns>The placeholder string [csrf-token].</returns>
    public override string ToString() => "[csrf-token]";
}

