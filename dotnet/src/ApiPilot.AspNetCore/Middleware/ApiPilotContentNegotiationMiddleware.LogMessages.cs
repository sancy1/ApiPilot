// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotContentNegotiationMiddleware.LogMessages.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Compiled LoggerMessage delegates for the content negotiation middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : ApiPilotContentNegotiationMiddleware.InvokeAsync
//   See also   : ApiPilotContentNegotiationMiddleware.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.AspNetCore.Middleware;

public sealed partial class ApiPilotContentNegotiationMiddleware
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogNotAcceptableDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3001, "NotAcceptable"),
            "Request rejected: unacceptable Accept header '{Accept}' for {Method} {Path}.");

    private static readonly Action<ILogger, string, string, string, Exception?> LogUnsupportedMediaTypeDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(3002, "UnsupportedMediaType"),
            "Request rejected: unsupported Content-Type '{ContentType}' for {Method} {Path}.");
}

