// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/FetchMetadataMiddlewareTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the Fetch Metadata middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test double)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Origin, ApiPilot.Security.Csrf,
//                ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Security.Tests (FakeLogger), Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : FetchMetadataMiddleware.cs
// -----------------------------------------------------------------------------

using System.Text.Json;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Tests.Origin;

/// <summary>
/// Contract tests for FetchMetadataMiddleware. Exercises the profile
/// matrix, the endpoint policy precedence, and the failure envelope.
/// </summary>
[TestClass]
public sealed class FetchMetadataMiddlewareTests
{
    private static FetchMetadataMiddleware Build(
        RequestDelegate? next = null,
        Action<FetchMetadataOptions>? configureFetch = null,
        Action<CsrfOptions>? configureCsrf = null,
        ICorrelationIdAccessor? accessor = null)
    {
        var fetchOpts = new FetchMetadataOptions();
        configureFetch?.Invoke(fetchOpts);
        var csrfOpts = new CsrfOptions();
        configureCsrf?.Invoke(csrfOpts);
        var logger = new FakeLogger<FetchMetadataMiddleware>();
        return new FetchMetadataMiddleware(
            next ?? (_ => Task.CompletedTask),
            Options.Create(fetchOpts),
            Options.Create(csrfOpts),
            logger,
            accessor);
    }

    private static DefaultHttpContext Context(string method = "POST")
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();
        var ctx = new DefaultHttpContext { RequestServices = provider };
        ctx.Request.Method = method;
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        return doc.RootElement.Clone();
    }

    /// <summary>A GET passes through regardless of profile.</summary>
    [Test]
    public async Task InvokeAsync_Get_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("GET");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>The default Off profile passes everything through.</summary>
    [Test]
    public async Task InvokeAsync_OffProfile_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Compat with a missing header passes.</summary>
    [Test]
    public async Task InvokeAsync_CompatMissingHeader_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Compat);
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Strict with a cross-site value returns 403.</summary>
    [Test]
    public async Task InvokeAsync_StrictCrossSite_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("FORBIDDEN",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>The failure envelope carries the correlation ID.</summary>
    [Test]
    public async Task InvokeAsync_Failure_CarriesCorrelationId()
    {
        var accessor = new FetchMetadataFixedAccessor("corr-fm-1");
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict,
            accessor: accessor);
        var ctx = Context("POST");
        ctx.TraceIdentifier = "raw-trace";
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        await middleware.InvokeAsync(ctx);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("corr-fm-1",
            json.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>A Skip policy bypasses the Fetch Metadata check.</summary>
    [Test]
    public async Task InvokeAsync_SkipPolicy_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachEndpoint(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A Require policy on GET enforces the check.</summary>
    [Test]
    public async Task InvokeAsync_RequirePolicyGet_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("GET");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachEndpoint(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>Strict with same-site passes.</summary>
    [Test]
    public async Task InvokeAsync_StrictSameSite_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "same-site";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A disallowed Sec-Fetch-Mode returns 403.</summary>
    [Test]
    public async Task InvokeAsync_StrictDisallowedMode_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "same-origin";
        ctx.Request.Headers["Sec-Fetch-Mode"] = "no-cors";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>A missing header with AllowMissingHeaders false returns 403.</summary>
    [Test]
    public async Task InvokeAsync_StrictMissingHeaderDisallowed_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f =>
            {
                f.Profile = FetchMetadataProfile.Strict;
                f.AllowMissingHeaders = false;
            });
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>Custom ProtectedMethods can add a safe method.</summary>
    [Test]
    public async Task InvokeAsync_CustomProtectedMethods_SafeMethodEnforced()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict,
            c => c.ProtectedMethods.Add("GET"));
        var ctx = Context("GET");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    private static void AttachEndpoint(HttpContext ctx, CsrfPolicy policy)
    {
        var collection = new List<object> { new CsrfEndpointMetadata(policy) };
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(collection),
            "test");
        ctx.SetEndpoint(endpoint);
    }
}

/// <summary>A Fixed ICorrelationIdAccessor for the Fetch Metadata tests.</summary>
internal sealed class FetchMetadataFixedAccessor : ICorrelationIdAccessor
{
    public FetchMetadataFixedAccessor(string? value) { RequestId = value; }

    public string? RequestId { get; set; }
}

