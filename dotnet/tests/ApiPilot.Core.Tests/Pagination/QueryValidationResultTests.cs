// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/QueryValidationResultTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the QueryValidationResult union type
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Core.Errors, ApiPilot.Core.Pagination, ApiPilot.Core.Validation
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : QueryValidationResult.cs, PageRequestTests.cs, ValidationErrorsTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Pagination;
using ApiPilot.Core.Validation;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for QueryValidationResult. Verifies the success and
/// failure factories, the discriminant, and null argument rejection.
/// </summary>
[TestClass]
public sealed class QueryValidationResultTests
{
    private static PaginationOptions Options()
    {
        return new PaginationOptions { DefaultPageSize = 20, MaxPageSize = 100 };
    }

    private static PageRequest Page()
    {
        return PageRequest.Create(1, 20, Options());
    }

    private static ValidationErrors Errors()
    {
        return ValidationErrors.From(new[]
        {
            ApiErrorField.WithMessage("query", "Invalid query parameter.")
        });
    }

    /// <summary>Success produces a successful result.</summary>
    [Test]
    public void Success_ProducesSuccessfulResult()
    {
        var result = QueryValidationResult.Success(Page(), null, FilterRequest.Empty);
        TestAssert.True(result.IsSuccess);
    }

    /// <summary>Success populates the page and filters.</summary>
    [Test]
    public void Success_PopulatesPageAndFilters()
    {
        var result = QueryValidationResult.Success(Page(), null, FilterRequest.Empty);
        TestAssert.NotNull(result.Page);
        TestAssert.Equal(1, result.Page!.Page);
        TestAssert.NotNull(result.Filters);
    }

    /// <summary>Sort may be null on success.</summary>
    [Test]
    public void Success_NullSort_IsAllowed()
    {
        var result = QueryValidationResult.Success(Page(), null, FilterRequest.Empty);
        TestAssert.Null(result.Sort);
    }

    /// <summary>Sort is populated when provided.</summary>
    [Test]
    public void Success_WithSort_PopulatesSort()
    {
        var sort = SortRequest.Create("name", SortDirection.Ascending);
        var result = QueryValidationResult.Success(Page(), sort, FilterRequest.Empty);
        TestAssert.NotNull(result.Sort);
        TestAssert.Equal("name", result.Sort!.Field);
    }

    /// <summary>Errors is null on success.</summary>
    [Test]
    public void Success_ErrorsIsNull()
    {
        var result = QueryValidationResult.Success(Page(), null, FilterRequest.Empty);
        TestAssert.Null(result.Errors);
    }

    /// <summary>Null page is rejected.</summary>
    [Test]
    public void Success_RejectsNullPage()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            QueryValidationResult.Success(null!, null, FilterRequest.Empty));
    }

    /// <summary>Null filters are rejected.</summary>
    [Test]
    public void Success_RejectsNullFilters()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            QueryValidationResult.Success(Page(), null, null!));
    }

    /// <summary>Failure produces a failed result.</summary>
    [Test]
    public void Failure_ProducesFailedResult()
    {
        var result = QueryValidationResult.Failure(Errors());
        TestAssert.False(result.IsSuccess);
    }

    /// <summary>Failure populates the errors.</summary>
    [Test]
    public void Failure_PopulatesErrors()
    {
        var result = QueryValidationResult.Failure(Errors());
        TestAssert.NotNull(result.Errors);
        TestAssert.Equal(1, result.Errors!.Count);
    }

    /// <summary>Page, Sort, and Filters are null on failure.</summary>
    [Test]
    public void Failure_PageAndFiltersAreNull()
    {
        var result = QueryValidationResult.Failure(Errors());
        TestAssert.Null(result.Page);
        TestAssert.Null(result.Sort);
        TestAssert.Null(result.Filters);
    }

    /// <summary>Null errors are rejected.</summary>
    [Test]
    public void Failure_RejectsNullErrors()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            QueryValidationResult.Failure(null!));
    }

    /// <summary>Empty errors are rejected.</summary>
    [Test]
    public void Failure_RejectsEmptyErrors()
    {
        TestAssert.Throws<ArgumentException>(() =>
            QueryValidationResult.Failure(ValidationErrors.Empty));
    }
}

