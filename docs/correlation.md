<!--
filepath: docs/correlation.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot correlation ID contract and the standing envelope-construction discipline.
-->

# The ApiPilot correlation contract

This document describes how every ApiPilot request acquires a correlation
ID, how that ID reaches the response envelope and the response header,
and the standing rule every envelope-writing component in the library
follows. The normative contract is `../SPEC.md`; where this document and
SPEC.md disagree, SPEC.md wins.

## What this document covers

- The correlation ID: source, default header, default format.
- The three interfaces that form the correlation contract.
- The middleware behavior: read, validate, generate, store, echo.
- The envelope-construction discipline (A-040, A-118, A-120).
- Where meta.requestId appears in the response.
- The startup validator.

## The correlation ID

Every ApiPilot request carries a correlation ID. The ID is read from a
configured header when present and valid, generated when absent or
invalid, and echoed in the response. The ID is the anchor that ties a
client-visible response to a server-side log line.

### Where it comes from

The correlation ID comes from one of two sources. When the request
carries a valid correlation header, the middleware uses the incoming
value. When the request carries no header or the header is invalid,
the middleware generates a new ID through the configured generator.

### The default header name (X-Request-Id)

The default header is X-Request-Id. The header name is configurable
through CorrelationOptions.HeaderName. An application whose client
stack already uses a different header (for example, X-Correlation-Id
or traceparent) sets HeaderName to that header.

### The default format (time-ordered, unpredictable)

The default generated ID uses a time-ordered, unpredictable form. The
default implementation produces a GUIDv7 value, which is ordered by
creation time and is not guessable. The choice lets log aggregation
sort by the ID and keeps the ID non-enumerable for security.

## The three interfaces

The correlation contract is three interfaces. The first two are the
application-facing surface. The third carries the configuration.

### ICorrelationIdAccessor

ICorrelationIdAccessor exposes a single RequestId string property. The
accessor is the read side of the contract. Any component that needs
the current request's correlation ID resolves the accessor from DI and
reads RequestId. The default implementation reads from HttpContext.Items
under a documented key; a test double supplies a fixed value.

### ICorrelationIdGenerator

ICorrelationIdGenerator exposes a single Generate method. The generator
is the produce side of the contract. The default implementation produces
a GUIDv7 value. An application that uses ULIDs, W3C traceparent, or a
custom format registers its own generator.

### CorrelationOptions

CorrelationOptions carries the configuration: HeaderName,
ValidationPattern, InvalidIncomingIdPolicy, EchoInResponseHeader, and
EchoInResponseBody. The full surface is in the XML documentation on the
type and in the source at
`ApiPilot.Core/Metadata/CorrelationOptions.cs`.

## The middleware

ApiPilotCorrelationMiddleware runs early in the pipeline. It performs
the read, validate, generate, store, and echo steps in order.

### UseApiPilotCorrelation (pipeline order)

The recommended order is UseApiPilotCorrelation first, then
UseApiPilotExceptions, then UseApiPilotContentNegotiation, then the
rest of the pipeline. The correlation middleware must run before any
component that writes a response envelope, so the envelope can carry
the ID.

### Reading the incoming header

The middleware reads the configured header from the request. When the
header is present and its value is valid, the value becomes the
correlation ID for the request.

### Validating against CorrelationOptions.ValidationPattern

The ValidationPattern is a regular expression. The default pattern
matches a GUIDv7 value. An application that accepts a different format
sets ValidationPattern to a regex that matches its format. The
middleware validates the incoming ID against the pattern.

### Generating when absent or invalid

When the incoming header is absent, the middleware generates a new ID
through the configured generator. When the incoming header is present
but invalid, the behavior depends on the policy.

### CorrelationInvalidIdPolicy (the switch)

CorrelationInvalidIdPolicy is an enum with three defined values. The
policy decides what happens when the incoming header is present but
does not match ValidationPattern:

- Reject: return an error response and do not run the endpoint.
- Replace: generate a new ID and use it instead of the invalid one.
- UseAnyway: use the invalid value as the correlation ID.

The default policy is Replace. The choice keeps the request flowing
while ensuring the correlation ID is well-formed.

### Storing on HttpContext.Items

After the middleware determines the effective ID, it stores the ID on
HttpContext.Items under a documented key. The ICorrelationIdAccessor
implementation reads from that key. Any component in the pipeline that
runs after the correlation middleware can read the ID through the
accessor.

### Echoing in the response header (EchoInResponseHeader)

