// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/03_ErrorEnvelopeScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 03. Proves that a real consumer of the published
//           ApiPilot.Core 1.0.3 package can construct the error envelope with
//           a stable code, a safe message, optional field-level errors, and
//           metadata, and serialize it to the documented error wire shape.
//           Asserts that no internal exception detail leaks into the payload.
// relates:  Uses ApiPilot.Core.Errors.ErrorResponse, ApiError, ApiErrorCode,
//           ApiErrorField. Consumed by Program.cs.
// authority: the shipped lib/net10.0/ApiPilot.Core.xml and the packaged
//           README.md. Not the source tree.

using System.Text.Json;
using System.Text.Json.Nodes;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 03. The error envelope. Asserts the documented error wire shape:
/// success == false, error.code, error.message, error.fields, meta.requestId,
/// and the absence of any internal exception detail.
/// </summary>
public static class ErrorEnvelopeScenario
{
    /// <summary>
    /// The scenario name, matching the file name without the .cs extension.
    /// </summary>
    public const string Name = "03_ErrorEnvelope";

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
            const string requestId = "01J8Z3K7X9Y2F0A6M3N5P7Q1R4";
            const string message   = "One or more values are invalid.";
            const string fieldName = "email";
            const string fieldMsg  = "Email is required.";

            var meta = ResponseMetadata.Empty.WithRequestId(requestId);

            // --- No-fields sub-scenario ---
            ApiError bare = ApiError.Create(ApiErrorCode.ValidationError, message);
            if (bare is null)                        return ScenarioResult.Failed(Name, "ApiError.Create returned null.");
            if (bare.Code is null)                   return ScenarioResult.Failed(Name, "ApiError.Code is null.");
            if (bare.Code.Code != "VALIDATION_ERROR") return ScenarioResult.Failed(Name, "Expected Code.Code == VALIDATION_ERROR; got " + bare.Code.Code + ".");
            if (bare.Message != message)             return ScenarioResult.Failed(Name, "ApiError.Message did not round-trip.");
            if (bare.Fields is not null)             return ScenarioResult.Failed(Name, "Expected Fields == null when no fields were supplied.");

            ErrorResponse bareResponse = new(bare, meta);
            if (bareResponse is null)                    return ScenarioResult.Failed(Name, "ErrorResponse constructor returned null.");
            if (bareResponse.Success != false)           return ScenarioResult.Failed(Name, "Expected ErrorResponse.Success == false.");
            if (bareResponse.Error is null)              return ScenarioResult.Failed(Name, "ErrorResponse.Error is null.");
            if (bareResponse.Meta is null)               return ScenarioResult.Failed(Name, "ErrorResponse.Meta is null.");
            if (bareResponse.Meta.RequestId != requestId) return ScenarioResult.Failed(Name, "ErrorResponse.Meta.RequestId did not round-trip.");

            // --- With-fields sub-scenario ---
            ApiErrorField field = ApiErrorField.WithMessage(fieldName, fieldMsg);
            if (field is null)                return ScenarioResult.Failed(Name, "ApiErrorField.WithMessage returned null.");
            if (field.Field != fieldName)     return ScenarioResult.Failed(Name, "ApiErrorField.Field did not round-trip.");
            if (field.Messages is null)       return ScenarioResult.Failed(Name, "ApiErrorField.Messages is null.");
            if (field.Messages.Count != 1)    return ScenarioResult.Failed(Name, "Expected exactly one message on the field error.");
            if (field.Messages[0] != fieldMsg) return ScenarioResult.Failed(Name, "Field message did not round-trip.");

            ApiError withFields = ApiError.Create(ApiErrorCode.ValidationError, message, new[] { field });
            if (withFields.Fields is null)                              return ScenarioResult.Failed(Name, "Expected Fields not null after supplying a field error.");
            if (withFields.Fields.Count != 1)                           return ScenarioResult.Failed(Name, "Expected exactly one field in the dictionary.");
            if (!withFields.Fields.ContainsKey(fieldName))              return ScenarioResult.Failed(Name, "Expected key '" + fieldName + "' in the fields dictionary.");
            if (withFields.Fields[fieldName].Count != 1)                return ScenarioResult.Failed(Name, "Expected one message for the field on the wire.");
            if (withFields.Fields[fieldName][0] != fieldMsg)            return ScenarioResult.Failed(Name, "Field message on the wire did not round-trip.");

