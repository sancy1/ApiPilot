// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/UnixSecondsDateTimeOffsetConverter.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: JSON converter that reads and writes DateTimeOffset as Unix epoch seconds
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.Text.Json.Serialization.JsonConverter<DateTimeOffset>
//   Depends on : System.Text.Json
//   Used by    : JsonSerializerConfigurator (when DateMode = UnixTimeSeconds)
//   See also   : DateSerializationMode.cs, UnixSecondsDateTimeConverter.cs
// -----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// Serializes <see cref="DateTimeOffset"/> as Unix epoch seconds (a JSON
/// number). Reading converts the number back to a UTC DateTimeOffset.
/// Sub-second precision is lost; use ISO 8601 (the default) when
/// millisecond precision matters.
/// </summary>
public sealed class UnixSecondsDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    /// <inheritdoc />
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var seconds = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(seconds);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.ToUnixTimeSeconds());
    }
}

