// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/PaginationOverridesTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the PaginationOverrides data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationOverrides.cs, PaginationResolverTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.Core.Pagination;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for PaginationOverrides. Verifies that every property
/// is nullable and defaults to null, and that each is settable.
/// </summary>
[TestClass]
public sealed class PaginationOverridesTests
{
    /// <summary>A fresh instance has all properties null.</summary>
    [Test]
    public void Defaults_AllPropertiesAreNull()
    {
        var overrides = new PaginationOverrides();
        TestAssert.Null(overrides.DefaultPageSize);
        TestAssert.Null(overrides.MaxPageSize);
        TestAssert.Null(overrides.StrictQueryValidation);
        TestAssert.Null(overrides.SuccessStatusCode);
        TestAssert.Null(overrides.ParameterNames);
        TestAssert.Null(overrides.PageNumberBase);
        TestAssert.Null(overrides.SortDirectionParser);
        TestAssert.Null(overrides.SortParser);
        TestAssert.Null(overrides.IsFilterParameter);
        TestAssert.Null(overrides.IntegerParser);
    }

    /// <summary>DefaultPageSize is settable.</summary>
    [Test]
    public void DefaultPageSize_IsSettable()
    {
        var overrides = new PaginationOverrides { DefaultPageSize = 50 };
        TestAssert.Equal(50, overrides.DefaultPageSize);
    }

    /// <summary>MaxPageSize is settable.</summary>
    [Test]
    public void MaxPageSize_IsSettable()
    {
        var overrides = new PaginationOverrides { MaxPageSize = 200 };
        TestAssert.Equal(200, overrides.MaxPageSize);
    }

    /// <summary>StrictQueryValidation is settable to true and false.</summary>
    [Test]
    public void StrictQueryValidation_IsSettable()
    {
        var trueCase = new PaginationOverrides { StrictQueryValidation = true };
        var falseCase = new PaginationOverrides { StrictQueryValidation = false };
        TestAssert.True(trueCase.StrictQueryValidation!.Value);
        TestAssert.False(falseCase.StrictQueryValidation!.Value);
    }

    /// <summary>SuccessStatusCode is settable.</summary>
    [Test]
    public void SuccessStatusCode_IsSettable()
    {
        var overrides = new PaginationOverrides { SuccessStatusCode = 206 };
        TestAssert.Equal(206, overrides.SuccessStatusCode);
    }

    /// <summary>ParameterNames is settable and preserves the instance.</summary>
    [Test]
    public void ParameterNames_IsSettable()
    {
        var names = new QueryParameterNames { Page = "pageNumber" };
        var overrides = new PaginationOverrides { ParameterNames = names };
        TestAssert.NotNull(overrides.ParameterNames);
        TestAssert.True(ReferenceEquals(names, overrides.ParameterNames));
    }

    /// <summary>PageNumberBase is settable.</summary>
    [Test]
    public void PageNumberBase_IsSettable()
    {
        var overrides = new PaginationOverrides { PageNumberBase = 0 };
        TestAssert.Equal(0, overrides.PageNumberBase);
    }

    /// <summary>All four delegate properties are settable.</summary>
    [Test]
    public void Delegates_AreSettable()
    {
        var overrides = new PaginationOverrides
        {
            SortDirectionParser = _ => SortDirection.Ascending,
            SortParser = (_, _) => SortRequest.Create("name", SortDirection.Ascending),
            IsFilterParameter = _ => true,
            IntegerParser = _ => 1,
        };
        TestAssert.NotNull(overrides.SortDirectionParser);
        TestAssert.NotNull(overrides.SortParser);
        TestAssert.NotNull(overrides.IsFilterParameter);
        TestAssert.NotNull(overrides.IntegerParser);
    }

    /// <summary>An object initializer can set multiple properties at once.</summary>
    [Test]
    public void ObjectInitializer_Works()
    {
        var overrides = new PaginationOverrides
        {
            DefaultPageSize = 25,
            MaxPageSize = 150,
            StrictQueryValidation = true,
            SuccessStatusCode = 200,
            PageNumberBase = 1,
        };
        TestAssert.Equal(25, overrides.DefaultPageSize);
        TestAssert.Equal(150, overrides.MaxPageSize);
        TestAssert.True(overrides.StrictQueryValidation!.Value);
        TestAssert.Equal(200, overrides.SuccessStatusCode);
        TestAssert.Equal(1, overrides.PageNumberBase);
    }
}

