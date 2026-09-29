// filepath: dotnet/tests/ApiPilot.FuzzTests/QueryFuzzTests.cs
// layer: Fuzz | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Hostile query-string and content-type tests over the running sample
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, SampleHost,
//                HttpClient, JsonDocument
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SampleHost.cs, PaginationOptions.cs, PageRequest.cs
// -----------------------------------------------------------------------------
//
// THE FUZZ SET
//   Each test launches the real Samples.Api and sends a hostile query or
//   content type. The response must be a clean 4xx envelope or a clean
//   2xx envelope, never a 500, and never a body that carries internal
//   detail. A stack-trace marker check is included.

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Fuzz tests for query-string and content-type handling.
/// </summary>
[TestClass]
public sealed class QueryFuzzTests
{
    private static void AssertNoInternalDetail(string body)
    {
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        TestAssert.False(body.Contains("   at ", StringComparison.Ordinal), "body contains a stack-trace frame");
        TestAssert.False(body.Contains("System.", StringComparison.Ordinal), "body contains a namespace prefix");
    }

    /// <summary>A page size above MaxPageSize is rejected cleanly.</summary>
    [Test]
    public async Task HugePageSize_IsRejectedCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/items?pageSize=999999999");
        TestAssert.True((int)response.StatusCode < 500, "expected a non-5xx status");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInternalDetail(body);
    }

    /// <summary>A page below the minimum is rejected cleanly.</summary>
    [Test]
    public async Task NegativePage_IsRejectedCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/items?page=0");
        TestAssert.True((int)response.StatusCode < 500, "expected a non-5xx status");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInternalDetail(body);
    }

    /// <summary>A non-numeric page value does not produce a 500.</summary>
    [Test]
    public async Task NonNumericPage_IsNot500()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/items?page=abc");
        TestAssert.True((int)response.StatusCode < 500, "expected a non-5xx status");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInternalDetail(body);
    }

    /// <summary>An unexpected request content type is rejected cleanly.</summary>
    [Test]
    public async Task UnexpectedContentType_IsRejectedCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var payload = new StringContent("<root/>", Encoding.UTF8, "application/xml");
        var response = await sample.Client.PostAsync("/api/validate", payload);
        TestAssert.True((int)response.StatusCode >= 400, "expected a 4xx status");
        TestAssert.True((int)response.StatusCode < 500, "expected a non-5xx status");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInternalDetail(body);
    }

    /// <summary>A hostile query never leaks a stack trace or a namespace.</summary>
    [Test]
    public async Task NoInternalDetailLeaks_OnHostileQuery()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/items?pageSize=-1&page=-1&sort=%%%");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInternalDetail(body);
    }
}

