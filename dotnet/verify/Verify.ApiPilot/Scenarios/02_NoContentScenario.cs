// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/02_NoContentScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 02. Proves that a real consumer of the published
//           ApiPilot.Core 1.0.3 package can construct the non-generic success
//           envelope via ApiResponseBuilder.NoContent and serialize it to the
//           documented wire shape, with data absent and status absent.
// relates:  Uses ApiPilot.Core.Responses.ApiResponseBuilder.NoContent,
//           ApiPilot.Core.Responses.ApiResponse, and
//           ApiPilot.Core.Metadata.ResponseMetadata. Consumed by Program.cs.
// authority: the shipped lib/net10.0/ApiPilot.Core.xml and the packaged
//           README.md. Not the source tree.

using System.Text.Json;
using System.Text.Json.Nodes;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 02. The non-generic success envelope, produced by
/// ApiResponseBuilder.NoContent. Asserts the documented wire shape for a
/// payload-free success.
/// </summary>
public static class NoContentScenario
{
    /// <summary>
    /// The scenario name, matching the file name without the .cs extension.
    /// </summary>
    public const string Name = "02_NoContent";

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
            const string requestId = "01J8Z3K7X9Y2F0A6M3N5P7Q1R3";
            const string message   = "Resource deleted.";

            var meta = ResponseMetadata.Empty.WithRequestId(requestId);

            ApiResponse response = ApiResponseBuilder.NoContent(meta, message);

            if (response is null)
                return ScenarioResult.Failed(Name, "ApiResponseBuilder.NoContent returned null.");
            if (response.Success != true)
                return ScenarioResult.Failed(Name, "Expected Success == true; got " + response.Success + ".");
            if (response.Message != message)
                return ScenarioResult.Failed(Name, "Expected Message == the passed message.");
            if (response.Meta is null)
                return ScenarioResult.Failed(Name, "Expected Meta not null.");
            if (response.Meta.RequestId != requestId)
                return ScenarioResult.Failed(Name, "Expected Meta.RequestId == " + requestId + "; got " + response.Meta.RequestId + ".");
            if (response.Status != ApiResponseStatus.NoContent)
                return ScenarioResult.Failed(Name, "Expected Status == NoContent; got " + response.Status + ".");

            var json = JsonSerializer.Serialize(response, WireJsonOptions);

            JsonNode? node;
            try { node = JsonNode.Parse(json); }
            catch (JsonException ex) { return ScenarioResult.Failed(Name, "Serialized response is not valid JSON.", ex); }
            if (node is null) return ScenarioResult.Failed(Name, "JsonNode.Parse returned null.");

            var obj = node.AsObject();

            if (!obj.ContainsKey("success"))  return ScenarioResult.Failed(Name, "Wire shape missing 'success'.");
            if (!obj.ContainsKey("message"))  return ScenarioResult.Failed(Name, "Wire shape missing 'message'.");
            if (!obj.ContainsKey("meta"))     return ScenarioResult.Failed(Name, "Wire shape missing 'meta'.");

            if (obj.ContainsKey("data"))
                return ScenarioResult.Failed(Name, "Non-generic success must not carry 'data' on the wire.");
            if (obj.ContainsKey("status"))
                return ScenarioResult.Failed(Name, "Wire shape must not contain 'status'.");

            if (obj["success"]!.GetValue<bool>() != true)
                return ScenarioResult.Failed(Name, "'success' is not true on the wire.");

            var metaObj = obj["meta"]!.AsObject();
            if (!metaObj.ContainsKey("requestId")) return ScenarioResult.Failed(Name, "'meta.requestId' missing on the wire.");
            if (!metaObj.ContainsKey("timestamp")) return ScenarioResult.Failed(Name, "'meta.timestamp' missing on the wire.");
            if (!metaObj.ContainsKey("extra"))     return ScenarioResult.Failed(Name, "'meta.extra' missing on the wire.");

            return ScenarioResult.Passed(
                Name,
                "Non-generic success envelope constructed and serialized to the documented wire shape.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }
}