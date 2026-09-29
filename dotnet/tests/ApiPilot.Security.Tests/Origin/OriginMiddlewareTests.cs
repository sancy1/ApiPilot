// filepath: dotnet/tests/ApiPilot.Security.Tests/Origin/OriginMiddlewareTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the Origin policy middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test doubles)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Origin, ApiPilot.Security.Csrf,
//                ApiPilot.Security.Tests (FakeLogger), Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : OriginMiddleware.cs
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
/// Contract tests for OriginMiddleware. Exercises the method-by-policy
/// matrix, the missing-origin behavior, the allowed-origin match, and
/// the failure envelope.
/// </summary>
[TestClass]
public sealed class OriginMiddlewareTests
{
    private static OriginMiddleware Build(
        RequestDelegate? next = null,
        Action<OriginPolicyOptions>? configureOrigin = null,
        Action<CsrfOptions>? configureCsrf = null,
        ICorrelationIdAccessor? accessor = null)
    {
        var originOpts = new OriginPolicyOptions();
        configureOrigin?.Invoke(originOpts);
        var csrfOpts = new CsrfOptions();
        configureCsrf?.Invoke(csrfOpts);
        var logger = new FakeLogger<OriginMiddleware>();
        return new OriginMiddleware(
            next ?? (_ => Task.CompletedTask),
            Options.Create(originOpts),
            Options.Create(csrfOpts),
            logger,
            accessor);
    }

    private static DefaultHttpContext Context(string method = "POST", string scheme = "https", string host = "example.com", int? port = null)
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();
        var ctx = new DefaultHttpContext { RequestServices = provider };
        ctx.Request.Method = method;
        ctx.Request.Scheme = scheme;
        ctx.Request.Host = port.HasValue
            ? new HostString(host, port.Value)
            : new HostString(host);
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        return doc.RootElement.Clone();
    }

    /// <summary>A GET with no Origin passes because GET is safe.</summary>
    [Test]
    public async Task InvokeAsync_Get_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("GET");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A POST with no Origin passes with AllowMissingOrigin=true (default).</summary>
    [Test]
    public async Task InvokeAsync_PostMissingOrigin_Default_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A POST with no Origin returns 403 with AllowMissingOrigin=false.</summary>
    [Test]
    public async Task InvokeAsync_PostMissingOrigin_Disallowed_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            origin => origin.AllowMissingOrigin = false);
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CSRF_ORIGIN_REJECTED",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A POST with a matching same-origin passes.</summary>
    [Test]
    public async Task InvokeAsync_PostSameOrigin_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST", scheme: "https", host: "example.com");
        ctx.Request.Headers.Origin = "https://example.com";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A POST with a hostile Origin returns 403.</summary>
    [Test]
    public async Task InvokeAsync_PostHostileOrigin_Returns403()
    {
        var middleware = Build();
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>The failure envelope carries the correlation ID.</summary>
    [Test]
    public async Task InvokeAsync_Failure_CarriesCorrelationId()
    {
        var accessor = new OriginFixedAccessor("corr-origin-1");
        var middleware = Build(accessor: accessor);
        var ctx = Context("POST");
        ctx.TraceIdentifier = "raw-trace";
        ctx.Request.Headers.Origin = "https://evil.example";
        await middleware.InvokeAsync(ctx);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("corr-origin-1",
            json.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>A custom RejectionCode is honored.</summary>
    [Test]
    public async Task InvokeAsync_CustomRejectionCode_HonorsOverride()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            origin => origin.RejectionCode = "CUSTOM_ORIGIN_REJECTED");
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        await middleware.InvokeAsync(ctx);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CUSTOM_ORIGIN_REJECTED",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>A skip-policy endpoint bypasses the Origin check.</summary>
    [Test]
    public async Task InvokeAsync_SkipPolicy_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachEndpoint(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A require-policy GET with a hostile Origin returns 403.</summary>
    [Test]
    public async Task InvokeAsync_RequirePolicyGet_Returns403()
    {
        var middleware = Build();
        var ctx = Context("GET");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachEndpoint(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>Enabled=false passes through everything.</summary>
    [Test]
    public async Task InvokeAsync_Disabled_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            origin => origin.Enabled = false);
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>An allowed-origin entry matches and the request passes.</summary>
    [Test]
    public async Task InvokeAsync_AllowedOrigin_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            origin =>
            {
                origin.AllowSameOrigin = false;
                origin.AllowedOrigins.Add("https://trusted.example");
            });
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://trusted.example";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
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

/// <summary>A Fixed ICorrelationIdAccessor for the Origin middleware tests.</summary>
internal sealed class OriginFixedAccessor : ICorrelationIdAccessor
{
    public OriginFixedAccessor(string? value) { RequestId = value; }

    public string? RequestId { get; set; }
}

