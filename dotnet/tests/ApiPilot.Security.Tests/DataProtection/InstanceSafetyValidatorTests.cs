// filepath: dotnet/tests/ApiPilot.Security.Tests/DataProtection/InstanceSafetyValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the multi-instance startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.DataProtection
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : InstanceSafetyValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.DataProtection;

namespace ApiPilot.Security.Tests.DataProtection;

/// <summary>
/// Contract tests for InstanceSafetyValidator. Verifies the fail-closed
/// rule for MultiInstance without a configured KeyStorage.
/// </summary>
[TestClass]
public sealed class InstanceSafetyValidatorTests
{
    private static InstanceSafetyValidator Validator() => new();

    /// <summary>MultiInstance=false always passes.</summary>
    [Test]
    public void Validate_SingleInstance_ReturnsSuccess()
    {
        var options = new ApiPilotDataProtectionOptions { MultiInstance = false };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>MultiInstance=true with KeyStorageConfigured passes.</summary>
    [Test]
    public void Validate_MultiInstanceWithKeyStorage_ReturnsSuccess()
    {
        var options = new ApiPilotDataProtectionOptions
        {
            MultiInstance = true,
            KeyStorageConfigured = true,
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>MultiInstance=true without KeyStorageConfigured fails.</summary>
    [Test]
    public void Validate_MultiInstanceWithoutKeyStorage_Fails()
    {
        var options = new ApiPilotDataProtectionOptions
        {
            MultiInstance = true,
            KeyStorageConfigured = false,
        };
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>The failure message names MultiInstance.</summary>
    [Test]
    public void Validate_Failure_NamesMultiInstance()
    {
        var options = new ApiPilotDataProtectionOptions { MultiInstance = true };
        var result = Validator().Validate(null, options);
        var found = false;
        foreach (var f in result.Failures!)
        {
            if (f.Contains("MultiInstance", StringComparison.Ordinal)) { found = true; break; }
        }
        TestAssert.True(found);
    }

    /// <summary>The failure message names AddApiPilotDataProtection.</summary>
    [Test]
    public void Validate_Failure_NamesRegistrationPath()
    {
        var options = new ApiPilotDataProtectionOptions { MultiInstance = true };
        var result = Validator().Validate(null, options);
        var found = false;
        foreach (var f in result.Failures!)
        {
            if (f.Contains("AddApiPilotDataProtection", StringComparison.Ordinal)) { found = true; break; }
        }
        TestAssert.True(found);
    }

    /// <summary>A null options argument throws.</summary>
    [Test]
    public void Validate_NullOptions_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            Validator().Validate(null, null!));
    }
}

