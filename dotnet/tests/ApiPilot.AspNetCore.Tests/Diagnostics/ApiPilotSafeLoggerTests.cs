// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Diagnostics/ApiPilotSafeLoggerTests.cs
// layer: Diagnostics.Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Tests for the ApiPilotSafeLogger ILogger decorator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : ApiPilotSafeLogger, ApiPilotRedactionPolicy, Microsoft.Extensions.Logging
//   Used by    : the test harness
//   See also   : ApiPilotSafeLogger.cs, CHANGELOG.md (findings A-210, A-211, A-215)
// -----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using ApiPilot.AspNetCore.Diagnostics;

namespace ApiPilot.AspNetCore.Tests.Diagnostics;

[TestClass]
public sealed class ApiPilotSafeLoggerTests
{
    [Test]
    public void Constructor_RejectsNullInner()
    {
        TestAssert.Throws<ArgumentNullException>(() => new ApiPilotSafeLogger(null!));
    }

    [Test]
    public void Wrap_RejectsNullInner()
    {
        TestAssert.Throws<ArgumentNullException>(() => ApiPilotSafeLogger.Wrap(null!));
    }

    [Test]
    public void IsEnabled_DelegatesToInner()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        TestAssert.True(logger.IsEnabled(LogLevel.Information));

        var innerDisabled = new CapturingLogger(isEnabledValue: false);
        var loggerDisabled = new ApiPilotSafeLogger(innerDisabled);
        TestAssert.False(loggerDisabled.IsEnabled(LogLevel.Information));
    }

    [Test]
    public void BeginScope_DelegatesToInner()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        using var scope = logger.BeginScope("test-scope");
        TestAssert.True(inner.BeginScopeCalled);
    }

    [Test]
    public void Log_WithNoRedactedKey_PassesStateUnchanged()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        var state = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("Method", "GET"),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Method: {Method}"),
        };
        Func<List<KeyValuePair<string, object?>>, Exception?, string> formatter =
            (s, e) => "Method: GET";

        logger.Log(LogLevel.Information, new EventId(1, "Test"), state, null, formatter);

        TestAssert.True(inner.LastState is List<KeyValuePair<string, object?>>);
        TestAssert.Equal(2, inner.LastStateCount);
    }

    [Test]
    public void Log_WithRedactedKey_ReplacesValueWithPlaceholder()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        var state = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("Authorization", "Bearer secret-token"),
            new KeyValuePair<string, object?>("Method", "GET"),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Authorization: {Authorization}, Method: {Method}"),
        };
        Func<List<KeyValuePair<string, object?>>, Exception?, string> formatter =
            (s, e) => "Authorization: Bearer secret-token, Method: GET";

        logger.Log(LogLevel.Information, new EventId(1, "Test"), state, null, formatter);

        TestAssert.NotNull(inner.LastState);
        var redactedPairs = (IReadOnlyList<KeyValuePair<string, object?>>)inner.LastState!;
        TestAssert.Equal(3, redactedPairs.Count);

        var authValue = redactedPairs[0].Value;
        TestAssert.Equal(ApiPilotRedactionPolicy.RedactedValue, authValue);

        var methodValue = redactedPairs[1].Value;
        TestAssert.Equal("GET", methodValue);
    }

    [Test]
    public void Log_WithRedactedKey_PreservesOriginalFormat()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        var template = "Authorization: {Authorization}, Method: {Method}";
        var state = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("Authorization", "Bearer secret"),
            new KeyValuePair<string, object?>("Method", "GET"),
            new KeyValuePair<string, object?>("{OriginalFormat}", template),
        };
        Func<List<KeyValuePair<string, object?>>, Exception?, string> formatter = (s, e) => template;

        logger.Log(LogLevel.Information, new EventId(1, "Test"), state, null, formatter);

        var redactedPairs = (IReadOnlyList<KeyValuePair<string, object?>>)inner.LastState!;
        var found = false;
        foreach (var kv in redactedPairs)
        {
            if (kv.Key == "{OriginalFormat}")
            {
                TestAssert.Equal(template, kv.Value);
                found = true;
            }
        }
        TestAssert.True(found);
    }

    [Test]
    public void Log_WithRedactedKey_DoesNotContainOriginalSecretInState()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        var secret = "Bearer secret-token-xyz";
        var state = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("Authorization", secret),
            new KeyValuePair<string, object?>("{OriginalFormat}", "Authorization: {Authorization}"),
        };
        Func<List<KeyValuePair<string, object?>>, Exception?, string> formatter = (s, e) => "Authorization: " + secret;

        logger.Log(LogLevel.Information, new EventId(1, "Test"), state, null, formatter);

        var redactedPairs = (IReadOnlyList<KeyValuePair<string, object?>>)inner.LastState!;
        foreach (var kv in redactedPairs)
        {
            if (kv.Value is string s)
            {
                TestAssert.False(s.Contains("secret-token-xyz", StringComparison.Ordinal));
            }
        }
    }

    [Test]
    public void Log_WithRedactedKey_PassesExceptionThroughUnchanged()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        var original = new InvalidOperationException("boom");
        var state = new List<KeyValuePair<string, object?>>
        {
            new KeyValuePair<string, object?>("Authorization", "Bearer secret"),
        };
        Func<List<KeyValuePair<string, object?>>, Exception?, string> formatter = (s, e) => "log";

        logger.Log(LogLevel.Error, new EventId(1, "Test"), state, original, formatter);

        TestAssert.Equal(original, inner.LastException);
    }

    [Test]
    public void Log_NonStructuredState_PassesThroughUnchanged()
    {
        var inner = new CapturingLogger(isEnabledValue: true);
        var logger = new ApiPilotSafeLogger(inner);
        Func<string, Exception?, string> formatter = (s, e) => s;

        logger.Log(LogLevel.Information, new EventId(1, "Test"), "plain-string-state", null, formatter);

        TestAssert.Equal("plain-string-state", inner.LastState);
    }

    private sealed class CapturingLogger : ILogger
{
        private readonly bool _isEnabledValue;

        public CapturingLogger(bool isEnabledValue)
        {
            _isEnabledValue = isEnabledValue;
        }

        public object? LastState { get; private set; }

        public int LastStateCount { get; private set; }

        public Exception? LastException { get; private set; }

        public bool BeginScopeCalled { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            BeginScopeCalled = true;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel) => _isEnabledValue;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LastState = state;
            if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
            {
                LastStateCount = pairs.Count;
            }
            LastException = exception;
        }
    }
}