When EchoInResponseHeader is true (the default), the middleware writes
the effective ID to the response header under the configured header
name. A client can read the header to log the ID alongside its own
request record.

### Echoing in the response body (EchoInResponseBody)

When EchoInResponseBody is true, the middleware arranges for the
effective ID to be written to meta.requestId on the response envelope.
When false, meta.requestId is set to the empty string. The default is
true. The envelope is written by the response result, which reads the
effective ID through the accessor.

## The envelope-construction discipline (standing rule)

Every component in ApiPilot that writes a response envelope must resolve
the correlation ID through ICorrelationIdAccessor. The rule was learned
from two defects: A-040 in the exception middleware, and A-118 in the
content negotiation middleware. Both were regressions where a new
component bypassed the accessor and used HttpContext.TraceIdentifier
directly. The result was inconsistent meta.requestId on the wire: some
envelopes carried the correlation ID, others carried the trace ID.

### Every envelope-writing component takes ICorrelationIdAccessor?

The constructor of every envelope-writing component receives an optional
ICorrelationIdAccessor parameter. The parameter is optional so the
component works in an application that did not register the correlation
service. The parameter is typed as ICorrelationIdAccessor? to signal
that the accessor may be absent.

### The fallback to HttpContext.TraceIdentifier

When the accessor is absent, the component falls back to
HttpContext.TraceIdentifier. The fallback preserves the
works-without-registration contract. When the accessor is present, its
RequestId is used. This is the single pattern every envelope-writing
component follows.

### Why this is a rule (A-040, A-118, A-120)

The rule exists because the same defect was paid for twice. A-040 was
the exception middleware. A-118 was the content negotiation middleware.
Both wrote an envelope that bypassed the accessor. A-120 is the
discipline correction: the pre-write checklist for any new component
that writes a response envelope must name the correlation accessor path,
the EchoInResponseBody path, and the ErrorResponseResult serialization
path. If any of the three is missing, the PR is incomplete.

### The three-item checklist for new envelope-writing components

1. Constructor takes ICorrelationIdAccessor? correlationAccessor = null.
   Envelope built from correlationAccessor?.RequestId ?? TraceIdentifier.
2. Constructor takes IOptions<CorrelationOptions>? (optional). The
   envelope RequestId is set to string.Empty when EchoInResponseBody
   is false.
3. The envelope is serialized through ErrorResponseResult, not by a
   hand-rolled JsonSerializer.SerializeAsync.

A component that writes a response envelope and does not satisfy all
three is incomplete. The rule is a code-review checkpoint.

## Where requestId appears in the response

The correlation ID appears in the response in up to two places.

### meta.requestId

Every success envelope and every error envelope carries meta.requestId
when correlation is enabled and EchoInResponseBody is true. The value
is the effective correlation ID for the request. When EchoInResponseBody
is false, meta.requestId is the empty string.

### The response header

When EchoInResponseHeader is true, the response carries the effective
ID in the configured header. A client reads the header to log the ID
alongside its own request record. The header is present on every
response, including error responses.

### Both, when EchoInResponseBody is true

When both echo options are true, the ID appears in both the response
header and the response envelope. The two values are identical. A
client can read either; the header is convenient for middleware-style
access, the body is convenient for the application-level handler.

## The validator: CorrelationOptionsValidator

CorrelationOptionsValidator is one of the five options validators added
in Phase 1.8. It runs at startup through ValidateOnStart.

### The rules it enforces

- HeaderName must not be empty.
- ValidationPattern must not be empty and must be a valid regular
  expression. The validator compiles the pattern at startup; a compile
  failure fails the host.
- InvalidIncomingIdPolicy must be a defined CorrelationInvalidIdPolicy
  value.

When validation fails, the host refuses to start. The failure message
names the misconfigured property. See [error-contract.md](error-contract.md)
for the CONFIGURATION_ERROR code.

## Compatibility guarantees

- The default header name X-Request-Id is stable.
- The default format GUIDv7 is stable.
- The header name, the validation pattern, the policy, and the echo
  options are all overridable through CorrelationOptions.
- The correlation ID is never a secret, an authorization credential, or
  a session identifier. This is a contract, not an implementation
  detail.
- Every envelope-writing component in the library honors the discipline
  described above. New components must honor it.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the envelope shape
  that carries meta.requestId.
- [error-contract.md](error-contract.md) - the error envelope and the
  correlation ID it carries for log correlation.
- [content-negotiation.md](content-negotiation.md) - the middleware
  that was the source of A-118 and that now follows the discipline.
- `../SPEC.md` - the normative "Correlation ID rules" section.