            ErrorResponse fullResponse = new(withFields, meta);

            // --- Wire shape ---
            var json = JsonSerializer.Serialize(fullResponse, WireJsonOptions);

            // No internal exception detail may appear in the payload.
            if (json.Contains("   at ", StringComparison.Ordinal))
                return ScenarioResult.Failed(Name, "Serialized payload contains a stack-trace frame.");
            if (json.Contains("Exception", StringComparison.Ordinal))
                return ScenarioResult.Failed(Name, "Serialized payload mentions an exception type.");
            if (json.Contains("StackTrace", StringComparison.Ordinal))
                return ScenarioResult.Failed(Name, "Serialized payload contains 'StackTrace'.");
            if (json.Contains("InnerException", StringComparison.Ordinal))
                return ScenarioResult.Failed(Name, "Serialized payload contains 'InnerException'.");

            JsonNode? node;
            try { node = JsonNode.Parse(json); }
            catch (JsonException ex) { return ScenarioResult.Failed(Name, "Serialized response is not valid JSON.", ex); }
            if (node is null) return ScenarioResult.Failed(Name, "JsonNode.Parse returned null.");

            var obj = node.AsObject();

            if (!obj.ContainsKey("success")) return ScenarioResult.Failed(Name, "Wire shape missing 'success'.");
            if (!obj.ContainsKey("error"))   return ScenarioResult.Failed(Name, "Wire shape missing 'error'.");
            if (!obj.ContainsKey("meta"))    return ScenarioResult.Failed(Name, "Wire shape missing 'meta'.");
            if (obj.ContainsKey("status"))   return ScenarioResult.Failed(Name, "Wire shape must not contain 'status'.");

            if (obj["success"]!.GetValue<bool>() != false)
                return ScenarioResult.Failed(Name, "'success' is not false on the wire.");

            var errObj = obj["error"]!.AsObject();
            if (!errObj.ContainsKey("code"))    return ScenarioResult.Failed(Name, "'error.code' missing on the wire.");
            if (!errObj.ContainsKey("message")) return ScenarioResult.Failed(Name, "'error.message' missing on the wire.");
            if (!errObj.ContainsKey("fields"))  return ScenarioResult.Failed(Name, "'error.fields' missing on the wire.");

            if (errObj["code"]!.GetValue<string>() != "VALIDATION_ERROR")
                return ScenarioResult.Failed(Name, "'error.code' is not VALIDATION_ERROR on the wire.");
            if (errObj["message"]!.GetValue<string>() != message)
                return ScenarioResult.Failed(Name, "'error.message' did not round-trip on the wire.");

            var fieldsObj = errObj["fields"]!.AsObject();
            if (!fieldsObj.ContainsKey(fieldName))
                return ScenarioResult.Failed(Name, "'error.fields' does not contain the expected field key.");

            var messagesArr = fieldsObj[fieldName]!.AsArray();
            if (messagesArr.Count != 1)
                return ScenarioResult.Failed(Name, "'error.fields.email' is not a single-element array on the wire.");
            if (messagesArr[0]!.GetValue<string>() != fieldMsg)
                return ScenarioResult.Failed(Name, "'error.fields.email[0]' did not round-trip on the wire.");

            var metaObj = obj["meta"]!.AsObject();
            if (!metaObj.ContainsKey("requestId")) return ScenarioResult.Failed(Name, "'meta.requestId' missing on the wire.");
            if (metaObj["requestId"]!.GetValue<string>() != requestId)
                return ScenarioResult.Failed(Name, "'meta.requestId' did not round-trip on the wire.");

            return ScenarioResult.Passed(
                Name,
                "Error envelope constructed with code, message, and fields, serialized to the documented wire shape, no internal detail leaked.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }
}