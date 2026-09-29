// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/PaginationOptionsTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the PaginationOptions data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : PaginationOptions.cs, PageRequestTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for PaginationOptions. The type is a mutable data holder;
/// validation happens in the ASP.NET Core options validation step, not here.
/// </summary>
[TestClass]
public sealed class PaginationOptionsTests
{
    /// <summary>Default values match the documented defaults.</summary>
    [Test]
    public void Defaults_AreSensible()
    {
        var options = new PaginationOptions();
        TestAssert.Equal(20, options.DefaultPageSize);
        TestAssert.Equal(100, options.MaxPageSize);
    }

    /// <summary>Properties can be updated after construction.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new PaginationOptions();
        options.DefaultPageSize = 25;
        options.MaxPageSize = 250;
        TestAssert.Equal(25, options.DefaultPageSize);
        TestAssert.Equal(250, options.MaxPageSize);
    }

    /// <summary>Defaults can be overridden via object initializer.</summary>
    [Test]
    public void Defaults_CanBeOverridden()
    {
        var options = new PaginationOptions
        {
            DefaultPageSize = 50,
            MaxPageSize = 500
        };
        TestAssert.Equal(50, options.DefaultPageSize);
        TestAssert.Equal(500, options.MaxPageSize);
    }

    /// <summary>The data holder accepts any integer values.</summary>
    [Test]
    public void ZeroValues_AreAllowedAtConstruction()
    {
        var options = new PaginationOptions { DefaultPageSize = 0, MaxPageSize = 0 };
        TestAssert.Equal(0, options.DefaultPageSize);
        TestAssert.Equal(0, options.MaxPageSize);
    }

    /// <summary>Negative values are also accepted; validation is elsewhere.</summary>
    [Test]
    public void NegativeValues_AreAllowedAtConstruction()
    {
        var options = new PaginationOptions { DefaultPageSize = -1, MaxPageSize = -1 };
        TestAssert.Equal(-1, options.DefaultPageSize);
        TestAssert.Equal(-1, options.MaxPageSize);
    }

    /// <summary>MaxPageSize can be set lower than DefaultPageSize; validation is elsewhere.</summary>
    [Test]
    public void MaxPageSize_CanBeLowerThanDefault()
    {
        var options = new PaginationOptions { DefaultPageSize = 100, MaxPageSize = 10 };
        TestAssert.Equal(100, options.DefaultPageSize);
        TestAssert.Equal(10, options.MaxPageSize);
    }
}

