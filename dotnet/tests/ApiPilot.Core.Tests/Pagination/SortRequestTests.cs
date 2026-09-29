// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/SortRequestTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for SortRequest and SortDirection extensions
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SortRequest.cs, FilterRequestTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for SortRequest and the SortDirection wire-value helpers.
/// Verifies construction, field validation, and wire parsing.
/// </summary>
[TestClass]
public sealed class SortRequestTests
{
    /// <summary>Valid arguments produce a request.</summary>
    [Test]
    public void Create_ValidArguments_ProducesRequest()
    {
        var request = SortRequest.Create("name", SortDirection.Ascending);
        TestAssert.Equal("name", request.Field);
        TestAssert.Equal(SortDirection.Ascending, request.Direction);
    }

    /// <summary>The field name is trimmed.</summary>
    [Test]
    public void Create_TrimsFieldName()
    {
        var request = SortRequest.Create("  name  ", SortDirection.Ascending);
        TestAssert.Equal("name", request.Field);
    }

    /// <summary>A null field name is rejected.</summary>
    [Test]
    public void Create_RejectsNullField()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            SortRequest.Create(null!, SortDirection.Ascending));
    }

    /// <summary>An empty field name is rejected.</summary>
    [Test]
    public void Create_RejectsEmptyField()
    {
        TestAssert.Throws<ArgumentException>(() =>
            SortRequest.Create("", SortDirection.Ascending));
    }

    /// <summary>A whitespace-only field name is rejected.</summary>
    [Test]
    public void Create_RejectsWhitespaceField()
    {
        TestAssert.Throws<ArgumentException>(() =>
            SortRequest.Create("   ", SortDirection.Ascending));
    }

    /// <summary>The Ascending direction is preserved.</summary>
    [Test]
    public void Create_AscendingDirection_IsPreserved()
    {
        var request = SortRequest.Create("name", SortDirection.Ascending);
        TestAssert.Equal(SortDirection.Ascending, request.Direction);
    }

    /// <summary>The Descending direction is preserved.</summary>
    [Test]
    public void Create_DescendingDirection_IsPreserved()
    {
        var request = SortRequest.Create("name", SortDirection.Descending);
        TestAssert.Equal(SortDirection.Descending, request.Direction);
    }

    /// <summary>ToWireValue returns "asc" for Ascending.</summary>
    [Test]
    public void ToWireValue_Ascending_ReturnsAsc()
    {
        TestAssert.Equal("asc", SortDirection.Ascending.ToWireValue());
    }

    /// <summary>ToWireValue returns "desc" for Descending.</summary>
    [Test]
    public void ToWireValue_Descending_ReturnsDesc()
    {
        TestAssert.Equal("desc", SortDirection.Descending.ToWireValue());
    }

    /// <summary>TryParseWireValue parses "asc".</summary>
    [Test]
    public void TryParseWireValue_Asc_ReturnsAscending()
    {
        var ok = SortDirectionExtensions.TryParseWireValue("asc", out var direction);
        TestAssert.True(ok);
        TestAssert.Equal(SortDirection.Ascending, direction);
    }

    /// <summary>TryParseWireValue parses "desc".</summary>
    [Test]
    public void TryParseWireValue_Desc_ReturnsDescending()
    {
        var ok = SortDirectionExtensions.TryParseWireValue("desc", out var direction);
        TestAssert.True(ok);
        TestAssert.Equal(SortDirection.Descending, direction);
    }

    /// <summary>TryParseWireValue is case-insensitive.</summary>
    [Test]
    public void TryParseWireValue_CaseInsensitive()
    {
        TestAssert.True(SortDirectionExtensions.TryParseWireValue("ASC", out var up));
        TestAssert.Equal(SortDirection.Ascending, up);
        TestAssert.True(SortDirectionExtensions.TryParseWireValue("DESC", out var down));
        TestAssert.Equal(SortDirection.Descending, down);
    }

    /// <summary>TryParseWireValue rejects unknown values.</summary>
    [Test]
    public void TryParseWireValue_UnknownValue_ReturnsFalse()
    {
        var ok = SortDirectionExtensions.TryParseWireValue("sideways", out _);
        TestAssert.False(ok);
    }

    /// <summary>TryParseWireValue rejects null.</summary>
    [Test]
    public void TryParseWireValue_NullValue_ReturnsFalse()
    {
        var ok = SortDirectionExtensions.TryParseWireValue(null, out _);
        TestAssert.False(ok);
    }
}

