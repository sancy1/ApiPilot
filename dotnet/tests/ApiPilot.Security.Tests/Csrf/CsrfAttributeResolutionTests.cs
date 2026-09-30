// filepath: dotnet/tests/ApiPilot.Security.Tests/Csrf/CsrfAttributeResolutionTests.cs
// layer: Tests | package: ApiPilot.Security.Tests | since: v1.0.3
// purpose: Regression tests for the CSRF attribute metadata-resolution defect (F-65)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test classes)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.Security.Csrf, ApiPilot.Security.Origin,
//                ApiPilot.Security.Tests (FakeLogger),
//                ApiPilot.Security.Tests.Csrf (FixedCsrfService),
//                ApiPilot.AspNetCore.Serialization, Microsoft.AspNetCore.Http,
//                Microsoft.Extensions.DependencyInjection,
//                Microsoft.Extensions.Options
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : CsrfAttributes.cs, CsrfMiddleware.cs, OriginMiddleware.cs,
//                FetchMetadataMiddleware.cs
// -----------------------------------------------------------------------------
//
// F-65 regression boundary.
//
// The tests exercise two metadata shapes:
//
//   1. RAW ATTRIBUTE METADATA REPRODUCTION. The metadata collection is built
//      directly as
//          new EndpointMetadataCollection(new object[] { new ApiPilotSkipCsrfAttribute() })
//      and attached via HttpContext.SetEndpoint. This models the collection
//      the framework produces for .WithMetadata(new ApiPilotSkipCsrfAttribute()).
//      It does NOT claim to prove or disprove whether the framework invokes
//      IEndpointMetadataProvider.PopulateMetadata for that path.
//
//   2. CANONICAL RECORD BASELINE. The metadata collection is built as
//          new EndpointMetadataCollection(new object[] { new CsrfEndpointMetadata(policy) })
//      These tests are green before the production fix.
//
// SKIP TESTS FORCE A VALIDATION FAILURE. The Skip-only tests set
// FixedCsrfService.NextResult to a failure. Pass-through is therefore
// satisfiable ONLY when the Skip policy bypasses the middleware. With the
// attribute inert, the global POST policy enforces and the Skip test goes red.
//
// PRECEDENCE IS A PAIR. SkipThenRequire alone is not discriminating because
// global POST protection also enforces. The pair is: Skip-only passes through,
// Skip+Require enforces. The first arm goes red when the attribute is inert;
// the second stays green in both worlds.
//
// NO SHARED RESOLVER EXISTS TODAY. Each middleware has a private static
// ResolvePolicy with an identical body. This file does not introduce a
// test-only shared abstraction.
// -----------------------------------------------------------------------------

using System.Text.Json;
using ApiPilot.AspNetCore.Serialization;
using ApiPilot.Security.Csrf;
using ApiPilot.Security.Origin;
using ApiPilot.Security.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiPilot.Security.Tests.Csrf;

