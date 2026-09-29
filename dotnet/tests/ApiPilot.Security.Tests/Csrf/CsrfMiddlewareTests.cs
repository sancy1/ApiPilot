// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfMiddlewareTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v0.3.0
// purpose: Contract tests for the CSRF protection middleware
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class + test doubles)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, Microsoft.AspNetCore.Http,
//                Microsoft.Extensions.Logging
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfMiddleware.cs
// -----------------------------------------------------------------------------

using System.Text.Json;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Core.Metadata;
using ApiPilot.Security.Csrf;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApiPilot.Security.Tests;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Contract tests for the CSRF protection middleware. Exercises the
/// method-by-policy matrix, the precedence rule, the exemptions, and the
/// failure envelope. Uses a FixedCsrfService so the middleware can be
/// tested without a real signer or Data Protection.
/// </summary>
[TestClass]
public sealed class CsrfMiddlewareTests
{
    private static (CsrfMiddleware middleware, FixedCsrfService service, FakeLogger<CsrfMiddleware> logger)
        Build(
            RequestDelegate? next = null,
            Action<CsrfOptions>? configure = null,
            ICorrelationIdAccessor? accessor = null)
    {
        var service = new FixedCsrfService();
        var opts = new CsrfOptions();
        configure?.Invoke(opts);
        var logger = new FakeLogger<CsrfMiddleware>();
        var middleware = new CsrfMiddleware(
            next ?? (_ => Task.CompletedTask),
            service,
            Options.Create(opts),
            logger,
            accessor);
        return (middleware, service, logger);
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

    // --- Method x no policy ---

    /// <summary>GET with no policy and no endpoint metadata passes through.</summary>
    [Test]
    public async Task InvokeAsync_Get_NoPolicy_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("GET");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>POST with no policy and no header returns 403 CSRF_HEADER_MISSING.</summary>
    [Test]
    public async Task InvokeAsync_Post_NoPolicy_NoHeader_Returns403()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CSRF_HEADER_MISSING",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>POST with a valid token passes through.</summary>
    [Test]
    public async Task InvokeAsync_Post_NoPolicy_ValidToken_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, service, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        service.NextResult = CsrfValidationResult.Success();
        var ctx = Context("POST");
        ctx.Request.Headers["X-CSRF-TOKEN"] = "any-token";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>POST with an invalid token returns 403 CSRF_TOKEN_INVALID.</summary>
    [Test]
    public async Task InvokeAsync_Post_NoPolicy_InvalidToken_Returns403()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.InvalidSignature, "CSRF_TOKEN_INVALID");
        var ctx = Context("POST");
        ctx.Request.Headers["X-CSRF-TOKEN"] = "bad";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CSRF_TOKEN_INVALID",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    /// <summary>POST with an expired token returns 403 CSRF_TOKEN_EXPIRED.</summary>
    [Test]
    public async Task InvokeAsync_Post_NoPolicy_ExpiredToken_Returns403()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Expired, "CSRF_TOKEN_EXPIRED");
        var ctx = Context("POST");
        ctx.Request.Headers["X-CSRF-TOKEN"] = "old";
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CSRF_TOKEN_EXPIRED",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    // --- Skip attribute ---

    /// <summary>Skip policy bypasses the middleware on POST.</summary>
    [Test]
    public async Task InvokeAsync_Post_SkipPolicy_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        AttachEndpoint(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Skip policy bypasses the middleware on DELETE.</summary>
    [Test]
    public async Task InvokeAsync_Delete_SkipPolicy_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("DELETE");
        AttachEndpoint(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    // --- Require attribute ---

    /// <summary>Require policy on GET with a valid token passes.</summary>
    [Test]
    public async Task InvokeAsync_Get_RequirePolicy_ValidToken_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, service, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        service.NextResult = CsrfValidationResult.Success();
        var ctx = Context("GET");
        ctx.Request.Headers["X-CSRF-TOKEN"] = "any-token";
        AttachEndpoint(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>Require policy on GET with no header returns 403.</summary>
    [Test]
    public async Task InvokeAsync_Get_RequirePolicy_NoHeader_Returns403()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("GET");
        AttachEndpoint(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    // --- Precedence Require > Skip ---

    /// <summary>Require beats Skip when both are attached to the endpoint.</summary>
    [Test]
    public async Task InvokeAsync_Post_BothPolicies_RequireWins()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        // Attach Skip first, then Require. Require wins.
        AttachEndpoint(ctx, CsrfPolicy.Skip);
        AttachEndpoint(ctx, CsrfPolicy.Require, append: true);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    // --- Exemptions ---

    /// <summary>The bootstrap path is exempt from enforcement.</summary>
    [Test]
    public async Task InvokeAsync_Post_BootstrapPath_IsExempt()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        ctx.Request.Path = "/api/csrf";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A configured exempt path is exempt.</summary>
    [Test]
    public async Task InvokeAsync_Post_ConfiguredExemptPath_IsExempt()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            opts => opts.ExemptPaths.Add(new PathString("/public")));
        var ctx = Context("POST");
        ctx.Request.Path = "/public/webhook";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>The exempt predicate returning true exempts the request.</summary>
    [Test]
    public async Task InvokeAsync_Post_ExemptPredicateTrue_IsExempt()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            opts => opts.ExemptPredicate = _ => true);
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>The exempt predicate returning false leaves the request enforced.</summary>
    [Test]
    public async Task InvokeAsync_Post_ExemptPredicateFalse_IsEnforced()
    {
        var (middleware, service, _) = Build(
            _ => Task.CompletedTask,
            opts => opts.ExemptPredicate = _ => false);
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    // --- Custom header ---

    /// <summary>A custom header name is read from the configured header.</summary>
    [Test]
    public async Task InvokeAsync_CustomHeaderName_ReadsFromCustomHeader()
    {
        var nextCalled = false;
        var (middleware, service, _) = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            opts => opts.HeaderName = "X-My-Csrf");
        service.NextResult = CsrfValidationResult.Success();
        service.ExpectedHeaderValue = "value-from-custom-header";
        var ctx = Context("POST");
        ctx.Request.Headers["X-My-Csrf"] = "value-from-custom-header";
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
        TestAssert.Equal("value-from-custom-header", service.LastHeaderValue);
    }

    // --- Envelope correlation ---

    /// <summary>The failure envelope carries the correlation ID from the accessor.</summary>
    [Test]
    public async Task InvokeAsync_Failure_EnvelopeCarriesCorrelationId()
    {
        var accessor = new FixedCsrfCorrelationAccessor("corr-1234");
        var (middleware, service, _) = Build(accessor: accessor);
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        ctx.TraceIdentifier = "raw-trace";
        await middleware.InvokeAsync(ctx);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("corr-1234",
            json.GetProperty("meta").GetProperty("requestId").GetString());
    }

    /// <summary>Without an accessor the envelope uses the trace identifier.</summary>
    [Test]
    public async Task InvokeAsync_Failure_WithoutAccessor_UsesTraceIdentifier()
    {
        var (middleware, service, _) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        ctx.TraceIdentifier = "trace-fallback";
        await middleware.InvokeAsync(ctx);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("trace-fallback",
            json.GetProperty("meta").GetProperty("requestId").GetString());
    }

    // --- Constructor ---

    /// <summary>Null next is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullNext()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfMiddleware(null!, new FixedCsrfService(),
                Options.Create(new CsrfOptions()), new FakeLogger<CsrfMiddleware>()));
    }

    /// <summary>Null service is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullService()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfMiddleware(_ => Task.CompletedTask, null!,
                Options.Create(new CsrfOptions()), new FakeLogger<CsrfMiddleware>()));
    }

    /// <summary>Null options is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullOptions()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfMiddleware(_ => Task.CompletedTask, new FixedCsrfService(),
                null!, new FakeLogger<CsrfMiddleware>()));
    }

    /// <summary>Null logger is rejected.</summary>
    [Test]
    public void Constructor_RejectsNullLogger()
    {
        TestAssert.Throws<ArgumentNullException>(() =>
            new CsrfMiddleware(_ => Task.CompletedTask, new FixedCsrfService(),
                Options.Create(new CsrfOptions()), null!));
    }

    // --- Custom ProtectedMethods ---

    /// <summary>A custom ProtectedMethods set only enforces configured methods.</summary>
    [Test]
    public async Task InvokeAsync_CustomProtectedMethods_OnlyEnforcesConfigured()
    {
        var nextCalled = false;
        var (middleware, _, _) = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            opts =>
            {
                opts.ProtectedMethods.Clear();
                opts.ProtectedMethods.Add("POST");
            });
        var ctx = Context("PUT");
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>A custom ProtectedMethods set can add a safe method.</summary>
    [Test]
    public async Task InvokeAsync_CustomProtectedMethods_SafeMethodEnforced()
    {
        var (middleware, service, _) = Build(
            _ => Task.CompletedTask,
            opts => opts.ProtectedMethods.Add("GET"));
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("GET");
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    private static void AttachEndpoint(HttpContext ctx, CsrfPolicy policy, bool append = false)
    {
        var existing = ctx.GetEndpoint();
        var builder = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                append && existing is not null ? existing.Metadata.ToArray() : Array.Empty<object>())
            { },
            "test-endpoint");
        var collection = new List<object>();
        if (append && existing is not null)
        {
            foreach (var m in existing.Metadata) { collection.Add(m); }
        }
        collection.Add(new CsrfEndpointMetadata(policy));
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(collection),
            "test");
        ctx.SetEndpoint(endpoint);
    }
}

