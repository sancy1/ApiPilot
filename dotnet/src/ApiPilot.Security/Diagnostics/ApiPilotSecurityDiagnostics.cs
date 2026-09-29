// filepath: dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotSecurityDiagnostics.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.3.0
// purpose: The public security diagnostics surface
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : DiagnosticCodes, ApiPilotDataProtectionOptions
//   Used by    : SecurityDiagnosticsHostedService, application code
//   See also   : DiagnosticCodes.cs, SecurityConfigurationValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.DataProtection;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// The public security diagnostics surface. A DI service that
/// reports the state of the security configuration. The surface has
/// no endpoint; it is resolved from the container by application code
/// or by the SecurityDiagnosticsHostedService at startup.
/// </summary>
/// <remarks>
/// The diagnostics surface reports only non-fatal conditions. Fatal
/// conditions are enforced by ValidateOnStart: the host does not
/// start when a validator fails. The diagnostics do not reimplement
/// validation and do not call IValidateOptions&lt;T&gt;.Validate.
///
/// The diagnostics never expose token values, bindings, key paths,
/// cookie values, or any other secret material.
/// </remarks>
public sealed class ApiPilotSecurityDiagnostics
{
    private readonly ApiPilotDataProtectionOptions _dataProtection;

    /// <summary>Creates the diagnostics surface.</summary>
    /// <param name="dataProtection">
    /// The effective Data Protection options. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dataProtection"/> is null.
    /// </exception>
    public ApiPilotSecurityDiagnostics(ApiPilotDataProtectionOptions dataProtection)
    {
        ArgumentNullException.ThrowIfNull(dataProtection);
        _dataProtection = dataProtection;
    }

    /// <summary>
    /// Returns the current non-fatal diagnostics.
    /// </summary>
    /// <returns>A read-only list of diagnostics. May be empty.</returns>
    public IReadOnlyList<SecurityDiagnostic> GetDiagnostics()
    {
        var result = new List<SecurityDiagnostic>();

        if (!_dataProtection.MultiInstance && !_dataProtection.KeyStorageConfigured)
        {
            result.Add(new SecurityDiagnostic(
                DiagnosticCodes.InMemoryKeyRing,
                SecurityDiagnosticLevel.Warning,
                "Data Protection is using the in-memory key ring. " +
                "This is acceptable for single-instance development " +
                "but unsuitable for multi-instance deployments."));
        }

        return result;
    }

    /// <summary>
    /// Emits every non-fatal diagnostic through the logger.
    /// </summary>
    /// <param name="sink">
    /// The callback that receives each diagnostic. Must not be null.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sink"/> is null.
    /// </exception>
    public void Emit(Action<SecurityDiagnostic> sink)
    {
        ArgumentNullException.ThrowIfNull(sink);

        foreach (var diagnostic in GetDiagnostics())
        {
            sink(diagnostic);
        }
    }
}

/// <summary>The severity of a security diagnostic.</summary>
public enum SecurityDiagnosticLevel
{
    /// <summary>
    /// The condition is a warning. The host has started. The operator
    /// should review the configuration.
    /// </summary>
    Warning,

    /// <summary>
    /// The condition is fatal. The host has not started. This level is
    /// reserved for the fail-closed path; it is not produced by
    /// ApiPilotSecurityDiagnostics, which reports only non-fatal
    /// conditions.
    /// </summary>
    Fatal,
}

/// <summary>A single security diagnostic.</summary>
/// <param name="Code">The internal diagnostic code (SEC or SECW).</param>
/// <param name="Level">The severity.</param>
/// <param name="Message">The non-secret, human-readable description.</param>
public sealed record SecurityDiagnostic(string Code, SecurityDiagnosticLevel Level, string Message);

