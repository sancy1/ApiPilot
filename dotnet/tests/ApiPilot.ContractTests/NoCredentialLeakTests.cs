// filepath: dotnet/tests/ApiPilot.ContractTests/NoCredentialLeakTests.cs
// layer: Contract | package: ApiPilot.ContractTests | since: v0.6.0
// purpose: Scan real response bodies for credential or token leakage
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, SampleHost, HttpClient
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SampleHost.cs, EnvelopeContractTests.cs
// -----------------------------------------------------------------------------
//
// THE LEAK GATE
//   Each test scans the actual response body for Set-Cookie, Cookie,
//   Authorization, and X-CSRF-TOKEN. The scan is case-insensitive. Every
//   scan asserts the body is non-empty first, so a scan over an empty
//   body cannot pass vacuously (A-187). The check lives in one helper so
//   a defect in the check is a defect in one place.

using System.Net.Http;
using System.Text;

namespace ApiPilot.ContractTests;

/// <summary>
/// Credential-leak contract tests: no ApiPilot response body may carry
/// credential or token text.
/// </summary>
[TestClass]
public sealed class NoCredentialLeakTests
{
    private static readonly string[] s_forbidden =
    {
        "Set-Cookie",
        "Cookie",
        "Authorization",
        "X-CSRF-TOKEN"
    };

    private static void AssertNoCredentialTokens(string body, string label)
    {
        TestAssert.True(body.Length > 0, label + ": expected a non-empty body");
        foreach (var token in s_forbidden)
        {
            var index = body.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            TestAssert.True(index < 0, label + ": body contains forbidden token [" + token + "]");
        }
    }

    /// <summary>The success body carries no credential tokens.</summary>
    [Test]
    public async Task Ok_Body_HasNoCredentialTokens()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/ok");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoCredentialTokens(body, "GET /api/ok");
    }

    /// <summary>The error body carries no credential tokens.</summary>
    [Test]
    public async Task Fail_Body_HasNoCredentialTokens()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/fail");
        var body = await response.Content.ReadAsStringAsync();
        AssertNoCredentialTokens(body, "GET /api/fail");
    }

    /// <summary>The validation body carries no credential tokens.</summary>
    [Test]
    public async Task Validate_Body_HasNoCredentialTokens()
    {
        await using var sample = await SampleHost.StartAsync();
        var payload = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await sample.Client.PostAsync("/api/validate", payload);
        var body = await response.Content.ReadAsStringAsync();
        AssertNoCredentialTokens(body, "POST /api/validate");
    }

    /// <summary>The CSRF-rejected body carries no credential tokens.</summary>
    [Test]
    public async Task SubmitRejected_Body_HasNoCredentialTokens()
    {
        await using var sample = await SampleHost.StartAsync();
        var payload = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await sample.Client.PostAsync("/api/submit", payload);
        var body = await response.Content.ReadAsStringAsync();
        AssertNoCredentialTokens(body, "POST /api/submit");
    }
}

