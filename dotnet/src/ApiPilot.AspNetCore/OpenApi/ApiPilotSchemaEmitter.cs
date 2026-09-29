// filepath: dotnet/src/ApiPilot.AspNetCore/OpenApi/ApiPilotSchemaEmitter.cs
// layer: OpenApi | package: ApiPilot.AspNetCore | since: v0.6.0
// purpose: Emits the OpenAPI paths section from the framework ApiExplorer descriptors
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : ApiPilotSchemaDocument, StandardErrorSchema, ApiPilotOpenApiOptions,
//                IApiDescriptionGroupCollectionProvider, ApiDescription,
//                System.Text.Json.Nodes
//   Used by    : ApiPilotOpenApiExtensions (the document endpoint)
//   See also   : SPEC.md, docs/openapi.md, ApiPilotSchemaDocument.cs
// -----------------------------------------------------------------------------
//
// SCOPE
//   This is a repository-owned emitter. It uses the framework ApiExplorer
//   descriptors as input and produces a valid OpenAPI 3.0.3 document
//   expressed entirely as System.Text.Json nodes. It does NOT reference
//   Microsoft.OpenApi, Swashbuckle, NSwag, or Swagger. It maps the
//   supported HTTP methods; an unsupported method is represented
//   explicitly rather than silently emitted or dropped. Path keys are
//   relative and begin with a forward slash.

using Microsoft.AspNetCore.Mvc.ApiExplorer;
using System.Text.Json.Nodes;

namespace ApiPilot.AspNetCore.OpenApi;

/// <summary>
/// Emits an OpenAPI 3.0.3 document from the framework ApiExplorer
/// descriptors. The document envelope is built by
/// <see cref="ApiPilotSchemaDocument"/>; this class adds the paths.
/// </summary>
/// <remarks>
/// The emitter is deterministic: descriptors are ordered by relative
/// path and then by HTTP method so identical inputs produce identical
/// documents.
/// </remarks>
public static class ApiPilotSchemaEmitter
{
    /// <summary>
    /// The set of HTTP methods OpenAPI 3.0.3 recognizes as path-item
    /// operations. A method outside this set is represented through a
    /// vendor extension rather than emitted as an operation.
    /// </summary>
    private static readonly string[] s_supportedMethods =
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace"
    };

    /// <summary>
    /// Builds the full OpenAPI document from the descriptors and options.
    /// </summary>
    /// <param name="provider">The descriptor provider. Must not be null.</param>
    /// <param name="options">The document options. Must not be null.</param>
    /// <returns>A fresh document node the caller owns.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="provider"/> or <paramref name="options"/> is null.
    /// </exception>
    public static JsonObject BuildDocument(
        IApiDescriptionGroupCollectionProvider provider,
        ApiPilotOpenApiOptions options)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(options);

        var document = ApiPilotSchemaDocument.Build(options);
        var paths = (JsonObject)document["paths"]!;

        var descriptors = new List<ApiDescription>();
        foreach (var group in provider.ApiDescriptionGroups.Items)
        {
            foreach (var description in group.Items)
            {
                descriptors.Add(description);
            }
        }

        descriptors.Sort(static (a, b) =>
        {
            var byPath = string.CompareOrdinal(a.RelativePath, b.RelativePath);
            if (byPath != 0)
            {
                return byPath;
            }
            return string.CompareOrdinal(a.HttpMethod, b.HttpMethod);
        });

        foreach (var description in descriptors)
        {
            AddPathItem(paths, description, options);
        }

        return document;
    }

    private static void AddPathItem(
        JsonObject paths,
        ApiDescription description,
        ApiPilotOpenApiOptions options)
    {
        if (string.IsNullOrEmpty(description.RelativePath))
        {
            return;
        }

        var relative = description.RelativePath!.TrimStart('/');
        var pathKey = "/" + relative;

        if (paths[pathKey] is not JsonObject pathItem)
        {
            pathItem = new JsonObject();
            paths[pathKey] = pathItem;
        }

        var method = (description.HttpMethod ?? string.Empty).ToLowerInvariant();

        if (!IsSupportedMethod(method))
        {
            // Represent the unsupported method explicitly instead of
            // silently dropping it or emitting an invalid operation.
            if (pathItem["x-apipilot-unsupported-methods"] is not JsonArray unsupported)
            {
                unsupported = new JsonArray();
                pathItem["x-apipilot-unsupported-methods"] = unsupported;
            }
            unsupported.Add(description.HttpMethod);
            return;
        }

        if (pathItem[method] is JsonObject)
        {
            // A duplicate path+method. Represent it explicitly rather than
            // silently overwriting the earlier operation.
            pathItem["x-apipilot-duplicate-operation"] = method + " " + pathKey;
            return;
        }

        var operation = new JsonObject();
        operation["operationId"] = BuildOperationId(method, relative);

        var parameters = BuildParameters(description);
        if (parameters.Count > 0)
        {
            operation["parameters"] = parameters;
        }

        operation["responses"] = BuildResponses(description, options);
        pathItem[method] = operation;
    }

    private static JsonArray BuildParameters(ApiDescription description)
    {
        var parameters = new JsonArray();
        foreach (var parameter in description.ParameterDescriptions)
        {
            if (string.IsNullOrEmpty(parameter.Name))
            {
                continue;
            }
            var node = new JsonObject();
            node["name"] = parameter.Name;
            node["in"] = "query";
            node["required"] = parameter.IsRequired;
            node["schema"] = new JsonObject { ["type"] = MapTypeName(parameter.Type) };
            parameters.Add(node);
        }
        return parameters;
    }

    private static JsonObject BuildResponses(
        ApiDescription description,
        ApiPilotOpenApiOptions options)
    {
        var responses = new JsonObject();
        foreach (var response in description.SupportedResponseTypes)
        {
            var key = response.IsDefaultResponse
                ? "default"
                : response.StatusCode.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (responses[key] is JsonObject)
            {
                continue;
            }

            var responseNode = new JsonObject();
            responseNode["description"] = string.IsNullOrEmpty(response.Description)
                ? "Response " + key
                : response.Description;

            if (options.IncludeErrorSchemas && IsErrorStatus(response.StatusCode))
            {
                var content = new JsonObject();
                var media = new JsonObject();
                var schemaRef = new JsonObject();
                schemaRef["$ref"] = "#/components/schemas/" + StandardErrorSchema.EnvelopeSchemaName;
                media["schema"] = schemaRef;
                content["application/json"] = media;
                responseNode["content"] = content;
            }

            responses[key] = responseNode;
        }

        if (responses.Count == 0)
        {
            var fallback = new JsonObject();
            fallback["description"] = "Default response";
            responses["default"] = fallback;
        }

        return responses;
    }

    private static bool IsSupportedMethod(string method)
    {
        foreach (var supported in s_supportedMethods)
        {
            if (string.Equals(supported, method, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsErrorStatus(int statusCode)
    {
        return statusCode >= 400;
    }

    private static string BuildOperationId(string method, string relativePath)
    {
        var cleaned = relativePath.Replace('/', '_');
        return method + "_" + cleaned;
    }

    private static string MapTypeName(Type? type)
    {
        if (type is null)
        {
            return "string";
        }
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying == typeof(int) || underlying == typeof(long) || underlying == typeof(short))
        {
            return "integer";
        }
        if (underlying == typeof(bool))
        {
            return "boolean";
        }
        if (underlying == typeof(double) || underlying == typeof(float) || underlying == typeof(decimal))
        {
            return "number";
        }
        return "string";
    }
}

