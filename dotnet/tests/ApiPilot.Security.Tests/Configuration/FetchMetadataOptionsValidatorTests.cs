// filepath: dotnet/tests/ApiPilot.Security.Tests/Configuration/FetchMetadataOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the Fetch Metadata startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Configuration, ApiPilot.Security.Origin
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Origin;

namespace ApiPilot.Security.Tests.Configuration;

/// <summary>
/// Contract tests for FetchMetadataOptionsValidator. Verifies the
/// fail-closed rules and the Strict-cross-site guard.
/// </summary>
[TestClass]
public sealed class FetchMetadataOptionsValidatorTests
{
    private static FetchMetadataOptionsValidator Validator() => new();

    private static FetchMetadataOptions Valid() => new();

    /// <summary>The default (Off) configuration passes.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A Compat profile with default sets passes.</summary>
    [Test]
    public void Validate_CompatWithDefaults_ReturnsSuccess()
    {
        var options = Valid();
        options.Profile = FetchMetadataProfile.Compat;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A Strict profile with default sets passes.</summary>
    [Test]
    public void Validate_StrictWithDefaults_ReturnsSuccess()
    {
        var options = Valid();
        options.Profile = FetchMetadataProfile.Strict;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>Strict with cross-site allowed fails.</summary>
    [Test]
    public void Validate_StrictWithCrossSite_Fails()
    {
        var options = Valid();
        options.Profile = FetchMetadataProfile.Strict;
        options.AllowedSiteValues.Add("cross-site");
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty AllowedSiteValues under an enabled profile fails.</summary>
    [Test]
    public void Validate_EmptySiteValues_Fails()
    {
        var options = Valid();
        options.Profile = FetchMetadataProfile.Strict;
        options.AllowedSiteValues.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty AllowedModeValues under an enabled profile fails.</summary>
    [Test]
    public void Validate_EmptyModeValues_Fails()
    {
        var options = Valid();
        options.Profile = FetchMetadataProfile.Strict;
        options.AllowedModeValues.Clear();
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An undefined profile value fails.</summary>
    [Test]
    public void Validate_UndefinedProfile_Fails()
    {
        var options = Valid();
        options.Profile = (FetchMetadataProfile)999;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

