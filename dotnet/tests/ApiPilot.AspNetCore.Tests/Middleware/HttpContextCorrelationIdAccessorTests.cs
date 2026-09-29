// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/Middleware/HttpContextCorrelationIdAccessorTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.2.0-alpha.0
// purpose: Contract tests for HttpContextCorrelationIdAccessor
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute,
//                ApiPilot.AspNetCore.Middleware, Microsoft.AspNetCore.Http
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : HttpContextCorrelationIdAccessor.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;

namespace ApiPilot.AspNetCore.Tests.Middleware;

/// <summary>
/// Contract tests for HttpContextCorrelationIdAccessor. Uses a controllable
/// IHttpContextAccessor to verify the read behavior.
/// </summary>
[TestClass]
public sealed class HttpContextCorrelationIdAccessorTests
{
    /// <summary>No HttpContext returns null.</summary>
    [Test]
    public void RequestId_NoHttpContext_ReturnsNull()
    {
        var accessor = new HttpContextCorrelationIdAccessor(new FakeHttpContextAccessor(null));
        TestAssert.Null(accessor.RequestId);
    }

    /// <summary>HttpContext without a stored value returns null.</summary>
    [Test]
    public void RequestId_HttpContextWithoutStoredValue_ReturnsNull()
    {
        var ctx = new DefaultHttpContext();
        var accessor = new HttpContextCorrelationIdAccessor(new FakeHttpContextAccessor(ctx));
        TestAssert.Null(accessor.RequestId);
    }

    /// <summary>HttpContext with a stored string returns it.</summary>
    [Test]
    public void RequestId_HttpContextWithStoredValue_ReturnsValue()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] = "my-correlation-id";
        var accessor = new HttpContextCorrelationIdAccessor(new FakeHttpContextAccessor(ctx));
        TestAssert.Equal("my-correlation-id", accessor.RequestId);
    }

    /// <summary>A stored value that is not a string returns null.</summary>
    [Test]
    public void RequestId_StoredValueNotString_ReturnsNull()
    {
        var ctx = new DefaultHttpContext();
        ctx.Items[ApiPilotCorrelationMiddleware.CorrelationIdKey] = 42;
        var accessor = new HttpContextCorrelationIdAccessor(new FakeHttpContextAccessor(ctx));
        TestAssert.Null(accessor.RequestId);
    }
}

/// <summary>A simple IHttpContextAccessor returning a fixed context.</summary>
internal sealed class FakeHttpContextAccessor : IHttpContextAccessor
{
    public FakeHttpContextAccessor(HttpContext? context) { HttpContext = context; }

    public HttpContext? HttpContext { get; set; }
}

