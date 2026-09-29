// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/PaginationMetadataTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the PaginationMetadata record
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationMetadata.cs, PagedResultTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for PaginationMetadata. Verifies construction, validation,
/// computed page count, and next/previous flags.
/// </summary>
[TestClass]
public sealed class PaginationMetadataTests
{
    /// <summary>Valid arguments produce a metadata instance.</summary>
    [Test]
    public void Create_ValidArguments_ProducesMetadata()
    {
        var meta = PaginationMetadata.Create(2, 20, 143);
        TestAssert.Equal(2, meta.Page);
        TestAssert.Equal(20, meta.PageSize);
        TestAssert.Equal(143, meta.TotalItems);
    }

    /// <summary>Page zero is rejected.</summary>
    [Test]
    public void Create_RejectsZeroPage()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationMetadata.Create(0, 20, 100));
    }

    /// <summary>Negative page is rejected.</summary>
    [Test]
    public void Create_RejectsNegativePage()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationMetadata.Create(-1, 20, 100));
    }

    /// <summary>Page size zero is rejected.</summary>
    [Test]
    public void Create_RejectsZeroPageSize()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationMetadata.Create(1, 0, 100));
    }

    /// <summary>Negative page size is rejected.</summary>
    [Test]
    public void Create_RejectsNegativePageSize()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationMetadata.Create(1, -5, 100));
    }

    /// <summary>Negative total items are rejected.</summary>
    [Test]
    public void Create_RejectsNegativeTotalItems()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            PaginationMetadata.Create(1, 20, -1));
    }

    /// <summary>TotalPages uses ceiling division.</summary>
    [Test]
    public void TotalPages_ComputesCeiling()
    {
        var meta = PaginationMetadata.Create(1, 20, 143);
        TestAssert.Equal(8, meta.TotalPages);
    }

    /// <summary>An empty collection still has one page.</summary>
    [Test]
    public void TotalPages_ZeroItems_ReturnsOne()
    {
        var meta = PaginationMetadata.Create(1, 20, 0);
        TestAssert.Equal(1, meta.TotalPages);
    }

    /// <summary>Exact division produces the exact page count.</summary>
    [Test]
    public void TotalPages_ExactDivision_ReturnsExact()
    {
        var meta = PaginationMetadata.Create(1, 20, 100);
        TestAssert.Equal(5, meta.TotalPages);
    }

    /// <summary>HasNext is true when more pages remain.</summary>
    [Test]
    public void HasNext_TrueWhenMorePagesRemain()
    {
        var meta = PaginationMetadata.Create(1, 20, 100);
        TestAssert.True(meta.HasNext);
    }

    /// <summary>HasNext is false on the last page.</summary>
    [Test]
    public void HasNext_FalseOnLastPage()
    {
        var meta = PaginationMetadata.Create(5, 20, 100);
        TestAssert.False(meta.HasNext);
    }

    /// <summary>HasPrevious is true after the first page.</summary>
    [Test]
    public void HasPrevious_TrueAfterFirstPage()
    {
        var meta = PaginationMetadata.Create(2, 20, 100);
        TestAssert.True(meta.HasPrevious);
    }

    /// <summary>HasPrevious is false on the first page.</summary>
    [Test]
    public void HasPrevious_FalseOnFirstPage()
    {
        var meta = PaginationMetadata.Create(1, 20, 100);
        TestAssert.False(meta.HasPrevious);
    }

    /// <summary>Records with primitives compare by value.</summary>
    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = PaginationMetadata.Create(2, 20, 143);
        var b = PaginationMetadata.Create(2, 20, 143);
        TestAssert.True(a == b);
    }

    /// <summary>Records with different values are not equal.</summary>
    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = PaginationMetadata.Create(1, 20, 143);
        var b = PaginationMetadata.Create(2, 20, 143);
        TestAssert.False(a == b);
    }
}

