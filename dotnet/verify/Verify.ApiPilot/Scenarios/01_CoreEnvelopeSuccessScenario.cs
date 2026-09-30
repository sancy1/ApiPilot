// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/01_CoreEnvelopeSuccessScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 01. Proves that a real consumer of the published
//           ApiPilot.Core 1.0.3 package can construct the documented success
//           envelope and serialize it to the documented wire shape.
// relates:  Uses ApiPilot.Core.Responses.ApiResponseBuilder,
//           ApiPilot.Core.Metadata.ResponseMetadata, and
//           ApiPilot.Core.Responses.ApiResponse<T>. Consumed by Program.cs.
// authority: the shipped lib/net10.0/ApiPilot.Core.xml and the packaged
//           README.md. Not the source tree.

using System.Text.Json;
using System.Text.Json.Nodes;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 01. The smallest observable proof that the published
/// ApiPilot.Core package can be consumed by an external developer and
/// produce the documented success envelope.
/// </summary>
public static class CoreEnvelopeSuccessScenario
{
    /// <summary>
    /// The scenario name, matching the file name without the .cs extension.
    /// </summary>
    public const string Name = "01_CoreEnvelopeSuccess";

    /// <summary>
    /// The JSON options used to serialize the response envelope.
    /// Hoisted to a static readonly field per CA1869.
    /// </summary>
    private static readonly JsonSerializerOptions WireJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Runs the scenario and returns the result.
    /// </summary>
    public static ScenarioResult Run()
    {
        try
        {
            // Construct a correlation id that the wire shape must echo.
            const string requestId = "01J8Z3K7X9Y2F0A6M3N5P7Q1R2";
            const string message   = "Order retrieved successfully.";

            // Build response metadata via the only construction path the
            // shipped XML documents: Empty.WithRequestId.
            var meta = ResponseMetadata.Empty.WithRequestId(requestId);

            // Build a small DTO that matches the documented example.
            var order = new OrderDto(Id: "ORD-10001", Status: "confirmed");

            // Build the success envelope.
            ApiResponse<OrderDto> response = ApiResponseBuilder.Ok(order, meta, message);

            // Assert the in-process shape.
            if (response is null)
                return ScenarioResult.Failed(Name, "ApiResponseBuilder.Ok returned null.");
            if (response.Success != true)
                return ScenarioResult.Failed(Name, "Expected Success == true; got " + response.Success + ".");
            if (response.Data is null)
                return ScenarioResult.Failed(Name, "Expected Data not null.");
            if (response.Data.Id != "ORD-10001" || response.Data.Status != "confirmed")
                return ScenarioResult.Failed(Name, "Data did not round-trip.");
            if (response.Message != message)
                return ScenarioResult.Failed(Name, "Expected Message == the passed message.");
            if (response.Meta is null)
                return ScenarioResult.Failed(Name, "Expected Meta not null.");
            if (response.Meta.RequestId != requestId)
                return ScenarioResult.Failed(Name, "Expected Meta.RequestId == " + requestId + "; got " + response.Meta.RequestId + ".");

            // Serialize and assert the wire shape documented in the README.
            var json = JsonSerializer.Serialize(response, WireJsonOptions);

            JsonNode? node;
            try
            {
                node = JsonNode.Parse(json);
            }
            catch (JsonException ex)
            {
                return ScenarioResult.Failed(Name, "Serialized response is not valid JSON.", ex);
            }

            if (node is null)
                return ScenarioResult.Failed(Name, "JsonNode.Parse returned null.");

            var obj = node.AsObject();

            // Top-level keys.
            if (!obj.ContainsKey("success"))  return ScenarioResult.Failed(Name, "Wire shape missing 'success'.");
            if (!obj.ContainsKey("data"))     return ScenarioResult.Failed(Name, "Wire shape missing 'data'.");
            if (!obj.ContainsKey("message"))  return ScenarioResult.Failed(Name, "Wire shape missing 'message'.");
            if (!obj.ContainsKey("meta"))     return ScenarioResult.Failed(Name, "Wire shape missing 'meta'.");

            // status must NOT be present.
            if (obj.ContainsKey("status"))
                return ScenarioResult.Failed(Name, "Wire shape must not contain 'status'.");

            // success must be true.
            if (obj["success"]!.GetValue<bool>() != true)
                return ScenarioResult.Failed(Name, "'success' is not true on the wire.");

            // meta sub-shape.
            var metaObj = obj["meta"]!.AsObject();
            if (!metaObj.ContainsKey("requestId"))
                return ScenarioResult.Failed(Name, "'meta.requestId' missing on the wire.");
            if (!metaObj.ContainsKey("timestamp"))
                return ScenarioResult.Failed(Name, "'meta.timestamp' missing on the wire.");
            if (!metaObj.ContainsKey("extra"))
                return ScenarioResult.Failed(Name, "'meta.extra' missing on the wire.");

            // message sub-value: must be present and equal to the message.
            var messageValue = obj["message"];
            if (messageValue is null)
                return ScenarioResult.Failed(Name, "'message' present but null.");

            return ScenarioResult.Passed(
                Name,
                "Success envelope constructed and serialized to the documented wire shape.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    /// <summary>
    /// The minimal DTO used by the scenario. Matches the documented example.
    /// </summary>
    private sealed record OrderDto(string Id, string Status);
}