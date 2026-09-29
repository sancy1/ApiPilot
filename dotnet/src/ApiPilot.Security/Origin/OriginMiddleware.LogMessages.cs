// filepath: dotnet/src/ApiPilot.Security/Origin/OriginMiddleware.LogMessages.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Compiled LoggerMessage delegates for the Origin policy middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : OriginMiddleware.InvokeAsync
//   See also   : OriginMiddleware.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Origin;

public sealed partial class OriginMiddleware
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogRejectedDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(7001, "OriginRejected"),
            "Origin policy rejected {Method} {Path}: reason {Reason}.");
}

