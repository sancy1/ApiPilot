// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotCorrelationMiddleware.LogMessages.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Compiled LoggerMessage delegates for the correlation middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : ApiPilotCorrelationMiddleware.InvokeAsync
//   See also   : ApiPilotCorrelationMiddleware.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.AspNetCore.Middleware;

public sealed partial class ApiPilotCorrelationMiddleware
{
    private static readonly Action<ILogger, string, string, string, string, Exception?> LogInvalidIdDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(2001, "CorrelationIdInvalid"),
            "Invalid incoming correlation ID '{IncomingId}' for {Method} {Path}; policy {Policy} applied.");

    private static readonly Action<ILogger, string, string, string, Exception?> LogRejectedDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(2002, "CorrelationIdRejected"),
            "Request rejected: invalid correlation ID '{IncomingId}' for {Method} {Path}.");
}

