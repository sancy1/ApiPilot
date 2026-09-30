// filepath: dotnet/verify/Verify.ApiPilot/Scenarios/06_ContentNegotiationScenario.cs
// layer:    Verification
// package:  Verify.ApiPilot
// purpose:  Scenario 06. Proves that a real consumer of the published
//           ApiPilot.AspNetCore 1.0.3 package can register the content
//           negotiation middleware, place it in the pipeline, and observe
//           the documented behaviors: 406 for an unacceptable Accept header,
//           415 for an unacceptable Content-Type on a body-carrying method,
//           the documented defaults, and the documented overrides.
// relates:  Uses ApiPilot.AspNetCore.Middleware.ApiPilotContentNegotiationExtensions,
//           ApiPilot.Core.Configuration.ContentNegotiationOptions,
//           ApiPilot.Core.Errors.ApiErrorCode.
// authority: the shipped lib/net10.0/ApiPilot.AspNetCore.xml,
//           lib/net10.0/ApiPilot.Core.xml, and the packaged README. Not the
//           source tree.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Results;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Configuration;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Verify.ApiPilot.Infrastructure;

namespace Verify.ApiPilot.Scenarios;

/// <summary>
/// Scenario 06. Content negotiation end to end over real HTTP. Ten sub-checks:
/// three defaults on the Accept header, one rejected Accept, one Accept override,
/// three defaults on Content-Type for a body-carrying method, one override for
/// AcceptMissingContentType, and one override for AcceptableResponseMediaTypes.
/// </summary>
public static class ContentNegotiationScenario
{
    /// <summary>The scenario name.</summary>
    public const string Name = "06_ContentNegotiation";

