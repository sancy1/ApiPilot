// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/09_ValidationScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 09. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the validation
//           integration, use the minimal API filter and both MVC paths
//           (attribute and [ApiController]), observe the standard
//           VALIDATION_ERROR envelope, and confirm the envelope parity
//           between the MVC paths. Also observes the KeyTransform override.
// relates:  Uses ApiPilot.AspNetCore.DependencyInjection.ApiPilotServiceCollectionExtensions,
//           ApiPilot.AspNetCore.Validation.ApiPilotValidateAttribute,
//           ApiPilot.AspNetCore.Validation.ApiPilotValidationEndpointExtensions,
//           ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions,
//           ApiPilot.Core.Errors.ApiErrorField,
//           ApiPilot.Core.Metadata.ResponseMetadata,
//           ApiPilot.Core.Responses.ApiResponseBuilder,
//           ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml and the
//           packaged README. Not the source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.DependencyInjection;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.AspNetCore.Validation;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 09. Validation end to end over real HTTP. Eight sub-checks:
/// minimal API raw callback, minimal API convenience callback, minimal API
/// valid request, MVC attribute path, MVC [ApiController] path, envelope
/// parity between the two MVC paths, KeyTransform identity override, and
/// KeyTransform null (default camelCase) path.
/// </summary>
public static class ValidationScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "09_Validation";

    /// <summary>Expected field-key set of one lowercase name.</summary>
    private static readonly string[] ExpectedCamelCaseField = { "email" };

    /// <summary>Expected field-key set of one PascalCase name.</summary>
    private static readonly string[] ExpectedPascalCaseField = { "Email" };

    /// <summary>Expected field-key set of one lowercase name, used for MVC paths.</summary>
    private static readonly string[] ExpectedNameField = { "name" };

    /// <summary>The message array shared by every convenience-callback dictionary.</summary>
    private static readonly string[] EmailRequiredMessages = { "Email is required." };

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckMinimalApiRawCallback(failures);
            await CheckMinimalApiConvenienceCallback(failures);
            await CheckMinimalApiValidRequest(failures);
            await CheckMvcAttributePath(failures);
            await CheckMvcApiControllerPath(failures);
            await CheckMvcEnvelopeParity(failures);
            await CheckKeyTransformIdentityOverride(failures);
            await CheckKeyTransformDefaultCamelCase(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "AddApiPilotValidation and AddApiPilotControllers are public and callable; the minimal API filter and both MVC paths produce the standard VALIDATION_ERROR envelope; the two MVC paths agree on error.code and error.fields; the default camelCase key normalization is on the wire; the KeyTransform identity override is honored.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: minimal API raw callback
    // -------------------------------------------------------------------
    private static async Task CheckMinimalApiRawCallback(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => { services.AddApiPilotJson(); },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext _) => Microsoft.AspNetCore.Http.Results.Ok("should not run"))
                   .WithApiPilotValidation(_ => (IReadOnlyList<ApiErrorField>)new List<ApiErrorField>
                   {
                       ApiErrorField.WithMessage("email", "Email is required."),
                   });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        if ((int)response.StatusCode != 400)
        {
            failures.Add("MinimalRaw: expected 400, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertValidationEnvelope(failures, "MinimalRaw", body, ExpectedCamelCaseField);
    }

    // -------------------------------------------------------------------
    // Sub-check 2: minimal API convenience callback
    // -------------------------------------------------------------------
    private static async Task CheckMinimalApiConvenienceCallback(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => { services.AddApiPilotJson(); },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext _) => Microsoft.AspNetCore.Http.Results.Ok("should not run"))
                   .WithApiPilotValidation(_ =>
                   {
                       var dict = new Dictionary<string, IReadOnlyList<string>>
                       {
                           ["Email"] = EmailRequiredMessages,
                       };
                       return (IReadOnlyDictionary<string, IReadOnlyList<string>>)dict;
                   });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        if ((int)response.StatusCode != 400)
        {
            failures.Add("MinimalConvenience: expected 400, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertValidationEnvelope(failures, "MinimalConvenience", body, ExpectedCamelCaseField);
    }

    // -------------------------------------------------------------------
    // Sub-check 3: minimal API valid request runs the handler
    // -------------------------------------------------------------------
    private static async Task CheckMinimalApiValidRequest(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => { services.AddApiPilotJson(); },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext context) =>
                {
                    var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
                    return ApiResponseBuilder.Ok(new { ok = true }, meta, "ok").ToResult();
                })
                .WithApiPilotValidation(_ => (IReadOnlyList<ApiErrorField>)new List<ApiErrorField>());
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("MinimalValid: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: MVC attribute path
    // -------------------------------------------------------------------
    private static async Task CheckMvcAttributePath(List<string> failures)
    {
        await using var host = await StartMvcHostAsync(null);
        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/api/validate-attribute", UriKind.Relative), content);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400)
        {
            failures.Add("MvcAttribute: expected 400, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertValidationEnvelope(failures, "MvcAttribute", body, ExpectedNameField);
    }

    // -------------------------------------------------------------------
    // Sub-check 5: MVC [ApiController] path
    // -------------------------------------------------------------------
    private static async Task CheckMvcApiControllerPath(List<string> failures)
    {
        await using var host = await StartMvcHostAsync(null);
        using var client = host.CreateClient();
        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(new Uri("/api/validate-apicontroller", UriKind.Relative), content);
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400)
        {
            failures.Add("MvcApiController: expected 400, got " + (int)response.StatusCode + ". Body=" + body);
            return;
        }
        AssertValidationEnvelope(failures, "MvcApiController", body, ExpectedNameField);
    }

    // -------------------------------------------------------------------
    // Sub-check 6: envelope parity between the two MVC paths
    // -------------------------------------------------------------------
    private static async Task CheckMvcEnvelopeParity(List<string> failures)
    {
        string bodyAttribute;
        string bodyApiController;

        await using (var host = await StartMvcHostAsync(null))
        {
            using var client = host.CreateClient();
            using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(new Uri("/api/validate-attribute", UriKind.Relative), content);
            bodyAttribute = await response.Content.ReadAsStringAsync();
        }

        await using (var host = await StartMvcHostAsync(null))
        {
            using var client = host.CreateClient();
            using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            using var response = await client.PostAsync(new Uri("/api/validate-apicontroller", UriKind.Relative), content);
            bodyApiController = await response.Content.ReadAsStringAsync();
        }

        var codeAttribute = TryGetErrorField(bodyAttribute, "code");
        var codeApiController = TryGetErrorField(bodyApiController, "code");
        if (!string.Equals(codeAttribute, codeApiController, StringComparison.Ordinal))
        {
            failures.Add("Parity: error.code differs ('" + codeAttribute + "' vs '" + codeApiController + "').");
        }
        if (codeAttribute != "VALIDATION_ERROR")
        {
            failures.Add("Parity: error.code was '" + codeAttribute + "', expected VALIDATION_ERROR.");
        }

        var fieldsAttribute = TryGetErrorFieldKeys(bodyAttribute);
        var fieldsApiController = TryGetErrorFieldKeys(bodyApiController);
        if (fieldsAttribute != fieldsApiController)
        {
            failures.Add("Parity: error.fields keys differ ('" + fieldsAttribute + "' vs '" + fieldsApiController + "').");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 7: KeyTransform identity override (observe, do not fail)
    // -------------------------------------------------------------------
    private static async Task CheckKeyTransformIdentityOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotValidation(o => o.KeyTransform = key => key);
            },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext _) => Microsoft.AspNetCore.Http.Results.Ok("should not run"))
                   .WithApiPilotValidation(_ =>
                   {
                       var dict = new Dictionary<string, IReadOnlyList<string>>
                       {
                           ["Email"] = EmailRequiredMessages,
                       };
                       return (IReadOnlyDictionary<string, IReadOnlyList<string>>)dict;
                   });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400)
        {
            failures.Add("KeyTransformIdentity: expected 400, got " + (int)response.StatusCode + ".");
            return;
        }
        // F-59 was resolved in 1.0.5. The identity transform now preserves the
        // raw key on the wire. This sub-check asserts the corrected behavior.
        AssertValidationEnvelope(failures, "KeyTransformIdentity", body, ExpectedPascalCaseField);
    }

    // -------------------------------------------------------------------
    // Sub-check 8: KeyTransform null (default camelCase)
    // -------------------------------------------------------------------
    private static async Task CheckKeyTransformDefaultCamelCase(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services => { services.AddApiPilotJson(); },
            configurePipeline: app =>
            {
                app.MapGet("/", (HttpContext _) => Microsoft.AspNetCore.Http.Results.Ok("should not run"))
                   .WithApiPilotValidation(_ =>
                   {
                       var dict = new Dictionary<string, IReadOnlyList<string>>
                       {
                           ["Email"] = EmailRequiredMessages,
                       };
                       return (IReadOnlyDictionary<string, IReadOnlyList<string>>)dict;
                   });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        if ((int)response.StatusCode != 400)
        {
            failures.Add("KeyTransformDefault: expected 400, got " + (int)response.StatusCode + ".");
            return;
        }
        AssertValidationEnvelope(failures, "KeyTransformDefault", body, ExpectedCamelCaseField);
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static async Task<InProcessHost> StartMvcHostAsync(Action<ApiPilotValidationOptions>? configureGlobal)
    {
        return await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddControllers();
                services.AddApiPilotControllers(configureGlobal);
            },
            configurePipeline: app =>
            {
                app.MapControllers();
            });
    }

    private static void AssertValidationEnvelope(List<string> failures, string label, string body, string[] expectedKeys)
    {
        if (!AssertValidationEnvelopeShape(failures, label, body)) { return; }
        var fields = JsonNode.Parse(body)!.AsObject()["error"]!.AsObject()["fields"]!.AsObject();
        foreach (var key in expectedKeys)
        {
            if (!fields.ContainsKey(key))
            {
                failures.Add(label + ": error.fields missing key '" + key + "'. Body=" + body);
            }
        }
    }

    private static bool AssertValidationEnvelopeShape(List<string> failures, string label, string body)
    {
        JsonNode? node;
        try { node = JsonNode.Parse(body); }
        catch (Exception ex) { failures.Add(label + ": body did not parse (" + ex.GetType().Name + ")."); return false; }
        if (node is null) { failures.Add(label + ": JsonNode.Parse returned null."); return false; }

        var obj = node.AsObject();
        if (!obj.ContainsKey("success")) { failures.Add(label + ": missing success."); return false; }
        if (obj["success"]!.GetValue<bool>() != false) { failures.Add(label + ": success was not false."); return false; }
        if (!obj.ContainsKey("error")) { failures.Add(label + ": missing error."); return false; }

        var err = obj["error"]!.AsObject();
        var code = err["code"]?.GetValue<string>();
        if (code != "VALIDATION_ERROR")
        {
            failures.Add(label + ": error.code was '" + (code ?? "<null>") + "', expected VALIDATION_ERROR.");
        }
        if (string.IsNullOrWhiteSpace(err["message"]?.GetValue<string>()))
        {
            failures.Add(label + ": error.message was empty.");
        }

        var fields = err["fields"]?.AsObject();
        if (fields is null)
        {
            failures.Add(label + ": error.fields missing or not an object. Body=" + body);
            return false;
        }
        if (obj["meta"] is null || obj["meta"]!["requestId"] is null)
        {
            failures.Add(label + ": meta.requestId missing.");
        }
        return true;
    }

    private static string? TryGetErrorField(string body, string fieldName)
    {
        try
        {
            var node = JsonNode.Parse(body);
            return node?["error"]?[fieldName]?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    private static string TryGetErrorFieldKeys(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            var fields = node?["error"]?["fields"]?.AsObject();
            if (fields is null) return string.Empty;
            return string.Join(",", fields.Select(kv => kv.Key).OrderBy(k => k, StringComparer.Ordinal));
        }
        catch
        {
            return string.Empty;
        }
    }
}

