// filepath: dotnet/tests/ApiPilot.Core.Tests/Errors/ApiErrorFieldTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ApiErrorField value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Errors
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiErrorField.cs, ApiError.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.Core.Tests.Errors;

/// <summary>
/// Contract tests for the ApiErrorField value object. Verifies construction,
/// validation, defensive copying, and property-level correctness.
/// </summary>
[TestClass]
public sealed class ApiErrorFieldTests
{
    /// <summary>Create preserves a single message.</summary>
    [Test]
    public void Create_WithSingleMessage_PreservesFieldAndMessage()
    {
        var field = ApiErrorField.Create("email", new[] { "Email is required." });
        TestAssert.Equal("email", field.Field);
        TestAssert.Equal(1, field.Messages.Count);
        TestAssert.Equal("Email is required.", field.Messages[0]);
    }

    /// <summary>Create preserves multiple messages in order.</summary>
    [Test]
    public void Create_WithMultipleMessages_PreservesOrder()
    {
        var field = ApiErrorField.Create("password", new[] { "Too short.", "Must contain a digit." });
        TestAssert.Equal(2, field.Messages.Count);
        TestAssert.Equal("Too short.", field.Messages[0]);
        TestAssert.Equal("Must contain a digit.", field.Messages[1]);
    }

    /// <summary>Create trims the field name.</summary>
    [Test]
    public void Create_TrimsFieldName()
    {
        var field = ApiErrorField.Create("  email  ", new[] { "msg" });
        TestAssert.Equal("email", field.Field);
    }

    /// <summary>Create rejects a null field name.</summary>
    [Test]
    public void Create_WithNullField_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiErrorField.Create(null!, new[] { "msg" }));
    }

    /// <summary>Create rejects a null messages collection.</summary>
    [Test]
    public void Create_WithNullMessages_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiErrorField.Create("email", null!));
    }

    /// <summary>Create rejects an empty field name.</summary>
    [Test]
    public void Create_WithEmptyField_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiErrorField.Create("", new[] { "msg" }));
    }

    /// <summary>Create rejects a whitespace-only field name.</summary>
    [Test]
    public void Create_WithWhitespaceField_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiErrorField.Create("   ", new[] { "msg" }));
    }

    /// <summary>Create rejects an empty messages collection.</summary>
    [Test]
    public void Create_WithEmptyMessages_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiErrorField.Create("email", Array.Empty<string>()));
    }

    /// <summary>Create rejects a null message entry.</summary>
    [Test]
    public void Create_WithNullMessageEntry_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiErrorField.Create("email", new string[] { "valid", null! }));
    }

    /// <summary>Single produces a one-message field.</summary>
    [Test]
    public void WithMessage_ProducesOneMessageField()
    {
        var field = ApiErrorField.WithMessage("email", "Email is required.");
        TestAssert.Equal("email", field.Field);
        TestAssert.Equal(1, field.Messages.Count);
        TestAssert.Equal("Email is required.", field.Messages[0]);
    }

    /// <summary>Single rejects a null message.</summary>
    [Test]
    public void WithMessage_WithNullMessage_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiErrorField.WithMessage("email", null!));
    }

    /// <summary>Mutating the source collection after Create does not affect the field.</summary>
    [Test]
    public void Create_TakesDefensiveCopyOfMessages()
    {
        var source = new List<string> { "first" };
        var field = ApiErrorField.Create("email", source);
        source.Add("second");
        source[0] = "modified";
        TestAssert.Equal(1, field.Messages.Count);
        TestAssert.Equal("first", field.Messages[0]);
    }

    /// <summary>Two fields with the same values carry the same property values.</summary>
    [Test]
    public void TwoFields_WithSameValues_CarrySameProperties()
    {
        var a = ApiErrorField.WithMessage("email", "Email is required.");
        var b = ApiErrorField.WithMessage("email", "Email is required.");
        TestAssert.Equal(a.Field, b.Field);
        TestAssert.Equal(a.Messages.Count, b.Messages.Count);
        TestAssert.Equal(a.Messages[0], b.Messages[0]);
    }
}

