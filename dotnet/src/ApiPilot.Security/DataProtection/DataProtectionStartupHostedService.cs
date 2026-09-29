// filepath: dotnet/src/ApiPilot.Security/DataProtection/DataProtectionStartupHostedService.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: Emits the Data Protection startup diagnostics once at host startup
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Hosting.IHostedService
//   Depends on : IDataProtectionStartupDiagnostics, ILogger
//   Used by    : AddApiPilotDataProtection via TryAddEnumerable
//   See also   : ApiPilotDataProtectionExtensions.cs, DevelopmentOnlyWarning.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Hosting;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Emits the Data Protection startup diagnostics once at host startup.
/// The service is registered by AddApiPilotDataProtection. The
/// diagnostics component decides whether a warning is appropriate;
/// the hosted service only calls Emit.
/// </summary>
public sealed class DataProtectionStartupHostedService : IHostedService
{
    private readonly IDataProtectionStartupDiagnostics _diagnostics;

    /// <summary>Creates the hosted service.</summary>
    /// <param name="diagnostics">The diagnostics component. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="diagnostics"/> is null.
    /// </exception>
    public DataProtectionStartupHostedService(IDataProtectionStartupDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        _diagnostics = diagnostics;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _diagnostics.Emit();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

