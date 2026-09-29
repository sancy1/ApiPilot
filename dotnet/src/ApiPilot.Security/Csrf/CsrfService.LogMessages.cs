// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfService.LogMessages.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Compiled LoggerMessage delegates for CsrfService
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : CsrfService.RotateAsync
//   See also   : CsrfService.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

public sealed partial class CsrfService
{
    private static readonly Action<ILogger, string, Exception?> LogRotationWithoutStoreDelegate =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4001, "RotationWithoutStore"),
            "CsrfService.RotateAsync was called for binding [redacted] but no {StoreInterface} is registered. " +
            "The previous token remains valid until its natural expiry. " +
            "Register an ICsrfRotationStore implementation for true rotation, or set " +
            "CsrfOptions.RotationRequirement to Required to fail startup when no store is present.");
}

