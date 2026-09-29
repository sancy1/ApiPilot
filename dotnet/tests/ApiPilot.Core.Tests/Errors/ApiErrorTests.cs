// filepath: dotnet/tests/ApiPilot.Core.Tests/Errors/ApiErrorTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ApiError value object
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Errors
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiError.cs, ApiErrorField.cs, ApiErrorCode.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;

namespace ApiPilot.Core.Tests.Errors;

/// <summary>
/// Contract tests for the ApiError value object. Verifies factory behavior,
/// message validation and trimming, field handling including the empty-to-null
/// collapse, and duplicate detection.
/// </summary>
[TestClass]
public sealed class ApiErrorTests
{
    /// <summary>The simple overload produces an error with no fields.</summary>
    [Test]
    public void Create_SimpleError_HasNoFields()
    {
        var error = ApiError.Create(ApiErrorCode.ResourceNotFound, "Order was not found.");
        TestAssert.Equal("RESOURCE_NOT_FOUND", error.Code.Code);
        TestAssert.Equal("Order was not found.", error.Message);
        TestAssert.Null(error.Fields);
    }

    /// <summary>The three-argument overload populates the field dictionary.</summary>
    [Test]
    public void Create_WithFields_PopulatesFieldDictionary()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required."),
            ApiErrorField.WithMessage("password", "Password is too short.")
        };
        var error = ApiError.Create(ApiErrorCode.ValidationError, "One or more values are invalid.", fields);
        TestAssert.NotNull(error.Fields);
        TestAssert.Equal(2, error.Fields!.Count);
        TestAssert.True(error.Fields.ContainsKey("email"));
        TestAssert.Equal(1, error.Fields["email"].Count);
        TestAssert.Equal("Email is required.", error.Fields["email"][0]);
        TestAssert.True(error.Fields.ContainsKey("password"));
    }

    /// <summary>A null fields collection produces null Fields.</summary>
    [Test]
    public void Create_WithNullFields_ProducesNullFields()
    {
        var error = ApiError.Create(ApiErrorCode.Conflict, "Duplicate order.", (IEnumerable<ApiErrorField>?)null);
        TestAssert.Null(error.Fields);
    }

    /// <summary>An empty fields collection collapses to null Fields.</summary>
    [Test]
    public void Create_WithEmptyFields_CollapsesToNull()
    {
        var error = ApiError.Create(ApiErrorCode.ValidationError, "No field errors.", Array.Empty<ApiErrorField>());
        TestAssert.Null(error.Fields);
    }

    /// <summary>Create rejects a null code.</summary>
    [Test]
    public void Create_WithNullCode_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiError.Create(null!, "msg"));
    }

    /// <summary>Create rejects a null message.</summary>
    [Test]
    public void Create_WithNullMessage_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiError.Create(ApiErrorCode.InternalError, null!));
    }

    /// <summary>Create rejects an empty message.</summary>
    [Test]
    public void Create_WithEmptyMessage_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiError.Create(ApiErrorCode.InternalError, ""));
    }

    /// <summary>Create rejects a whitespace-only message.</summary>
    [Test]
    public void Create_WithWhitespaceMessage_Throws()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ApiError.Create(ApiErrorCode.InternalError, "   "));
    }

    /// <summary>Create trims the message.</summary>
    [Test]
    public void Create_TrimsMessage()
    {
        var error = ApiError.Create(ApiErrorCode.Conflict, "  Duplicate order.  ");
        TestAssert.Equal("Duplicate order.", error.Message);
    }

    /// <summary>Create rejects two field entries with the same name.</summary>
    [Test]
    public void Create_WithDuplicateFieldName_Throws()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "First message."),
            ApiErrorField.WithMessage("email", "Second message.")
        };
        TestAssert.Throws<ArgumentException>(() =>
            ApiError.Create(ApiErrorCode.ValidationError, "Duplicate fields.", fields));
    }

    /// <summary>Create rejects null field entries.</summary>
    [Test]
    public void Create_WithNullFieldEntry_Throws()
    {
        var fields = new ApiErrorField[]
        {
            ApiErrorField.WithMessage("email", "Valid entry."),
            null!
        };
        TestAssert.Throws<ArgumentException>(() =>
            ApiError.Create(ApiErrorCode.ValidationError, "Null field.", fields));
    }

    /// <summary>Two errors with the same values carry the same property values.</summary>
    [Test]
    public void Properties_MatchWhenConstructedWithSameValues()
    {
        var a = ApiError.Create(ApiErrorCode.Conflict, "Duplicate order.");
        var b = ApiError.Create(ApiErrorCode.Conflict, "Duplicate order.");
        TestAssert.Equal(a.Code.Code, b.Code.Code);
        TestAssert.Equal(a.Message, b.Message);
        TestAssert.Null(a.Fields);
        TestAssert.Null(b.Fields);
    }
}