/// <summary>A Fixed ICsrfService that returns a configurable result.</summary>
internal sealed class FixedCsrfService : ICsrfService
{
    public CsrfValidationResult NextResult { get; set; } = CsrfValidationResult.Success();

    public string? ExpectedHeaderValue { get; set; }

    public string? LastHeaderValue { get; private set; }

    public Task<CsrfToken?> IssueAsync(HttpContext context, CancellationToken cancellationToken = default)
        => Task.FromResult<CsrfToken?>(CsrfToken.From("fixed-token"));

    public Task<CsrfValidationResult> ValidateAsync(
        HttpContext context,
        string? rawHeaderValue,
        CancellationToken cancellationToken = default)
    {
        LastHeaderValue = rawHeaderValue;
        return Task.FromResult(NextResult);
    }

    public Task<CsrfToken?> RotateAsync(HttpContext context, CancellationToken cancellationToken = default)
        => Task.FromResult<CsrfToken?>(CsrfToken.From("fixed-token"));
}

/// <summary>A Fixed ICorrelationIdAccessor for tests.</summary>
internal sealed class FixedCsrfCorrelationAccessor : ICorrelationIdAccessor
{
    public FixedCsrfCorrelationAccessor(string? value) { RequestId = value; }

    public string? RequestId { get; set; }
}

