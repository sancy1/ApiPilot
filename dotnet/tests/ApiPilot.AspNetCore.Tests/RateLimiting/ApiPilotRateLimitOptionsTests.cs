// filepath: dotnet/tests/ApiPilot.AspNetCore.Tests/RateLimiting/ApiPilotRateLimitOptionsTests.cs
// layer: Tests | package: ApiPilot.AspNetCore.Tests | since: v0.5.0
// purpose: Contract tests for the ApiPilotRateLimitOptions defaults and overrides
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test class)
//   Depends on : TestAssert, TestClassAttribute, TestAttribute, ApiPilot.AspNetCore.RateLimiting
//   Used by    : TestRunner.DiscoverAndRun
//   See also   : ApiPilotRateLimitOptions.cs, docs/rate-limiting.md
// -----------------------------------------------------------------------------

using ApiPilot.AspNetCore.RateLimiting;

namespace ApiPilot.AspNetCore.Tests.RateLimiting;

/// <summary>
/// Contract tests for ApiPilotRateLimitOptions. Verifies the documented
/// defaults and proves every property is settable and retained (the
/// get; set; contract).
/// </summary>
[TestClass]
public sealed class ApiPilotRateLimitOptionsTests
{
    /// <summary>The documented defaults are the expected values.</summary>
    [Test]
    public void Defaults_AreExpected()
    {
        var options = new ApiPilotRateLimitOptions();
        TestAssert.Equal(429, options.StatusCode);
        TestAssert.Equal("Too many requests. Retry later.", options.Message);
        TestAssert.True(options.EmitRetryAfter);
    }

    /// <summary>Every property is mutable and retained after assignment.</summary>
    [Test]
    public void Properties_AreMutable()
    {
        var options = new ApiPilotRateLimitOptions();
        options.StatusCode = 503;
        options.Message = "custom message";
        options.EmitRetryAfter = false;

        TestAssert.Equal(503, options.StatusCode);
        TestAssert.Equal("custom message", options.Message);
        TestAssert.False(options.EmitRetryAfter);
    }

    /// <summary>A StatusCode override is retained.</summary>
    [Test]
    public void StatusCode_Override_IsRetained()
    {
        var options = new ApiPilotRateLimitOptions { StatusCode = 429 };
        TestAssert.Equal(429, options.StatusCode);

        var overridden = new ApiPilotRateLimitOptions { StatusCode = 503 };
        TestAssert.Equal(503, overridden.StatusCode);
    }

    /// <summary>A Message override is retained.</summary>
    [Test]
    public void Message_Override_IsRetained()
    {
        var options = new ApiPilotRateLimitOptions { Message = "slow down" };
        TestAssert.Equal("slow down", options.Message);
    }

    /// <summary>An EmitRetryAfter override is retained.</summary>
    [Test]
    public void EmitRetryAfter_Override_IsRetained()
    {
        var enabled = new ApiPilotRateLimitOptions { EmitRetryAfter = true };
        TestAssert.True(enabled.EmitRetryAfter);

        var disabled = new ApiPilotRateLimitOptions { EmitRetryAfter = false };
        TestAssert.False(disabled.EmitRetryAfter);
    }
}

