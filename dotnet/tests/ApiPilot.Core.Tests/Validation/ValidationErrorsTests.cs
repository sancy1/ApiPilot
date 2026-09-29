// filepath: dotnet/tests/ApiPilot.Core.Tests/Validation/ValidationErrorsTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ValidationErrors type
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Core.Errors, ApiPilot.Core.Validation
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ValidationErrors.cs, ApiErrorFieldTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Errors;
using ApiPilot.Core.Validation;

namespace ApiPilot.Core.Tests.Validation;

/// <summary>
/// Contract tests for ValidationErrors. Verifies construction,
/// deduplication, sorting, message merging, and immutability-friendly
/// operations.
/// </summary>
[TestClass]
public sealed class ValidationErrorsTests
{
    /// <summary>The Empty singleton is empty.</summary>
    [Test]
    public void Empty_IsEmpty()
    {
        TestAssert.True(ValidationErrors.Empty.IsEmpty);
        TestAssert.Equal(0, ValidationErrors.Empty.Count);
        TestAssert.Equal(0, ValidationErrors.Empty.Fields.Count);
    }

    /// <summary>From an empty sequence returns the Empty singleton.</summary>
    [Test]
    public void From_EmptySequence_ReturnsEmptySingleton()
    {
        var collection = ValidationErrors.From(Array.Empty<ApiErrorField>());
        TestAssert.True(ReferenceEquals(collection, ValidationErrors.Empty));
    }

    /// <summary>From a single field produces one field.</summary>
    [Test]
    public void From_SingleField_ReturnsOneField()
    {
        var field = ApiErrorField.WithMessage("email", "Email is required.");
        var collection = ValidationErrors.From(new[] { field });
        TestAssert.Equal(1, collection.Count);
        TestAssert.Equal("email", collection.Fields[0].Field);
    }

    /// <summary>From rejects a null sequence.</summary>
    [Test]
    public void From_RejectsNullSequence()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ValidationErrors.From(null!));
    }

    /// <summary>From rejects a null entry.</summary>
    [Test]
    public void From_RejectsNullEntry()
    {
        var fields = new ApiErrorField[]
        {
            ApiErrorField.WithMessage("email", "ok"),
            null!
        };
        TestAssert.Throws<ArgumentException>(() =>
            ValidationErrors.From(fields));
    }

    /// <summary>From sorts field names ordinally.</summary>
    [Test]
    public void From_SortsFieldsOrdinally()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("zebra", "msg z"),
            ApiErrorField.WithMessage("alpha", "msg a"),
            ApiErrorField.WithMessage("middle", "msg m")
        };
        var collection = ValidationErrors.From(fields);
        TestAssert.Equal(3, collection.Count);
        TestAssert.Equal("alpha", collection.Fields[0].Field);
        TestAssert.Equal("middle", collection.Fields[1].Field);
        TestAssert.Equal("zebra", collection.Fields[2].Field);
    }

    /// <summary>From merges fields with the same name and identical messages.</summary>
    [Test]
    public void From_DeduplicatesIdenticalFields()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required."),
            ApiErrorField.WithMessage("email", "Email is required.")
        };
        var collection = ValidationErrors.From(fields);
        TestAssert.Equal(1, collection.Count);
        TestAssert.Equal(1, collection.Fields[0].Messages.Count);
    }

    /// <summary>From merges fields with the same name and different messages.</summary>
    [Test]
    public void From_MergesFieldsWithSameNameAndDifferentMessages()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required."),
            ApiErrorField.WithMessage("email", "Email must be valid.")
        };
        var collection = ValidationErrors.From(fields);
        TestAssert.Equal(1, collection.Count);
        TestAssert.Equal(2, collection.Fields[0].Messages.Count);
    }

    /// <summary>Duplicate messages within a merged field appear once.</summary>
    [Test]
    public void From_DeduplicatesMessagesWithinMergedField()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required."),
            ApiErrorField.WithMessage("email", "Email is required."),
            ApiErrorField.WithMessage("email", "Email must be valid.")
        };
        var collection = ValidationErrors.From(fields);
        TestAssert.Equal(1, collection.Count);
        TestAssert.Equal(2, collection.Fields[0].Messages.Count);
    }

    /// <summary>Merged messages preserve first-occurrence order.</summary>
    [Test]
    public void From_PreservesFirstOccurrenceOrderOfMessages()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "first"),
            ApiErrorField.WithMessage("email", "second"),
            ApiErrorField.WithMessage("email", "first")
        };
        var collection = ValidationErrors.From(fields);
        TestAssert.Equal(2, collection.Fields[0].Messages.Count);
        TestAssert.Equal("first", collection.Fields[0].Messages[0]);
        TestAssert.Equal("second", collection.Fields[0].Messages[1]);
    }

    /// <summary>With adds a field to the collection.</summary>
    [Test]
    public void With_AddsFieldToCollection()
    {
        var initial = ValidationErrors.From(new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        });
        var expanded = initial.With(ApiErrorField.WithMessage("password", "Too short."));
        TestAssert.Equal(2, expanded.Count);
        TestAssert.Equal("email", expanded.Fields[0].Field);
        TestAssert.Equal("password", expanded.Fields[1].Field);
    }

    /// <summary>With does not mutate the original collection.</summary>
    [Test]
    public void With_OriginalCollectionUnchanged()
    {
        var initial = ValidationErrors.From(new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        });
        var expanded = initial.With(ApiErrorField.WithMessage("password", "Too short."));
        TestAssert.Equal(1, initial.Count);
        TestAssert.Equal(2, expanded.Count);
    }

    /// <summary>WithRange adds multiple fields.</summary>
    [Test]
    public void WithRange_AddsMultipleFields()
    {
        var initial = ValidationErrors.From(new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        });
        var expanded = initial.WithRange(new[]
        {
            ApiErrorField.WithMessage("password", "Too short."),
            ApiErrorField.WithMessage("name", "Name is required.")
        });
        TestAssert.Equal(3, expanded.Count);
        TestAssert.Equal("email", expanded.Fields[0].Field);
        TestAssert.Equal("name", expanded.Fields[1].Field);
        TestAssert.Equal("password", expanded.Fields[2].Field);
    }

    /// <summary>With rejects a null argument.</summary>
    [Test]
    public void With_RejectsNull()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ValidationErrors.Empty.With(null!));
    }
}

