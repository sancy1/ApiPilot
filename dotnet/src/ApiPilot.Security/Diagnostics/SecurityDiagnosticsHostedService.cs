// filepath: dotnet/src/ApiPilot.Security/Diagnostics/SecurityDiagnosticsHostedService.cs
// layer: Diagnostics | package: ApiPilot.Security | since: v0.3.0
// purpose: Emits security diagnostics at host startup and fails closed on missing validators
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Hosting.IHostedService
//   Depends on : SecurityConfigurationValidator, ApiPilotSecurityDiagnostics, ILogger
//   Used by    : security diagnostics registration
//   See also   : SecurityConfigurationValidator.cs, ApiPilotSecurityDiagnostics.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Diagnostics;

/// <summary>
/// Emits the security diagnostics at host startup. The service runs
/// after ValidateOnStart has enforced every fatal option failure. Its
/// remaining responsibility is to verify that the expected validators
/// are registered and to emit the non-fatal warnings.
/// </summary>
/// <remarks>
/// The service fails closed when a Fatal diagnostic is present. A
/// missing validator means an options type is not being validated at
/// startup; the host refuses to start. Non-fatal warnings are
/// emitted through the logger.
/// 
/// The service does not re-run validation and does not produce
/// duplicate startup failures. ValidateOnStart handles fatal option
/// failures; this service handles the registration-completeness
/// check.
/// </remarks>
public sealed class SecurityDiagnosticsHostedService : IHostedService
{
    private readonly SecurityConfigurationValidator _validator;
    private readonly ApiPilotSecurityDiagnostics _diagnostics;
    private readonly ILogger<SecurityDiagnosticsHostedService> _logger;

    /// <summary>Creates the hosted service.</summary>
    /// <param name="validator">The registration checker. Must not be null.</param>
    /// <param name="diagnostics">The diagnostics surface. Must not be null.</param>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when an argument is null.
    /// </exception>
    public SecurityDiagnosticsHostedService(
        SecurityConfigurationValidator validator,
        ApiPilotSecurityDiagnostics diagnostics,
        ILogger<SecurityDiagnosticsHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(logger);

        _validator = validator;
        _diagnostics = diagnostics;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Fatal check first: a missing validator prevents the host from
        // starting. The check is registration-completeness only; it does
        // not re-run validation.
        var fatal = new List<string>();
        foreach (var diagnostic in _validator.GetDiagnostics())
        {
            if (diagnostic.Level == SecurityDiagnosticLevel.Fatal)
            {
                fatal.Add(diagnostic.Code + ": " + diagnostic.Message);
            }
        }

        if (fatal.Count > 0)
        {
            throw new InvalidOperationException(
                "ApiPilot security configuration is incomplete: " +
                string.Join("; ", fatal));
        }

        // Non-fatal warnings.
        _diagnostics.Emit(diagnostic =>
        {
            LogDiagnosticDelegate(_logger, diagnostic.Code, diagnostic.Message, null);
        });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private static readonly Action<ILogger, string, string, Exception?> LogDiagnosticDelegate =
        LoggerMessage.Define<string, string>(
            LogLevel.Warning,
            new EventId(9100, "SecurityDiagnostic"),
            "ApiPilot security diagnostic {Code}: {Message}");
}

