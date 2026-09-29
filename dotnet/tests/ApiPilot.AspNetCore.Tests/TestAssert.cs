// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/TestAssert.cs
// layer: TestInfrastructure | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Assertion helpers that throw on failure with file and line diagnostics
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : System.Runtime.CompilerServices (CallerMemberName, CallerFilePath, CallerLineNumber)
//   Used by    : every test class in ApiPilot.AspNetCore.Tests
//   See also   : TestRunner.cs, Program.cs
// -----------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace ApiPilot.AspNetCore.Tests;

/// <summary>
/// Assertion helpers for the repository-local integration test harness.
/// Each method throws <see cref="TestFailureException"/> on failure. The
/// failure message includes the calling test name, source file, and line
/// number so that failures point at the exact assertion.
/// </summary>
public static class TestAssert
{
    /// <summary>Asserts that a condition is true.</summary>
    public static void True(
        bool condition,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            throw new TestFailureException(
                BuildMessage("Expected true but was false", message, caller, file, line));
        }
    }

    /// <summary>Asserts that a condition is false.</summary>
    public static void False(
        bool condition,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (condition)
        {
            throw new TestFailureException(
                BuildMessage("Expected false but was true", message, caller, file, line));
        }
    }

    /// <summary>Asserts that two values are equal.</summary>
    public static void Equal<T>(
        T expected,
        T actual,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new TestFailureException(
                BuildMessage(
                    $"Expected [{Format(expected)}] but was [{Format(actual)}]",
                    message, caller, file, line));
        }
    }

    /// <summary>Asserts that two values are not equal.</summary>
    public static void NotEqual<T>(
        T expected,
        T actual,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new TestFailureException(
                BuildMessage(
                    $"Expected value different from [{Format(expected)}]",
                    message, caller, file, line));
        }
    }

    /// <summary>Asserts that a value is null.</summary>
    public static void Null(
        object? value,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (value is not null)
        {
            throw new TestFailureException(
                BuildMessage($"Expected null but was [{Format(value)}]", message, caller, file, line));
        }
    }

    /// <summary>Asserts that a value is not null.</summary>
    public static void NotNull(
        object? value,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (value is null)
        {
            throw new TestFailureException(
                BuildMessage("Expected non-null but was null", message, caller, file, line));
        }
    }

    /// <summary>Asserts that a string contains a substring.</summary>
    public static void Contains(
        string expectedSubstring,
        string actual,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        ArgumentNullException.ThrowIfNull(expectedSubstring);
        ArgumentNullException.ThrowIfNull(actual);
        if (!actual.Contains(expectedSubstring, StringComparison.Ordinal))
        {
            throw new TestFailureException(
                BuildMessage(
                    $"Expected string to contain [{expectedSubstring}] but was [{actual}]",
                    message, caller, file, line));
        }
    }

    /// <summary>
    /// Asserts that an action throws an exception of the specified type, and
    /// returns the exception instance so the caller can inspect it.
    /// </summary>
    public static TException Throws<TException>(
        Action action,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception other)
        {
            throw new TestFailureException(
                BuildMessage(
                    $"Expected {typeof(TException).Name} but got {other.GetType().Name}: {other.Message}",
                    message, caller, file, line));
        }
        throw new TestFailureException(
            BuildMessage(
                $"Expected {typeof(TException).Name} but no exception was thrown",
                message, caller, file, line));
    }

    /// <summary>
    /// Asserts that an asynchronous action throws an exception of the specified
    /// type, and returns the exception instance so the caller can inspect it.
    /// </summary>
    public static async Task<TException> ThrowsAsync<TException>(
        Func<Task> action,
        string? message = null,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            await action();
        }
        catch (TException ex)
        {
            return ex;
        }
        catch (Exception other)
        {
            throw new TestFailureException(
                BuildMessage(
                    $"Expected {typeof(TException).Name} but got {other.GetType().Name}: {other.Message}",
                    message, caller, file, line));
        }
        throw new TestFailureException(
            BuildMessage(
                $"Expected {typeof(TException).Name} but no exception was thrown",
                message, caller, file, line));
    }

    private static string BuildMessage(
        string core,
        string? userMessage,
        string caller,
        string file,
        int line)
    {
        var prefix = userMessage is null ? core : $"{core} - {userMessage}";
        var location = $"{Path.GetFileName(file)}:{line}";
        return $"{caller} at {location}: {prefix}";
    }

    private static string Format<T>(T value)
    {
        return value is null ? "null" : value.ToString() ?? "null";
    }
}

/// <summary>
/// Exception thrown by <see cref="TestAssert"/> when an assertion fails.
/// The test runner catches this type and reports a test failure.
/// </summary>
public sealed class TestFailureException : Exception
{
    /// <summary>Creates a new failure with the given message.</summary>
    public TestFailureException(string message) : base(message) { }
}

