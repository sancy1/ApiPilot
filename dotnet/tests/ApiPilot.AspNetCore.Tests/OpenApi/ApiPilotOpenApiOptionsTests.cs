// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/OpenApi/ApiPilotOpenApiOptionsTests.cs
// layer: OpenApi | package: ApiPilot.AspNetCore.Tests | since: v0.6.0
// purpose: Contract tests for the ApiPilotOpenApiOptions data holder
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.OpenApi
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotOpenApiOptions.cs, ApiPilotOpenApiOptionsValidator.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.OpenApi;

namespace ApiPilot.AspNetCore.Tests.OpenApi;

/// <summary>
/// Contract tests for ApiPilotOpenApiOptions. Verifies the defaults and
/// that every property is mutable so the Option D overrides are usable.
/// </summary>
[TestClass]
public sealed class ApiPilotOpenApiOptionsTests
{
    /// <summary>The defaults match the documented values.</summary>
    [Test]
    public void Defaults_AreSensible()
    {
        var options = new ApiPilotOpenApiOptions();
        TestAssert.Equal("/openapi/v1.json", options.DocumentPath);
        TestAssert.Equal("ApiPilot API", options.DocumentTitle);
        TestAssert.Equal("1.0.0", options.DocumentVersion);
        TestAssert.True(options.IncludeErrorSchemas);
        TestAssert.True(options.IncludePaginationSchema);
    }

    /// <summary>Every property can be updated after construction.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new ApiPilotOpenApiOptions();
        options.DocumentPath = "/spec.json";
        options.DocumentTitle = "Custom API";
        options.DocumentVersion = "2.5.0";
        options.IncludeErrorSchemas = false;
        options.IncludePaginationSchema = false;

        TestAssert.Equal("/spec.json", options.DocumentPath);
        TestAssert.Equal("Custom API", options.DocumentTitle);
        TestAssert.Equal("2.5.0", options.DocumentVersion);
        TestAssert.False(options.IncludeErrorSchemas);
        TestAssert.False(options.IncludePaginationSchema);
    }

    /// <summary>IncludeErrorSchemas can be turned off (the override is consulted downstream).</summary>
    [Test]
    public void IncludeErrorSchemas_CanBeDisabled()
    {
        var options = new ApiPilotOpenApiOptions { IncludeErrorSchemas = false };
        TestAssert.False(options.IncludeErrorSchemas);
    }

    /// <summary>DocumentPath can be overridden to a different absolute route path.</summary>
    [Test]
    public void DocumentPath_CanBeOverridden()
    {
        var options = new ApiPilotOpenApiOptions { DocumentPath = "/v2/openapi.json" };
        TestAssert.Equal("/v2/openapi.json", options.DocumentPath);
    }
}

