// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/RateLimitReasonCodesTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Contract tests for the RateLimitReasonCodes constants
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : RateLimitReasonCodes.cs
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.RateLimiting;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Contract tests for RateLimitReasonCodes. Verifies the stable string
/// values of the diagnostic reason codes.
/// </summary>
[TestClass]
public sealed class RateLimitReasonCodesTests
{
    /// <summary>The None reason code is the exact literal "none".</summary>
    [Test]
    public void None_IsTheExactLiteral()
    {
        TestAssert.Equal("none", RateLimitReasonCodes.None);
    }

    /// <summary>The LimitExceeded reason code is the exact literal "limit_exceeded".</summary>
    [Test]
    public void LimitExceeded_IsTheExactLiteral()
    {
        TestAssert.Equal("limit_exceeded", RateLimitReasonCodes.LimitExceeded);
    }

    /// <summary>The QueueLimit reason code is the exact literal "queue_limit".</summary>
    [Test]
    public void QueueLimit_IsTheExactLiteral()
    {
        TestAssert.Equal("queue_limit", RateLimitReasonCodes.QueueLimit);
    }

    /// <summary>The Unknown reason code is the exact literal "unknown".</summary>
    [Test]
    public void Unknown_IsTheExactLiteral()
    {
        TestAssert.Equal("unknown", RateLimitReasonCodes.Unknown);
    }
}

