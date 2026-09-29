// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/CorrelationOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the CorrelationOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CorrelationOptionsValidator.cs, CorrelationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Metadata;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for CorrelationOptionsValidator. Verifies each validation
/// rule fires on the corresponding invalid configuration and that the
/// default configuration passes.
/// </summary>
[TestClass]
public sealed class CorrelationOptionsValidatorTests
{
    private static CorrelationOptionsValidator Validator() => new();

    private static CorrelationOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>Empty HeaderName fails.</summary>
    [Test]
    public void Validate_EmptyHeaderName_Fails()
    {
        var options = Valid();
        options.HeaderName = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An unclosed parenthesis in ValidationPattern fails.</summary>
    [Test]
    public void Validate_InvalidRegex_Fails()
    {
        var options = Valid();
        options.ValidationPattern = "([unclosed";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A valid custom regex passes.</summary>
    [Test]
    public void Validate_ValidCustomRegex_ReturnsSuccess()
    {
        var options = Valid();
        options.ValidationPattern = "^.{1,256}$";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>An undefined enum value for InvalidIncomingIdPolicy fails.</summary>
    [Test]
    public void Validate_UndefinedEnumValue_Fails()
    {
        var options = Valid();
        options.InvalidIncomingIdPolicy = (CorrelationInvalidIdPolicy)999;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

