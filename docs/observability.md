<!--
filepath: docs/observability.md
package:  n/a (repository root docs)
since:    v0.5.0
purpose:  The ApiPilot observability surface: the log event catalogue, the redaction policy, the metrics, and the tracing.
-->

# The ApiPilot observability contract

This document describes the observability surface ApiPilot exposes:
the stable log event identifiers, the redaction policy, the metric
instruments, and the tracing source. ApiPilot provides instrumentation
primitives only. It does not ship an external telemetry SDK or exporter.
The application connects its own exporter to the platform abstractions.
The normative contract is `../SPEC.md`.

## What this document covers

- The log event catalogue.
- The redaction policy and its exact scope.
- The metric instruments.
- The tracing source.
- The Option D surface.

## The log event catalogue

Every ApiPilot component logs structured events with a stable event id.
The ids are a shipped contract: an application may key a log filter on
them. The values are never renumbered; a future component allocates an
unused id. The catalogue is exposed as constants on ApiPilotLogEvents.

| Id | Component | Meaning |
| --- | --- | --- |
| 1001-1007 | ApiPilotExceptionMiddleware | Exception mapped at each log level; response already started |
| 2001-2002 | ApiPilotCorrelationMiddleware | Incoming id replaced or rejected |
| 3001-3002 | ApiPilotContentNegotiationMiddleware | 406 and 415 rejections |
| 4001 | RateLimitDiagnosticsHook | Rate-limit rejection |
| 5001 | CsrfMiddleware | CSRF rejection |
| 6001-6002 | CsrfTransitionEvents | Binding unresolved; no rotation store |
| 7001 | OriginMiddleware | Origin rejected |
| 8001 | FetchMetadataMiddleware | Fetch Metadata rejected |
| 9001 | DataProtectionStartupDiagnostics | In-memory key ring warning (once per process) |
| 9100 | SecurityDiagnosticsHostedService | Non-fatal startup warning |

## The redaction policy

ApiPilotSafeLogger is an ILogger decorator that redacts the VALUES of
structured state entries whose KEYS match ApiPilotRedactionPolicy.RedactedNames.
The redacted names are Authorization, Cookie, Set-Cookie, X-CSRF-TOKEN,
X-XSRF-TOKEN, Proxy-Authorization, and WWW-Authenticate. The comparison is
case-insensitive; a matching value becomes the literal [redacted].

The decorator preserves the original message template ({OriginalFormat}),
the non-redacted structured entries, and the exception instance. It does
not reconstruct the message through the framework own rendering; the
formatter is a documented contract of the decorator.

The scope is exact: the decorator redacts by KEY NAME. It does not
inspect, rewrite, or scrub opaque string content. An exception message, a
scope payload, or a value logged under a generic key is outside its reach.
Callers must not place secrets in fields whose names are not in the
redacted set. The redacted set is fixed by the security contract; it is
not overridable at runtime.

## The metric instruments

ApiPilot emits instruments on a meter named ApiPilot. Applications consume
them through the platform MeterListener abstraction. There is no
third-party metrics dependency.

| Instrument | Type | Notes |
| --- | --- | --- |
| apipilot.csrf.issued | Counter<long> | One per token issued |
| apipilot.csrf.validated.ok | Counter<long> | One per successful validation |
| apipilot.csrf.validated.failed | Counter<long> | Tagged with reason |
| apipilot.csrf.rotated | Counter<long> | One per rotation |
| apipilot.csrf.validation.duration | Histogram<double> | Unit ms |
| apipilot.ratelimit.rejected | Counter<long> | Tagged with reason |

The CSRF instruments are opt-in through AddApiPilotCsrfObservability.
The rate-limit instrument is emitted by the rejection hook when the
application installs it. The meter name and the instrument names are a
compatibility contract of a version.

Note: the CSRF counters class owns its own Meter instance, and the
rate-limit hook owns its own Meter instance. Both share the public meter
name ApiPilot. A listener keyed on the name may observe both. The
consolidation of the same-name meters is a scheduled backfill item.

## The tracing source

ApiPilot exposes a process-wide ActivitySource named ApiPilot through
ApiPilotActivitySource. It is a static source and is never disposed. The
Start methods set only the HTTP method and the error code; they never set
the request path, the query string, or the exception type name, because
those values can carry sensitive content. The application attaches its own
ActivityListener.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Log event ids | fixed constants | not overridable; filter on the ILogger |
| Redacted header set | fixed | not overridable; wrap the logger |
| Meter name and instruments | fixed | not overridable; register a MeterListener |
| Activity source name | fixed | not overridable; register an ActivityListener |
| CSRF metrics opt-in | off | AddApiPilotCsrfObservability |
| Logger redaction | off unless wrapped | ApiPilotSafeLogger.Wrap |

## Compatibility guarantees

- The event ids, the meter name, the instrument names, and the activity
  source name are a compatibility contract of a version.
- ApiPilot provides instrumentation primitives only; it does not ship an
  external telemetry SDK.
- The redaction decorator never logs token, cookie, or key material.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [rate-limiting.md](rate-limiting.md) - the rate-limit metric.
- [csrf.md](csrf.md) - the CSRF flow and the failure codes.
- [data-protection.md](data-protection.md) - the key ring and the
  startup diagnostics.
- `../SPEC.md` - the normative references.