/// <summary>
/// A sample DTO with a required property, used only by the MVC controllers.
/// </summary>
public sealed class ValidationSampleDto
{
    /// <summary>The name. Marked required so an empty body fails ModelState.</summary>
    [System.ComponentModel.DataAnnotations.Required]
    public string? Name { get; set; }
}

/// <summary>
/// MVC controller for the attribute path. No [ApiController] attribute, so the
/// [ApiPilotValidate] filter runs before the action.
/// </summary>
[ApiPilotValidate]
[Route("api/validate-attribute")]
public sealed class ValidateAttributeController : ControllerBase
{
    /// <summary>Returns a success response when the model state is valid.</summary>
    [HttpPost]
    public IActionResult Post([FromBody] ValidationSampleDto? dto)
    {
        return Ok(new { ok = true, name = dto?.Name });
    }
}

/// <summary>
/// MVC controller for the [ApiController] path. The AddApiPilotControllers
/// registration replaces the default ProblemDetails short-circuit with the
/// ApiPilot validation envelope.
/// </summary>
[ApiController]
[Route("api/validate-apicontroller")]
public sealed class ValidateApiControllerController : ControllerBase
{
    /// <summary>Returns a success response when the model state is valid.</summary>
    [HttpPost]
    public IActionResult Post([FromBody] ValidationSampleDto? dto)
    {
        return Ok(new { ok = true, name = dto?.Name });
    }
}