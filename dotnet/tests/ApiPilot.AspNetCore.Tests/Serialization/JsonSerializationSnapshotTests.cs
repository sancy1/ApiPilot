// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Serialization/JsonSerializationSnapshotTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Wire-format contract tests for the ApiPilot response envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + helper types)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Core.Metadata, ApiPilot.Core.Responses, System.Text.Json
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SPEC.md (success envelope), JsonSerializerConfigurator.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Serialization;

/// <summary>
/// Wire-format contract tests for the ApiPilot response envelope. Each test
/// serializes a canonical envelope with ApiPilot JSON conventions and then
/// parses the result back into a JsonDocument for structural assertions.
/// The tests do not compare literal JSON strings, so they are resilient to
/// whitespace and property ordering.
/// </summary>
[TestClass]
public sealed class JsonSerializationSnapshotTests
{
    private static JsonSerializerOptions Options()
    {
        var target = new JsonSerializerOptions();
        JsonSerializerConfigurator.Configure(target, new ApiPilotJsonOptions());
        return target;
    }

    private static ResponseMetadata Meta()
    {
        return new ResponseMetadata { RequestId = "req-snapshot-001" };
    }

    private static string Serialize(ApiResponse<SampleOrder> response)
    {
        return JsonSerializer.Serialize(response, Options());
    }

    /// <summary>The serialized success envelope contains success and data keys.</summary>
    [Test]
    public void Serialize_SuccessEnvelope_HasSuccessAndDataKeys()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-1", Status = "confirmed" },
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("success", out var success));
        TestAssert.True(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("data", out _));
    }

    /// <summary>Metadata keys use camelCase on the wire.</summary>
    [Test]
    public void Serialize_SuccessEnvelope_UsesCamelCaseKeys()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-2", Status = "confirmed" },
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("meta", out var meta));
        TestAssert.True(meta.TryGetProperty("requestId", out var requestId));
        TestAssert.Equal("req-snapshot-001", requestId.GetString());
    }

    /// <summary>The data payload preserves its nested shape.</summary>
    [Test]
    public void Serialize_SuccessEnvelope_DataPreservesNestedShape()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-3", Status = "pending" },
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var data = doc.RootElement.GetProperty("data");
        TestAssert.Equal("ORD-3", data.GetProperty("id").GetString());
        TestAssert.Equal("pending", data.GetProperty("status").GetString());
    }

    /// <summary>Null data is written explicitly.</summary>
    [Test]
    public void Serialize_EnvelopeWithNullData_WritesNullDataKey()
    {
        var response = new ApiResponse<SampleOrder>(
            null,
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("data", out var data));
        TestAssert.Equal(JsonValueKind.Null, data.ValueKind);
    }

    /// <summary>A supplied message appears on the wire.</summary>
    [Test]
    public void Serialize_EnvelopeWithMessage_IncludesMessageKey()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-4", Status = "ok" },
            ApiResponseStatus.Ok,
            Meta(),
            "Order retrieved.");
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.True(root.TryGetProperty("message", out var message));
        TestAssert.Equal("Order retrieved.", message.GetString());
    }

    /// <summary>The metadata timestamp round-trips as ISO 8601.</summary>
    [Test]
    public void Serialize_ResponseMetadata_TimestampIsIso8601()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-5", Status = "ok" },
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var meta = doc.RootElement.GetProperty("meta");
        var ts = meta.GetProperty("timestamp").GetString();
        TestAssert.NotNull(ts);
        TestAssert.True(DateTimeOffset.TryParse(ts, out var parsed));
        TestAssert.Equal(TimeSpan.Zero, parsed.Offset);
    }

    /// <summary>
    /// The success envelope does not include a transport-neutral status
    /// property on the wire. The HTTP status is carried by the status
    /// line, not the body. This test enforces the SPEC.md success
    /// envelope shape after finding A-072a.
    /// </summary>
    [Test]
    public void Serialize_SuccessEnvelope_DoesNotIncludeStatusKey()
    {
        var response = new ApiResponse<SampleOrder>(
            new SampleOrder { Id = "ORD-6", Status = "ok" },
            ApiResponseStatus.Ok,
            Meta());
        var json = Serialize(response);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        TestAssert.False(root.TryGetProperty("status", out _));
    }
}

/// <summary>A minimal DTO for envelope serialization tests.</summary>
public sealed class SampleOrder
{
    /// <summary>The order identifier.</summary>
    public string? Id { get; set; }

    /// <summary>The order status.</summary>
    public string? Status { get; set; }
}
