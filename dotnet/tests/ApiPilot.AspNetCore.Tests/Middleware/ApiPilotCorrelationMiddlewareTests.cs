// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Middleware/ApiPilotCorrelationMiddlewareTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the correlation middleware with a fake logger
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test double generator)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.AspNetCore.Serialization,
//                ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotCorrelationMiddleware.cs, CorrelationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiPilot.AspNetCore.Tests.Middleware;

/// <summary>
/// Contract tests for ApiPilotCorrelationMiddleware. Uses an in-memory
/// DefaultHttpContext to verify reading, validation, generation, and
/// header echo without a live HTTP server.
/// </summary>
[TestClass]
public sealed class ApiPilotCorrelationMiddlewareTests
{
    private static (ApiPilotCorrelationMiddleware middleware, FakeLogger<ApiPilotCorrelationMiddleware> logger)
        Build(RequestDelegate next,
              Action<CorrelationOptions>? configure = null,
              ICorrelationIdGenerator? generator = null)
    {
        var concrete = new CorrelationOptions();
        configure?.Invoke(concrete);
        var gen = generator ?? new DefaultCorrelationIdGenerator();
        var logger = new FakeLogger<ApiPilotCorrelationMiddleware>();
        var wrapped = Options.Create(concrete);
        var middleware = new ApiPilotCorrelationMiddleware(next, wrapped, gen, logger);
        return (middleware, logger);
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

    /// <summary>No incoming header generates a new ID.</summary>
    [Test]
    public async Task InvokeAsync_NoIncomingHeader_GeneratesId()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.NotNull(stored);
        TestAssert.True(stored!.Length > 0);
    }

    /// <summary>A valid incoming header value is used.</summary>
    [Test]
    public async Task InvokeAsync_ValidIncomingHeader_UsesIncomingId()
    {
        var incoming = "abcdef12-3456-7890-abcd-ef1234567890";
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = incoming;
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.Equal(incoming, stored);
    }

    /// <summary>An invalid incoming header with the default Replace policy generates a new ID.</summary>
    [Test]
    public async Task InvokeAsync_InvalidIncomingHeader_DefaultPolicy_ReplacesId()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = "short";
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.NotNull(stored);
        TestAssert.NotEqual("short", stored);
    }

    /// <summary>An invalid incoming header with the Reject policy returns 400.</summary>
    [Test]
    public async Task InvokeAsync_InvalidIncomingHeader_RejectPolicy_Returns400()
    {
        var (middleware, _) = Build(
            _ => Task.CompletedTask,
            o => o.InvalidIncomingIdPolicy = CorrelationInvalidIdPolicy.Reject);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = "short";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(400, ctx.Response.StatusCode);
    }

    /// <summary>An invalid incoming header with the Reject policy logs a warning.</summary>
    [Test]
    public async Task InvokeAsync_InvalidIncomingHeader_RejectPolicy_LogsWarning()
    {
        var (middleware, logger) = Build(
            _ => Task.CompletedTask,
            o => o.InvalidIncomingIdPolicy = CorrelationInvalidIdPolicy.Reject);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = "short";
        await middleware.InvokeAsync(ctx);
        var found = false;
        foreach (var e in logger.Entries)
        {
            if (e.Level == LogLevel.Warning) { found = true; break; }
        }
        TestAssert.True(found);
    }

    /// <summary>An invalid incoming header with the UseAsIs policy keeps the invalid ID.</summary>
    [Test]
    public async Task InvokeAsync_InvalidIncomingHeader_UseAsIsPolicy_UsesInvalidId()
    {
        var (middleware, _) = Build(
            _ => Task.CompletedTask,
            o => o.InvalidIncomingIdPolicy = CorrelationInvalidIdPolicy.UseAsIs);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = "short";
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.Equal("short", stored);
    }

    /// <summary>EchoInResponseHeader true sets the response header.</summary>
    [Test]
    public async Task InvokeAsync_EchoInResponseHeaderTrue_SetsHeader()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        var headerValue = ctx.Response.Headers["X-Request-Id"].ToString();
        TestAssert.True(headerValue.Length > 0);
    }

    /// <summary>EchoInResponseHeader false does not set the response header.</summary>
    [Test]
    public async Task InvokeAsync_EchoInResponseHeaderFalse_DoesNotSetHeader()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask, o => o.EchoInResponseHeader = false);
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(0, ctx.Response.Headers["X-Request-Id"].Count);
    }

    /// <summary>ValidateIncoming false uses any incoming value.</summary>
    [Test]
    public async Task InvokeAsync_ValidateIncomingFalse_UsesIncomingIdUnchecked()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask, o => o.ValidateIncoming = false);
        var ctx = Context();
        ctx.Request.Headers["X-Request-Id"] = "short";
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.Equal("short", stored);
    }

    /// <summary>The effective ID is stored on HttpContext.Items.</summary>
    [Test]
    public async Task InvokeAsync_StoresIdInHttpContextItems()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.True(ctx.Items.ContainsKey(ApiPilotCorrelationMiddleware.CorrelationIdKey));
    }

    /// <summary>A custom header name is read.</summary>
    [Test]
    public async Task InvokeAsync_CustomHeaderName_ReadsFromCustomHeader()
    {
        var incoming = "customheader1234";
        var (middleware, _) = Build(_ => Task.CompletedTask, o => o.HeaderName = "X-Correlation-Id");
        var ctx = Context();
        ctx.Request.Headers["X-Correlation-Id"] = incoming;
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.Equal(incoming, stored);
    }

    /// <summary>A custom generator is used for generated IDs.</summary>
    [Test]
    public async Task InvokeAsync_CustomGenerator_UsesSuppliedGenerator()
    {
        var gen = new FixedGenerator("fixed-id-000001");
        var (middleware, _) = Build(_ => Task.CompletedTask, generator: gen);
        var ctx = Context();
        await middleware.InvokeAsync(ctx);
        var stored = ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] as string;
        TestAssert.Equal("fixed-id-000001", stored);
    }

    /// <summary>Null next is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullNext()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotCorrelationMiddleware(null!, Options.Create(new CorrelationOptions()),
                new DefaultCorrelationIdGenerator(),
                new FakeLogger<ApiPilotCorrelationMiddleware>()));
    }

    /// <summary>Null options is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotCorrelationMiddleware(_ => Task.CompletedTask, null!,
                new DefaultCorrelationIdGenerator(),
                new FakeLogger<ApiPilotCorrelationMiddleware>()));
    }

    /// <summary>Null generator is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullGenerator()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new ApiPilotCorrelationMiddleware(_ => Task.CompletedTask, Options.Create(new CorrelationOptions()),
                null!,
                new FakeLogger<ApiPilotCorrelationMiddleware>()));
    }
}

/// <summary>An ICorrelationIdGenerator that always returns the same value.</summary>
internal sealed class FixedGenerator : ICorrelationIdGenerator
{
    private readonly string _value;

    public FixedGenerator(string value) { _value = value; }

    public string NewId() => _value;
}

