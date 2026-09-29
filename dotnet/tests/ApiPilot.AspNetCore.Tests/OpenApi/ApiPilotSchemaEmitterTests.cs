// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/OpenApi/ApiPilotSchemaEmitterTests.cs
// layer: OpenApi | package: ApiPilot.AspNetCore.Tests | since: v0.6.0
// purpose: Tests for the repository-owned OpenAPI emitter structure and method mapping
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, ApiPilotSchemaEmitter, ApiPilotOpenApiOptions,
//                StandardErrorSchema, IApiDescriptionGroupCollectionProvider,
//                WebApplication, JsonObject
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotSchemaEmitter.cs, StandardErrorSchema.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Nodes;

namespace ApiPilot.AspNetCore.Tests.OpenApi;

/// <summary>
/// Tests for the OpenAPI emitter: the document structure, path keys,
/// method mapping, and the error-schema reference. The provider is built
/// through AddControllers(), the registration the repository uses.
/// </summary>
[TestClass]
public sealed class ApiPilotSchemaEmitterTests
{
    private static IApiDescriptionGroupCollectionProvider BuildProvider()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(EmitterSampleController).Assembly);
        var app = builder.Build();
        app.MapControllers();
        return app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>();
    }

    /// <summary>The document has the required OpenAPI top-level keys.</summary>
    [Test]
    public void BuildDocument_HasRequiredTopLevelKeys()
    {
        var provider = BuildProvider();
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, new ApiPilotOpenApiOptions());
        TestAssert.NotNull(document["openapi"]);
        TestAssert.NotNull(document["info"]);
        TestAssert.NotNull(document["paths"]);
        TestAssert.NotNull(document["components"]);
    }

    /// <summary>The openapi version is the explicitly selected 3.0.3.</summary>
    [Test]
    public void BuildDocument_UsesOpenApi303()
    {
        var provider = BuildProvider();
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, new ApiPilotOpenApiOptions());
        TestAssert.Equal("3.0.3", document["openapi"]!.GetValue<string>());
    }

    /// <summary>Every path key begins with a forward slash.</summary>
    [Test]
    public void BuildDocument_PathKeysAreRelativeAbsolute()
    {
        var provider = BuildProvider();
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, new ApiPilotOpenApiOptions());
        var paths = (JsonObject)document["paths"]!;
        TestAssert.True(paths.Count > 0, "expected at least one path");
        foreach (var kvp in paths)
        {
            TestAssert.True(kvp.Key.StartsWith('/'), "path key must start with a slash: " + kvp.Key);
        }
    }

    /// <summary>The items path has a get operation (lowercase method key).</summary>
    [Test]
    public void BuildDocument_MapsSupportedMethods()
    {
        var provider = BuildProvider();
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, new ApiPilotOpenApiOptions());
        var paths = (JsonObject)document["paths"]!;
        var hasGet = false;
        foreach (var kvp in paths)
        {
            if (kvp.Value is JsonObject item && item["get"] is not null)
            {
                hasGet = true;
                break;
            }
        }
        TestAssert.True(hasGet, "expected at least one GET operation");
    }

    /// <summary>The error envelope schema is published in components.schemas.</summary>
    [Test]
    public void BuildDocument_PublishesErrorSchema()
    {
        var provider = BuildProvider();
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, new ApiPilotOpenApiOptions());
        var schemas = (JsonObject)document["components"]!["schemas"]!;
        TestAssert.NotNull(schemas[StandardErrorSchema.EnvelopeSchemaName]);
        TestAssert.NotNull(schemas[StandardErrorSchema.ErrorObjectSchemaName]);
        TestAssert.NotNull(schemas[StandardErrorSchema.FieldMapSchemaName]);
    }

    /// <summary>When IncludeErrorSchemas is false, the error schemas are absent.</summary>
    [Test]
    public void BuildDocument_OmitsErrorSchema_WhenDisabled()
    {
        var provider = BuildProvider();
        var options = new ApiPilotOpenApiOptions { IncludeErrorSchemas = false };
        var document = ApiPilotSchemaEmitter.BuildDocument(provider, options);
        var schemas = (JsonObject)document["components"]!["schemas"]!;
        TestAssert.Null(schemas[StandardErrorSchema.EnvelopeSchemaName]);
    }
}

/// <summary>A sample controller that gives the ApiExplorer one endpoint.</summary>
[ApiController]
[Route("emitter-sample")]
public sealed class EmitterSampleController : ControllerBase
{
    /// <summary>Returns a sample value.</summary>
    /// <returns>A fixed string.</returns>
    [HttpGet]
    public string Get() => "ok";
}

