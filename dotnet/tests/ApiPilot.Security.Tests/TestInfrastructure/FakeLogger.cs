// filepath: dotnet/tests/ApiPilot.Security.Tests/TestInfrastructure/FakeLogger.cs
// layer: TestInfrastructure | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Shared test-only ILogger that captures entries
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : Microsoft.Extensions.Logging.ILogger<T>
//   Depends on : Microsoft.Extensions.Logging
//   Used by    : every test class that asserts on log output
//   See also   : TestAssert.cs, TestRunner.cs, InProcessHost.cs
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace ApiPilot.Security.Tests;

/// <summary>A minimal ILogger for tests that captures entries.</summary>
internal sealed class FakeLogger<T> : ILogger<T>
{
    /// <summary>Captured log entries in the order they were written.</summary>
    public List<LogEntry> Entries { get; } = new();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
    }
}

/// <summary>A single captured log entry.</summary>
internal sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

