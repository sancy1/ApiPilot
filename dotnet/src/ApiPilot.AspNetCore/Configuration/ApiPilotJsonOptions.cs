// filepath: dotnet/src/ApiPilot.AspNetCore/Configuration/ApiPilotJsonOptions.cs
// layer: Configuration | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Mutable JSON serialization conventions for ApiPilot responses
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (sealed class)
//   Depends on : EnumSerializationMode, DateSerializationMode, System.Text.Json
//   Used by    : JsonSerializerConfigurator, JsonSerializationExtensions
//   See also   : SPEC.md, EnumSerializationMode.cs, DateSerializationMode.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Configuration;

/// <summary>
/// Mutable configuration for JSON serialization of ApiPilot responses.
/// Intended for binding from application configuration and for programmatic
/// setup in Program.cs. Defaults match the wire contract documented in
/// SPEC.md: camelCase properties, ISO 8601 dates, string enums, and null
/// values written explicitly (data is never omitted).
/// </summary>
public sealed class ApiPilotJsonOptions
{
    /// <summary>
    /// How enum values are serialized. Defaults to string form for
    /// readability and stability. Set to Number for a strict numeric
    /// wire contract.
    /// </summary>
    public EnumSerializationMode EnumMode { get; set; } = EnumSerializationMode.AsString;

    /// <summary>
    /// How date and time values are serialized. Defaults to ISO 8601.
    /// </summary>
    public DateSerializationMode DateMode { get; set; } = DateSerializationMode.Iso8601;

    /// <summary>
    /// Whether JSON is written with indentation. Defaults to false for
    /// compact output. Enable during development for readable response bodies.
    /// </summary>
    public bool WriteIndented { get; set; }

    /// <summary>
    /// The naming policy for property names. Defaults to camelCase.
    /// Set to null to use PascalCase (the CLR default).
    /// </summary>
    public JsonNamingPolicy? PropertyNamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// The naming policy for dictionary keys. Defaults to camelCase.
    /// Set to null to use the key as written.
    /// </summary>
    public JsonNamingPolicy? DictionaryKeyPolicy { get; set; } = JsonNamingPolicy.CamelCase;

    /// <summary>
    /// When to ignore properties during serialization. Defaults to Never,
    /// which writes null values explicitly. This matches the ApiPilot
    /// contract that the data key is never omitted.
    /// </summary>
    public JsonIgnoreCondition DefaultIgnoreCondition { get; set; } = JsonIgnoreCondition.Never;

    /// <summary>
    /// Whether trailing commas are allowed in incoming JSON. Defaults to
    /// false for strict input parsing.
    /// </summary>
    public bool AllowTrailingCommas { get; set; }

    /// <summary>
    /// How JSON comments in incoming input are handled. Defaults to
    /// Disallow for strict input parsing.
    /// </summary>
    public JsonCommentHandling ReadCommentHandling { get; set; } = JsonCommentHandling.Disallow;

    /// <summary>
    /// The maximum depth allowed when reading or writing JSON. Defaults
    /// to 64, matching the System.Text.Json default. Set a positive
    /// integer to lower or raise the limit for deeply nested payloads.
    /// </summary>
    public int MaxDepth { get; set; } = 64;
}

