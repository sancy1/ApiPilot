// filepath: dotnet/src/ApiPilot.Security/Origin/FetchMetadataMiddleware.LogMessages.cs
// layer: Origin | package: ApiPilot.Security | since: v0.3.0
// purpose: Compiled LoggerMessage delegates for the Fetch Metadata middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : FetchMetadataMiddleware.InvokeAsync
//   See also   : FetchMetadataMiddleware.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Origin;

public sealed partial class FetchMetadataMiddleware
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogRejectedDelegate =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Warning,
            new EventId(8001, "FetchMetadataRejected"),
            "Fetch Metadata policy rejected {Method} {Path}: reason {Reason}.");
}

