// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/ApiPilotOpenApiOptions.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: The Option D override surface for the repository-owned OpenAPI emitter
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (options type)
//   Depends on : n/a
//   Used by    : ApiPilotOpenApiExtensions, ApiPilotSchemaEmitter
//   See also   : docs/openapi.md, SPEC.md (response envelope)
// -----------------------------------------------------------------------------
//
// OPTION D
//   Every configurable part of the OpenAPI document has a sensible
//   default and a first-class override. The document PATH is the single
//   override mechanism: the mapping extension reads DocumentPath. There
//   is no competing method parameter. The wire SHAPES (the response and
//   error schemas) are fixed by SPEC.md and are not overridable.

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// The overridable surface for the repository-owned OpenAPI document.
/// Every property has a sensible default and may be replaced by the
/// application.
/// </summary>
/// <remarks>
/// <para>
/// The document path is configured here and nowhere else. The mapping
/// extension <c>MapApiPilotOpenApi</c> reads <see cref="DocumentPath"/>;
/// it does not take a path parameter. This keeps one coherent override
/// mechanism.
/// </para>
/// <para>
/// The values are validated at startup through the options pipeline. An
/// empty title, an empty version, or a path that is not an absolute
/// route path fails the host at startup, not at request time.
/// </para>
/// </remarks>
public sealed class ApiPilotOpenApiOptions
{
    /// <summary>
    /// The route path at which the OpenAPI document is served. Defaults
    /// to <c>/openapi/v1.json</c>. Must be an absolute route path
    /// (starting with a forward slash).
    /// </summary>
    public string DocumentPath { get; set; } = "/openapi/v1.json";

    /// <summary>
    /// The document title written into the <c>info</c> object. Must not
    /// be null, empty, or whitespace. Defaults to <c>ApiPilot API</c>.
    /// </summary>
    public string DocumentTitle { get; set; } = "ApiPilot API";

    /// <summary>
    /// The document version written into the <c>info</c> object. Must not
    /// be null, empty, or whitespace. Defaults to <c>1.0.0</c>.
    /// </summary>
    public string DocumentVersion { get; set; } = "1.0.0";

    /// <summary>
    /// When true, the standard error and validation schemas are included
    /// in the generated document. Defaults to true. Applications that do
    /// not want the error schemas in a public document set this to false.
    /// </summary>
    public bool IncludeErrorSchemas { get; set; } = true;

    /// <summary>
    /// When true, the pagination schema is included in the generated
    /// document. Defaults to true.
    /// </summary>
    public bool IncludePaginationSchema { get; set; } = true;
}

