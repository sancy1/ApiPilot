// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Results/ErrorResponseResultStatusMappingTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Tests for the ErrorCodeToStatusMap override on ErrorResponseResult
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.Configuration,
//                ApiPilot.AspNetCore.Results, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Core.Errors, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ErrorResponseResult.cs, ApiExceptionOptions.cs, PLANNING.md (finding A-049 audit)
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace ApiPilot.AspNetCore.Tests.Results;

[TestClass]
public sealed class ErrorResponseResultStatusMappingTests
{
    private static HttpContext BuildContext()
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static ErrorResponse BuildResponse(string code)
    {
        var error = ApiError.Create(ApiErrorCode.From(code), "test message");
        var meta = new ResponseMetadata { RequestId = "req-status-test" };
        return new ErrorResponse(error, meta);
    }

    [Test]
    public async Task ExecuteAsync_DefaultMap_ValidationError_Returns400()
    {
        var httpContext = BuildContext();
        var response = BuildResponse("VALIDATION_ERROR");
        var result = new ErrorResponseResult(response, options: null);

        await result.ExecuteAsync(httpContext);
        TestAssert.Equal(400, httpContext.Response.StatusCode);
    }

    [Test]
    public async Task ExecuteAsync_CustomMap_ValidationError_Returns422()
    {
        var httpContext = BuildContext();
        var response = BuildResponse("VALIDATION_ERROR");

        var options = new ApiExceptionOptions();
        options.ErrorCodeToStatusMap = new Dictionary<string, int>
        {
            { "VALIDATION_ERROR", 422 }
        };

        var result = new ErrorResponseResult(response, options);
        await result.ExecuteAsync(httpContext);
        TestAssert.Equal(422, httpContext.Response.StatusCode);
    }

    [Test]
    public async Task ExecuteAsync_CustomMap_CustomCode_Returns402()
    {
        var httpContext = BuildContext();
        var response = BuildResponse("TENANT_QUOTA_EXCEEDED");

        var options = new ApiExceptionOptions();
        options.ErrorCodeToStatusMap = new Dictionary<string, int>
        {
            { "TENANT_QUOTA_EXCEEDED", 402 }
        };

        var result = new ErrorResponseResult(response, options);
        await result.ExecuteAsync(httpContext);
        TestAssert.Equal(402, httpContext.Response.StatusCode);
    }

    [Test]
    public async Task ExecuteAsync_DefaultMap_UnknownCode_Returns500()
    {
        var httpContext = BuildContext();
        var response = BuildResponse("SOMETHING_UNMAPPED");
        var result = new ErrorResponseResult(response, options: null);

        await result.ExecuteAsync(httpContext);
        TestAssert.Equal(500, httpContext.Response.StatusCode);
    }

    [Test]
    public void ApiExceptionOptions_ErrorCodeToStatusMap_DefaultsToEmpty()
    {
        var options = new ApiExceptionOptions();
        TestAssert.NotNull(options.ErrorCodeToStatusMap);
        TestAssert.Equal(0, options.ErrorCodeToStatusMap.Count);
    }
}

