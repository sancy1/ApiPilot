// filepath: dotnet/tests/ApiPilot.Core.Tests/Errors/ErrorResponseSerializationTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.2.0-alpha.0
// purpose: Wire-shape tests for the serialized error envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Core.Errors, ApiPilot.Core.Metadata, System.Text.Json
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : SPEC.md (error envelope), ErrorResponse.cs, ApiError.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using System.Text.Json;

namespace ApiPilot.Core.Tests.Errors;

/// <summary>
/// Wire-shape tests for the serialized error envelope. Uses default
/// JsonSerializerOptions (PascalCase property names) so the tests focus
/// on shape, not naming. Naming is a separate concern tested through the
/// ApiPilot JSON configuration in the ASP.NET Core adapter package.
/// </summary>
[TestClass]
public sealed class ErrorResponseSerializationTests
{
    private static ErrorResponse BuildResponse()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        };
        var error = ApiError.Create(
            ApiErrorCode.ValidationError,
            "One or more values are invalid.",
            fields);
        var meta = new ResponseMetadata { RequestId = "req-serialize-001" };
        return new ErrorResponse(error, meta);
    }

    private static JsonElement Serialize(ErrorResponse response)
    {
        var json = JsonSerializer.Serialize(response);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    /// <summary>The top-level shape has Success, Error, and Meta.</summary>
    [Test]
    public void Serialize_ErrorEnvelope_HasExpectedTopLevelShape()
    {
        var root = Serialize(BuildResponse());
        TestAssert.True(root.TryGetProperty("Success", out var success));
        TestAssert.False(success.GetBoolean());
        TestAssert.True(root.TryGetProperty("Error", out _));
        TestAssert.True(root.TryGetProperty("Meta", out _));
    }

    /// <summary>The error code is a plain JSON string, not a nested object.</summary>
    [Test]
    public void Serialize_ErrorEnvelope_ErrorCodeIsPlainString()
    {
        var root = Serialize(BuildResponse());
        var code = root.GetProperty("Error").GetProperty("Code");
        TestAssert.Equal(JsonValueKind.String, code.ValueKind);
        TestAssert.Equal("VALIDATION_ERROR", code.GetString());
    }

    /// <summary>The Fields map is a dictionary of arrays, not nested objects.</summary>
    [Test]
    public void Serialize_ErrorEnvelope_FieldsIsDictionaryOfArrays()
    {
        var root = Serialize(BuildResponse());
        var fields = root.GetProperty("Error").GetProperty("Fields");
        TestAssert.Equal(JsonValueKind.Object, fields.ValueKind);

        var emailMessages = fields.GetProperty("email");
        TestAssert.Equal(JsonValueKind.Array, emailMessages.ValueKind);
    }

    /// <summary>The messages inside the array are strings.</summary>
    [Test]
    public void Serialize_ErrorEnvelope_FieldsMessagesAreStrings()
    {
        var root = Serialize(BuildResponse());
        var messages = root.GetProperty("Error").GetProperty("Fields").GetProperty("email");
        TestAssert.Equal(1, messages.GetArrayLength());
        TestAssert.Equal("Email is required.", messages[0].GetString());
    }
}

