// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/ApiPilotSchemaDocument.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: Builds the OpenAPI document skeleton (openapi, info, paths, components.schemas) from the ApiPilot options
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilotOpenApiOptions, StandardErrorSchema, System.Text.Json.Nodes
//   Used by    : ApiPilotSchemaEmitter
//   See also   : SPEC.md (error envelope), docs/openapi.md, StandardErrorSchema.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This builds the document envelope and the components.schemas section.
//   The paths section is built by the emitter from the ApiExplorer
//   descriptors. The OpenAPI version is explicitly selected (3.0.3).
//   Every schema is a repository-owned System.Text.Json node. No
//   third-party OpenAPI type is referenced.
//
// OPTION D
//   IncludeErrorSchemas is consulted here: when false, the three error
//   schemas are omitted from components.schemas. IncludePaginationSchema
//   is consulted by the emitter when it writes the paginated path.

using System.Text.Json.Nodes;

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// Builds the OpenAPI document skeleton: the openapi version string, the
/// info object, an empty paths object (the emitter fills it), and the
/// components.schemas section derived from the options and
/// StandardErrorSchema.
/// </summary>
/// <remarks>
/// The document is an OpenAPI 3.0.3 document. The version is fixed for
/// this repository and is not an application override; an application
/// that needs a different OpenAPI version is out of scope for the
/// repository-owned emitter.
/// </remarks>
public static class ApiPilotSchemaDocument
{
    /// <summary>
    /// The OpenAPI specification version emitted in the document.
    /// </summary>
    public const string OpenApiVersion = "3.0.3";

    /// <summary>
    /// Builds the document skeleton for the given options. The paths
    /// object is empty; the emitter adds path items.
    /// </summary>
    /// <param name="options">The document options. Must not be null.</param>
    /// <returns>A fresh document node the caller owns.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    public static JsonObject Build(ApiPilotOpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var info = new JsonObject();
        info["title"] = options.DocumentTitle;
        info["version"] = options.DocumentVersion;

        var paths = new JsonObject();

        var schemas = new JsonObject();
        if (options.IncludeErrorSchemas)
        {
            schemas[StandardErrorSchema.EnvelopeSchemaName] = StandardErrorSchema.BuildEnvelopeSchema();
            schemas[StandardErrorSchema.ErrorObjectSchemaName] = StandardErrorSchema.BuildErrorObjectSchema();
            schemas[StandardErrorSchema.FieldMapSchemaName] = StandardErrorSchema.BuildFieldMapSchema();
        }

        var components = new JsonObject();
        components["schemas"] = schemas;

        var document = new JsonObject();
        document["openapi"] = OpenApiVersion;
        document["info"] = info;
        document["paths"] = paths;
        document["components"] = components;
        return document;
    }
}

