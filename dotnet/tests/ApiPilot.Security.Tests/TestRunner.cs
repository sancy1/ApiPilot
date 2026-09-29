// filepath: dotnet/tests/ApiPilot.Security.Tests/TestRunner.cs
// layer: TestInfrastructure | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Reflection-based discovery and execution of test classes and methods
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : TestAssert.cs (TestFailureException, TestClassAttribute, TestAttribute)
//                ApiPilot.TestHarness (TestOutcome, TestOutcomeClassifier, TestExitPolicy)
//   Used by    : Program.cs
//   See also   : TestAssert.cs, Program.cs, TestHarness.cs
// -----------------------------------------------------------------------------

using ApiPilot.TestHarness;
using System.Diagnostics;
using System.Reflection;

namespace ApiPilot.Security.Tests;

/// <summary>
/// Marks a class as a test class. Test classes must be public, non-abstract,
/// and have a parameterless constructor. Every method marked with
/// <see cref="TestAttribute"/> is discovered and executed.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestClassAttribute : Attribute { }

/// <summary>
/// Marks a method as a test. A test method must be public, parameterless,
/// non-static, and return void, Task, or TestOutcome. A void or Task method
/// is Passed when it completes normally and Failed when it throws. A method
/// returning TestOutcome reports its own kind and message explicitly.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class TestAttribute : Attribute { }

/// <summary>
/// Discovers and runs every test in the assembly that contains this type.
/// Prints one line per test and a summary at the end. Returns the exit code
/// from TestExitPolicy: 0 on pass, 1 on failure, 2 when a skip or
/// unavailability is present and --allow-skip was not supplied.
/// </summary>
public static class TestRunner
{
    private static readonly TimeSpan PerTestTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Runs all discovered tests. Returns the exit code.
    /// </summary>
    public static int DiscoverAndRun()
    {
        var allowSkip = false;
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, "--allow-skip", StringComparison.Ordinal))
            {
                allowSkip = true;
            }
        }

        var suiteStart = Stopwatch.StartNew();

        var testClasses = typeof(TestRunner).Assembly
            .GetTypes()
            .Where(t => t.GetCustomAttribute<TestClassAttribute>() is not null)
            .Where(t => t.IsClass && !t.IsAbstract && t.IsPublic)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .ToList();

        var passed = 0;
        var failed = 0;
        var skipped = 0;
        var unavailable = 0;
        var failures = new List<string>();

        foreach (var type in testClasses)
        {
            var methods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<TestAttribute>() is not null)
                .Where(m => m.GetParameters().Length == 0)
                .Where(m => IsSupportedReturnType(m.ReturnType))
                .OrderBy(m => m.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var method in methods)
            {
                var label = $"{type.Name}.{method.Name}";
                var result = RunOne(type, method);
                switch (result.Kind)
                {
                    case TestOutcomeKind.Passed:
                        passed++;
                        Console.WriteLine($"[PASS] {label}");
                        break;
                    case TestOutcomeKind.Skipped:
                        skipped++;
                        Console.WriteLine($"[SKIP] {label}");
                        if (result.Message.Length > 0)
                        {
                            Console.WriteLine($"       {result.Message}");
                        }
                        break;
                    case TestOutcomeKind.Unavailable:
                        unavailable++;
                        Console.WriteLine($"[UNAVAILABLE] {label}");
                        if (result.Message.Length > 0)
                        {
                            Console.WriteLine($"       {result.Message}");
                        }
                        break;
                    default:
                        failed++;
                        var reason = result.Message.Length > 0 ? result.Message : "unknown failure";
                        failures.Add($"{label}: {reason}");
                        Console.WriteLine($"[FAIL] {label}");
                        Console.WriteLine($"       {reason}");
                        break;
                }
            }
        }

        suiteStart.Stop();
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        Console.WriteLine($"Test classes: {testClasses.Count}");
        Console.WriteLine($"Tests run:    {passed + failed}");
        Console.WriteLine($"Passed:       {passed}");
        Console.WriteLine($"Skipped:      {skipped}");
        Console.WriteLine($"Unavailable:  {unavailable}");
        Console.WriteLine($"Failed:       {failed}");
        Console.WriteLine($"Duration:     {suiteStart.ElapsedMilliseconds} ms");

        if (failures.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("=== Failures ===");
            foreach (var f in failures)
            {
                Console.WriteLine($"  - {f}");
            }
        }

        Console.WriteLine();

        var exitCode = TestExitPolicy.ExitCode(failed, skipped, unavailable, allowSkip);
        if (exitCode == 1)
        {
            Console.WriteLine("TEST RESULT: FAIL");
        }
        else if (exitCode == 2)
        {
            Console.WriteLine("TEST RESULT: SKIP-NOT-ACCEPTED");
        }
        else
        {
            Console.WriteLine("TEST RESULT: PASS");
        }
        return exitCode;
    }

    private static bool IsSupportedReturnType(Type returnType)
    {
        return returnType == typeof(void)
            || returnType == typeof(Task)
            || returnType == typeof(TestOutcome)
            || returnType == typeof(Task<TestOutcome>);
    }

    private static TestResult RunOne(Type type, MethodInfo method)
    {
        object? instance;
        try
        {
            instance = Activator.CreateInstance(type);
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Could not instantiate {type.Name}: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            var returned = method.Invoke(instance, null);

            if (returned is Task<TestOutcome> outcomeTask)
            {
                if (!outcomeTask.Wait(PerTestTimeout))
                {
                    return TestResult.Fail($"Test exceeded {PerTestTimeout.TotalSeconds:0} second timeout");
                }
                var outcome = outcomeTask.Result;
                return new TestResult(outcome.Kind, outcome.Message ?? string.Empty);
            }

            if (returned is Task task)
            {
                if (!task.Wait(PerTestTimeout))
                {
                    return TestResult.Fail($"Test exceeded {PerTestTimeout.TotalSeconds:0} second timeout");
                }
                return TestResult.Pass();
            }

            if (returned is TestOutcome direct)
            {
                return new TestResult(direct.Kind, direct.Message ?? string.Empty);
            }

            return TestResult.Pass();
        }
        catch (TargetInvocationException tie) when (tie.InnerException is TestFailureException tfe)
        {
            return TestResult.Fail(tfe.Message);
        }
        catch (TargetInvocationException tie) when (tie.InnerException is not null)
        {
            var inner = tie.InnerException;
            return TestResult.Fail($"Unexpected {inner.GetType().Name}: {inner.Message}");
        }
        catch (Exception ex)
        {
            return TestResult.Fail($"Unexpected {ex.GetType().Name}: {ex.Message}");
        }
    }

    private readonly record struct TestResult(TestOutcomeKind Kind, string Message)
    {
        public static TestResult Pass() => new(TestOutcomeKind.Passed, string.Empty);
        public static TestResult Fail(string message) => new(TestOutcomeKind.Failed, message);
    }
}