/// <summary>
/// Attribute-instance resolution tests for the CSRF middleware (F-65).
/// The attribute-instance tests are RED until the production fix.
/// The record baseline tests are GREEN and stay green.
/// </summary>
[TestClass]
public sealed class CsrfAttributeResolutionTests
{
    private static (CsrfMiddleware middleware, FixedCsrfService service)
        Build(RequestDelegate? next = null, Action<CsrfOptions>? configure = null)
    {
        var service = new FixedCsrfService();
        var opts = new CsrfOptions();
        configure?.Invoke(opts);
        var logger = new FakeLogger<CsrfMiddleware>();
        var middleware = new CsrfMiddleware(
            next ?? (_ => Task.CompletedTask),
            service,
            Options.Create(opts),
            logger);
        return (middleware, service);
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

    // -----------------------------------------------------------------------
    // CANONICAL RECORD BASELINE (green before the production fix)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Record Skip-only passes through POST even with a forced validation
    /// failure. Green because the record is honored.
    /// </summary>
    [Test]
    public async Task Record_SkipOnly_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, service) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        AttachRecord(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Record Require enforces on GET. Green before the production fix.
    /// </summary>
    [Test]
    public async Task Record_Require_Get_Enforces()
    {
        var (middleware, service) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("GET");
        AttachRecord(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
        var json = await ReadJsonAsync(ctx);
        TestAssert.Equal("CSRF_HEADER_MISSING",
            json.GetProperty("error").GetProperty("code").GetString());
    }

    // -----------------------------------------------------------------------
    // RAW ATTRIBUTE METADATA REPRODUCTION (red before the production fix)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Skip attribute on POST with a forced validation failure must pass
    /// through. RED until the production fix honors the attribute.
    /// </summary>
    [Test]
    public async Task AttributeInstance_SkipOnly_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, service) = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        AttachRawAttribute(ctx, new ApiPilotSkipCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Require attribute on GET must enforce. RED until the production fix.
    /// </summary>
    [Test]
    public async Task AttributeInstance_Require_Get_Enforces()
    {
        var (middleware, service) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("GET");
        AttachRawAttribute(ctx, new ApiPilotRequireCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>
    /// Skip+Require on POST must enforce. This arm stays green in both
    /// worlds; its pair with SkipOnly is what proves precedence.
    /// </summary>
    [Test]
    public async Task AttributeInstance_SkipThenRequire_Enforces()
    {
        var (middleware, service) = Build();
        service.NextResult = CsrfValidationResult.Failure(
            CsrfTokenParseResult.Malformed, "CSRF_HEADER_MISSING");
        var ctx = Context("POST");
        AttachRawAttribute(ctx, new ApiPilotSkipCsrfAttribute());
        AttachRawAttribute(ctx, new ApiPilotRequireCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    private static void AttachRecord(HttpContext ctx, CsrfPolicy policy)
    {
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                new object[] { new CsrfEndpointMetadata(policy) }),
            "record-baseline");
        ctx.SetEndpoint(endpoint);
    }

    private static void AttachRawAttribute(HttpContext ctx, object attribute)
    {
        var existing = ctx.GetEndpoint();
        var collection = new System.Collections.Generic.List<object>();
        if (existing is not null)
        {
            foreach (var m in existing.Metadata) { collection.Add(m); }
        }
        collection.Add(attribute);
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(collection),
            "raw-attribute-reproduction");
        ctx.SetEndpoint(endpoint);
    }
}

/// <summary>
/// Attribute-instance resolution tests for the Origin middleware (F-65).
/// Requires OriginPolicyOptions and a hostile Origin. RED until the fix.
/// </summary>
[TestClass]
public sealed class OriginAttributeResolutionTests
{
    private static OriginMiddleware Build(
        RequestDelegate? next = null,
        Action<OriginPolicyOptions>? configureOrigin = null,
        Action<CsrfOptions>? configureCsrf = null)
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
            logger);
    }

    private static DefaultHttpContext Context(string method = "POST")
    {
        var services = new ServiceCollection();
        services.AddApiPilotJson();
        var provider = services.BuildServiceProvider();
        var ctx = new DefaultHttpContext { RequestServices = provider };
        ctx.Request.Method = method;
        ctx.Request.Scheme = "https";
        ctx.Request.Host = new HostString("example.com");
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    /// <summary>
    /// Record Skip bypasses the Origin check on hostile Origin POST.
    /// Green before the fix.
    /// </summary>
    [Test]
    public async Task Record_Skip_Post_HostileOrigin_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachRecord(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Record Require enforces the Origin check on hostile Origin GET.
    /// Green before the fix.
    /// </summary>
    [Test]
    public async Task Record_Require_Get_HostileOrigin_Returns403()
    {
        var middleware = Build();
        var ctx = Context("GET");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachRecord(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>
    /// Skip attribute bypasses the Origin check on hostile Origin POST.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task AttributeInstance_Skip_Post_HostileOrigin_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = Context("POST");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachRawAttribute(ctx, new ApiPilotSkipCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Require attribute enforces on hostile Origin GET. RED until the fix.
    /// </summary>
    [Test]
    public async Task AttributeInstance_Require_Get_HostileOrigin_Returns403()
    {
        var middleware = Build();
        var ctx = Context("GET");
        ctx.Request.Headers.Origin = "https://evil.example";
        AttachRawAttribute(ctx, new ApiPilotRequireCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    private static void AttachRecord(HttpContext ctx, CsrfPolicy policy)
    {
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                new object[] { new CsrfEndpointMetadata(policy) }),
            "record-baseline");
        ctx.SetEndpoint(endpoint);
    }

    private static void AttachRawAttribute(HttpContext ctx, object attribute)
    {
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                new object[] { attribute }),
            "raw-attribute-reproduction");
        ctx.SetEndpoint(endpoint);
    }
}

/// <summary>
/// Attribute-instance resolution tests for the Fetch Metadata middleware
/// (F-65). Requires Strict profile and Sec-Fetch-Site. RED until the fix.
/// </summary>
[TestClass]
public sealed class FetchMetadataAttributeResolutionTests
{
    private static FetchMetadataMiddleware Build(
        RequestDelegate? next = null,
        Action<FetchMetadataOptions>? configureFetch = null,
        Action<CsrfOptions>? configureCsrf = null)
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
            logger);
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

    /// <summary>
    /// Record Skip bypasses the Fetch Metadata check on cross-site POST.
    /// Green before the fix.
    /// </summary>
    [Test]
    public async Task Record_Skip_Post_CrossSite_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachRecord(ctx, CsrfPolicy.Skip);
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Record Require enforces on cross-site GET under Strict.
    /// Green before the fix.
    /// </summary>
    [Test]
    public async Task Record_Require_Get_CrossSite_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("GET");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachRecord(ctx, CsrfPolicy.Require);
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    /// <summary>
    /// Skip attribute bypasses on cross-site POST under Strict.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task AttributeInstance_Skip_Post_CrossSite_PassesThrough()
    {
        var nextCalled = false;
        var middleware = Build(
            _ => { nextCalled = true; return Task.CompletedTask; },
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("POST");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachRawAttribute(ctx, new ApiPilotSkipCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.True(nextCalled);
    }

    /// <summary>
    /// Require attribute enforces on cross-site GET under Strict.
    /// RED until the fix.
    /// </summary>
    [Test]
    public async Task AttributeInstance_Require_Get_CrossSite_Returns403()
    {
        var middleware = Build(
            _ => Task.CompletedTask,
            f => f.Profile = FetchMetadataProfile.Strict);
        var ctx = Context("GET");
        ctx.Request.Headers["Sec-Fetch-Site"] = "cross-site";
        AttachRawAttribute(ctx, new ApiPilotRequireCsrfAttribute());
        await middleware.InvokeAsync(ctx);
        TestAssert.Equal(403, ctx.Response.StatusCode);
    }

    private static void AttachRecord(HttpContext ctx, CsrfPolicy policy)
    {
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                new object[] { new CsrfEndpointMetadata(policy) }),
            "record-baseline");
        ctx.SetEndpoint(endpoint);
    }

    private static void AttachRawAttribute(HttpContext ctx, object attribute)
    {
        var endpoint = new Microsoft.AspNetCore.Http.Endpoint(
            _ => Task.CompletedTask,
            new Microsoft.AspNetCore.Http.EndpointMetadataCollection(
                new object[] { attribute }),
            "raw-attribute-reproduction");
        ctx.SetEndpoint(endpoint);
    }
}

