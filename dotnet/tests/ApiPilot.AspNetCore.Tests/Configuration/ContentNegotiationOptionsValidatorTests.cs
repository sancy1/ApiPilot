// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/ContentNegotiationOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the ContentNegotiationOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.Core.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ContentNegotiationOptionsValidator.cs, ContentNegotiationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Configuration;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for ContentNegotiationOptionsValidator. Verifies each
/// validation rule fires on the corresponding invalid configuration and
/// that the default configuration passes.
/// </summary>
[TestClass]
public sealed class ContentNegotiationOptionsValidatorTests
{
    private static ContentNegotiationOptionsValidator Validator() => new();

    private static ContentNegotiationOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>Empty AcceptableResponseMediaTypes fails.</summary>
    [Test]
    public void Validate_EmptyResponseMediaTypes_Fails()
    {
        var options = Valid();
        options.AcceptableResponseMediaTypes.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Empty AcceptableRequestMediaTypes fails.</summary>
    [Test]
    public void Validate_EmptyRequestMediaTypes_Fails()
    {
        var options = Valid();
        options.AcceptableRequestMediaTypes.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An entry without a slash in the media type fails.</summary>
    [Test]
    public void Validate_InvalidMediaType_Fails()
    {
        var options = Valid();
        options.AcceptableResponseMediaTypes.Add("notamediatype");
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Empty BodyCarryingMethods fails.</summary>
    [Test]
    public void Validate_EmptyBodyCarryingMethods_Fails()
    {
        var options = Valid();
        options.BodyCarryingMethods.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An entry with a non-letter, non-hyphen character in the method fails.</summary>
    [Test]
    public void Validate_InvalidHttpMethod_Fails()
    {
        var options = Valid();
        options.BodyCarryingMethods.Add("P0ST");
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>AcceptWildcard false with an empty response media type set fails.</summary>
    [Test]
    public void Validate_WildcardFalseWithEmptyResponseSet_Fails()
    {
        var options = Valid();
        options.AcceptWildcard = false;
        options.AcceptableResponseMediaTypes.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

