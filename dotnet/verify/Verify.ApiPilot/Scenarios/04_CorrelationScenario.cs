// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/04_CorrelationScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 04. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the correlation
//           middleware, place it in the pipeline, issue a real HTTP request,
//           and observe the documented behavior: the response carries the
//           correlation header, the header value equals meta.requestId in the
//           response body, and the header-name override is consumed.
// relates:  Uses ApiPilot.AspNetCore.Middleware.ApiPilotCorrelationExtensions,
//           ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions,
//           ApiPilot.Core.Metadata.ResponseMetadata,
//           ApiPilot.Core.Metadata.CorrelationOptions,
//           ApiPilot.Core.Responses.ApiResponseBuilder.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml and the
//           packaged README. Not the source tree.

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 04. Correlation end to end over real HTTP. Four sub-checks:
/// default header generated when absent; valid incoming header echoed;
/// invalid incoming header replaced under the default Replace policy;
/// override header name consumed.
/// </summary>
public static class CorrelationScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "04_Correlation";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckDefaultHeaderGeneratedWhenAbsent(failures);
            await CheckValidIncomingHeaderEchoed(failures);
            await CheckInvalidIncomingHeaderReplaced(failures);
            await CheckCustomHeaderNameOverrideConsumed(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "Correlation middleware placed in a real pipeline responds on the documented header, echoes valid incoming IDs, replaces invalid ones, and consumes the header-name override.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-checks
    // -------------------------------------------------------------------

    private static async Task CheckDefaultHeaderGeneratedWhenAbsent(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotCorrelation();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.MapGet("/", (HttpContext context) =>
                {
                    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
                    var meta = ResponseMetadata.Empty.WithRequestId(accessor?.RequestId ?? context.TraceIdentifier);
                    var payload = new { ok = true };
                    return ApiResponseBuilder.Ok(payload, meta, "correlation check").ToResult();
                });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.Headers.TryGetValues("X-Request-Id", out var values))
        {
            failures.Add("Default: response did not carry X-Request-Id.");
            return;
        }
        var headerValue = values.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            failures.Add("Default: X-Request-Id was empty.");
            return;
        }
        var metaId = TryReadMetaRequestId(body);
        if (metaId is null)
        {
            failures.Add("Default: response body did not contain meta.requestId. Body: " + body);
            return;
        }
        if (!string.Equals(headerValue, metaId, StringComparison.Ordinal))
        {
            failures.Add("Default: header value '" + headerValue + "' did not equal meta.requestId '" + metaId + "'.");
        }
    }

    private static async Task CheckValidIncomingHeaderEchoed(List<string> failures)
    {
        const string incoming = "abc-123_XYZ";

        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotCorrelation();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.MapGet("/", (HttpContext context) =>
                {
                    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
                    var meta = ResponseMetadata.Empty.WithRequestId(accessor?.RequestId ?? context.TraceIdentifier);
                    return ApiResponseBuilder.Ok(new { ok = true }, meta, "valid incoming").ToResult();
                });
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("X-Request-Id", incoming);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.Headers.TryGetValues("X-Request-Id", out var values))
        {
            failures.Add("ValidIncoming: response did not carry X-Request-Id.");
            return;
        }
        var headerValue = values.FirstOrDefault() ?? string.Empty;
        if (!string.Equals(headerValue, incoming, StringComparison.Ordinal))
        {
            failures.Add("ValidIncoming: header was '" + headerValue + "', expected '" + incoming + "'.");
        }
        var metaId = TryReadMetaRequestId(body);
        if (metaId is null)
        {
            failures.Add("ValidIncoming: meta.requestId missing.");
            return;
        }
        if (!string.Equals(metaId, incoming, StringComparison.Ordinal))
        {
            failures.Add("ValidIncoming: meta.requestId was '" + metaId + "', expected '" + incoming + "'.");
        }
    }

    private static async Task CheckInvalidIncomingHeaderReplaced(List<string> failures)
    {
        // A value that will not match the default pattern ^[A-Za-z0-9_-]{8,128}$.
        const string invalid = "no spaces allowed!";

        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotCorrelation();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.MapGet("/", (HttpContext context) =>
                {
                    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
                    var meta = ResponseMetadata.Empty.WithRequestId(accessor?.RequestId ?? context.TraceIdentifier);
                    return ApiResponseBuilder.Ok(new { ok = true }, meta, "invalid incoming").ToResult();
                });
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("X-Request-Id", invalid);
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.Headers.TryGetValues("X-Request-Id", out var values))
        {
            failures.Add("InvalidIncoming: response did not carry X-Request-Id.");
            return;
        }
        var headerValue = values.FirstOrDefault() ?? string.Empty;
        if (string.Equals(headerValue, invalid, StringComparison.Ordinal))
        {
            failures.Add("InvalidIncoming: header was the invalid value '" + invalid + "', expected a replacement.");
            return;
        }
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            failures.Add("InvalidIncoming: header was empty after replacement.");
        }
        var metaId = TryReadMetaRequestId(body);
        if (metaId is null)
        {
            failures.Add("InvalidIncoming: meta.requestId missing.");
            return;
        }
        if (!string.Equals(headerValue, metaId, StringComparison.Ordinal))
        {
            failures.Add("InvalidIncoming: header '" + headerValue + "' did not equal meta.requestId '" + metaId + "'.");
        }
    }

    private static async Task CheckCustomHeaderNameOverrideConsumed(List<string> failures)
    {
        const string customHeader = "X-Correlation-Id";

        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotCorrelation(o => o.HeaderName = customHeader);
            },
            configurePipeline: app =>
            {
                app.UseApiPilotCorrelation();
                app.MapGet("/", (HttpContext context) =>
                {
                    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
                    var meta = ResponseMetadata.Empty.WithRequestId(accessor?.RequestId ?? context.TraceIdentifier);
                    return ApiResponseBuilder.Ok(new { ok = true }, meta, "override").ToResult();
                });
            });

        using var client = host.CreateClient();
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.Headers.TryGetValues(customHeader, out var values))
        {
            failures.Add("Override: response did not carry " + customHeader + ".");
            return;
        }
        var headerValue = values.FirstOrDefault() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            failures.Add("Override: " + customHeader + " was empty.");
            return;
        }
        if (response.Headers.TryGetValues("X-Request-Id", out _))
        {
            failures.Add("Override: response unexpectedly still carried X-Request-Id.");
        }
        var metaId = TryReadMetaRequestId(body);
        if (metaId is null)
        {
            failures.Add("Override: meta.requestId missing.");
            return;
        }
        if (!string.Equals(headerValue, metaId, StringComparison.Ordinal))
        {
            failures.Add("Override: header '" + headerValue + "' did not equal meta.requestId '" + metaId + "'.");
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static string? TryReadMetaRequestId(string body)
    {
        try
        {
            var node = JsonNode.Parse(body);
            var meta = node?["meta"];
            var id = meta?["requestId"];
            return id?.GetValue<string>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}