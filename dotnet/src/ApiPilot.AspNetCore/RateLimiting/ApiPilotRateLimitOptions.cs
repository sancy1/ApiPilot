// filepath: dotnet/src/ApiPilot.AspNetCore/RateLimiting/ApiPilotRateLimitOptions.cs
// layer: RateLimiting | package: ApiPilot.AspNetCore | since: v0.5.0
// purpose: The Option D override surface for the ApiPilot rate-limit rejection
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (options type)
//   Depends on : n/a
//   Used by    : ApiPilotRateLimitRejection, ApiPilotRateLimitExtensions
//   See also   : docs/rate-limiting.md, CHANGELOG.md (Phase 4.1)
// -----------------------------------------------------------------------------
//
// OPTION D
//   Every transformation on this path has a sensible default and a
//   first-class override. StatusCode defaults to 429 (the SPEC.md and
//   ErrorResponseResult mapping for RATE_LIMITED). Message defaults to a
//   fixed safe ApiPilot string; SPEC.md defines no normative message, so
//   the message is an ApiPilot default, not a wire contract. EmitRetryAfter
//   defaults to true; the header is emitted only when the platform lease
//   supplies Retry-After metadata. The wire error code RATE_LIMITED is
//   fixed and is not overridable.

namespace ApiPilot.AspNetCore.RateLimiting;

/// <summary>
/// The overridable surface for the ApiPilot rate-limit rejection. Every
/// property has a sensible default and may be replaced by the application.
/// </summary>
/// <remarks>
/// <para>
/// The wire error code (<c>RATE_LIMITED</c>) is fixed by the wire contract
/// and is not overridable. The properties on this type override the parts
/// of the rejection that applications legitimately configure differently.
/// </para>
/// <para>
/// The defaults are validated at startup through the options pipeline. An
/// invalid StatusCode (outside 400-599) or an empty Message fails the host
/// at startup (fail-closed), not at request time.
/// </para>
/// </remarks>
public sealed class ApiPilotRateLimitOptions
{
    /// <summary>
    /// The HTTP status code written for a rate-limit rejection. Defaults to
    /// 429 (Too Many Requests), the SPEC.md mapping for RATE_LIMITED. Must be
    /// in the range 400-599.
    /// </summary>
    public int StatusCode { get; set; } = 429;

    /// <summary>
    /// The safe message written in the error envelope. Defaults to a fixed
    /// ApiPilot string. SPEC.md defines no normative message for
    /// RATE_LIMITED, so this is an ApiPilot default, not a wire contract.
    /// Must not be null, empty, or whitespace.
    /// </summary>
    public string Message { get; set; } = "Too many requests. Retry later.";

    /// <summary>
    /// When true, a Retry-After header is emitted if (and only if) the
    /// platform rate-limit lease supplies Retry-After metadata. ApiPilot
    /// never invents a Retry-After value. Defaults to true.
    /// </summary>
    public bool EmitRetryAfter { get; set; } = true;
}

