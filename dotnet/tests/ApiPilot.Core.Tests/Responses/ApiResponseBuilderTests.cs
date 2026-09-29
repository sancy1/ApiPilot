// filepath: dotnet/tests/ApiPilot.Core.Tests/Responses/ApiResponseBuilderTests.cs
// layer: Tests | package: ApiPilot.Core.Tests | since: v0.1.0-alpha.0
// purpose: Contract tests for ApiResponseBuilder and DefaultApiResponseBuilder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.Core.Abstractions,
//                ApiPilot.Core.Metadata, ApiPilot.Core.Responses
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiResponseBuilder.cs, ApiResponse.cs, ApiResponseOfT.cs
// -----------------------------------------------------------------------------

using ApiPilot.Core.Abstractions;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;

namespace ApiPilot.Core.Tests.Responses;

/// <summary>
/// Contract tests for the response builder. Verifies that every factory
/// method produces a response with the expected shape, that the response
/// category is correct, that metadata and messages are preserved, and that
/// invalid arguments are rejected.
/// </summary>
[TestClass]
public sealed class ApiResponseBuilderTests
{
    private static ResponseMetadata Meta()
    {
        return new ResponseMetadata { RequestId = "req-test-001" };
    }

    /// <summary>NoContent returns a non-generic successful response.</summary>
    [Test]
    public void NoContent_ReturnsNonGenericSuccess()
    {
        var meta = Meta();
        var response = ApiResponseBuilder.NoContent(meta);
        TestAssert.NotNull(response);
        TestAssert.True(response.Success);
        TestAssert.Equal(ApiResponseStatus.NoContent, response.Status);
        TestAssert.Equal("req-test-001", response.Meta.RequestId);
    }

    /// <summary>NoContent preserves an optional message.</summary>
    [Test]
    public void NoContent_WithMessage_PreservesMessage()
    {
        var response = ApiResponseBuilder.NoContent(Meta(), "deleted");
        TestAssert.Equal("deleted", response.Message);
    }

    /// <summary>Ok returns a generic successful response with the payload.</summary>
    [Test]
    public void Ok_WithData_ReturnsGenericSuccessWithData()
    {
        var order = new { Id = "ORD-10001", Status = "confirmed" };
        var response = ApiResponseBuilder.Ok(order, Meta());
        TestAssert.NotNull(response);
        TestAssert.True(response.Success);
        TestAssert.Equal(ApiResponseStatus.Ok, response.Status);
        TestAssert.NotNull(response.Data);
        TestAssert.Equal("ORD-10001", response.Data!.Id);
        TestAssert.Equal("req-test-001", response.Meta.RequestId);
    }

    /// <summary>Ok with null data is still a successful response.</summary>
    [Test]
    public void Ok_WithNullData_IsStillSuccessful()
    {
        var response = ApiResponseBuilder.Ok<string?>(null, Meta());
        TestAssert.True(response.Success);
        TestAssert.Null(response.Data);
        TestAssert.Equal(ApiResponseStatus.Ok, response.Status);
    }

    /// <summary>Ok preserves an optional message.</summary>
    [Test]
    public void Ok_WithMessage_PreservesMessage()
    {
        var response = ApiResponseBuilder.Ok("payload", Meta(), "retrieved");
        TestAssert.Equal("retrieved", response.Message);
    }

    /// <summary>Created assigns the Created category.</summary>
    [Test]
    public void Created_AssignsCreatedStatus()
    {
        var response = ApiResponseBuilder.Created(new { Id = 42 }, Meta());
        TestAssert.True(response.Success);
        TestAssert.Equal(ApiResponseStatus.Created, response.Status);
        TestAssert.Equal(42, response.Data!.Id);
    }

    /// <summary>Accepted assigns the Accepted category.</summary>
    [Test]
    public void Accepted_AssignsAcceptedStatus()
    {
        var response = ApiResponseBuilder.Accepted("job-1", Meta());
        TestAssert.True(response.Success);
        TestAssert.Equal(ApiResponseStatus.Accepted, response.Status);
        TestAssert.Equal("job-1", response.Data);
    }

    /// <summary>The builder works with value types.</summary>
    [Test]
    public void Ok_WithValueType_ReturnsValue()
    {
        var response = ApiResponseBuilder.Ok(42, Meta());
        TestAssert.Equal(42, response.Data);
    }

    /// <summary>The builder works with collections.</summary>
    [Test]
    public void Ok_WithCollection_ReturnsCollection()
    {
        var items = new[] { 1, 2, 3 };
        var response = ApiResponseBuilder.Ok(items, Meta());
        TestAssert.NotNull(response.Data);
        TestAssert.Equal(3, response.Data!.Length);
    }

    /// <summary>Null metadata is rejected by the builder.</summary>
    [Test]
    public void Ok_WithNullMeta_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiResponseBuilder.Ok("payload", null!));
    }

    /// <summary>NoContent rejects null metadata.</summary>
    [Test]
    public void NoContent_WithNullMeta_Throws()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            ApiResponseBuilder.NoContent(null!));
    }

    /// <summary>The static facade delegates to the same default builder instance.</summary>
    [Test]
    public void StaticFacade_UsesSharedDefaultInstance()
    {
        IApiResponseBuilder a = ApiResponseBuilder.Default;
        IApiResponseBuilder b = ApiResponseBuilder.Default;
        TestAssert.True(ReferenceEquals(a, b));
    }

    /// <summary>
    /// The static facade and the injected interface produce responses of
    /// the same shape for the same inputs.
    /// </summary>
    [Test]
    public void StaticFacade_And_Instance_ProduceSameShape()
    {
        IApiResponseBuilder builder = ApiResponseBuilder.Default;
        var meta = Meta();
        var viaStatic = ApiResponseBuilder.Ok("x", meta, "msg");
        var viaInstance = builder.Ok("x", meta, "msg");
        TestAssert.Equal(viaStatic.Success, viaInstance.Success);
        TestAssert.Equal(viaStatic.Status, viaInstance.Status);
        TestAssert.Equal(viaStatic.Data, viaInstance.Data);
        TestAssert.Equal(viaStatic.Message, viaInstance.Message);
        TestAssert.Equal(viaStatic.Meta.RequestId, viaInstance.Meta.RequestId);
    }
}

