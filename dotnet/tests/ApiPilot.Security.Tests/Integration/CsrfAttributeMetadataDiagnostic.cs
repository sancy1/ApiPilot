// filepath: dotnet/tests/ApiPilot.Security.Tests/Integration/CsrfAttributeMetadataDiagnostic.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v1.0.3
// purpose: Diagnostic capture of endpoint metadata for the F-65 attribute paths
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (diagnostic test class + diagnostic endpoints)
//   Depends on : InProcessHost, TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.Builder,
//                Microsoft.AspNetCore.Http, Microsoft.AspNetCore.Mvc,
//                Microsoft.Extensions.DependencyInjection, System.Net.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfAttributes.cs, CsrfMiddleware.cs
// -----------------------------------------------------------------------------
//
// DIAGNOSTIC ONLY. Asserts only that the handler executed and the evidence
// carries the required field markers. Does NOT assert whether
// CsrfEndpointMetadata is present or absent; that is the observation.
//
// GET routes are used so the CSRF middleware passes the request through and
// the handlers execute.
//
// EVIDENCE CHANNEL. Each diagnostic writes its own file, atomically, with
// File.WriteAllText. No delete, no append, no race.
//   artifacts/diagnostic-metadata-withmetadata.txt
//   artifacts/diagnostic-metadata-decorated.txt
//   artifacts/diagnostic-metadata-controller.txt
// A write failure is rethrown so the harness reports it instead of
// swallowing it.
// -----------------------------------------------------------------------------

using System.Text;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiPilot.Security.Tests.Integration;

/// <summary>
/// Diagnostic-only test class. Each diagnostic writes one evidence file to
/// the repository artifacts directory with File.WriteAllText.
/// </summary>
[TestClass]
public sealed class CsrfAttributeMetadataDiagnostic
{
    private const string PreAuthBinding = "csrf-diagnostic-binding";
    private const string ArtifactsDir = @"C:\Users\HP\Desktop\ApiPilot\artifacts";

    private static string BuildEvidence(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var sb = new StringBuilder();
        sb.AppendLine("EndpointType: " + (endpoint is null ? "(null)" : endpoint.GetType().FullName ?? "(unnamed)"));
        if (endpoint is null)
        {
            sb.AppendLine("MetadataCount: 0");
            sb.AppendLine("HasSkipAttribute: False");
            sb.AppendLine("HasRequireAttribute: False");
            sb.AppendLine("HasCsrfEndpointMetadata: False");
            return sb.ToString();
        }

        var items = new System.Collections.Generic.List<object>();
        foreach (var item in endpoint.Metadata) { items.Add(item); }
        sb.AppendLine("MetadataCount: " + items.Count);
        foreach (var item in items)
        {
            sb.AppendLine("MetadataType: " + (item.GetType().FullName ?? "(unnamed)"));
        }

        var hasSkip = false;
        var hasRequire = false;
        var hasRecord = false;
        foreach (var item in items)
        {
            if (item is ApiPilotSkipCsrfAttribute) { hasSkip = true; }
            if (item is ApiPilotRequireCsrfAttribute) { hasRequire = true; }
            if (item is CsrfEndpointMetadata) { hasRecord = true; }
        }
        sb.AppendLine("HasSkipAttribute: " + hasSkip);
        sb.AppendLine("HasRequireAttribute: " + hasRequire);
        sb.AppendLine("HasCsrfEndpointMetadata: " + hasRecord);
        return sb.ToString();
    }

