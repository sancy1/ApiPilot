// filepath: dotnet/src/ApiPilot.Security/DataProtection/DevelopmentOnlyWarning.cs
// layer: DataProtection | package: ApiPilot.Security | since: v0.3.0
// purpose: One-shot startup warning when the in-memory Data Protection key ring is used
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ILogger, Microsoft.Extensions.Logging
//   Used by    : ApiPilotDataProtectionExtensions
//   See also   : DataProtectionOptions.cs, InstanceSafetyValidator.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.DataProtection;

/// <summary>
/// Emits a one-shot warning when the in-memory Data Protection key
/// ring is used. The warning is informational: the in-memory key ring
/// is acceptable for single-instance development but unsuitable for
/// multi-instance deployments. The warning fires only once per
/// process.
/// </summary>
/// <remarks>
/// The warning never includes key material, key ring paths, or any
/// other filesystem detail. It states the condition and the
/// recommended remedy.
/// </remarks>
public static class DevelopmentOnlyWarning
{
    private static readonly Action<ILogger, Exception?> WarnDelegate =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(9001, "InMemoryKeyRingInUse"),
            "ApiPilot: Data Protection is using the in-memory key ring. " +
            "This is acceptable for single-instance development but unsuitable for " +
            "multi-instance deployments. Configure persistent key storage through " +
            "AddApiPilotDataProtection and set MultiInstance=true for production.");

    private static int _warned;

    /// <summary>
    /// Emits the warning if it has not already been emitted in this
    /// process.
    /// </summary>
    /// <param name="logger">The logger. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public static void WarnOnce(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        if (Interlocked.CompareExchange(ref _warned, 1, 0) != 0)
        {
            return;
        }

        WarnDelegate(logger, null);
    }
}

