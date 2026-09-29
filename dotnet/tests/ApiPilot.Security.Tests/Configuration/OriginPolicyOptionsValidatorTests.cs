// filepath: dotnet/tests/ApiPilot.Security.Tests/Configuration/OriginPolicyOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the Origin policy startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Configuration, ApiPilot.Security.Origin
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginPolicyOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.Security.Configuration;
using ApiPilot.Security.Origin;

namespace ApiPilot.Security.Tests.Configuration;

/// <summary>
/// Contract tests for OriginPolicyOptionsValidator. Verifies the
/// fail-closed rule and each failure mode.
/// </summary>
[TestClass]
public sealed class OriginPolicyOptionsValidatorTests
{
    private static OriginPolicyOptionsValidator Validator() => new();

    private static OriginPolicyOptions Valid() => new();

    /// <summary>The default configuration passes.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>Disabled policy passes even with invalid inner config.</summary>
    [Test]
    public void Validate_Disabled_SkipsInnerChecks()
    {
        var options = Valid();
        options.Enabled = false;
        options.AllowSameOrigin = false;
        options.RejectionCode = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>Empty AllowedOrigins with AllowSameOrigin=false fails.</summary>
    [Test]
    public void Validate_EmptyAllowedWithNoSameOrigin_Fails()
    {
        var options = Valid();
        options.AllowSameOrigin = false;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Empty AllowedOrigins with AllowSameOrigin=true passes.</summary>
    [Test]
    public void Validate_EmptyAllowedWithSameOrigin_Passes()
    {
        var options = Valid();
        options.AllowSameOrigin = true;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Succeeded);
    }

    /// <summary>A malformed origin entry fails.</summary>
    [Test]
    public void Validate_MalformedOrigin_Fails()
    {
        var options = Valid();
        options.AllowedOrigins.Add("not-an-origin");
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>The SameSite match mode fails as not implemented.</summary>
    [Test]
    public void Validate_SameSiteMode_Fails()
    {
        var options = Valid();
        options.MatchMode = OriginMatchMode.SameSite;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>An empty RejectionCode fails.</summary>
    [Test]
    public void Validate_EmptyRejectionCode_Fails()
    {
        var options = Valid();
        options.RejectionCode = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

