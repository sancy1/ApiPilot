// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Middleware/ApiPilotContentNegotiationMiddlewareTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the content negotiation middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test double accessor)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Core.Configuration, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotContentNegotiationMiddleware.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Configuration;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Middleware;

/// <summary>
/// Contract tests for ApiPilotContentNegotiationMiddleware. Uses an
/// in-memory DefaultHttpContext to verify Accept and Content-Type handling
/// without a live HTTP server.
/// </summary>
[TestClass]
public sealed class ApiPilotContentNegotiationMiddlewareTests
{
    private static ApiPilotContentNegotiationMiddleware Build(
        RequestDelegate next,
        Action<ContentNegotiationOptions>? configure = null,
        ICorrelationIdAccessor? accessor = null)
    {
        var concrete = new ContentNegotiationOptions();
        configure?.Invoke(concrete);
        var logger = new FakeLogger<ApiPilotContentNegotiationMiddleware>();
        var wrapped = Options.Create(concrete);
        return new ApiPilotContentNegotiationMiddleware(next, wrapped, logger, accessor);
    }

    private static DefaultHttpContext Context()
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpContext ctx)
    {
        ctx.Response.Body.Position = 0;
        var doc = await JsonDocument.ParseAsync(ctx.Response.Body);
        return doc.RootElement.Clone();
    }

    /// <summary>Missing Accept header is acceptable per RFC 7231.</summary>
    [Test]
    public async Task InvokeAsync_NoAcceptHeader_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
        TestAssert.Equal(200, ctx.Response.StatusCode);
    }

    /// <summary>Accept: application/json passes.</summary>
    [Test]
    public async Task InvokeAsync_AcceptJson_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Headers.Accept = "application/json";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Accept: */* passes when wildcard is accepted.</summary>
    [Test]
    public async Task InvokeAsync_AcceptWildcard_PassesThroughWhenWildcardAccepted()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Headers.Accept = "*/*";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Accept: application/xml rejects with 406.</summary>
    [Test]
    public async Task InvokeAsync_AcceptXml_Rejects406()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Headers.Accept = "application/xml";
        await middleware.InvokeAsync(ctx);
        TestAssert.False(nextCalled);
        TestAssert.Equal(406, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(ctx);
        TestAssert.False(json.GetProperty("success").GetBoolean());
        TestAssert.Equal("NOT_ACCEPTABLE", json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>Accept: */* rejects when wildcard is disabled.</summary>
    [Test]
    public async Task InvokeAsync_AcceptWildcardDisabled_Rejects406()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; },
            o => o.AcceptWildcard = false);
        var ctx = Context();
        ctx.Request.Headers.Accept = "*/*";
        await middleware.InvokeAsync(ctx);
        TestAssert.False(nextCalled);
        TestAssert.Equal(406, ctx.Response.StatusCode);
    }

    /// <summary>Accept with quality parameter passes.</summary>
    [Test]
    public async Task InvokeAsync_AcceptJsonWithQuality_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Headers.Accept = "application/json;q=0.9";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A custom acceptable type passes when configured.</summary>
    [Test]
    public async Task InvokeAsync_AcceptCustomType_PassesThroughWhenConfigured()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; },
            o => o.AcceptableResponseMediaTypes.Add("application/vnd.api+json"));
        var ctx = Context();
        ctx.Request.Headers.Accept = "application/vnd.api+json";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>POST with no Content-Type rejects with 415.</summary>
    [Test]
    public async Task InvokeAsync_PostWithNoContentType_Rejects415()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Method = "POST";
        await middleware.InvokeAsync(ctx);
        TestAssert.False(nextCalled);
        TestAssert.Equal(415, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("UNSUPPORTED_MEDIA_TYPE", json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>POST with no Content-Type passes when AcceptMissingContentType is true.</summary>
    [Test]
    public async Task InvokeAsync_PostWithNoContentType_AcceptsWhenAcceptMissingContentTypeTrue()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; },
            o => o.AcceptMissingContentType = true);
        var ctx = Context();
        ctx.Request.Method = "POST";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>POST with application/json passes.</summary>
    [Test]
    public async Task InvokeAsync_PostWithJsonContentType_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/json";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>POST with application/xml rejects with 415.</summary>
    [Test]
    public async Task InvokeAsync_PostWithXmlContentType_Rejects415()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/xml";
        await middleware.InvokeAsync(ctx);
        TestAssert.False(nextCalled);
        TestAssert.Equal(415, ctx.Response.StatusCode);
    }

    /// <summary>POST with application/json; charset=utf-8 passes.</summary>
    [Test]
    public async Task InvokeAsync_PostWithJsonContentTypeWithCharset_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Method = "POST";
        ctx.Request.ContentType = "application/json; charset=utf-8";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>GET with no Content-Type passes (GET is not body-carrying by default).</summary>
    [Test]
    public async Task InvokeAsync_GetWithNoContentType_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context();
        ctx.Request.Method = "GET";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// When an ICorrelationIdAccessor is supplied, the 406 envelope uses
    /// the accessor value, not the raw TraceIdentifier.
    /// </summary>
    [Test]
    public async Task InvokeAsync_Reject406_UsesCorrelationAccessorId()
    {
        var accessor = new FixedAccessor("corr-id-12345");
        var middleware = Build(_ => Task.CompletedTask, accessor: accessor);
        var ctx = Context();
        ctx.TraceIdentifier = "raw-trace-id";
        ctx.Request.Headers.Accept = "application/xml";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(406, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(ctx);
        var requestId = json.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal("corr-id-12345", requestId);
    }

    /// <summary>Without a correlation accessor, the 406 envelope uses TraceIdentifier.</summary>
    [Test]
    public async Task InvokeAsync_Reject406_WithoutAccessor_UsesTraceIdentifier()
    {
        var middleware = Build(_ => Task.CompletedTask);
        var ctx = Context();
        ctx.TraceIdentifier = "trace-fallback-id";
        ctx.Request.Headers.Accept = "application/xml";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(406, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(ctx);
        var requestId = json.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal("trace-fallback-id", requestId);
    }

    /// <summary>Null next is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullNext()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotContentNegotiationMiddleware(null!,
                Options.Create(new ContentNegotiationOptions()),
                new FakeLogger<ApiPilotContentNegotiationMiddleware>()));
    }

    /// <summary>Null options is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotContentNegotiationMiddleware(_ => Task.CompletedTask,
                null!,
                new FakeLogger<ApiPilotContentNegotiationMiddleware>()));
    }

    /// <summary>Null logger is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullLogger()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotContentNegotiationMiddleware(_ => Task.CompletedTask,
                Options.Create(new ContentNegotiationOptions()),
                null!));
    }
}

/// <summary>A fixed ICorrelationIdAccessor for tests.</summary>
internal sealed class FixedAccessor : ICorrelationIdAccessor
{
    public FixedAccessor(string? value) { RequestId = value; }

    public string? RequestId { get; set; }
}

