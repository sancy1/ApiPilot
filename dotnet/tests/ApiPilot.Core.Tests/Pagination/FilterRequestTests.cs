// filepath: dotnet/tests/ApiPilot.Core.Tests/Pagination/FilterRequestTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the FilterRequest value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Pagination
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FilterRequest.cs, SortRequestTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Pagination;

namespace ApiPilot.Core.Tests.Pagination;

/// <summary>
/// Contract tests for FilterRequest. Verifies construction, key validation,
/// duplicate detection, and defensive copying.
/// </summary>
[TestClass]
public sealed class FilterRequestTests
{
    /// <summary>The Empty singleton has no pairs.</summary>
    [Test]
    public void Empty_IsEmpty()
    {
        TestAssert.Equal(0, FilterRequest.Empty.Pairs.Count);
    }

    /// <summary>An empty sequence returns the Empty singleton.</summary>
    [Test]
    public void Create_EmptySequence_ReturnsEmptySingleton()
    {
        var request = FilterRequest.Create(Array.Empty<KeyValuePair<string, string>>());
        TestAssert.True(ReferenceEquals(request, FilterRequest.Empty));
    }

    /// <summary>A single pair is preserved.</summary>
    [Test]
    public void Create_SinglePair_PreservesPair()
    {
        var pairs = new[] { new KeyValuePair<string, string>("status", "active") };
        var request = FilterRequest.Create(pairs);
        TestAssert.Equal(1, request.Pairs.Count);
        TestAssert.Equal("active", request.Pairs["status"]);
    }

    /// <summary>Multiple pairs are preserved.</summary>
    [Test]
    public void Create_MultiplePairs_PreservesAll()
    {
        var pairs = new[]
        {
            new KeyValuePair<string, string>("status", "active"),
            new KeyValuePair<string, string>("region", "eu-west-1"),
            new KeyValuePair<string, string>("tier", "premium")
        };
        var request = FilterRequest.Create(pairs);
        TestAssert.Equal(3, request.Pairs.Count);
        TestAssert.Equal("active", request.Pairs["status"]);
        TestAssert.Equal("eu-west-1", request.Pairs["region"]);
        TestAssert.Equal("premium", request.Pairs["tier"]);
    }

    /// <summary>Keys are trimmed of surrounding whitespace.</summary>
    [Test]
    public void Create_TrimsKeys()
    {
        var pairs = new[] { new KeyValuePair<string, string>("  status  ", "active") };
        var request = FilterRequest.Create(pairs);
        TestAssert.Equal("active", request.Pairs["status"]);
    }

    /// <summary>A null pair sequence is rejected.</summary>
    [Test]
    public void Create_RejectsNullSequence()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            FilterRequest.Create(null!));
    }

    /// <summary>A null key is rejected.</summary>
    [Test]
    public void Create_RejectsNullKey()
    {
        var pairs = new[] { new KeyValuePair<string, string>(null!, "value") };
        TestAssert.Throws<ArgumentException>(() =>
            FilterRequest.Create(pairs));
    }

    /// <summary>An empty key is rejected.</summary>
    [Test]
    public void Create_RejectsEmptyKey()
    {
        var pairs = new[] { new KeyValuePair<string, string>("", "value") };
        TestAssert.Throws<ArgumentException>(() =>
            FilterRequest.Create(pairs));
    }

    /// <summary>A whitespace-only key is rejected.</summary>
    [Test]
    public void Create_RejectsWhitespaceKey()
    {
        var pairs = new[] { new KeyValuePair<string, string>("   ", "value") };
        TestAssert.Throws<ArgumentException>(() =>
            FilterRequest.Create(pairs));
    }

    /// <summary>Two pairs with the same key are rejected.</summary>
    [Test]
    public void Create_RejectsDuplicateKeys()
    {
        var pairs = new[]
        {
            new KeyValuePair<string, string>("status", "active"),
            new KeyValuePair<string, string>("status", "inactive")
        };
        TestAssert.Throws<ArgumentException>(() =>
            FilterRequest.Create(pairs));
    }

    /// <summary>Empty string values are permitted.</summary>
    [Test]
    public void Create_AllowsEmptyValues()
    {
        var pairs = new[] { new KeyValuePair<string, string>("status", "") };
        var request = FilterRequest.Create(pairs);
        TestAssert.Equal("", request.Pairs["status"]);
    }

    /// <summary>Mutating the source after Create does not affect the request.</summary>
    [Test]
    public void Create_TakesDefensiveCopyOfPairs()
    {
        var source = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("status", "active")
        };
        var request = FilterRequest.Create(source);
        source.Add(new KeyValuePair<string, string>("region", "eu-west-1"));
        TestAssert.Equal(1, request.Pairs.Count);
    }
}

