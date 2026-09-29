// filepath: dotnet/src/ApiPilot.Core/Errors/ApiErrorCodeJsonConverter.cs
// layer: Errors | package: ApiPilot.Core | since: v0.2.0-alpha.0
// purpose: Serializes ApiErrorCode as a plain string matching the wire contract
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.Text.Json.Serialization.JsonConverter<ApiErrorCode>
//   Depends on : ApiErrorCode, System.Text.Json
//   Used by    : applied via [JsonConverter] on ApiErrorCode
//   See also   : ApiErrorCode.cs, SPEC.md (error code table)
// -----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.Core.Errors;

/// <summary>
/// Reads and writes <see cref="ApiErrorCode"/> as a plain JSON string. Without
/// this converter the code would serialize as a nested object with a Code
/// property, which does not match the ApiPilot wire contract in SPEC.md.
/// </summary>
public sealed class ApiErrorCodeJsonConverter : JsonConverter<ApiErrorCode>
{
    /// <inheritdoc />
    public override ApiErrorCode Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            throw new JsonException("ApiErrorCode must be a non-empty string.");
        }
        return ApiErrorCode.From(value);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        ApiErrorCode value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Code);
    }
}

