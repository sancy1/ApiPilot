// filepath: dotnet/tests/ApiPilot.Core.Tests/Validation/ValidationResponseFactoryTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ValidationResponseFactory
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test double)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Core.Errors, ApiPilot.Core.Metadata, ApiPilot.Core.Validation
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ValidationResponseFactory.cs, ValidationErrorCollectionTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Validation;

namespace ApiPilot.Core.Tests.Validation;

/// <summary>
/// Contract tests for ValidationResponseFactory. Verifies the produced
/// envelope shape, default and custom message handling, null and empty
/// argument rejection, and the source overload.
/// </summary>
[TestClass]
public sealed class ValidationResponseFactoryTests
{
    private static ResponseMetadata Meta()
    {
        return new ResponseMetadata { RequestId = "req-val-001" };
    }

    private static ApiErrorField[] OneField()
    {
        return new[] { ApiErrorField.WithMessage("email", "Email is required.") };
    }

    /// <summary>FromFields produces a non-null ErrorResponse.</summary>
    [Test]
    public void FromFields_ProducesErrorResponse()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.NotNull(response);
    }

    /// <summary>FromFields assigns the VALIDATION_ERROR code.</summary>
    [Test]
    public void FromFields_SetsValidationErrorCode()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.Equal("VALIDATION_ERROR", response.Error.Code.Code);
    }

    /// <summary>FromFields preserves the field errors.</summary>
    [Test]
    public void FromFields_PreservesFieldErrors()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.NotNull(response.Error.Fields);
        TestAssert.Equal(1, response.Error.Fields!.Count);
        TestAssert.True(response.Error.Fields.ContainsKey("email"));
        TestAssert.Equal(1, response.Error.Fields["email"].Count);
        TestAssert.Equal("Email is required.", response.Error.Fields["email"][0]);
    }

    /// <summary>FromFields uses the default message when none is supplied.</summary>
    [Test]
    public void FromFields_UsesDefaultMessage()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.Equal(ValidationResponseFactory.DefaultMessage, response.Error.Message);
    }

    /// <summary>FromFields uses a custom message when supplied.</summary>
    [Test]
    public void FromFields_UsesCustomMessage()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta(), "Custom summary.");
        TestAssert.Equal("Custom summary.", response.Error.Message);
    }

    /// <summary>FromFields trims a custom message.</summary>
    [Test]
    public void FromFields_TrimsCustomMessage()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta(), "  Trimmed.  ");
        TestAssert.Equal("Trimmed.", response.Error.Message);
    }

    /// <summary>A whitespace-only message falls back to the default.</summary>
    [Test]
    public void FromFields_WhitespaceMessage_UsesDefault()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta(), "   ");
        TestAssert.Equal(ValidationResponseFactory.DefaultMessage, response.Error.Message);
    }

    /// <summary>FromFields preserves the metadata.</summary>
    [Test]
    public void FromFields_PreservesMetadata()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.Equal("req-val-001", response.Meta.RequestId);
    }

    /// <summary>FromFields produces a response with Success false.</summary>
    [Test]
    public void FromFields_SuccessIsFalse()
    {
        var response = ValidationResponseFactory.FromFields(OneField(), Meta());
        TestAssert.False(response.Success);
    }

    /// <summary>FromFields rejects an empty field set.</summary>
    [Test]
    public void FromFields_RejectsEmptyFields()
    {
        TestAssert.Throws<ArgumentException>(() =>
            ValidationResponseFactory.FromFields(Array.Empty<ApiErrorField>(), Meta()));
    }

    /// <summary>FromFields rejects a null field set.</summary>
    [Test]
    public void FromFields_RejectsNullFields()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ValidationResponseFactory.FromFields(null!, Meta()));
    }

    /// <summary>FromFields rejects null metadata.</summary>
    [Test]
    public void FromFields_RejectsNullMeta()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ValidationResponseFactory.FromFields(OneField(), null!));
    }

    /// <summary>FromFields rejects a null entry inside the field collection.</summary>
    [Test]
    public void FromFields_RejectsNullEntry()
    {
        var fields = new ApiErrorField[]
        {
            ApiErrorField.WithMessage("email", "ok"),
            null!
        };
        TestAssert.Throws<ArgumentException>(() =>
            ValidationResponseFactory.FromFields(fields, Meta()));
    }

    /// <summary>FromSource delegates to FromFields with the source output.</summary>
    [Test]
    public void FromSource_DelegatesToFromFields()
    {
        var source = new FixedSource(OneField());
        var response = ValidationResponseFactory.FromSource(source, Meta());
        TestAssert.Equal("VALIDATION_ERROR", response.Error.Code.Code);
        TestAssert.NotNull(response.Error.Fields);
        TestAssert.Equal(1, response.Error.Fields!.Count);
    }

    /// <summary>FromSource rejects a null source.</summary>
    [Test]
    public void FromSource_RejectsNullSource()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ValidationResponseFactory.FromSource(null!, Meta()));
    }

    /// <summary>FromSource throws when the source produces no errors.</summary>
    [Test]
    public void FromSource_WithEmptySourceResult_Throws()
    {
        var source = new FixedSource(Array.Empty<ApiErrorField>());
        TestAssert.Throws<ArgumentException>(() =>
            ValidationResponseFactory.FromSource(source, Meta()));
    }
}

/// <summary>
/// A test double for IValidationErrorSource that always returns a fixed
/// set of field errors. Used to test the FromSource overload in isolation.
/// </summary>
internal sealed class FixedSource : IValidationErrorSource
{
    private readonly IReadOnlyList<ApiErrorField> _errors;

    public FixedSource(IReadOnlyList<ApiErrorField> errors)
    {
        _errors = errors;
    }

    public IReadOnlyList<ApiErrorField> GetErrors() => _errors;
}

