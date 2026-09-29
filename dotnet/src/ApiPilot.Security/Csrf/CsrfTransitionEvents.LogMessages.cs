// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfTransitionEvents.LogMessages.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Compiled LoggerMessage delegates for the CSRF transition listener
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : DefaultCsrfTransitionListener
//   See also   : CsrfTransitionEvents.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

public sealed partial class DefaultCsrfTransitionListener
{
    private static readonly Action<ILogger, string, Exception?> LogBindingUnresolvedAtLogoutDelegate =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6001, "BindingUnresolvedAtLogout"),
            "OnLogoutAsync could not resolve a binding from the current context (reason: {Reason}). " +
            "If session state was already cleared, call OnLogoutAsync with the binding captured before clearing.");

    private static readonly Action<ILogger, string, Exception?> LogNoStoreAtLogoutDelegate =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(6002, "NoStoreAtLogout"),
            "OnLogoutAsync was called but no {StoreInterface} is registered. " +
            "No rotation marker was cleared. The behavior is a no-op.");
}

