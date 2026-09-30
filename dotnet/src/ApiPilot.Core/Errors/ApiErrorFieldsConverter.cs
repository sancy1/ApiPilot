// filepath: dotnet/src/ApiPilot.Core/Errors/ApiErrorFieldsConverter.cs
// layer: Errors | package: ApiPilot.Core | since: v1.0.4
// purpose: JsonConverter for ApiError.Fields that preserves dictionary key casing verbatim
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.Text.Json.Serialization.JsonConverter<T>
//   Depends on : System.Text.Json
//   Used by    : ApiError.Fields (attribute [JsonConverter])
//   See also   : ApiError.cs, SPEC.md (error envelope, fields object)
// -----------------------------------------------------------------------------
//
// F-59 fix, write path. ApiError.Fields is a dictionary, so System.Text.Json
// applies the serializer DictionaryKeyPolicy to its keys. ApiPilot default
// JSON options set DictionaryKeyPolicy = CamelCase, so a field key the
// application deliberately produced with KeyTransform = key => key is
// camelCased on the wire even though the in-memory key is preserved. This
// converter writes each key exactly as held in the dictionary, and reads
// each key exactly as parsed, so the field key casing the application chose
// survives to the wire.
//
// The converter is scoped to ApiError.Fields only. The global
// DictionaryKeyPolicy is unchanged, so every other dictionary in an ApiPilot
// response (meta.extra, application dictionaries) continues to be camelCased
// by the JSON options.
//
// The converter supports both directions. Deserialization validates the
// shape: a JSON object whose property values are arrays of strings. Any
// other shape throws System.Text.Json.JsonException. A JSON null yields a
// null dictionary.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.Core.Errors;

/// <summary>
/// Serializes and deserializes the <see cref="ApiError.Fields"/> dictionary
/// with keys written and read verbatim, so the field key casing chosen by the
/// application is not altered by the serializer DictionaryKeyPolicy.
/// </summary>
/// <remarks>
/// Write: each key is written exactly as held; each value is a JSON array of
/// message strings.
/// Read: the token must be a JSON object whose values are arrays of strings;
/// any other shape throws <see cref="JsonException"/>. A JSON null yields a
/// null dictionary. Keys are preserved exactly.
/// </remarks>
public sealed class ApiErrorFieldsConverter
    : JsonConverter<IReadOnlyDictionary<string, IReadOnlyList<string>>>
{
    /// <inheritdoc />
    public override IReadOnlyDictionary<string, IReadOnlyList<string>>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("ApiError.Fields must be a JSON object of field name to array of strings.");
        }

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("ApiError.Fields must contain property names.");
            }

            var key = reader.GetString()
                ?? throw new JsonException("ApiError.Fields keys must not be null.");

            if (!reader.Read())
            {
                throw new JsonException("ApiError.Fields value was truncated.");
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException(
                    "ApiError.Fields value for key " + key + " must be a JSON array of strings.");
            }

            var messages = new List<string>();
            var arrayEnded = false;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    arrayEnded = true;
                    break;
                }

                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException(
                        "ApiError.Fields message entries for key " + key + " must be strings.");
                }

                messages.Add(reader.GetString()!);
            }

            if (!arrayEnded)
            {
                throw new JsonException(
                    "ApiError.Fields message array for key " + key + " was not terminated.");
            }

            if (!result.TryAdd(key, messages))
            {
                throw new JsonException("Duplicate field key in ApiError.Fields: " + key + ".");
            }
        }

        throw new JsonException("ApiError.Fields object was not terminated.");
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        IReadOnlyDictionary<string, IReadOnlyList<string>> value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        foreach (var pair in value)
        {
            // Key written exactly as held; the serializer DictionaryKeyPolicy
            // is deliberately not applied to this property.
            writer.WritePropertyName(pair.Key);
            writer.WriteStartArray();
            if (pair.Value is not null)
            {
                foreach (var message in pair.Value)
                {
                    writer.WriteStringValue(message);
                }
            }
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }
}

