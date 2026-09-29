// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Serialization/JsonSerializerConfiguratorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for JsonSerializerConfigurator behavior
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + helper types)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.Serialization,
//                System.Text.Json
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : JsonSerializerConfigurator.cs, ApiPilotJsonOptionsTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Serialization;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Tests.Serialization;

/// <summary>
/// Contract tests for JsonSerializerConfigurator. Verifies that each
/// ApiPilotJsonOptions setting is correctly applied to a JsonSerializerOptions
/// instance, and that the configurator is idempotent.
/// </summary>
[TestClass]
public sealed class JsonSerializerConfiguratorTests
{
    private static JsonSerializerOptions ConfigureWith(Action<ApiPilotJsonOptions>? setup = null)
    {
        var options = new ApiPilotJsonOptions();
        setup?.Invoke(options);
        var target = new JsonSerializerOptions();
        JsonSerializerConfigurator.Configure(target, options);
        return target;
    }

    /// <summary>Null target is rejected.</summary>
    [Test]
    public void Configure_RejectsNullTarget()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            JsonSerializerConfigurator.Configure(null!, new ApiPilotJsonOptions()));
    }

    /// <summary>Null options is rejected.</summary>
    [Test]
    public void Configure_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            JsonSerializerConfigurator.Configure(new JsonSerializerOptions(), null!));
    }

    /// <summary>WriteIndented flag is transferred.</summary>
    [Test]
    public void Configure_AppliesWriteIndented()
    {
        var target = ConfigureWith(o => o.WriteIndented = true);
        TestAssert.True(target.WriteIndented);
    }

    /// <summary>Default camelCase policy produces camelCase property names.</summary>
    [Test]
    public void Configure_CamelCaseProperties_SerializesCamelCase()
    {
        var target = ConfigureWith();
        var json = JsonSerializer.Serialize(new SampleDto { FirstName = "Ada" }, target);
        TestAssert.Contains("\"firstName\"", json);
    }

    /// <summary>Null naming policy preserves CLR property names.</summary>
    [Test]
    public void Configure_NullNamingPolicy_SerializesPascalCase()
    {
        var target = ConfigureWith(o => o.PropertyNamingPolicy = null);
        var json = JsonSerializer.Serialize(new SampleDto { FirstName = "Ada" }, target);
        TestAssert.Contains("\"FirstName\"", json);
    }

    /// <summary>Default ignore condition Never writes null values.</summary>
    [Test]
    public void Configure_NeverIgnoreCondition_WritesNullValues()
    {
        var target = ConfigureWith();
        var json = JsonSerializer.Serialize(new SampleDto { FirstName = null }, target);
        TestAssert.Contains("\"firstName\":null", json);
    }

    /// <summary>WhenWritingNull ignores null values.</summary>
    [Test]
    public void Configure_WhenWritingNullIgnoreCondition_OmitsNulls()
    {
        var target = ConfigureWith(o => o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);
        var json = JsonSerializer.Serialize(new SampleDto { FirstName = null }, target);
        TestAssert.False(json.Contains("\"firstName\""));
    }

    /// <summary>String enum mode serializes enums as string names.</summary>
    [Test]
    public void Configure_StringEnumMode_SerializesEnumAsString()
    {
        var target = ConfigureWith();
        var json = JsonSerializer.Serialize(SampleStatus.Active, target);
        TestAssert.Equal("\"Active\"", json);
    }

    /// <summary>Number enum mode serializes enums as their ordinal.</summary>
    [Test]
    public void Configure_NumberEnumMode_SerializesEnumAsNumber()
    {
        var target = ConfigureWith(o => o.EnumMode = EnumSerializationMode.Number);
        var json = JsonSerializer.Serialize(SampleStatus.Inactive, target);
        TestAssert.Equal("1", json);
    }

    /// <summary>UnixTimeSeconds date mode serializes dates as integers.</summary>
    [Test]
    public void Configure_UnixTimeSecondsDateMode_SerializesDateAsNumber()
    {
        var target = ConfigureWith(o => o.DateMode = DateSerializationMode.UnixTimeSeconds);
        var dto = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
        var json = JsonSerializer.Serialize(dto, target);
        var expected = dto.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        TestAssert.Equal(expected, json);
    }

    /// <summary>The configurator is idempotent for converter registration.</summary>
    [Test]
    public void Configure_CalledTwice_DoesNotDuplicateConverters()
    {
        var options = new ApiPilotJsonOptions();
        var target = new JsonSerializerOptions();
        JsonSerializerConfigurator.Configure(target, options);
        var countAfterFirst = target.Converters.Count;
        JsonSerializerConfigurator.Configure(target, options);
        TestAssert.Equal(countAfterFirst, target.Converters.Count);
    }
}

/// <summary>Enum used for enum serialization tests.</summary>
public enum SampleStatus
{
    /// <summary>Active status (ordinal 0).</summary>
    Active,

    /// <summary>Inactive status (ordinal 1).</summary>
    Inactive,
}

/// <summary>A minimal DTO for naming and null handling tests.</summary>
public sealed class SampleDto
{
    /// <summary>A nullable string property.</summary>
    public string? FirstName { get; set; }
}

