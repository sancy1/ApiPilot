// filepath: dotnet/tests/ApiPilot.Core.Tests/Errors/ErrorResponseTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for the ErrorResponse envelope
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Core.Abstractions, ApiPilot.Core.Errors, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ErrorResponse.cs, ApiError.cs, ApiResponseBuilderTests.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;

namespace ApiPilot.Core.Tests.Errors;

/// <summary>
/// Contract tests for the ErrorResponse envelope. Verifies that success is
/// always false, that the error and meta are preserved, that the type
/// implements IApiResponse, and that null arguments are rejected.
/// </summary>
[TestClass]
public sealed class ErrorResponseTests
{
    private static ResponseMetadata Meta()
    {
        return new ResponseMetadata { RequestId = "req-err-001" };
    }

    /// <summary>Success is always false on an error response.</summary>
    [Test]
    public void Success_IsAlwaysFalse()
    {
        var error = ApiError.Create(ApiErrorCode.ResourceNotFound, "Not found.");
        var response = new ErrorResponse(error, Meta());
        TestAssert.False(response.Success);
    }

    /// <summary>The error object is preserved on the response.</summary>
    [Test]
    public void Error_IsPreserved()
    {
        var error = ApiError.Create(ApiErrorCode.Conflict, "Conflict.");
        var response = new ErrorResponse(error, Meta());
        TestAssert.Equal(error.Code.Code, response.Error.Code.Code);
        TestAssert.Equal(error.Message, response.Error.Message);
        TestAssert.Null(response.Error.Fields);
    }

    /// <summary>The metadata is preserved on the response.</summary>
    [Test]
    public void Meta_IsPreserved()
    {
        var meta = Meta();
        var error = ApiError.Create(ApiErrorCode.InternalError, "Unexpected error.");
        var response = new ErrorResponse(error, meta);
        TestAssert.Equal("req-err-001", response.Meta.RequestId);
    }

    /// <summary>The constructor rejects a null error.</summary>
    [Test]
    public void Constructor_WithNullError_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ErrorResponse(null!, Meta()));
    }

    /// <summary>The constructor rejects null metadata.</summary>
    [Test]
    public void Constructor_WithNullMeta_Throws()
    {
        var error = ApiError.Create(ApiErrorCode.InternalError, "Unexpected error.");
        TestAssert.Throws<ArgumentNullException>(() =>
            new ErrorResponse(error, null!));
    }

    /// <summary>ErrorResponse is assignable to IApiResponse.</summary>
    [Test]
    public void Implements_IApiResponse()
    {
        var error = ApiError.Create(ApiErrorCode.Forbidden, "Forbidden.");
        var response = new ErrorResponse(error, Meta());
        TestAssert.True(response is IApiResponse);
    }

    /// <summary>Through the IApiResponse interface, success and meta are accessible.</summary>
    [Test]
    public void IApiResponse_View_ExposesSuccessAndMeta()
    {
        var error = ApiError.Create(ApiErrorCode.AuthenticationRequired, "no");
        var response = new ErrorResponse(error, Meta());
        IApiResponse asInterface = response;
        TestAssert.False(asInterface.Success);
        TestAssert.Equal("req-err-001", asInterface.Meta.RequestId);
    }

    /// <summary>A validation error with fields is preserved on the response.</summary>
    [Test]
    public void ErrorWithFields_IsPreserved()
    {
        var fields = new[]
        {
            ApiErrorField.WithMessage("email", "Email is required.")
        };
        var error = ApiError.Create(ApiErrorCode.ValidationError, "Invalid input.", fields);
        var response = new ErrorResponse(error, Meta());
        TestAssert.NotNull(response.Error.Fields);
        TestAssert.Equal(1, response.Error.Fields!.Count);
        TestAssert.True(response.Error.Fields.ContainsKey("email"));
        TestAssert.Equal(1, response.Error.Fields["email"].Count);
        TestAssert.Equal("Email is required.", response.Error.Fields["email"][0]);
    }
}

