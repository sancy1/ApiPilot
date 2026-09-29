// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTokenOptions.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Mutable configuration for CSRF token signing and validation
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : n/a (BCL only)
//   Used by    : DataProtectionCsrfTokenSigner, CsrfOptionsValidator
//   See also   : SPEC.md (CSRF flow), CsrfTokenFormat.cs, ICsrfTokenSigner.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Csrf;

/// <summary>
/// Mutable configuration for CSRF token signing and validation. The
/// defaults match the security contract in SPEC.md. Every property is
/// an Option D override point: an application changes the value without
/// forking the library.
/// </summary>
/// <remarks>
/// The ProtectionPurpose is the versioning mechanism. Two instances of
/// an application must use the same purpose to validate each other's
/// tokens. A future token format that is incompatible with v1 uses a
/// different purpose and can run alongside v1 during a migration.
///
/// The TokenLifetime and TimeProvider work together. The lifetime is
/// compared against the clock at validation time. Tests that need a
/// deterministic clock override TimeProvider rather than sleeping.
/// </remarks>
public sealed class CsrfTokenOptions
{
    /// <summary>
    /// The Data Protection purpose string. Defaults to the v1 purpose
    /// documented in SPEC.md. Two instances of an application must use
    /// the same purpose for their tokens to validate against each other.
    /// An application that changes the purpose invalidates every existing
    /// token.
    /// </summary>
    public string ProtectionPurpose { get; set; } = "ApiPilot.Csrf.v1";

    /// <summary>
    /// The number of random bytes placed in the token payload. Defaults
    /// to 32. The bytes are produced by the platform cryptographic random
    /// number generator and are base64url-encoded into the payload. A
    /// value below 16 weakens the token's uniqueness guarantee and is
    /// rejected by the startup validator.
    /// </summary>
    public int TokenEntropyBytes { get; set; } = 32;

    /// <summary>
    /// The token lifetime. A token whose issue time plus this lifetime is
    /// before the current time is rejected as expired. Defaults to two
    /// hours.
    /// </summary>
    public TimeSpan TokenLifetime { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// The clock used to determine the current time for issue and expiry
    /// checks. Defaults to the system clock. Tests substitute a fixed or
    /// advancing clock for determinism.
    /// </summary>
    public TimeProvider TimeProvider { get; set; } = TimeProvider.System;
}

