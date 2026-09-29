// filepath: dotnet/tests/ApiPilot.ContractTests/EnvelopeContractTests.cs
// layer: Contract | package: ApiPilot.ContractTests | since: v0.6.0
// purpose: Sweep the running sample endpoint surface and assert the ApiPilot envelope contract
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, SampleHost,
//                HttpClient, JsonDocument
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SampleHost.cs, StatusMappingContractTests.cs, SPEC.md
// -----------------------------------------------------------------------------
//
// THE SWEEP
//   Each test launches the real Samples.Api, issues a real HTTP request,
//   and asserts the envelope on the actual response bytes. The tests read
//   camelCase keys because the sample registers AddApiPilotJson, which
//   applies camelCase naming to the wire. The error-envelope tests assert
//   the envelope shape and a stable code; they do not hardcode a status
//   that depends on the application exception map.

using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ApiPilot.ContractTests;

/// <summary>
/// Contract tests for the ApiPilot envelope over the running sample.
/// </summary>
[TestClass]
public sealed class EnvelopeContractTests
{
    /// <summary>GET /api/ok returns the success envelope.</summary>
    [Test]
    public async Task Ok_ReturnsSuccessEnvelope()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/ok");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.True(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("data", out _));
    }

    /// <summary>GET /api/fail returns the error envelope with a stable code.</summary>
    [Test]
    public async Task Fail_ReturnsErrorEnvelope()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/fail");
        TestAssert.True((int)response.StatusCode >= 400, "expected an error status");
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.False(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("error", out var error));
        TestAssert.True(error.TryGetProperty("code", out var code));
        TestAssert.Equal(JsonValueKind.String, code.ValueKind);
        TestAssert.True(code.GetString()!.Length > 0, "expected a non-empty error code");
    }

    /// <summary>POST /api/validate returns the validation error envelope.</summary>
    [Test]
    public async Task Validate_ReturnsValidationEnvelope()
    {
        await using var sample = await SampleHost.StartAsync();
        var payload = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await sample.Client.PostAsync("/api/validate", payload);
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.False(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("error", out _));
    }

    /// <summary>GET /api/items returns a success envelope whose data carries pagination.</summary>
    [Test]
    public async Task Items_ReturnsPaginatedEnvelope()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/items");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.True(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("data", out var data));
        TestAssert.True(data.TryGetProperty("pagination", out _));
    }

    /// <summary>GET the CSRF bootstrap path returns the documented bare token shape.</summary>
    [Test]
    public async Task CsrfBootstrap_ReturnsTokenShape()
    {
        await using var sample = await SampleHost.StartAsync();
        var response = await sample.Client.GetAsync("/api/csrf");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("token", out var token));
        TestAssert.Equal(JsonValueKind.String, token.ValueKind);
    }

    /// <summary>POST /api/submit without the CSRF header is rejected with the error envelope.</summary>
    [Test]
    public async Task Submit_WithoutToken_IsRejectedWithEnvelope()
    {
        await using var sample = await SampleHost.StartAsync();
        var payload = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await sample.Client.PostAsync("/api/submit", payload);
        TestAssert.Equal(403, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        TestAssert.True(body.Length > 0, "expected a non-empty body");
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.False(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("error", out _));
    }
}

