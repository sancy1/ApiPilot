// filepath: dotnet/tests/ApiPilot.ContractTests/StatusMappingContractTests.cs
// layer: Contract | package: ApiPilot.ContractTests | since: v0.6.0
// purpose: Prove the error-code-to-status mapping by exercising ErrorResponseResult directly (A-250)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ErrorResponseResult,
//                ErrorResponse, ApiError, ApiErrorCode, ResponseMetadata, DefaultHttpContext
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ErrorResponseResult.cs, SPEC.md (error code table)
// -----------------------------------------------------------------------------
//
// THE A-250 PATTERN
//   These tests do not copy the status table. They build an ErrorResponse,
//   run it through the real ErrorResponseResult, and read the status the
//   result wrote. If the mapping changes, the test follows it.

using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.ContractTests;

/// <summary>
/// Contract tests for the error-code-to-status mapping, exercised through
/// the real ErrorResponseResult seam.
/// </summary>
[TestClass]
public sealed class StatusMappingContractTests
{
    private static async Task<int> ExecuteAndReadStatusAsync(ApiErrorCode code)
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        using var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Response.Body = new MemoryStream();

        var error = ApiError.Create(code, "safe message");
        var meta = new ResponseMetadata { RequestId = "req-status-001" };
        var result = new ErrorResponseResult(new ErrorResponse(error, meta));

        await result.ExecuteAsync(context);
        return context.Response.StatusCode;
    }

    /// <summary>VALIDATION_ERROR maps to 400.</summary>
    [Test]
    public async Task ValidationError_MapsTo400()
    {
        var status = await ExecuteAndReadStatusAsync(ApiErrorCode.ValidationError);
        TestAssert.Equal(400, status);
    }

    /// <summary>RESOURCE_NOT_FOUND maps to 404.</summary>
    [Test]
    public async Task ResourceNotFound_MapsTo404()
    {
        var status = await ExecuteAndReadStatusAsync(ApiErrorCode.ResourceNotFound);
        TestAssert.Equal(404, status);
    }

    /// <summary>RATE_LIMITED maps to 429.</summary>
    [Test]
    public async Task RateLimited_MapsTo429()
    {
        var status = await ExecuteAndReadStatusAsync(ApiErrorCode.RateLimited);
        TestAssert.Equal(429, status);
    }

    /// <summary>INTERNAL_ERROR maps to 500.</summary>
    [Test]
    public async Task InternalError_MapsTo500()
    {
        var status = await ExecuteAndReadStatusAsync(ApiErrorCode.InternalError);
        TestAssert.Equal(500, status);
    }

    /// <summary>An unrecognized wire code falls back to 500.</summary>
    [Test]
    public async Task UnknownCode_FallsBackTo500()
    {
        var status = await ExecuteAndReadStatusAsync(ApiErrorCode.From("NOT_A_REAL_CODE"));
        TestAssert.Equal(500, status);
    }
}

