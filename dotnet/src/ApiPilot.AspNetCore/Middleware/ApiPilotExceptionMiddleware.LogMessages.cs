// filepath: dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotExceptionMiddleware.LogMessages.cs
// layer: Middleware | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Compiled LoggerMessage delegates for each log level used by the middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (partial class continuation)
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : ApiPilotExceptionMiddleware.LogSafely
//   See also   : ApiPilotExceptionMiddleware.cs, PLANNING.md (finding A-042)
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.AspNetCore.Middleware;

public sealed partial class ApiPilotExceptionMiddleware
{
    private static readonly Action<ILogger, string, string, string, string, Exception?> LogErrorDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Error,
            new EventId(1001, "ExceptionMappedError"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> LogWarningDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Warning,
            new EventId(1002, "ExceptionMappedWarning"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> LogInformationDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Information,
            new EventId(1003, "ExceptionMappedInformation"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> LogDebugDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Debug,
            new EventId(1004, "ExceptionMappedDebug"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> LogCriticalDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Critical,
            new EventId(1005, "ExceptionMappedCritical"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, string, string, string, string, Exception?> LogTraceDelegate =
        LoggerMessage.Define<string, string, string, string>(
            LogLevel.Trace,
            new EventId(1006, "ExceptionMappedTrace"),
            "Exception mapped to {Code} for {Method} {Path}{Detail}");

    private static readonly Action<ILogger, Exception?> LogResponseStartedDelegate =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1007, "ExceptionAfterResponseStarted"),
            "Unhandled exception after the response had started.");

    private static Action<ILogger, string, string, string, string, Exception?> ResolveLogDelegate(LogLevel level)
    {
        return level switch
        {
            LogLevel.Critical    => LogCriticalDelegate,
            LogLevel.Error       => LogErrorDelegate,
            LogLevel.Warning     => LogWarningDelegate,
            LogLevel.Information => LogInformationDelegate,
            LogLevel.Debug       => LogDebugDelegate,
            LogLevel.Trace       => LogTraceDelegate,
            _                    => LogErrorDelegate,
        };
    }
}

