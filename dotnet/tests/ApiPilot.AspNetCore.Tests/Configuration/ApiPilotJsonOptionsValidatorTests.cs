// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/ApiPilotJsonOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the ApiPilotJsonOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.Serialization
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotJsonOptionsValidator.cs, ApiPilotJsonOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for ApiPilotJsonOptionsValidator. Verifies each
/// validation rule fires on the corresponding invalid configuration and
/// that the default configuration passes.
/// </summary>
[TestClass]
public sealed class ApiPilotJsonOptionsValidatorTests
{
    private static ApiPilotJsonOptionsValidator Validator() => new();

    private static ApiPilotJsonOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>MaxDepth less than 1 fails.</summary>
    [Test]
    public void Validate_MaxDepthZero_Fails()
    {
        var options = Valid();
        options.MaxDepth = 0;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined EnumMode fails.</summary>
    [Test]
    public void Validate_UndefinedEnumMode_Fails()
    {
        var options = Valid();
        options.EnumMode = (EnumSerializationMode)999;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined DateMode fails.</summary>
    [Test]
    public void Validate_UndefinedDateMode_Fails()
    {
        var options = Valid();
        options.DateMode = (DateSerializationMode)999;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined ReadCommentHandling fails.</summary>
    [Test]
    public void Validate_UndefinedReadCommentHandling_Fails()
    {
        var options = Valid();
        options.ReadCommentHandling = (JsonCommentHandling)200;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined DefaultIgnoreCondition fails.</summary>
    [Test]
    public void Validate_UndefinedDefaultIgnoreCondition_Fails()
    {
        var options = Valid();
        options.DefaultIgnoreCondition = (JsonIgnoreCondition)999;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

