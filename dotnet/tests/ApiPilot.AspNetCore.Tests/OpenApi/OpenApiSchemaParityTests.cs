// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/OpenApi/OpenApiSchemaParityTests.cs
// layer: OpenApi | package: ApiPilot.AspNetCore.Tests | since: v0.6.0
// purpose: Parity tests that prove the generated error schema matches the actual serialized error envelope (A-235)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, StandardErrorSchema, ErrorResponse, ApiError,
//                ResponseMetadata, System.Text.Json
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : StandardErrorSchema.cs, ErrorResponseSerializationTests.cs
// -----------------------------------------------------------------------------
//
// THE A-235 PROOF
//   These tests serialize a real ErrorResponse and assert that the
//   generated schema describes the same shape. The schema is USED, not
//   merely asserted to exist. The serialization uses camelCase options
//   so the actual keys match the wire naming the schema declares.

using ApiPilot.AspNetCore.OpenApi;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ApiPilot.AspNetCore.Tests.OpenApi;

/// <summary>
/// Parity tests between the generated error schema and the actual
/// serialized error envelope, serialized with camelCase naming to match
/// the ApiPilot wire contract.
/// </summary>
[TestClass]
public sealed class OpenApiSchemaParityTests
{
    private static readonly JsonSerializerOptions s_wireOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static JsonElement SerializeRealError()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        };
        var error = ApiError.Create(
            ApiErrorCode.ValidationError,
            "One or more values are invalid.",
            fields);
        var meta = new ResponseMetadata { RequestId = "req-parity-001" };
        var response = new ErrorResponse(error, meta);
        var json = JsonSerializer.Serialize(response, s_wireOptions);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private static HashSet<string> KeysOf(JsonElement element)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            keys.Add(property.Name);
        }
        return keys;
    }

    private static HashSet<string> PropertyNamesOf(JsonObject schema)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (schema["properties"] is JsonObject properties)
        {
            foreach (var kvp in properties)
            {
                names.Add(kvp.Key);
            }
        }
        return names;
    }

    /// <summary>The envelope schema declares exactly the serialized top-level keys.</summary>
    [Test]
    public void EnvelopeSchema_MatchesSerializedTopLevelKeys()
    {
        var actual = KeysOf(SerializeRealError());
        var schema = StandardErrorSchema.BuildEnvelopeSchema();
        var declared = PropertyNamesOf(schema);

        foreach (var key in actual)
        {
            TestAssert.True(declared.Contains(key), "schema missing declared key: " + key);
        }
        foreach (var key in declared)
        {
            TestAssert.True(actual.Contains(key), "schema declares a key not present on the wire: " + key);
        }
    }

    /// <summary>The error object schema declares exactly the serialized error keys.</summary>
    [Test]
    public void ErrorObjectSchema_MatchesSerializedErrorKeys()
    {
        var root = SerializeRealError();
        var errorElement = root.GetProperty("error");
        var actual = KeysOf(errorElement);
        var schema = StandardErrorSchema.BuildErrorObjectSchema();
        var declared = PropertyNamesOf(schema);

        foreach (var key in actual)
        {
            TestAssert.True(declared.Contains(key), "error schema missing declared key: " + key);
        }
        foreach (var key in declared)
        {
            TestAssert.True(actual.Contains(key), "error schema declares a key not present on the wire: " + key);
        }
    }

    /// <summary>The schema required set is a subset of the actual keys.</summary>
    [Test]
    public void EnvelopeSchema_RequiredIsSubsetOfActual()
    {
        var actual = KeysOf(SerializeRealError());
        var schema = StandardErrorSchema.BuildEnvelopeSchema();
        var required = schema["required"] as JsonArray;
        TestAssert.NotNull(required);
        TestAssert.True(required!.Count > 0, "expected a non-empty required set");
        foreach (var node in required)
        {
            var name = node!.GetValue<string>();
            TestAssert.True(actual.Contains(name), "required key not present on the wire: " + name);
        }
    }

    /// <summary>The serialized error code is a plain string, matching the schema type.</summary>
    [Test]
    public void ErrorCode_IsPlainString_AsSchemaDeclares()
    {
        var root = SerializeRealError();
        var code = root.GetProperty("error").GetProperty("code");
        TestAssert.Equal(JsonValueKind.String, code.ValueKind);
        var schema = StandardErrorSchema.BuildErrorObjectSchema();
        var codeSchema = (JsonObject)schema["properties"]!["code"]!;
        TestAssert.Equal("string", codeSchema["type"]!.GetValue<string>());
    }

    /// <summary>The fields value is an object of arrays, matching the field-map schema.</summary>
    [Test]
    public void Fields_AreDictionaryOfArrays_AsSchemaDeclares()
    {
        var root = SerializeRealError();
        var fields = root.GetProperty("error").GetProperty("fields");
        TestAssert.Equal(JsonValueKind.Object, fields.ValueKind);
        var first = fields.EnumerateObject().First().Value;
        TestAssert.Equal(JsonValueKind.Array, first.ValueKind);

        var schema = StandardErrorSchema.BuildFieldMapSchema();
        TestAssert.Equal("object", schema["type"]!.GetValue<string>());
        TestAssert.Equal("array", schema["additionalProperties"]!["type"]!.GetValue<string>());
    }
}

