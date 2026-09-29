// filepath: dotnet/src/ApiPilot.Security/Diagnostics/DiagnosticCodes.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.3.0
// purpose: Internal structured log identifiers for the security diagnostics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : n/a
//   Used by    : ApiPilotSecurityDiagnostics, SecurityDiagnosticsHostedService
//   See also   : ApiPilotSecurityDiagnostics.cs, SecurityConfigurationValidator.cs
// -----------------------------------------------------------------------------

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// Internal structured log identifiers for the security diagnostics.
/// The codes are used only in server-side log lines. They are never
/// exposed on the wire and are not part of the error code table in
/// SPEC.md. A code is a stable identifier for a specific diagnostic
/// condition, not an HTTP status or a client-visible value.
/// </summary>
/// <remarks>
/// The prefix SEC marks a fatal condition (the host refuses to start
/// or the configuration is unsafe). The prefix SECW marks a
/// non-fatal warning (the configuration is acceptable but has a
/// known deficiency the operator should see).
/// </remarks>
public static class DiagnosticCodes
{
    /// <summary>
    /// The Data Protection layer is using the in-memory key ring.
    /// Non-fatal. Warning only. Emitted when MultiInstance is false
    /// and no KeyStorage delegate was supplied.
    /// </summary>
    public const string InMemoryKeyRing = "SECW001";

    /// <summary>
    /// The CSRF rotation requirement is Required but no
    /// ICsrfRotationStore is registered. Fatal. The host refuses to
    /// start.
    /// </summary>
    public const string RotationStoreMissing = "SEC001";

    /// <summary>
    /// An expected security validator is not registered in the
    /// container. Fatal. The host refuses to start, because a
    /// missing validator means an entire options type is unvalidated.
    /// </summary>
    public const string ValidatorMissing = "SEC002";

    /// <summary>
    /// A security options type is registered but its validator is not.
    /// Fatal. The host refuses to start.
    /// </summary>
    public const string OptionsWithoutValidator = "SEC003";
}