    /// <summary>Runs the scenario.</summary>
    public static async Task<ScenarioResult> RunAsync()
    {
        try
        {
            var failures = new List<string>();

            await CheckMissingAcceptIsAccepted(failures);
            await CheckAcceptJsonIsAccepted(failures);
            await CheckAcceptWildcardIsAccepted(failures);
            await CheckAcceptXmlIsRejectedWith406(failures);
            await CheckAcceptWildcardDisabledRejects(failures);
            await CheckPostMissingContentTypeIsRejectedWith415(failures);
            await CheckPostJsonIsAccepted(failures);
            await CheckPostXmlIsRejectedWith415(failures);
            await CheckAcceptMissingContentTypeOverride(failures);
            await CheckCustomAcceptableResponseMediaTypesOverride(failures);

            if (failures.Count > 0)
            {
                return ScenarioResult.Failed(Name, string.Join(" | ", failures));
            }

            return ScenarioResult.Passed(
                Name,
                "Content negotiation middleware rejects an unacceptable Accept with 406 NOT_ACCEPTABLE, rejects an unacceptable Content-Type on a body-carrying method with 415 UNSUPPORTED_MEDIA_TYPE, honours the documented defaults, and consumes the documented overrides.");
        }
        catch (Exception ex)
        {
            return ScenarioResult.Failed(Name, "Unhandled exception during scenario.", ex);
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 1: missing Accept is accepted by default
    // -------------------------------------------------------------------
    private static async Task CheckMissingAcceptIsAccepted(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        // No Accept header.
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("MissingAccept: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 2: Accept: application/json is accepted
    // -------------------------------------------------------------------
    private static async Task CheckAcceptJsonIsAccepted(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("AcceptJson: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 3: Accept: */* is accepted by default
    // -------------------------------------------------------------------
    private static async Task CheckAcceptWildcardIsAccepted(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("AcceptWildcard: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 4: Accept: application/xml is rejected with 406
    // -------------------------------------------------------------------
    private static async Task CheckAcceptXmlIsRejectedWith406(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Accept", "application/xml");
        using var response = await client.SendAsync(request);
        if ((int)response.StatusCode != 406)
        {
            failures.Add("AcceptXml: expected 406, got " + (int)response.StatusCode + ".");
        }
        var body = await response.Content.ReadAsStringAsync();
        AssertErrorEnvelope(failures, "AcceptXml", body, "NOT_ACCEPTABLE");
    }

    // -------------------------------------------------------------------
    // Sub-check 5: AcceptWildcard=false rejects */*
    // -------------------------------------------------------------------
    private static async Task CheckAcceptWildcardDisabledRejects(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotContentNegotiation(o => o.AcceptWildcard = false);
            },
            configurePipeline: app =>
            {
                app.UseApiPilotContentNegotiation();
                app.MapGet("/", (HttpContext context) => BuildSuccessResult(context));
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        using var response = await client.SendAsync(request);
        if ((int)response.StatusCode != 406)
        {
            failures.Add("WildcardDisabled: expected 406, got " + (int)response.StatusCode + ".");
        }
        var body = await response.Content.ReadAsStringAsync();
        AssertErrorEnvelope(failures, "WildcardDisabled", body, "NOT_ACCEPTABLE");
    }

    // -------------------------------------------------------------------
    // Sub-check 6: POST with no Content-Type is rejected with 415 by default
    // -------------------------------------------------------------------
    private static async Task CheckPostMissingContentTypeIsRejectedWith415(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        // No Content-Type. Body is present as bytes so the middleware sees a body-carrying method.
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
        request.Content.Headers.ContentType = null;
        using var response = await client.SendAsync(request);
        if ((int)response.StatusCode != 415)
        {
            failures.Add("PostMissingCT: expected 415, got " + (int)response.StatusCode + ".");
        }
        var body = await response.Content.ReadAsStringAsync();
        AssertErrorEnvelope(failures, "PostMissingCT", body, "UNSUPPORTED_MEDIA_TYPE");
    }

    // -------------------------------------------------------------------
    // Sub-check 7: POST with Content-Type application/json is accepted
    // -------------------------------------------------------------------
    private static async Task CheckPostJsonIsAccepted(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("PostJson: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 8: POST with Content-Type application/xml is rejected with 415
    // -------------------------------------------------------------------
    private static async Task CheckPostXmlIsRejectedWith415(List<string> failures)
    {
        await using var host = await StartDefaultHostAsync();
        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new StringContent("<x/>", Encoding.UTF8, "application/xml");
        using var response = await client.SendAsync(request);
        if ((int)response.StatusCode != 415)
        {
            failures.Add("PostXml: expected 415, got " + (int)response.StatusCode + ".");
        }
        var body = await response.Content.ReadAsStringAsync();
        AssertErrorEnvelope(failures, "PostXml", body, "UNSUPPORTED_MEDIA_TYPE");
    }

    // -------------------------------------------------------------------
    // Sub-check 9: AcceptMissingContentType=true accepts POST with no Content-Type
    // -------------------------------------------------------------------
    private static async Task CheckAcceptMissingContentTypeOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotContentNegotiation(o => o.AcceptMissingContentType = true);
            },
            configurePipeline: app =>
            {
                app.UseApiPilotContentNegotiation();
                app.MapPost("/", (HttpContext context) => BuildSuccessResult(context));
            });

        using var client = host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/", UriKind.Relative));
        request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("{}"));
        request.Content.Headers.ContentType = null;
        using var response = await client.SendAsync(request);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            failures.Add("AcceptMissingCT: expected 200, got " + (int)response.StatusCode + ".");
        }
    }

    // -------------------------------------------------------------------
    // Sub-check 10: Custom AcceptableResponseMediaTypes override is consumed
    // -------------------------------------------------------------------
    private static async Task CheckCustomAcceptableResponseMediaTypesOverride(List<string> failures)
    {
        await using var host = await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotContentNegotiation(o =>
                {
                    o.AcceptableResponseMediaTypes.Clear();
                    o.AcceptableResponseMediaTypes.Add("application/vnd.example+json");
                    o.AcceptWildcard = false;
                });
            },
            configurePipeline: app =>
            {
                app.UseApiPilotContentNegotiation();
                app.MapGet("/", (HttpContext context) => BuildSuccessResult(context));
            });

        using var client = host.CreateClient();

        // The custom type is accepted.
        using (var req = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative)))
        {
            req.Headers.TryAddWithoutValidation("Accept", "application/vnd.example+json");
            using var resp = await client.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                failures.Add("CustomAccept: custom media type expected 200, got " + (int)resp.StatusCode + ".");
            }
        }

        // The default type is now rejected because the list was cleared.
        using (var req = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative)))
        {
            req.Headers.TryAddWithoutValidation("Accept", "application/json");
            using var resp = await client.SendAsync(req);
            if ((int)resp.StatusCode != 406)
            {
                failures.Add("CustomAccept: former default media type expected 406, got " + (int)resp.StatusCode + ".");
            }
        }
    }

    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------
    private static async Task<InProcessHost> StartDefaultHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: services =>
            {
                services.AddApiPilotJson();
                services.AddApiPilotContentNegotiation();
            },
            configurePipeline: app =>
            {
                app.UseApiPilotContentNegotiation();
                app.MapGet("/", (HttpContext context) => BuildSuccessResult(context));
                app.MapPost("/", (HttpContext context) => BuildSuccessResult(context));
            });
    }

    private static IResult BuildSuccessResult(HttpContext context)
    {
        var meta = ResponseMetadata.Empty.WithRequestId(context.TraceIdentifier);
        return ApiResponseBuilder.Ok(new { ok = true }, meta, "ok").ToResult();
    }

    private static void AssertErrorEnvelope(List<string> failures, string label, string body, string expectedCode)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            failures.Add(label + ": empty body.");
            return;
        }
        JsonNode? node;
        try { node = JsonNode.Parse(body); }
        catch (Exception ex)
        {
            failures.Add(label + ": body is not valid JSON (" + ex.GetType().Name + ").");
            return;
        }
        if (node is null) { failures.Add(label + ": JsonNode.Parse returned null."); return; }
        var obj = node.AsObject();
        if (!obj.ContainsKey("success")) { failures.Add(label + ": missing success."); return; }
        var success = obj["success"]!.GetValue<bool>();
        if (success) failures.Add(label + ": success was true on an error response.");
        if (!obj.ContainsKey("error")) { failures.Add(label + ": missing error."); return; }
        var err = obj["error"]!.AsObject();
        if (!err.ContainsKey("code")) { failures.Add(label + ": missing error.code."); return; }
        var code = err["code"]!.GetValue<string>();
        if (code != expectedCode) failures.Add(label + ": error.code was '" + code + "', expected '" + expectedCode + "'.");
        if (err["message"] is null || string.IsNullOrWhiteSpace(err["message"]!.GetValue<string>()))
            failures.Add(label + ": error.message was empty.");
        if (obj["meta"] is null || obj["meta"]!["requestId"] is null)
            failures.Add(label + ": meta.requestId missing.");
        if (body.Contains("   at ", StringComparison.Ordinal)
            || body.Contains("StackTrace", StringComparison.Ordinal)
            || body.Contains("InnerException", StringComparison.Ordinal))
            failures.Add(label + ": body contains internal exception detail.");
    }
}