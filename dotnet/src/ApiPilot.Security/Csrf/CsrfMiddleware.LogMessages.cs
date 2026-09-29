// filepath: dotnet/src/ApiPilot.Security/Csrf/CsrfMiddleware.LogMessages.cs
// layer: Csrf | package: ApiPilot.Security | since: v0.3.0
// purpose: Compiled LoggerMessage delegates for the CSRF protection middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : CsrfMiddleware.InvokeAsync
//   See also   : CsrfMiddleware.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Csrf;

public sealed partial class CsrfMiddleware
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogRejectedDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(5001, "CsrfRejected"),
            "CSRF validation failed for {Method} {Path}: reason {Reason}.");
}

