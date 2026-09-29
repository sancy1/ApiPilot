<!--
filepath: docs/openapi.md
package:  n/a (repository root docs)
since:    v0.6.0
purpose:  The ApiPilot OpenAPI contract: the repository-owned emitter, the document shape, and the Option D surface.
-->

# The ApiPilot OpenAPI contract

This document describes how ApiPilot publishes its response and error
contracts in an OpenAPI document. ApiPilot does not own OpenAPI
generation as a platform feature; it contributes a repository-owned
emitter that reads the framework ApiExplorer descriptors and writes a
valid OpenAPI 3.0.3 document. The normative wire contract is
`../SPEC.md`. Where this document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The emitter boundary: what ApiPilot owns and what it does not.
- The document shape: the top-level keys and the schema section.
- The input model: the framework ApiExplorer descriptors.
- The Option D surface.
- How an application registers and maps the document.
- The compatibility contract.

## The emitter boundary

ApiPilot does not reference Microsoft.OpenApi, Swashbuckle, NSwag, or
Swagger. The document is produced by a repository-owned emitter that
uses the framework ApiExplorer descriptors as input and writes
System.Text.Json nodes. The emitter is the only producer of the
document; there is no third-party generator in the dependency graph.

The emitter consumes `IApiDescriptionGroupCollectionProvider`, the
framework provider that MVC and the endpoint API explorer populate.
An application that already calls AddControllers or
AddEndpointsApiExplorer has the provider in the container. ApiPilot
does not register the provider; it consumes it.

## The document shape

The document is an OpenAPI 3.0.3 object. The version is fixed for the
repository-owned emitter; an application that needs a different
OpenAPI version is out of scope for this emitter.

The top-level keys are:

- `openapi` - the specification version, `3.0.3`.
- `info` - the document title and version, from the options.
- `paths` - the path items, built from the descriptors.
- `components.schemas` - the error envelope, error object, and field
  map schemas, unless IncludeErrorSchemas is false.

Every path key is a relative path beginning with a forward slash, as
the OpenAPI specification requires. Every HTTP method key is
lowercase. The operation id is derived from the method and the
relative path.

### Supported and unsupported methods

The emitter maps the methods OpenAPI 3.0.3 recognizes: get, put,
post, delete, options, head, patch, and trace. A method outside that
set is represented explicitly through the vendor extension
`x-apipilot-unsupported-methods` on the path item, rather than
silently dropped or emitted as an invalid operation.

### Duplicate operations

When two descriptors resolve to the same path and method, the second
is represented explicitly through the vendor extension
`x-apipilot-duplicate-operation` on the path item, rather than
silently overwriting the first operation.

## The error schema

The emitter publishes three schemas in `components.schemas`:

- `ApiPilotErrorResponse` - the error envelope: success (always
  false), error, and meta.
- `ApiPilotError` - the nested error object: code, message, and an
  optional fields map.
- `ApiPilotErrorFields` - the field map: an object whose values are
  arrays of message strings.

The schemas are derived from the wire contract in SPEC.md. A parity
test serializes a real error envelope and asserts the schema
describes the same shape; a divergence fails the test.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Document path | `/openapi/v1.json` | `ApiPilotOpenApiOptions.DocumentPath` |
| Document title | `ApiPilot API` | `ApiPilotOpenApiOptions.DocumentTitle` |
| Document version | `1.0.0` | `ApiPilotOpenApiOptions.DocumentVersion` |
| Error schemas | included | `ApiPilotOpenApiOptions.IncludeErrorSchemas` |
| Pagination schema | included | `ApiPilotOpenApiOptions.IncludePaginationSchema` |
| OpenAPI version | `3.0.3` | not overridable |
| Wire schema shapes | derived from SPEC.md | not overridable |

The document path is configured through the options and read by the
mapping extension. The mapping method takes no path parameter, so
there is a single coherent override mechanism.

The options are registered through `AddApiPilotOpenApi`. The
registration is fail-closed: an empty title, an empty version, or a
document path that is not an absolute route path prevents the host
from starting.

## Registering and mapping

Register the options and map the document endpoint:

    builder.Services.AddControllers();
    builder.Services.AddApiPilotOpenApi(o =>
    {
        o.DocumentTitle = "Orders API";
        o.DocumentVersion = "2.1.0";
        o.DocumentPath = "/openapi/v2.json";
    });

    var app = builder.Build();
    app.MapControllers();
    app.MapApiPilotOpenApi();

The document is served as JSON at the configured path. To view it,
issue a GET against the path with an Accept header of
application/json.

## Compatibility

The OpenAPI version string (`3.0.3`), the schema names
(`ApiPilotErrorResponse`, `ApiPilotError`, `ApiPilotErrorFields`),
and the vendor extension names (`x-apipilot-unsupported-methods`,
`x-apipilot-duplicate-operation`) are a compatibility contract of
this version. They are not renamed without a version bump.

## Related documents

- `../SPEC.md` - the wire contract.
- `error-contract.md` - the error envelope shape.
- `response-contract.md` - the success envelope shape.
- `pagination.md` - the pagination shape.

