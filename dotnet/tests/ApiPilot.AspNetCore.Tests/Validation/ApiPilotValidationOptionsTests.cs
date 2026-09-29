// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Validation/ApiPilotValidationOptionsTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for ApiPilotValidationOptions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotValidationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;

namespace ApiPilot.AspNetCore.Tests.Validation;

/// <summary>
/// Contract tests for ApiPilotValidationOptions. Verifies defaults, mutability,
/// and the null semantics that mean "use the library default."
/// </summary>
[TestClass]
public sealed class ApiPilotValidationOptionsTests
{
    /// <summary>A fresh instance has a null KeyTransform.</summary>
    [Test]
    public void Defaults_KeyTransformIsNull()
    {
        var options = new ApiPilotValidationOptions();
        TestAssert.Null(options.KeyTransform);
    }

    /// <summary>KeyTransform can be set to a custom delegate.</summary>
    [Test]
    public void KeyTransform_IsMutable()
    {
        var options = new ApiPilotValidationOptions();
        Func<string, string> custom = k => "custom." + k;
        options.KeyTransform = custom;
        TestAssert.NotNull(options.KeyTransform);
        TestAssert.Equal("custom.foo", options.KeyTransform!("foo"));
    }

    /// <summary>KeyTransform can be set back to null.</summary>
    [Test]
    public void KeyTransform_NullIsAllowed()
    {
        var options = new ApiPilotValidationOptions { KeyTransform = k => k };
        options.KeyTransform = null;
        TestAssert.Null(options.KeyTransform);
    }

    /// <summary>The identity function is a valid override.</summary>
    [Test]
    public void KeyTransform_IdentityFunction_DisablesNormalization()
    {
        var options = new ApiPilotValidationOptions { KeyTransform = k => k };
        TestAssert.Equal("Items[0].Price", options.KeyTransform!("Items[0].Price"));
    }

    /// <summary>Defaults can be set via object initializer.</summary>
    [Test]
    public void Defaults_CanBeSetViaObjectInitializer()
    {
        Func<string, string> custom = k => k.ToLowerInvariant();
        var options = new ApiPilotValidationOptions { KeyTransform = custom };
        TestAssert.NotNull(options.KeyTransform);
        TestAssert.Equal("email", options.KeyTransform!("Email"));
    }
}

