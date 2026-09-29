// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Results/ApiResponseHttpMapperTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the ApiResponseHttpMapper
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Results, ApiPilot.Core.Responses
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiResponseHttpMapper.cs, SPEC.md (error code table)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Responses;

namespace ApiPilot.AspNetCore.Tests.Results;

/// <summary>
/// Contract tests for ApiResponseHttpMapper. Verifies the HTTP status code
/// for every ApiResponseStatus member and the rejection of unknown values.
/// </summary>
[TestClass]
public sealed class ApiResponseHttpMapperTests
{
    /// <summary>Ok maps to 200.</summary>
    [Test]
    public void Map_Ok_Returns200()
    {
        TestAssert.Equal(200, ApiResponseHttpMapper.Map(ApiResponseStatus.Ok));
    }

    /// <summary>Created maps to 201.</summary>
    [Test]
    public void Map_Created_Returns201()
    {
        TestAssert.Equal(201, ApiResponseHttpMapper.Map(ApiResponseStatus.Created));
    }

    /// <summary>Accepted maps to 202.</summary>
    [Test]
    public void Map_Accepted_Returns202()
    {
        TestAssert.Equal(202, ApiResponseHttpMapper.Map(ApiResponseStatus.Accepted));
    }

    /// <summary>NoContent maps to 204.</summary>
    [Test]
    public void Map_NoContent_Returns204()
    {
        TestAssert.Equal(204, ApiResponseHttpMapper.Map(ApiResponseStatus.NoContent));
    }

    /// <summary>BadRequest maps to 400.</summary>
    [Test]
    public void Map_BadRequest_Returns400()
    {
        TestAssert.Equal(400, ApiResponseHttpMapper.Map(ApiResponseStatus.BadRequest));
    }

    /// <summary>Unauthorized maps to 401.</summary>
    [Test]
    public void Map_Unauthorized_Returns401()
    {
        TestAssert.Equal(401, ApiResponseHttpMapper.Map(ApiResponseStatus.Unauthorized));
    }

    /// <summary>Forbidden maps to 403.</summary>
    [Test]
    public void Map_Forbidden_Returns403()
    {
        TestAssert.Equal(403, ApiResponseHttpMapper.Map(ApiResponseStatus.Forbidden));
    }

    /// <summary>NotFound maps to 404.</summary>
    [Test]
    public void Map_NotFound_Returns404()
    {
        TestAssert.Equal(404, ApiResponseHttpMapper.Map(ApiResponseStatus.NotFound));
    }

    /// <summary>Conflict maps to 409.</summary>
    [Test]
    public void Map_Conflict_Returns409()
    {
        TestAssert.Equal(409, ApiResponseHttpMapper.Map(ApiResponseStatus.Conflict));
    }

    /// <summary>Unprocessable maps to 422.</summary>
    [Test]
    public void Map_Unprocessable_Returns422()
    {
        TestAssert.Equal(422, ApiResponseHttpMapper.Map(ApiResponseStatus.Unprocessable));
    }

    /// <summary>TooManyRequests maps to 429.</summary>
    [Test]
    public void Map_TooManyRequests_Returns429()
    {
        TestAssert.Equal(429, ApiResponseHttpMapper.Map(ApiResponseStatus.TooManyRequests));
    }

    /// <summary>ServerError maps to 500.</summary>
    [Test]
    public void Map_ServerError_Returns500()
    {
        TestAssert.Equal(500, ApiResponseHttpMapper.Map(ApiResponseStatus.ServerError));
    }

    /// <summary>An unknown status value is rejected.</summary>
    [Test]
    public void Map_UnknownStatus_Throws()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            ApiResponseHttpMapper.Map((ApiResponseStatus)999));
    }
}

