// filepath: dotnet/tests/ApiPilot.Security.Tests/Configuration/CsrfOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CsrfOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Configuration, ApiPilot.Security.Csrf
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Csrf;

namespace ApiPilot.Security.Tests.Configuration;

/// <summary>
/// Contract tests for CsrfOptionsValidator. Verifies the default
/// configuration passes and each failure mode is detected.
/// </summary>
[TestClass]
public sealed class CsrfOptionsValidatorTests
{
    private static CsrfOptionsValidator Validator() => new();

    private static CsrfOptions Valid() => new();

    /// <summary>The default configuration passes.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>An empty HeaderName fails.</summary>
    [Test]
    public void Validate_EmptyHeaderName_Fails()
    {
        var options = Valid();
        options.HeaderName = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty BootstrapPath fails.</summary>
    [Test]
    public void Validate_EmptyBootstrapPath_Fails()
    {
        var options = Valid();
        options.BootstrapPath = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A BootstrapPath without a leading slash fails.</summary>
    [Test]
    public void Validate_BootstrapPathWithoutSlash_Fails()
    {
        var options = Valid();
        options.BootstrapPath = "csrf";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty MissingHeaderCode fails.</summary>
    [Test]
    public void Validate_EmptyMissingHeaderCode_Fails()
    {
        var options = Valid();
        options.MissingHeaderCode = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty ProtectedMethods set fails.</summary>
    [Test]
    public void Validate_EmptyProtectedMethods_Fails()
    {
        var options = Valid();
        options.ProtectedMethods.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An incomplete CodeMapping fails.</summary>
    [Test]
    public void Validate_IncompleteCodeMapping_Fails()
    {
        var options = Valid();
        options.CodeMapping.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>A null options argument throws.</summary>
    [Test]
    public void Validate_NullOptions_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            Validator().Validate(null, null!));
    }
}

