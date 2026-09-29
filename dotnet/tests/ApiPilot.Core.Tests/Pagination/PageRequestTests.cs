// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/PageRequestTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the PageRequest validated value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PageRequest.cs, PaginationOptionsTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for PageRequest. Verifies factory validation, boundary
/// cases, and the query overload that fills defaults.
/// </summary>
[TestClass]
public sealed class PageRequestTests
{
    private static PaginationOptions Options()
    {
        return new PaginationOptions { DefaultPageSize = 20, MaxPageSize = 100 };
    }

    /// <summary>Valid arguments produce a request.</summary>
    [Test]
    public void Create_ValidArguments_ProducesRequest()
    {
        var request = PageRequest.Create(2, 20, Options());
        TestAssert.Equal(2, request.Page);
        TestAssert.Equal(20, request.PageSize);
    }

    /// <summary>Page zero is rejected.</summary>
    [Test]
    public void Create_RejectsZeroPage()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.Create(0, 20, Options()));
    }

    /// <summary>Negative page is rejected.</summary>
    [Test]
    public void Create_RejectsNegativePage()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.Create(-1, 20, Options()));
    }

    /// <summary>Page size zero is rejected.</summary>
    [Test]
    public void Create_RejectsZeroPageSize()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.Create(1, 0, Options()));
    }

    /// <summary>Negative page size is rejected.</summary>
    [Test]
    public void Create_RejectsNegativePageSize()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.Create(1, -5, Options()));
    }

    /// <summary>Page size above the configured maximum is rejected.</summary>
    [Test]
    public void Create_RejectsPageSizeAboveMax()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.Create(1, 101, Options()));
    }

    /// <summary>The boundary page size equal to the maximum is accepted.</summary>
    [Test]
    public void Create_AcceptsMaxPageSize()
    {
        var request = PageRequest.Create(1, 100, Options());
        TestAssert.Equal(100, request.PageSize);
    }

    /// <summary>Null options are rejected.</summary>
    [Test]
    public void Create_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            PageRequest.Create(1, 20, null!));
    }

    /// <summary>FromQuery with nulls fills the defaults.</summary>
    [Test]
    public void FromQuery_WithNulls_UsesDefaults()
    {
        var request = PageRequest.FromQuery(null, null, Options());
        TestAssert.Equal(1, request.Page);
        TestAssert.Equal(20, request.PageSize);
    }

    /// <summary>FromQuery with explicit values uses them.</summary>
    [Test]
    public void FromQuery_WithExplicitValues_UsesThem()
    {
        var request = PageRequest.FromQuery(3, 15, Options());
        TestAssert.Equal(3, request.Page);
        TestAssert.Equal(15, request.PageSize);
    }

    /// <summary>FromQuery still validates the maximum.</summary>
    [Test]
    public void FromQuery_ExplicitSizeAboveMax_Throws()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PageRequest.FromQuery(1, 500, Options()));
    }

    /// <summary>Page and PageSize are read-only.</summary>
    [Test]
    public void PageAndPageSize_AreReadOnly()
    {
        var request = PageRequest.Create(5, 25, Options());
        TestAssert.Equal(5, request.Page);
        TestAssert.Equal(25, request.PageSize);
    }
}

