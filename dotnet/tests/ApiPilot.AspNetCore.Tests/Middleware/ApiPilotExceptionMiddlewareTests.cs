// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Middleware/ApiPilotExceptionMiddlewareTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for the exception middleware with a fake logger
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + fake logger)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.ExceptionHandling, ApiPilot.AspNetCore.Middleware,
//                ApiPilot.Core.Errors, Microsoft.AspNetCore.Http, Microsoft.Extensions.Logging
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotExceptionMiddleware.cs, ApiExceptionOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Middleware;

/// <summary>
/// Contract tests for ApiPilotExceptionMiddleware. Uses an in-memory
/// DefaultHttpContext and a fake logger to verify response writing and
/// log redaction without needing a live HTTP server.
/// </summary>
[TestClass]
public sealed class ApiPilotExceptionMiddlewareTests
{
    private static (ApiPilotExceptionMiddleware middleware, FakeLogger<ApiPilotExceptionMiddleware> logger)
        Build(RequestDelegate next, Action<ApiExceptionOptions>? configure = null)
    {
        var concrete = new ApiExceptionOptions();
        configure?.Invoke(concrete);
        var wrapped = Options.Create(concrete);
        var mapper = new DefaultApiExceptionMapper(wrapped);
        var logger = new FakeLogger<ApiPilotExceptionMiddleware>();
        var middleware = new ApiPilotExceptionMiddleware(next, mapper, wrapped, logger);
        return (middleware, logger);
    }

    private static (HttpContext context, MemoryStream body) Context()
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        httpContext.Response.Body = body;
        return (httpContext, body);
    }

    private static async Task<JsonElement> ReadJsonAsync(MemoryStream body)
    {
        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        return doc.RootElement.Clone();
    }

    /// <summary>A successful downstream does not change the response status.</summary>
    [Test]
    public async Task InvokeAsync_NoException_DoesNotWriteResponse()
    {
        var (middleware, _) = Build(_ => Task.CompletedTask);
        var (ctx, _) = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(200, ctx.Response.StatusCode);
    }

    /// <summary>A mapped exception produces the mapped status and error code.</summary>
    [Test]
    public async Task InvokeAsync_MappedException_WritesErrorResponse()
    {
        var (middleware, _) = Build(_ => throw new ArgumentException("bad"));
        var (ctx, body) = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(400, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(body);
        TestAssert.False(json.GetProperty("success").GetBoolean());
        TestAssert.Equal("VALIDATION_ERROR", json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>An unmapped exception produces INTERNAL_ERROR with 500.</summary>
    [Test]
    public async Task InvokeAsync_UnknownException_WritesInternalError()
    {
        var (middleware, _) = Build(_ => throw new InvalidCastException("boom"));
        var (ctx, body) = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(500, ctx.Response.StatusCode);

        var json = await ReadJsonAsync(body);
        TestAssert.Equal("INTERNAL_ERROR", json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>The fallback response does not leak the exception message.</summary>
    [Test]
    public async Task InvokeAsync_UnknownException_DoesNotLeakExceptionMessage()
    {
        var (middleware, _) = Build(_ => throw new InvalidCastException("secret-detail"));
        var (ctx, body) = Context();
        await middleware.InvokeAsync(ctx);

        var json = await ReadJsonAsync(body);
        var message = json.GetProperty("error").GetProperty("message").GetString()!;
        TestAssert.False(message.Contains("secret-detail"));
    }

    /// <summary>The fallback response does not leak the exception type name.</summary>
    [Test]
    public async Task InvokeAsync_UnknownException_DoesNotLeakExceptionType()
    {
        var (middleware, _) = Build(_ => throw new InvalidCastException("boom"));
        var (ctx, body) = Context();
        await middleware.InvokeAsync(ctx);

        var json = await ReadJsonAsync(body);
        var message = json.GetProperty("error").GetProperty("message").GetString()!;
        TestAssert.False(message.Contains("InvalidCastException"));
    }

    /// <summary>With logging of messages disabled, no message appears in the log.</summary>
    [Test]
    public async Task InvokeAsync_LogDoesNotContainExceptionMessageWhenDisabled()
    {
        var (middleware, logger) = Build(
            _ => throw new InvalidCastException("secret-detail"),
            o => { o.IncludeExceptionMessageInLogs = false; o.IncludeExceptionTypeInLogs = false; });
        var (ctx, _) = Context();
        await middleware.InvokeAsync(ctx);

        foreach (var entry in logger.Entries)
        {
            TestAssert.False(entry.Message.Contains("secret-detail"));
        }
    }

    /// <summary>With logging of types enabled, the type name appears in the log.</summary>
    [Test]
    public async Task InvokeAsync_LogContainsTypeWhenEnabled()
    {
        var (middleware, logger) = Build(
            _ => throw new InvalidCastException("boom"),
            o => { o.IncludeExceptionTypeInLogs = true; o.IncludeExceptionMessageInLogs = false; });
        var (ctx, _) = Context();
        await middleware.InvokeAsync(ctx);

        var found = false;
        foreach (var entry in logger.Entries)
        {
            if (entry.Message.Contains("InvalidCastException")) { found = true; break; }
        }
        TestAssert.True(found);
    }

    /// <summary>An OperationCanceledException with an aborted request propagates.</summary>
    [Test]
    public async Task InvokeAsync_OperationCanceled_WhenRequestAborted_Rethrows()
    {
        var (middleware, _) = Build(_ =>
        {
            throw new OperationCanceledException();
        });
        var (ctx, _) = Context();
        ctx.RequestAborted = new CancellationToken(canceled: true);

        await TestAssert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(ctx));
    }

    /// <summary>If the response has already started, the exception propagates.</summary>
    [Test]
    public async Task InvokeAsync_ResponseAlreadyStarted_Rethrows()
    {
        var (middleware, _) = Build(_ => throw new InvalidCastException("boom"));
        var (ctx, _) = Context();
        ctx.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());

        await TestAssert.ThrowsAsync<InvalidCastException>(() => middleware.InvokeAsync(ctx));
    }

    /// <summary>A KeyNotFoundException produces 404.</summary>
    [Test]
    public async Task InvokeAsync_MapsExceptionCodeToHttpStatus()
    {
        var (middleware, _) = Build(_ => throw new KeyNotFoundException());
        var (ctx, _) = Context();
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(404, ctx.Response.StatusCode);
    }
}

/// <summary>A minimal in-memory logger that captures entries for assertion.</summary>
internal sealed class FakeLogger<T> : ILogger<T>
{
    /// <summary>Captured log entries in the order they were written.</summary>
    public List<LogEntry> Entries { get; } = new();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
    }
}

/// <summary>A single captured log entry.</summary>
internal sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);

/// <summary>A response feature reporting HasStarted = true for tests.</summary>
internal sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
{
    /// <inheritdoc />
    public int StatusCode { get; set; } = 200;

    /// <inheritdoc />
    public string? ReasonPhrase { get; set; }

    /// <inheritdoc />
    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

    /// <inheritdoc />
    public Stream Body { get; set; } = Stream.Null;

    /// <inheritdoc />
    public bool HasStarted => true;

    /// <inheritdoc />
    public void OnStarting(Func<object, Task> callback, object state) { }

    /// <inheritdoc />
    public void OnCompleted(Func<object, Task> callback, object state) { }
}

