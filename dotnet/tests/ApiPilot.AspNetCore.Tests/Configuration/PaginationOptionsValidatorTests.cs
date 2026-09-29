// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/PaginationOptionsValidatorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the PaginationOptions startup validator
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationOptionsValidator.cs, PaginationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Pagination;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for PaginationOptionsValidator. Verifies each validation
/// rule fires on the corresponding invalid configuration and that the
/// default configuration passes.
/// </summary>
[TestClass]
public sealed class PaginationOptionsValidatorTests
{
    private static PaginationOptionsValidator Validator() => new();

    private static PaginationOptions Valid() => new();

    /// <summary>The default configuration passes validation.</summary>
    [Test]
    public void Validate_Defaults_ReturnsSuccess()
    {
        var result = Validator().Validate(null, Valid());
        TestAssert.True(result.Succeeded);
    }

    /// <summary>DefaultPageSize less than 1 fails.</summary>
    [Test]
    public void Validate_DefaultPageSizeZero_Fails()
    {
        var options = Valid();
        options.DefaultPageSize = 0;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>MaxPageSize less than 1 fails.</summary>
    [Test]
    public void Validate_MaxPageSizeZero_Fails()
    {
        var options = Valid();
        options.MaxPageSize = 0;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>DefaultPageSize greater than MaxPageSize fails.</summary>
    [Test]
    public void Validate_DefaultPageSizeExceedsMax_Fails()
    {
        var options = Valid();
        options.DefaultPageSize = 200;
        options.MaxPageSize = 100;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Negative PageNumberBase fails.</summary>
    [Test]
    public void Validate_PageNumberBaseNegative_Fails()
    {
        var options = Valid();
        options.PageNumberBase = -1;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>SuccessStatusCode outside 2xx fails.</summary>
    [Test]
    public void Validate_SuccessStatusCodeOutOf2xx_Fails()
    {
        var options = Valid();
        options.SuccessStatusCode = 500;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Null ParameterNames fails.</summary>
    [Test]
    public void Validate_ParameterNamesNull_Fails()
    {
        var options = Valid();
        options.ParameterNames = null!;
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }

    /// <summary>Empty entry in ParameterNames fails.</summary>
    [Test]
    public void Validate_ParameterNamesEmptyEntry_Fails()
    {
        var options = Valid();
        options.ParameterNames.Page = "";
        var result = Validator().Validate(null, options);
        TestAssert.True(result.Failed);
    }
}

