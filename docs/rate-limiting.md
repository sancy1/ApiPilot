<!--
filepath: docs/rate-limiting.md
package:  n/a (repository root docs)
since:    v0.5.0
purpose:  The ApiPilot rate-limiting contract: the integration seam, the rejection envelope, and the diagnostics.
-->

# The ApiPilot rate-limiting contract

This document describes how ApiPilot integrates with the platform
rate limiter. ApiPilot does not implement a limiter, a policy, or a
partition; the application owns the limiter configuration. The
normative contract is `../SPEC.md`. Where this document and SPEC.md
disagree, SPEC.md wins.

## What this document covers

- The integration model: the platform rejection callback.
- The rejection response envelope.
- The diagnostics: the metric, the structured log event, the reason codes.
- The Retry-After behavior.
- The Option D surface.

## The integration model

ApiPilot integrates through the platform own extension point, the
`RateLimiterOptions.OnRejected` callback. The application configures
the limiter and assigns the ApiPilot handler inside its own
`AddRateLimiter` call:

    builder.Services.AddRateLimiter(options =>
    {
        options.OnRejected = RateLimitDiagnosticsHook.HandleAsync;
        options.AddFixedWindowLimiter("api", o => { o.PermitLimit = 100; });
    });

ApiPilot never calls `AddRateLimiter`, never constructs a limiter,
never defines a policy, and never partitions. The audit forbids the
construction types (`TokenBucketRateLimiter`, `SlidingWindowRateLimiter`,
`FixedWindowRateLimiter`, `ConcurrencyLimiter`, `RateLimiter`,
`PartitionedRateLimiter`) in ApiPilot source.

The platform rate-limiting surface spans two namespaces:
`Microsoft.AspNetCore.RateLimiting` (the middleware and options) and
`System.Threading.RateLimiting` (the lease and metadata types). A
component that integrates with the limiter references both.

## The rejection response

A rejection produces the standard error envelope with the wire code
`RATE_LIMITED` and the HTTP status 429 (Too Many Requests). The
envelope is serialized through `ErrorResponseResult`, the single
serialization seam, never by a hand-rolled serialization call.

The request id in the envelope follows the same two rules as every
other ApiPilot error envelope:

- The id is the correlation accessor value when one is registered,
  falling back to `HttpContext.TraceIdentifier`.
- When `CorrelationOptions.EchoInResponseBody` is false, the request id
  in the body is the empty string.

These two rules are applied through the shared `CorrelationEnvelope`
seam, so the rejection response cannot drift from the exception
middleware response.

## The diagnostics

The rejection emits a metric and a structured log event.

The metric is a counter on the `ApiPilot` meter, named
`apipilot.ratelimit.rejected`, tagged with `reason`. The reason values
are the constants in `RateLimitReasonCodes`.

The structured log event uses the id `4001` and carries the wire code
and the reason. It never carries the request path, the query string, a
partition key, an identity, or an IP address.

The meter instance is owned by the hook and lives for the process. It
is a separate Meter instance from the CSRF counters meter, sharing the
public name `ApiPilot`. A listener keyed on the name may observe both.
A future release will consolidate the meters; this is recorded as a
scheduled backfill item, not a silent state.

## Retry-After

The framework exposes a delta form for the retry interval, carried as
a `TimeSpan` under `MetadataName.RetryAfter`. There is no HTTP-date
form.

When `EmitRetryAfter` is true and the lease supplies the metadata:

- A positive value is emitted as integer delta-seconds, rounded up
  (ceiling), using the invariant culture.
- A zero or negative value emits nothing.

When the lease supplies no metadata, nothing is emitted and no retry
interval is calculated. ApiPilot never invents a Retry-After value.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| HTTP status | 429 | `ApiPilotRateLimitOptions.StatusCode` |
| Message | a fixed safe ApiPilot string | `ApiPilotRateLimitOptions.Message` |
| Retry-After emission | pass through when the lease supplies it | `ApiPilotRateLimitOptions.EmitRetryAfter` |
| Rejection handler | `RateLimitDiagnosticsHook.HandleAsync` | the application own `OnRejected` |
| Wire error code | `RATE_LIMITED` | not overridable |

The message default is an ApiPilot string. SPEC.md defines no
normative message for RATE_LIMITED, so the message is not part of the
wire contract; it is overridable.

The options are registered through `AddApiPilotRateLimitRejection`.
The registration is fail-closed: an invalid status code (outside
400-599) or an empty message prevents the host from starting.

## Compatibility

The metric name `apipilot.ratelimit.rejected`, the meter name
`ApiPilot`, the log event id `4001`, and the reason-code string values
are a compatibility contract of this version. They are not renumbered
or renamed without a version bump.

## Related documents

- `../SPEC.md` - the wire contract.
- `correlation.md` - the request id and the echo rule.
- `error-contract.md` - the error envelope shape.
- `observability.md` - the metric, log, and trace surface.

