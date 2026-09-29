// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/UnixSecondsDateTimeConverter.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: JSON converter that reads and writes DateTime as Unix epoch seconds
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : System.Text.Json.Serialization.JsonConverter<DateTime>
//   Depends on : System.Text.Json
//   Used by    : JsonSerializerConfigurator (when DateMode = UnixTimeSeconds)
//   See also   : DateSerializationMode.cs, UnixSecondsDateTimeOffsetConverter.cs
// -----------------------------------------------------------------------------

using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// Serializes <see cref="DateTime"/> as Unix epoch seconds (a JSON number).
/// Reading converts the number back to a UTC DateTime. Sub-second precision
/// is lost; use ISO 8601 (the default) when millisecond precision matters.
/// </summary>
public sealed class UnixSecondsDateTimeConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var seconds = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        DateTime value,
        JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
        var seconds = new DateTimeOffset(utc).ToUnixTimeSeconds();
        writer.WriteNumberValue(seconds);
    }
}

