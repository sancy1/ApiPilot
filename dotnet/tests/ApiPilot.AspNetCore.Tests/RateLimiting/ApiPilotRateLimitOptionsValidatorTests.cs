// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/ApiPilotRateLimitOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Contract tests for the ApiPilotRateLimitOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotRateLimitOptionsValidator.cs, ApiPilotRateLimitOptions.cs
// -----------------------------------------------------------------------------

using System.Linq;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.RateLimiting;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Contract tests for ApiPilotRateLimitOptionsValidator. Verifies each
/// validation rule fires on the corresponding invalid configuration, that
/// the boundary values pass, and that the default configuration passes.
/// </summary>
[TestClass]
public sealed class ApiPilotRateLimitOptionsValidatorTests
{
    private static ApiPilotRateLimitOptionsValidator Validator() => new();

    private static ApiPilotRateLimitOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A StatusCode below 400 fails.</summary>
    [Test]
    public void Validate_StatusCodeBelow400_Fails()
    {
        var options = Valid();
        options.StatusCode = 399;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A StatusCode above 599 fails.</summary>
    [Test]
    public void Validate_StatusCodeAbove599_Fails()
    {
        var options = Valid();
        options.StatusCode = 600;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>The lower boundary 400 passes.</summary>
    [Test]
    public void Validate_StatusCode400_Passes()
    {
        var options = Valid();
        options.StatusCode = 400;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>The upper boundary 599 passes.</summary>
    [Test]
    public void Validate_StatusCode599_Passes()
    {
        var options = Valid();
        options.StatusCode = 599;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A null message fails.</summary>
    [Test]
    public void Validate_NullMessage_Fails()
    {
        var options = Valid();
        options.Message = null!;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty message fails.</summary>
    [Test]
    public void Validate_EmptyMessage_Fails()
    {
        var options = Valid();
        options.Message = string.Empty;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A whitespace message fails.</summary>
    [Test]
    public void Validate_WhitespaceMessage_Fails()
    {
        var options = Valid();
        options.Message = "   ";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A null options instance throws.</summary>
    [Test]
    public void Validate_NullOptions_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(
            () => Validator().Validate(null, null!));
    }

    /// <summary>The failure message names the StatusCode property.</summary>
    [Test]
    public void Validate_FailureMessage_NamesStatusCode()
    {
        var options = Valid();
        options.StatusCode = 200;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
        var failures = result.Failures;
        if (failures is null)
        {
            throw new TestFailureException("Expected Failures to be present on a failed result.");
        }
        TestAssert.True(failures.Any(
            f => f.Contains("ApiPilotRateLimitOptions.StatusCode", StringComparison.Ordinal)));
    }

    /// <summary>The failure message names the Message property.</summary>
    [Test]
    public void Validate_FailureMessage_NamesMessage()
    {
        var options = Valid();
        options.Message = string.Empty;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
        var failures = result.Failures;
        if (failures is null)
        {
            throw new TestFailureException("Expected Failures to be present on a failed result.");
        }
        TestAssert.True(failures.Any(
            f => f.Contains("ApiPilotRateLimitOptions.Message", StringComparison.Ordinal)));
    }
}

