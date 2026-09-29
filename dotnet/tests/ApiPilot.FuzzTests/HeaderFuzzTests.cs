// filepath: dotnet/tests/ApiPilot.FuzzTests/HeaderFuzzTests.cs
// layer: Fuzz | package: ApiPilot.FuzzTests | since: v0.6.0
// purpose: Hostile header and method tests over the running sample
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, SampleHost,
//                HttpClient, HttpRequestMessage
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SampleHost.cs, QueryFuzzTests.cs
// -----------------------------------------------------------------------------
//
// THE FUZZ SET
//   Each test launches the real Samples.Api and sends a hostile header or
//   an unusual method. The response must never be a 500. A clean 4xx or a
//   clean 405 is acceptable; an unhandled exception is not.

using System.Net.Http;
using System.Text;

namespace ApiPilot.FuzzTests;

/// <summary>
/// Fuzz tests for hostile headers and unusual HTTP methods.
/// </summary>
[TestClass]
public sealed class HeaderFuzzTests
{
    private static void AssertNotServerError(HttpResponseMessage response, string label)
    {
        var status = (int)response.StatusCode;
        TestAssert.True(status < 500, label + ": expected a non-5xx status but was " + status.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>A duplicate CSRF header is handled without a server error.</summary>
    [Test]
    public async Task DuplicateCsrfHeader_IsRejectedCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/submit");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", "first");
        request.Headers.TryAddWithoutValidation("X-CSRF-TOKEN", "second");
        var response = await sample.Client.SendAsync(request);
        AssertNotServerError(response, "duplicate CSRF header");
    }

    /// <summary>An OPTIONS request is handled without a server error.</summary>
    [Test]
    public async Task UnusualMethod_Options_IsHandledCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/ok");
        var response = await sample.Client.SendAsync(request);
        AssertNotServerError(response, "OPTIONS /api/ok");
    }

    /// <summary>A PATCH to a GET-only endpoint is handled without a server error.</summary>
    [Test]
    public async Task UnusualMethod_Patch_OnGet_IsHandledCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/ok");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await sample.Client.SendAsync(request);
        AssertNotServerError(response, "PATCH /api/ok");
    }

    /// <summary>An oversized Origin header is handled without a server error.</summary>
    [Test]
    public async Task OversizedHeaderValue_IsHandledCleanly()
    {
        await using var sample = await SampleHost.StartAsync();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/submit");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        var oversized = "https://" + new string('a', 2048) + ".example.com";
        request.Headers.TryAddWithoutValidation("Origin", oversized);
        var response = await sample.Client.SendAsync(request);
        AssertNotServerError(response, "oversized Origin header");
    }
}

