// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/PagedResultTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the PagedResult<T> wrapper
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PagedResult.cs, PaginationMetadataTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for PagedResult. Verifies construction, defensive copying,
/// and null argument rejection.
/// </summary>
[TestClass]
public sealed class PagedResultTests
{
    private static PaginationMetadata Meta()
    {
        return PaginationMetadata.Create(1, 20, 50);
    }

    /// <summary>The items collection is exposed.</summary>
    [Test]
    public void Constructor_PreservesItems()
    {
        var items = new[] { "a", "b", "c" };
        var result = new PagedResult<string>(items, Meta());
        TestAssert.Equal(3, result.Items.Count);
        TestAssert.Equal("a", result.Items[0]);
        TestAssert.Equal("c", result.Items[2]);
    }

    /// <summary>The pagination metadata is preserved.</summary>
    [Test]
    public void Constructor_PreservesPagination()
    {
        var pagination = PaginationMetadata.Create(2, 10, 100);
        var result = new PagedResult<int>(new[] { 1, 2 }, pagination);
        TestAssert.Equal(2, result.Pagination.Page);
        TestAssert.Equal(10, result.Pagination.PageSize);
        TestAssert.Equal(100, result.Pagination.TotalItems);
    }

    /// <summary>Null items are rejected.</summary>
    [Test]
    public void Constructor_RejectsNullItems()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new PagedResult<string>(null!, Meta()));
    }

    /// <summary>Null pagination is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullPagination()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new PagedResult<string>(new[] { "a" }, null!));
    }

    /// <summary>Mutating the source list does not affect the result.</summary>
    [Test]
    public void Constructor_TakesDefensiveCopyOfItems()
    {
        var source = new List<string> { "a", "b" };
        var result = new PagedResult<string>(source, Meta());
        source.Add("c");
        source[0] = "modified";
        TestAssert.Equal(2, result.Items.Count);
        TestAssert.Equal("a", result.Items[0]);
    }

    /// <summary>An empty items list is valid.</summary>
    [Test]
    public void EmptyItems_ProducesEmptyResult()
    {
        var result = new PagedResult<string>(Array.Empty<string>(), Meta());
        TestAssert.Equal(0, result.Items.Count);
    }

    /// <summary>PagedResult works with value types.</summary>
    [Test]
    public void GenericType_ValueType_Works()
    {
        var result = new PagedResult<int>(new[] { 1, 2, 3 }, Meta());
        TestAssert.Equal(3, result.Items.Count);
        TestAssert.Equal(2, result.Items[1]);
    }

    /// <summary>Two results with the same values carry the same properties.</summary>
    [Test]
    public void Properties_MatchWhenConstructedWithSameValues()
    {
        var items = new[] { "x", "y" };
        var meta = Meta();
        var a = new PagedResult<string>(items, meta);
        var b = new PagedResult<string>(items, meta);
        TestAssert.Equal(a.Items.Count, b.Items.Count);
        TestAssert.Equal(a.Pagination.Page, b.Pagination.Page);
    }
}

