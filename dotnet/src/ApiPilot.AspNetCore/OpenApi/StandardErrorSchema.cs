// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/StandardErrorSchema.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: The canonical error-response schema derived from SPEC.md, expressed as repository-owned JSON nodes
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : System.Text.Json.Nodes
//   Used by    : ApiPilotSchemaEmitter (components.schemas)
//   See also   : SPEC.md (error envelope), docs/error-contract.md, ErrorResponse.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This schema describes the ApiPilot error envelope exactly as it is
//   serialized today. The shape is fixed by SPEC.md and is not
//   overridable. The schema is built as a System.Text.Json JsonObject,
//   not through a third-party OpenAPI model. No Microsoft.OpenApi,
//   Swashbuckle, NSwag, or Swagger type is referenced.

using System.Text.Json.Nodes;

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// The canonical error-response schema for the ApiPilot wire contract,
/// derived from SPEC.md. Exposes the error envelope schema and its
/// nested error-object schema as JsonObject values that the emitter
/// composes into components.schemas.
/// </summary>
/// <remarks>
/// The envelope is an object with success (always false), error, and
/// meta. The nested error object carries code, message, and an optional
/// fields object mapping field names to arrays of messages. Property
/// names are camelCase to match the wire naming applied by the ApiPilot
/// JSON configuration.
/// </remarks>
public static class StandardErrorSchema
{
    /// <summary>
    /// The name under which the error envelope schema is published in
    /// components.schemas.
    /// </summary>
    public const string EnvelopeSchemaName = "ApiPilotErrorResponse";

    /// <summary>
    /// The name under which the nested error object schema is published
    /// in components.schemas.
    /// </summary>
    public const string ErrorObjectSchemaName = "ApiPilotError";

    /// <summary>
    /// The name under which the field map schema is published in
    /// components.schemas.
    /// </summary>
    public const string FieldMapSchemaName = "ApiPilotErrorFields";

    /// <summary>
    /// Builds the error envelope schema: success (always false), error,
    /// and meta. Only success and error are required.
    /// </summary>
    /// <returns>A fresh schema object the caller owns.</returns>
    public static JsonObject BuildEnvelopeSchema()
    {
        var success = new JsonObject();
        success["type"] = "boolean";
        success["description"] = "Always false for an error response.";
        success["enum"] = new JsonArray { false };

        var errorRef = new JsonObject();
        errorRef["$ref"] = "#/components/schemas/" + ErrorObjectSchemaName;

        var requestId = new JsonObject();
        requestId["type"] = "string";
        requestId["description"] = "The correlation identifier for the request.";

        var metaProperties = new JsonObject();
        metaProperties["requestId"] = requestId;

        var meta = new JsonObject();
        meta["type"] = "object";
        meta["description"] = "Non-business metadata.";
        meta["properties"] = metaProperties;

        var properties = new JsonObject();
        properties["success"] = success;
        properties["error"] = errorRef;
        properties["meta"] = meta;

        var schema = new JsonObject();
        schema["type"] = "object";
        schema["properties"] = properties;
        schema["required"] = new JsonArray { "success", "error" };
        return schema;
    }

    /// <summary>
    /// Builds the nested error object schema: code, message, and an
    /// optional fields map. Code and message are required.
    /// </summary>
    /// <returns>A fresh schema object the caller owns.</returns>
    public static JsonObject BuildErrorObjectSchema()
    {
        var code = new JsonObject();
        code["type"] = "string";
        code["description"] = "The stable, machine-readable error code.";

        var message = new JsonObject();
        message["type"] = "string";
        message["description"] = "A short, safe, human-readable message.";

        var fieldsRef = new JsonObject();
        fieldsRef["$ref"] = "#/components/schemas/" + FieldMapSchemaName;

        var properties = new JsonObject();
        properties["code"] = code;
        properties["message"] = message;
        properties["fields"] = fieldsRef;

        var schema = new JsonObject();
        schema["type"] = "object";
        schema["properties"] = properties;
        schema["required"] = new JsonArray { "code", "message" };
        return schema;
    }

    /// <summary>
    /// Builds the field map schema: an object whose values are arrays of
    /// message strings. Present only for validation failures.
    /// </summary>
    /// <returns>A fresh schema object the caller owns.</returns>
    public static JsonObject BuildFieldMapSchema()
    {
        var items = new JsonObject();
        items["type"] = "string";

        var additional = new JsonObject();
        additional["type"] = "array";
        additional["items"] = items;

        var schema = new JsonObject();
        schema["type"] = "object";
        schema["description"] = "Field-level errors, keyed by field name.";
        schema["additionalProperties"] = additional;
        return schema;
    }
}

