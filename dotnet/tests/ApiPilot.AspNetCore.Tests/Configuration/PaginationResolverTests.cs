// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/PaginationResolverTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for PaginationResolver covering every override point
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.EndpointMetadata,
//                ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationResolver.cs, PaginationOverrides.cs, PaginationMetadataAttribute.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.Core.Pagination;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for PaginationResolver. Verifies that every override
/// field is applied when present and falls through to the global when
/// null, and that FromAttribute correctly translates a TriState.
/// </summary>
[TestClass]
public sealed class PaginationResolverTests
{
    private static PaginationOptions GlobalOptions()
    {
        return new PaginationOptions
        {
            DefaultPageSize = 20,
            MaxPageSize = 100,
            StrictQueryValidation = false,
            SuccessStatusCode = 200,
            PageNumberBase = 1,
        };
    }

    // ---------- Resolve ----------

    /// <summary>When overrides is null, the global instance is returned.</summary>
    [Test]
    public void Resolve_NullOverrides_ReturnsGlobalInstanceUnchanged()
    {
        var global = GlobalOptions();
        var result = PaginationResolver.Resolve(global, null);
        TestAssert.True(ReferenceEquals(global, result));
    }

    /// <summary>Null global is rejected.</summary>
    [Test]
    public void Resolve_RejectsNullGlobal()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            PaginationResolver.Resolve(null!, null));
    }

    /// <summary>DefaultPageSize override is applied.</summary>
    [Test]
    public void Resolve_DefaultPageSize_OverrideApplied()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { DefaultPageSize = 50 });
        TestAssert.Equal(50, result.DefaultPageSize);
    }

    /// <summary>MaxPageSize override is applied.</summary>
    [Test]
    public void Resolve_MaxPageSize_OverrideApplied()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { MaxPageSize = 500 });
        TestAssert.Equal(500, result.MaxPageSize);
    }

    /// <summary>StrictQueryValidation override is applied.</summary>
    [Test]
    public void Resolve_StrictQueryValidation_OverrideApplied()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { StrictQueryValidation = true });
        TestAssert.True(result.StrictQueryValidation);
    }

    /// <summary>SuccessStatusCode override is applied.</summary>
    [Test]
    public void Resolve_SuccessStatusCode_OverrideApplied()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { SuccessStatusCode = 206 });
        TestAssert.Equal(206, result.SuccessStatusCode);
    }

    /// <summary>ParameterNames override is applied.</summary>
    [Test]
    public void Resolve_ParameterNames_OverrideApplied()
    {
        var names = new QueryParameterNames { Page = "pageNumber" };
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { ParameterNames = names });
        TestAssert.True(ReferenceEquals(names, result.ParameterNames));
    }

    /// <summary>PageNumberBase override is applied.</summary>
    [Test]
    public void Resolve_PageNumberBase_OverrideApplied()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { PageNumberBase = 0 });
        TestAssert.Equal(0, result.PageNumberBase);
    }

    /// <summary>SortDirectionParser override is applied.</summary>
    [Test]
    public void Resolve_SortDirectionParser_OverrideApplied()
    {
        Func<string, SortDirection?> parser = _ => SortDirection.Descending;
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { SortDirectionParser = parser });
        TestAssert.True(ReferenceEquals(parser, result.SortDirectionParser));
    }

    /// <summary>SortParser override is applied.</summary>
    [Test]
    public void Resolve_SortParser_OverrideApplied()
    {
        Func<string, string, SortRequest?> parser = (f, d) => SortRequest.Create(f, SortDirection.Ascending);
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { SortParser = parser });
        TestAssert.True(ReferenceEquals(parser, result.SortParser));
    }

    /// <summary>IsFilterParameter override is applied.</summary>
    [Test]
    public void Resolve_IsFilterParameter_OverrideApplied()
    {
        Func<string, bool> predicate = _ => true;
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { IsFilterParameter = predicate });
        TestAssert.True(ReferenceEquals(predicate, result.IsFilterParameter));
    }

    /// <summary>IntegerParser override is applied.</summary>
    [Test]
    public void Resolve_IntegerParser_OverrideApplied()
    {
        Func<string, int?> parser = _ => 42;
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { IntegerParser = parser });
        TestAssert.True(ReferenceEquals(parser, result.IntegerParser));
    }

    /// <summary>Unset overrides leave the global values unchanged.</summary>
    [Test]
    public void Resolve_OnlySomeFieldsOverridden_LeavesOthersAtGlobal()
    {
        var result = PaginationResolver.Resolve(GlobalOptions(),
            new PaginationOverrides { MaxPageSize = 500 });
        TestAssert.Equal(500, result.MaxPageSize);
        TestAssert.Equal(20, result.DefaultPageSize);
        TestAssert.Equal(1, result.PageNumberBase);
        TestAssert.Equal(200, result.SuccessStatusCode);
    }

    // ---------- FromAttribute ----------

    /// <summary>Null attribute returns null.</summary>
    [Test]
    public void FromAttribute_NullAttribute_ReturnsNull()
    {
        TestAssert.Null(PaginationResolver.FromAttribute(null));
    }

    /// <summary>All-sentinel attribute returns null.</summary>
    [Test]
    public void FromAttribute_AllSentinels_ReturnsNull()
    {
        var attr = new PaginationMetadataAttribute();
        TestAssert.Null(PaginationResolver.FromAttribute(attr));
    }

    /// <summary>DefaultPageSize sentinel -1 leaves the field unset.</summary>
    [Test]
    public void FromAttribute_DefaultPageSizeSet_ReturnsOverride()
    {
        var attr = new PaginationMetadataAttribute { DefaultPageSize = 50 };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Equal(50, overrides!.DefaultPageSize);
    }

    /// <summary>MaxPageSize is read.</summary>
    [Test]
    public void FromAttribute_MaxPageSizeSet_ReturnsOverride()
    {
        var attr = new PaginationMetadataAttribute { MaxPageSize = 500 };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Equal(500, overrides!.MaxPageSize);
    }

    /// <summary>TriState.True is translated to explicit true.</summary>
    [Test]
    public void FromAttribute_StrictQueryValidationTrue_ReturnsOverride()
    {
        var attr = new PaginationMetadataAttribute { StrictQueryValidation = TriState.True };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.True(overrides!.StrictQueryValidation!.Value);
    }

    /// <summary>TriState.False is translated to explicit false.</summary>
    [Test]
    public void FromAttribute_StrictQueryValidationFalse_ReturnsExplicitFalse()
    {
        var attr = new PaginationMetadataAttribute { StrictQueryValidation = TriState.False };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.False(overrides!.StrictQueryValidation!.Value);
    }

    /// <summary>SuccessStatusCode is read.</summary>
    [Test]
    public void FromAttribute_SuccessStatusCodeSet_ReturnsOverride()
    {
        var attr = new PaginationMetadataAttribute { SuccessStatusCode = 206 };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Equal(206, overrides!.SuccessStatusCode);
    }

    /// <summary>PageNumberBase is read.</summary>
    [Test]
    public void FromAttribute_PageNumberBaseSet_ReturnsOverride()
    {
        var attr = new PaginationMetadataAttribute { PageNumberBase = 0 };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Equal(0, overrides!.PageNumberBase);
    }

    /// <summary>Multiple attribute fields are combined.</summary>
    [Test]
    public void FromAttribute_CombinedFields_ReturnsAllOverrides()
    {
        var attr = new PaginationMetadataAttribute
        {
            DefaultPageSize = 25,
            MaxPageSize = 200,
            StrictQueryValidation = TriState.False,
            SuccessStatusCode = 206,
            PageNumberBase = 0,
        };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Equal(25, overrides!.DefaultPageSize);
        TestAssert.Equal(200, overrides.MaxPageSize);
        TestAssert.False(overrides.StrictQueryValidation!.Value);
        TestAssert.Equal(206, overrides.SuccessStatusCode);
        TestAssert.Equal(0, overrides.PageNumberBase);
    }

    /// <summary>Delegate fields are always null when read from an attribute.</summary>
    [Test]
    public void FromAttribute_DelegatesNeverPopulated()
    {
        var attr = new PaginationMetadataAttribute { MaxPageSize = 500 };
        var overrides = PaginationResolver.FromAttribute(attr);
        TestAssert.NotNull(overrides);
        TestAssert.Null(overrides!.SortDirectionParser);
        TestAssert.Null(overrides.SortParser);
        TestAssert.Null(overrides.IsFilterParameter);
        TestAssert.Null(overrides.IntegerParser);
        TestAssert.Null(overrides.ParameterNames);
    }

    // ---------- MergeOverrides ----------

    /// <summary>Both inputs null returns null.</summary>
    [Test]
    public void MergeOverrides_BothNull_ReturnsNull()
    {
        var merged = PaginationResolver.MergeOverrides(null, null);
        TestAssert.Null(merged);
    }

    /// <summary>Only fluent returns the fluent instance unchanged.</summary>
    [Test]
    public void MergeOverrides_OnlyFluent_ReturnsFluent()
    {
        var fluent = new PaginationOverrides { MaxPageSize = 50 };
        var merged = PaginationResolver.MergeOverrides(fluent, null);
        TestAssert.True(ReferenceEquals(fluent, merged));
    }

    /// <summary>Only attribute returns the attribute instance unchanged.</summary>
    [Test]
    public void MergeOverrides_OnlyAttribute_ReturnsAttribute()
    {
        var attribute = new PaginationOverrides { MaxPageSize = 50 };
        var merged = PaginationResolver.MergeOverrides(null, attribute);
        TestAssert.True(ReferenceEquals(attribute, merged));
    }

    /// <summary>
    /// Both inputs present: primitive fields take the attribute value,
    /// delegate and object fields take the fluent value.
    /// </summary>
    [Test]
    public void MergeOverrides_BothPresent_PrimitivesAttributeWins_DelegatesFluentWins()
    {
        Func<string, SortDirection?> directionParser = _ => SortDirection.Descending;
        var names = new QueryParameterNames { Page = "pageNumber" };
        var fluent = new PaginationOverrides
        {
            MaxPageSize = 500,
            PageNumberBase = 0,
            SortDirectionParser = directionParser,
            ParameterNames = names,
        };
        var attribute = new PaginationOverrides
        {
            MaxPageSize = 50,
            PageNumberBase = 1,
        };
        var merged = PaginationResolver.MergeOverrides(fluent, attribute);
        TestAssert.NotNull(merged);
        TestAssert.Equal(50, merged!.MaxPageSize);
        TestAssert.Equal(1, merged.PageNumberBase);
        TestAssert.True(ReferenceEquals(directionParser, merged.SortDirectionParser));
        TestAssert.True(ReferenceEquals(names, merged.ParameterNames));
    }
}