    private static async Task<InProcessHost> StartHostAsync(
        Action<WebApplication> mapEndpoints)
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => PreAuthBinding;
                });
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                mapEndpoints(app);
            });
    }

    private static async Task<InProcessHost> StartControllerHostAsync()
    {
        return await InProcessHost.StartAsync(
            configureServices: builder =>
            {
                builder.Services.AddDataProtection();
                builder.Services.AddApiPilotJson();
                builder.Services.AddApiPilotCorrelation();
                builder.Services.AddApiPilotCsrf(o =>
                {
                    o.PreAuthBindingSource = _ => PreAuthBinding;
                });
                builder.Services.AddControllers()
                    .AddApplicationPart(typeof(CsrfAttributeMetadataDiagnostic).Assembly);
            },
            configureApp: app =>
            {
                app.UseApiPilotCorrelation();
                app.UseRouting();
                app.UseApiPilotCsrfProtection();
                app.MapApiPilotCsrf();
                app.MapControllers();
            });
    }

    private static void WriteEvidenceFile(string fileName, string label, string body)
    {
        try
        {
            if (!System.IO.Directory.Exists(ArtifactsDir))
            {
                System.IO.Directory.CreateDirectory(ArtifactsDir);
            }
            var path = System.IO.Path.Combine(ArtifactsDir, fileName);
            var content = "=== DIAGNOSTIC: " + label + " ===" + Environment.NewLine +
                body +
                "=== END DIAGNOSTIC: " + label + " ===" + Environment.NewLine;
            System.IO.File.WriteAllText(path, content);
            Console.WriteLine("DIAGNOSTIC WROTE: " + path);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Diagnostic evidence write failed for " + label + ": " + ex.Message, ex);
        }
    }

    private static void AssertEvidenceShape(string body)
    {
        TestAssert.True(body.Length > 0);
        TestAssert.True(body.Contains("EndpointType:"));
        TestAssert.True(body.Contains("MetadataCount:"));
        TestAssert.True(body.Contains("HasSkipAttribute:"));
        TestAssert.True(body.Contains("HasRequireAttribute:"));
        TestAssert.True(body.Contains("HasCsrfEndpointMetadata:"));
    }

    /// <summary>
    /// Path A: minimal API .WithMetadata(new ApiPilotSkipCsrfAttribute()).
    /// </summary>
    [Test]
    public async Task Diagnostic_MinimalApi_WithMetadata_ReportsMetadata()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/diag-withmetadata", (HttpContext context) => BuildEvidence(context))
               .WithMetadata(new ApiPilotSkipCsrfAttribute()));

        var response = await host.Client.GetAsync("/diag-withmetadata");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        WriteEvidenceFile("diagnostic-metadata-withmetadata.txt", "minimal API WithMetadata", body);
        AssertEvidenceShape(body);
    }

    /// <summary>
    /// Path B: decorated minimal-API handler with [ApiPilotSkipCsrf].
    /// </summary>
    [Test]
    public async Task Diagnostic_DecoratedHandler_ReportsMetadata()
    {
        await using var host = await StartHostAsync(app =>
            app.MapGet("/diag-decorated", DiagnosticDecoratedHandler));

        var response = await host.Client.GetAsync("/diag-decorated");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        WriteEvidenceFile("diagnostic-metadata-decorated.txt", "decorated handler", body);
        AssertEvidenceShape(body);
    }

    [ApiPilotSkipCsrf]
    private static string DiagnosticDecoratedHandler(HttpContext context) => BuildEvidence(context);

    /// <summary>
    /// Path C: controller action with [ApiPilotSkipCsrf].
    /// </summary>
    [Test]
    public async Task Diagnostic_Controller_ReportsMetadata()
    {
        await using var host = await StartControllerHostAsync();

        var response = await host.Client.GetAsync("/api/diag-controller");
        TestAssert.Equal(200, (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        WriteEvidenceFile("diagnostic-metadata-controller.txt", "controller action", body);
        AssertEvidenceShape(body);
    }
}

/// <summary>A controller whose diagnostic GET action reports its endpoint metadata.</summary>
[ApiController]
[Route("api")]
public sealed class CsrfMetadataDiagnosticController : ControllerBase
{
    /// <summary>Returns the endpoint metadata evidence for this action.</summary>
    [HttpGet("diag-controller")]
    [ApiPilotSkipCsrf]
    public IActionResult Report()
    {
        var endpoint = HttpContext.GetEndpoint();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("EndpointType: " + (endpoint is null ? "(null)" : endpoint.GetType().FullName ?? "(unnamed)"));
        if (endpoint is null)
        {
            sb.AppendLine("MetadataCount: 0");
            sb.AppendLine("HasSkipAttribute: False");
            sb.AppendLine("HasRequireAttribute: False");
            sb.AppendLine("HasCsrfEndpointMetadata: False");
            return Content(sb.ToString(), "text/plain");
        }
        var items = new System.Collections.Generic.List<object>();
        foreach (var item in endpoint.Metadata) { items.Add(item); }
        sb.AppendLine("MetadataCount: " + items.Count);
        foreach (var item in items)
        {
            sb.AppendLine("MetadataType: " + (item.GetType().FullName ?? "(unnamed)"));
        }
        var hasSkip = false;
        var hasRequire = false;
        var hasRecord = false;
        foreach (var item in items)
        {
            if (item is ApiPilotSkipCsrfAttribute) { hasSkip = true; }
            if (item is ApiPilotRequireCsrfAttribute) { hasRequire = true; }
            if (item is CsrfEndpointMetadata) { hasRecord = true; }
        }
        sb.AppendLine("HasSkipAttribute: " + hasSkip);
        sb.AppendLine("HasRequireAttribute: " + hasRequire);
        sb.AppendLine("HasCsrfEndpointMetadata: " + hasRecord);
        return Content(sb.ToString(), "text/plain");
    }
}

