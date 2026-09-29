// filepath: dotnet/src/ApiPilot.AspNetCore/RateLimiting/RateLimitReasonCodes.cs
// layer: RateLimiting | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: The stable diagnostic reason-code constants for rate-limit rejections
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (static class)
//   Depends on : n/a
//   Used by    : ApiPilotRateLimitRejection (as the metric reason tag value)
//   See also   : docs/rate-limiting.md, CHANGELOG.md (Phase 4.1)
// -----------------------------------------------------------------------------
//
// SCOPE
//   These are diagnostic reason codes, not wire error codes. The wire
//   error code for a rate-limit rejection is RATE_LIMITED (SPEC.md). The
//   reason code is a tag value on the apipilot.ratelimit.rejected metric.
//   The values are a fixed contract of this version; they are not
//   overridable. The metric consumer keys on the string value.

namespace ApiPilot.AspNetCore.RateLimiting;

/// <summary>
/// The stable reason-code constants used as the <c>reason</c> tag value on
/// the <c>apipilot.ratelimit.rejected</c> metric. These are diagnostic
/// codes, not wire error codes. The wire error code is <c>RATE_LIMITED</c>.
/// </summary>
public static class RateLimitReasonCodes
{
    /// <summary>No specific reason was supplied by the platform lease.</summary>
    public const string None = "none";

    /// <summary>The configured permit limit was exceeded.</summary>
    public const string LimitExceeded = "limit_exceeded";

    /// <summary>The configured queue limit was exceeded.</summary>
    public const string QueueLimit = "queue_limit";

    /// <summary>A denial reason that ApiPilot does not recognize.</summary>
    public const string Unknown = "unknown";
}

