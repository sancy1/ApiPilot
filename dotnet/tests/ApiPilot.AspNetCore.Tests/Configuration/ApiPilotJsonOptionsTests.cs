// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Configuration/ApiPilotJsonOptionsTests.cs
// layer: Configuration | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the ApiPilotJsonOptions data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.Configuration
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotJsonOptions.cs, JsonSerializerConfiguratorTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiPilot.AspNetCore.Tests.Configuration;

/// <summary>
/// Contract tests for ApiPilotJsonOptions. Verifies defaults and mutability.
/// </summary>
[TestClass]
public sealed class ApiPilotJsonOptionsTests
{
    /// <summary>Default values match the documented defaults.</summary>
    [Test]
    public void Defaults_AreSensible()
    {
        var options = new ApiPilotJsonOptions();
        TestAssert.Equal(EnumSerializationMode.AsString, options.EnumMode);
        TestAssert.Equal(DateSerializationMode.Iso8601, options.DateMode);
        TestAssert.False(options.WriteIndented);
        TestAssert.True(ReferenceEquals(options.PropertyNamingPolicy, JsonNamingPolicy.CamelCase));
        TestAssert.True(ReferenceEquals(options.DictionaryKeyPolicy, JsonNamingPolicy.CamelCase));
        TestAssert.Equal(JsonIgnoreCondition.Never, options.DefaultIgnoreCondition);
        TestAssert.False(options.AllowTrailingCommas);
        TestAssert.Equal(JsonCommentHandling.Disallow, options.ReadCommentHandling);
        TestAssert.Equal(64, options.MaxDepth);
    }

    /// <summary>Properties can be updated after construction.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new ApiPilotJsonOptions();
        options.EnumMode = EnumSerializationMode.Number;
        options.DateMode = DateSerializationMode.UnixTimeSeconds;
        options.WriteIndented = true;
        options.PropertyNamingPolicy = null;
        options.DictionaryKeyPolicy = null;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.AllowTrailingCommas = true;
        options.ReadCommentHandling = JsonCommentHandling.Skip;
        options.MaxDepth = 32;

        TestAssert.Equal(EnumSerializationMode.Number, options.EnumMode);
        TestAssert.Equal(DateSerializationMode.UnixTimeSeconds, options.DateMode);
        TestAssert.True(options.WriteIndented);
        TestAssert.Null(options.PropertyNamingPolicy);
        TestAssert.Null(options.DictionaryKeyPolicy);
        TestAssert.Equal(JsonIgnoreCondition.WhenWritingNull, options.DefaultIgnoreCondition);
        TestAssert.True(options.AllowTrailingCommas);
        TestAssert.Equal(JsonCommentHandling.Skip, options.ReadCommentHandling);
        TestAssert.Equal(32, options.MaxDepth);
    }

    /// <summary>Defaults can be overridden via object initializer.</summary>
    [Test]
    public void Defaults_CanBeOverridden()
    {
        var options = new ApiPilotJsonOptions
        {
            WriteIndented = true,
            MaxDepth = 128
        };
        TestAssert.True(options.WriteIndented);
        TestAssert.Equal(128, options.MaxDepth);
    }

    /// <summary>Naming policies can be set to null (PascalCase).</summary>
    [Test]
    public void NamingPolicies_CanBeNull()
    {
        var options = new ApiPilotJsonOptions
        {
            PropertyNamingPolicy = null,
            DictionaryKeyPolicy = null
        };
        TestAssert.Null(options.PropertyNamingPolicy);
        TestAssert.Null(options.DictionaryKeyPolicy);
    }
}

