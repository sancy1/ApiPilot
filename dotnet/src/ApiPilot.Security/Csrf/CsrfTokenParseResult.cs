// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTokenParseResult.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: The outcome of a CSRF token validation attempt
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (enum)
//   Depends on : n/a
//   Used by    : ICsrfTokenSigner, DataProtectionCsrfTokenSigner, CsrfService (Phase 2.2)
//   See also   : ICsrfTokenSigner.cs, CsrfTokenFormat.cs, SPEC.md (CSRF flow)
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// The outcome of a CSRF token validation attempt. A token either parses
/// and validates, or fails for one of five specific reasons. The reason
/// is logged server-side. It is never returned to the client in the
/// response body; the client sees the general error code documented in
/// SPEC.md.
/// </summary>
public enum CsrfTokenParseResult
{
    /// <summary>
    /// The token was well-formed, its signature verified, it was issued
    /// within its lifetime, and its binding matched the current request.
    /// </summary>
    Ok,

    /// <summary>
    /// The token could not be decoded. The signature may have been valid
    /// but the plaintext did not carry the expected structure. This can
    /// occur if a token from a different producer is presented, or if the
    /// payload format has changed without a purpose-version bump.
    /// </summary>
    Malformed,

    /// <summary>
    /// The token was produced for a different version of the CSRF token
    /// format. Reserved for a future version. In v1 this reason is not
    /// produced by the default signer; it exists so that the enum surface
    /// is stable and a future v2 signer can use it without adding a new
    /// member.
    /// </summary>
    WrongVersion,

    /// <summary>
    /// The token's signature did not verify. This covers tampering, a
    /// token from a different Data Protection key ring, an expired key,
    /// and any other integrity failure that Data Protection reports as a
    /// CryptographicException. The reason for the failure is not
    /// distinguished, and no cryptographic detail is exposed.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// The token parsed and its signature verified, but its issue time
    /// plus its lifetime is before the current time. The token is expired
    /// and must not be accepted.
    /// </summary>
    Expired,

    /// <summary>
    /// The token parsed and its signature verified and it is within its
    /// lifetime, but its binding does not match the current request. This
    /// is the cross-session replay case. The reason is logged but the
    /// client sees only the general CSRF error code.
    /// </summary>
    WrongSession,

    /// <summary>
    /// The token was well-formed and its signature verified, but a
    /// rotation marker recorded for its binding is newer than the
    /// token's issue time. The token is no longer the current token
    /// for its binding and is rejected.
    /// </summary>
    /// <remarks>
    /// This reason is produced only when an ICsrfRotationStore is
    /// registered and a rotation marker for the binding is present.
    /// Without a store, rotation is expiry-only and this reason is
    /// never produced. The reason is logged server-side; the client
    /// sees the code selected by CsrfOptions.CodeMapping, which
    /// defaults to CSRF_TOKEN_INVALID for this reason.
    /// </remarks>
    Rotated,

    /// <summary>
    /// The current request has no resolvable binding for CSRF
    /// validation. This is a pre-signature outcome: the request
    /// could not produce a binding, so no token could be validated.
    /// The reason is logged server-side; the client sees the code
    /// selected by CsrfOptions.CodeMapping, which defaults to
    /// CSRF_TOKEN_INVALID for this reason.
    /// </summary>
    BindingMissing,
}

