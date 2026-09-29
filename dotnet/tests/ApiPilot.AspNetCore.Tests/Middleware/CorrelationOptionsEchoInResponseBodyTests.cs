// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Middleware/CorrelationOptionsEchoInResponseBodyTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Tests for the EchoInResponseBody wiring in the exception middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Configuration, ApiPilot.AspNetCore.ExceptionHandling,
//                ApiPilot.AspNetCore.Middleware, ApiPilot.Core.Metadata
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotExceptionMiddleware.cs, CorrelationOptions.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Configuration;
using ApiPilot.AspNetCore.ExceptionHandling;
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ApiPilot.AspNetCore.Tests.Middleware;

/// <summary>
/// Tests that verify the exception middleware honors
/// CorrelationOptions.EchoInResponseBody when building the response meta.
/// </summary>
[TestClass]
public sealed class CorrelationOptionsEchoInResponseBodyTests
{
    private static (ApiPilotExceptionMiddleware middleware, HttpContext context, MemoryStream body)
        Build(Action<CorrelationOptions>? configureCorrelation,
              ICorrelationIdAccessor? accessor)
    {
        var options = Options.Create(new ApiExceptionOptions());
        var mapper = new DefaultApiExceptionMapper(options);
        var logger = new FakeLogger<ApiPilotExceptionMiddleware>();

        var correlationOptions = new CorrelationOptions();
        configureCorrelation?.Invoke(correlationOptions);
        var wrapped = Options.Create(correlationOptions);

        var middleware = new ApiPilotExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            mapper, options, logger, accessor, wrapped);

        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = provider };
        var body = new MemoryStream();
        context.Response.Body = body;
        return (middleware, context, body);
    }

    /// <summary>EchoInResponseBody true populates requestId in the response meta.</summary>
    [Test]
    public async Task ExceptionMiddleware_EchoInResponseBodyTrue_RequestIdPopulated()
    {
        var (middleware, ctx, body) = Build(
            configureCorrelation: o => o.EchoInResponseBody = true,
            accessor: null);

        await middleware.InvokeAsync(ctx);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.NotNull(requestId);
        TestAssert.True(requestId!.Length > 0);
    }

    /// <summary>EchoInResponseBody false produces an empty requestId in the response meta.</summary>
    [Test]
    public async Task ExceptionMiddleware_EchoInResponseBodyFalse_RequestIdEmpty()
    {
        var (middleware, ctx, body) = Build(
            configureCorrelation: o => o.EchoInResponseBody = false,
            accessor: null);

        await middleware.InvokeAsync(ctx);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal(string.Empty, requestId);
    }

    /// <summary>Without correlation options the middleware falls back to TraceIdentifier.</summary>
    [Test]
    public async Task ExceptionMiddleware_NoCorrelationRegistration_FallsBackToTraceIdentifier()
    {
        var (middleware, ctx, body) = Build(
            configureCorrelation: null,
            accessor: null);

        ctx.TraceIdentifier = "fallback-trace-id";
        await middleware.InvokeAsync(ctx);

        body.Position = 0;
        var doc = await JsonDocument.ParseAsync(body);
        var requestId = doc.RootElement.GetProperty("meta").GetProperty("requestId").GetString();
        TestAssert.Equal("fallback-trace-id", requestId);
    }
}

