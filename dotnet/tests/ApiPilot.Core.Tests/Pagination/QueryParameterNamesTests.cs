// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/QueryParameterNamesTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the QueryParameterNames data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : QueryParameterNames.cs, PaginationOptionsTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for QueryParameterNames. Verifies the default names
/// and mutability.
/// </summary>
[TestClass]
public sealed class QueryParameterNamesTests
{
    /// <summary>The Default instance carries the standard names.</summary>
    [Test]
    public void Default_HasStandardNames()
    {
        var names = QueryParameterNames.Default;
        TestAssert.Equal("page", names.Page);
        TestAssert.Equal("pageSize", names.PageSize);
        TestAssert.Equal("sort", names.Sort);
        TestAssert.Equal("direction", names.Direction);
    }

    /// <summary>A fresh instance carries the standard names.</summary>
    [Test]
    public void NewInstance_HasStandardNames()
    {
        var names = new QueryParameterNames();
        TestAssert.Equal("page", names.Page);
        TestAssert.Equal("pageSize", names.PageSize);
        TestAssert.Equal("sort", names.Sort);
        TestAssert.Equal("direction", names.Direction);
    }

    /// <summary>The Page property is settable.</summary>
    [Test]
    public void Page_IsMutable()
    {
        var names = new QueryParameterNames();
        names.Page = "pageNumber";
        TestAssert.Equal("pageNumber", names.Page);
    }

    /// <summary>The PageSize property is settable.</summary>
    [Test]
    public void PageSize_IsMutable()
    {
        var names = new QueryParameterNames();
        names.PageSize = "perPage";
        TestAssert.Equal("perPage", names.PageSize);
    }

    /// <summary>The Sort property is settable.</summary>
    [Test]
    public void Sort_IsMutable()
    {
        var names = new QueryParameterNames();
        names.Sort = "orderBy";
        TestAssert.Equal("orderBy", names.Sort);
    }

    /// <summary>The Direction property is settable.</summary>
    [Test]
    public void Direction_IsMutable()
    {
        var names = new QueryParameterNames();
        names.Direction = "order";
        TestAssert.Equal("order", names.Direction);
    }

    /// <summary>All four properties are independently settable.</summary>
    [Test]
    public void AllProperties_AreIndependentlySettable()
    {
        var names = new QueryParameterNames
        {
            Page = "p",
            PageSize = "s",
            Sort = "by",
            Direction = "dir",
        };
        TestAssert.Equal("p", names.Page);
        TestAssert.Equal("s", names.PageSize);
        TestAssert.Equal("by", names.Sort);
        TestAssert.Equal("dir", names.Direction);
    }
}

