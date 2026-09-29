// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/ApiExceptionOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the ApiExceptionOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.ExceptionHandling,
//                ApiPilot.Core.Errors
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiExceptionOptionsValidator.cs, ApiExceptionOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.Core.Errors;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for ApiExceptionOptionsValidator. Verifies each
/// validation rule fires on the corresponding invalid configuration and
/// that the default configuration passes.
/// </summary>
[TestClass]
public sealed class ApiExceptionOptionsValidatorTests
{
    private static ApiExceptionOptionsValidator Validator() => new();

    private static ApiExceptionOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>An empty mappings list fails.</summary>
    [Test]
    public void Validate_EmptyMappings_Fails()
    {
        var options = Valid();
        options.Mappings.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A mapping with a null code fails.</summary>
    [Test]
    public void Validate_NullCodeInMapping_Fails()
    {
        var options = Valid();
        options.Mappings.Add(new KnownExceptionType(
            typeof(InvalidOperationException),
            null!,
            "message",
            IncludeExceptionMessage: false));
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A mapping with an empty safe message fails.</summary>
    [Test]
    public void Validate_EmptySafeMessage_Fails()
    {
        var options = Valid();
        options.Mappings.Add(new KnownExceptionType(
            typeof(InvalidOperationException),
            ApiErrorCode.Conflict,
            "",
            IncludeExceptionMessage: false));
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An ErrorCodeToStatusMap value outside 100..599 fails.</summary>
    [Test]
    public void Validate_ErrorCodeToStatusMapValueOutOfRange_Fails()
    {
        var options = Valid();
        options.ErrorCodeToStatusMap = new Dictionary<string, int>
        {
            { "CUSTOM_ERROR", 999 }
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty key in ErrorCodeToStatusMap fails.</summary>
    [Test]
    public void Validate_ErrorCodeToStatusMapEmptyKey_Fails()
    {
        var options = Valid();
        options.ErrorCodeToStatusMap = new Dictionary<string, int>
        {
            { "", 422 }
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

