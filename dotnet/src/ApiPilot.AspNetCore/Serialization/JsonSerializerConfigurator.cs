// filepath: dotnet/src/ApiPilot.AspNetCore/Serialization/JsonSerializerConfigurator.cs
// layer: Serialization | package: ApiPilot.AspNetCore | since: v0.2.0-alpha.0
// purpose: Applies ApiPilot JSON conventions to a JsonSerializerOptions instance
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilotJsonOptions, EnumSerializationMode, DateSerializationMode,
//                UnixSecondsDateTimeConverter, UnixSecondsDateTimeOffsetConverter,
//                System.Text.Json
//   Used by    : JsonSerializationExtensions, application setup code
//   See also   : ApiPilotJsonOptions.cs, SPEC.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Serialization;

/// <summary>
/// Applies ApiPilot JSON conventions from an <see cref="ApiPilotJsonOptions"/>
/// instance to a <see cref="JsonSerializerOptions"/> instance. The method is
/// idempotent with respect to converter registration: calling it more than
/// once with the same options does not add duplicate converters.
/// </summary>
public static class JsonSerializerConfigurator
{
    /// <summary>
    /// Applies the given options to the target serializer options. The target
    /// is mutated in place. Converters are registered only if an instance of
    /// the same converter type is not already present.
    /// </summary>
    /// <param name="target">The serializer options to configure. Must not be null.</param>
    /// <param name="options">The ApiPilot options to apply. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="target"/> or <paramref name="options"/> is null.
    /// </exception>
    public static void Configure(JsonSerializerOptions target, ApiPilotJsonOptions options)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(options);

        target.WriteIndented = options.WriteIndented;
        target.PropertyNamingPolicy = options.PropertyNamingPolicy;
        target.DictionaryKeyPolicy = options.DictionaryKeyPolicy;
        target.DefaultIgnoreCondition = options.DefaultIgnoreCondition;
        target.AllowTrailingCommas = options.AllowTrailingCommas;
        target.ReadCommentHandling = options.ReadCommentHandling;
        target.MaxDepth = options.MaxDepth;

        // Date converters must be registered before the serializer is used.
        if (options.DateMode == DateSerializationMode.UnixTimeSeconds)
        {
            AddConverterIfMissing<UnixSecondsDateTimeConverter>(target);
            AddConverterIfMissing<UnixSecondsDateTimeOffsetConverter>(target);
        }

        // Enum string converter is registered only for the String mode.
        if (options.EnumMode == EnumSerializationMode.AsString)
        {
            AddConverterIfMissing<JsonStringEnumConverter>(target);
        }
    }

    private static void AddConverterIfMissing<TConverter>(JsonSerializerOptions target)
        where TConverter : JsonConverter, new()
    {
        foreach (var existing in target.Converters)
        {
            if (existing is TConverter)
            {
                return;
            }
        }
        target.Converters.Add(new TConverter());
    }
}

