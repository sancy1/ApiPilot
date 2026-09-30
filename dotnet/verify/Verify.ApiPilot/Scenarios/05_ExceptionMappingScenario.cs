// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/05_ExceptionMappingScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 05. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the exception
//           middleware, place it in the pipeline, throw mapped, unmapped,
//           and custom-mapped exceptions, and observe the documented
//           behaviors: the standard error envelope, a stable non-empty
//           error code, a safe non-empty message, no internal exception
//           detail, and the documented override surfaces (Mappings and
//           ErrorCodeToStatusMap) are consumed.
// relates:  Uses ApiPilot.AspNetCore.Middleware.ApiPilotExceptionExtensions,
//           ApiPilot.AspNetCore.Configuration.ApiExceptionOptions,
//           ApiPilot.AspNetCore.ExceptionHandling.KnownExceptionType,
//           ApiPilot.Core.Errors.ApiErrorCode,
//           ApiPilot.Core.Metadata.ResponseMetadata,
//           ApiPilot.Core.Responses.ApiResponseBuilder,
//           ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions.
//           Reuses the correlation setup already proven by scenario 04,
//           but asserts only that meta.requestId is present and non-empty.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml, the packaged
//           README, and a reflection probe of the shipped DLL. Not the
//           source tree.

using System.Net;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Errors;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 05. Exception mapping end to end over real HTTP. Five sub-checks,
/// each with its own host lifetime so no state leaks between checks.
/// </summary>
public static class ExceptionMappingScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "05_ExceptionMapping";

    /// <summary>The fourteen documented wire error codes, as they appear in the packaged README.</summary>
    private static readonly HashSet<string> DocumentedCodes = new(StringComparer.Ordinal)
    {
        "VALIDATION_ERROR",
        "AUTHENTICATION_REQUIRED",
        "FORBIDDEN",
        "CSRF_HEADER_MISSING",
        "CSRF_TOKEN_INVALID",
        "CSRF_TOKEN_EXPIRED",
        "CSRF_ORIGIN_REJECTED",
        "RESOURCE_NOT_FOUND",
        "CONFLICT",
        "RATE_LIMITED",
        "INTERNAL_ERROR",
        "CONFIGURATION_ERROR",
        "NOT_ACCEPTABLE",
        "UNSUPPORTED_MEDIA_TYPE",
    };

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();
            var observations = new List<string>();

            await CheckMappedException(failures, observations);
            await CheckUnmappedException(failures, observations);
            await CheckCustomMapping(failures);
            await CheckStatusOverride(failures);
            await CheckSuccessPathUnaffected(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            var suffix = observations.Count > 0 ? " Observations: " + string.Join("; ", observations) : string.Empty;
            return ScenarioResult.Passed(
                Name,
                "Exception middleware maps known, unknown, and custom-mapped exceptions to the standard error envelope; the status override is consumed; the success path is unaffected; no internal exception detail leaks." + suffix);
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: mapped exception (ArgumentNullException)
    // -------------------------------------------------------------------
    private static async Task CheckMappedException(List<string> failures, List<string> observations)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/", (HttpContext _) =>
                {
                    // CA2208 is scoped-disabled because the exception is
                    // deliberately constructed only so the middleware can map
                    // it by type. The parameter name is irrelevant to the
                    // mapping behavior and to the assertions.
#pragma warning disable CA2208
                    throw new ArgumentNullException("widget");
#pragma warning restore CA2208
                });
            });

        using var client = host.CreateClient();
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(new Uri("/", UriKind.Relative));
        }
        catch (Exception ex)
        {
            failures.Add("Mapped: HTTP request failed: " + ex.GetType().Name + ": " + ex.Message);
            return;
        }
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            var status = (int)response.StatusCode;

            var shape = TryReadErrorShape(body);
            if (shape is null)
            {
                failures.Add("Mapped: body did not parse as the documented error envelope. Status=" + status + " Body=" + body);
                return;
            }

            if (shape.Success != false) failures.Add("Mapped: success was not false.");
            if (string.IsNullOrWhiteSpace(shape.Code)) failures.Add("Mapped: error.code was empty.");
            if (string.IsNullOrWhiteSpace(shape.Message)) failures.Add("Mapped: error.message was empty.");
            if (string.IsNullOrWhiteSpace(shape.RequestId)) failures.Add("Mapped: meta.requestId was empty.");
            if (body.Contains("\"status\"", StringComparison.Ordinal)) failures.Add("Mapped: body contains a status key.");
            if (ContainsInternalDetail(body)) failures.Add("Mapped: body contains internal exception detail.");

            if (!string.IsNullOrWhiteSpace(shape.Code))
            {
                var documented = DocumentedCodes.Contains(shape.Code);
                observations.Add("ArgumentNullException -> code=" + shape.Code + ", status=" + status + (documented ? "" : " (NOT in the documented code table)"));
            }
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: unmapped exception. Assertions reflect what the shipped
    // artifact documents: the envelope shape, a stable non-empty code, a
    // safe non-empty message, and no internal detail. The specific code and
    // status are observed, not asserted, because the shipped artifact does
    // not document them. The observation is recorded as finding F-30.
    // -------------------------------------------------------------------
    private static async Task CheckUnmappedException(List<string> failures, List<string> observations)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/", (HttpContext _) => { throw new InvalidOperationException("should not leak"); });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        // Status is a valid error response: any 4xx or 5xx.
        if (status < 400 || status >= 600)
            failures.Add("Unmapped: HTTP status was " + status + ", expected a 4xx or 5xx error status.");

        var shape = TryReadErrorShape(body);
        if (shape is null) { failures.Add("Unmapped: body did not parse. Body=" + body); return; }

        if (shape.Success != false) failures.Add("Unmapped: success was not false.");
        if (string.IsNullOrWhiteSpace(shape.Code)) failures.Add("Unmapped: error.code was empty.");
        if (string.IsNullOrWhiteSpace(shape.Message)) failures.Add("Unmapped: error.message was empty.");
        if (string.IsNullOrWhiteSpace(shape.RequestId)) failures.Add("Unmapped: meta.requestId was empty.");

        if (shape.Message.Contains("should not leak", StringComparison.Ordinal))
            failures.Add("Unmapped: error.message revealed the exception message.");
        if (shape.Message.Contains("InvalidOperationException", StringComparison.Ordinal))
            failures.Add("Unmapped: error.message revealed the exception type name.");
        if (ContainsInternalDetail(body))
            failures.Add("Unmapped: body contains internal exception detail.");

        // Record the observed behavior. This is finding F-30.
        if (!string.IsNullOrWhiteSpace(shape.Code))
        {
            observations.Add("InvalidOperationException -> code=" + shape.Code + ", status=" + status);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: custom mapping (scenario-local exception -> CONFLICT)
    // -------------------------------------------------------------------
    private static async Task CheckCustomMapping(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions(o =>
                {
                    o.Mappings.Add(new KnownExceptionType(
                        typeof(ScenarioCustomException),
                        ApiErrorCode.Conflict,
                        "A conflict occurred.",
                        false));
                });
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/", (HttpContext _) => { throw new ScenarioCustomException("internal note"); });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        if (status != 409) failures.Add("CustomMapping: HTTP status was " + status + ", expected 409 for CONFLICT.");

        var shape = TryReadErrorShape(body);
        if (shape is null) { failures.Add("CustomMapping: body did not parse. Body=" + body); return; }
        if (shape.Code != "CONFLICT") failures.Add("CustomMapping: error.code was '" + shape.Code + "', expected 'CONFLICT'.");
        if (shape.Message != "A conflict occurred.") failures.Add("CustomMapping: error.message was '" + shape.Message + "', expected 'A conflict occurred.'.");
        if (shape.Message.Contains("internal note", StringComparison.Ordinal)) failures.Add("CustomMapping: error.message revealed the exception message.");
        if (string.IsNullOrWhiteSpace(shape.RequestId)) failures.Add("CustomMapping: meta.requestId was empty.");
    }

    // -------------------------------------------------------------------
    // Sub-check 4: status override (CONFLICT -> 503)
    // The key type is System.String, per the reflection probe of the shipped
    // DLL. The declared property type is IReadOnlyDictionary<string, int>,
    // so the whole-value setter is the correct consumer pattern.
    // InvalidOperationException maps to CONFLICT by default, so the override
    // of CONFLICT -> 503 isolates the test to the override mechanism.
    // -------------------------------------------------------------------
    private static async Task CheckStatusOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions(o =>
                {
                    o.ErrorCodeToStatusMap = new Dictionary<string, int>(StringComparer.Ordinal)
                    {
                        ["CONFLICT"] = 503,
                    };
                });
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/", (HttpContext _) => { throw new InvalidOperationException("no"); });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        if (status != 503) failures.Add("Override: HTTP status was " + status + ", expected 503 from the ErrorCodeToStatusMap override.");

        var shape = TryReadErrorShape(body);
        if (shape is null) { failures.Add("Override: body did not parse. Body=" + body); return; }
        if (shape.Code != "CONFLICT") failures.Add("Override: error.code was '" + shape.Code + "', expected 'CONFLICT'.");
        if (string.IsNullOrWhiteSpace(shape.RequestId)) failures.Add("Override: meta.requestId was empty.");
    }

    // -------------------------------------------------------------------
    // Sub-check 5: success path unaffected
    // -------------------------------------------------------------------
    private static async Task CheckSuccessPathUnaffected(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotCorrelation();
                services.AddApiPilotExceptions();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseApiPilotExceptions();
                app.MapGet("/", (HttpContext context) =>
                {
                    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
                    var meta = ResponseMetadata.Empty.WithRequestId(accessor?.RequestId ?? context.TraceIdentifier);
                    return ApiResponseBuilder.Ok(new { ok = true }, meta, "ok").ToResult();
                });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        if (status != 200) failures.Add("Success: HTTP status was " + status + ", expected 200.");

        var node = JsonNode.Parse(body)?.AsObject();
        if (node is null) { failures.Add("Success: body did not parse. Body=" + body); return; }
        if (!node.ContainsKey("success")) failures.Add("Success: body missing 'success'.");
        if (node["success"] is null || node["success"]!.GetValue<bool>() != true) failures.Add("Success: 'success' was not true.");
        if (node.ContainsKey("status")) failures.Add("Success: body contains a status key.");
        if (node["meta"] is null || node["meta"]!["requestId"] is null) failures.Add("Success: meta.requestId missing.");
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private sealed record ErrorShape(bool Success, string Code, string Message, string RequestId);

    private static ErrorShape? TryReadErrorShape(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            if (node is null) return null;
            var obj = node.AsObject();
            if (!obj.ContainsKey("success")) return null;

            var success = obj["success"]!.GetValue<bool>();
            var err = obj["error"]?.AsObject();
            var meta = obj["meta"]?.AsObject();

            return new ErrorShape(
                Success: success,
                Code: err?["code"]?.GetValue<string>() ?? string.Empty,
                Message: err?["message"]?.GetValue<string>() ?? string.Empty,
                RequestId: meta?["requestId"]?.GetValue<string>() ?? string.Empty);
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsInternalDetail(string body)
    {
        return body.Contains("   at ", StringComparison.Ordinal)
            || body.Contains("StackTrace", StringComparison.Ordinal)
            || body.Contains("InnerException", StringComparison.Ordinal)
            || body.Contains("System.InvalidOperationException", StringComparison.Ordinal)
            || body.Contains("System.ArgumentNullException", StringComparison.Ordinal)
            || body.Contains("Verify.ApiPilot.Scenarios.ScenarioCustomException", StringComparison.Ordinal);
    }

    /// <summary>
    /// A scenario-local exception type used only for the custom-mapping
    /// sub-check. It is deliberately not one of the framework exceptions in
    /// the default mapping table, so any successful mapping observed for this
    /// type must come from the custom entry the scenario registered.
    /// </summary>
    private sealed class ScenarioCustomException : Exception
    {
        public ScenarioCustomException(string message) : base(message) { }
    }
}