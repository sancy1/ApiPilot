<!--
filepath: docs/content-negotiation.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot 406 and 415 contract, the five transformations, and the wildcard scope decision.
-->

# The ApiPilot content negotiation contract

This document describes how ApiPilot rejects a request whose Accept
header does not include an acceptable media type (HTTP 406) and a
request whose Content-Type header on a body-carrying method is not
acceptable (HTTP 415). Both rejections use the standard ApiPilot error
envelope. The normative contract is `../SPEC.md`; where this document
and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The two rejections and their RFC 9110 definitions.
- The middleware and the recommended pipeline order.
- The five transformations (N1-N5) that each have a default and a
  first-class override.
- The Accept header check and the Content-Type header check.
- The envelope construction with the correlation accessor.
- The wildcard scope decision (A-119) and the reasoning behind it.
- The startup validator.

## The two rejections

Content negotiation produces two distinct HTTP errors. Both carry the
standard ApiPilot error envelope and a correlation ID.

### 406 Not Acceptable (RFC 9110 Section 15.5.7)

RFC 9110 Section 15.5.7 defines 406 as the response when the origin
server cannot produce a representation acceptable to the client, as
indicated by the Accept header. ApiPilot returns 406 when the request
Accept header names one or more media types and none of them matches
an acceptable response media type. The error code is NOT_ACCEPTABLE.

### 415 Unsupported Media Type (RFC 9110 Section 15.5.16)

RFC 9110 Section 15.5.16 defines 415 as the response when the request
payload format is not supported. ApiPilot returns 415 when a request on
a body-carrying method carries a Content-Type header that is not an
acceptable request media type, or carries no Content-Type header at all
when the application has not enabled the accept-missing behavior. The
error code is UNSUPPORTED_MEDIA_TYPE.

### Both use the standard error envelope

A 406 or 415 response has the shape defined in
[response-contract.md](response-contract.md): success is false, error
carries a code and a message, meta carries the correlation ID. The
response body is JSON.

### The two new error codes

NOT_ACCEPTABLE and UNSUPPORTED_MEDIA_TYPE were added to the standard
error code set in Phase 1.7. The full set is fourteen codes. See
[error-contract.md](error-contract.md) for the code table and the
status mapping.

## The middleware

Content negotiation is enforced by ApiPilotContentNegotiationMiddleware.
The middleware runs early in the pipeline and short-circuits the request
when a header is not acceptable.

### UseApiPilotContentNegotiation (pipeline order)

The recommended pipeline order is: UseApiPilotCorrelation first to
establish the correlation ID, UseApiPilotExceptions second to catch
downstream exceptions, and UseApiPilotContentNegotiation third. The
content negotiation middleware reads the correlation ID when it writes
its error envelope.

### Why middleware and not an endpoint filter

Content negotiation is a policy of the host, not of an individual
endpoint. The middleware enforces the policy for every request, before
the endpoint runs. An endpoint filter would have to be attached to each
endpoint and would run after model binding. The middleware is the
natural place for a policy that applies uniformly.

### The short-circuit behavior

When the middleware detects an unacceptable header, it writes the error
envelope directly and returns without calling the next middleware. The
endpoint does not run. This is the short-circuit.

## The five transformations (N1-N5)

Each transformation has a sensible default and a first-class override on
ContentNegotiationOptions. The five together define the full content
negotiation surface an application can configure.

### N1 - Acceptable response media types

Default: application/json. Override: the AcceptableResponseMediaTypes
property, an ISet<string> with case-insensitive comparison. An
application that serves additional response types (for example,
application/problem+json alongside application/json) adds them to the
set.

### N2 - Full wildcard */* acceptance

Default: true. Override: the AcceptWildcard property, a bool. When true,
a request that names */* in its Accept header is accepted regardless of
the other entries. When false, */* is treated as a literal media type
that must be in AcceptableResponseMediaTypes to be accepted.

### N3 - Acceptable request media types

Default: application/json. Override: the AcceptableRequestMediaTypes
property, an ISet<string> with case-insensitive comparison. An
application that accepts additional request body types adds them to
the set.

### N4 - Missing Content-Type policy

Default: false. Override: the AcceptMissingContentType property, a bool.
When false (the default), a request on a body-carrying method with no
Content-Type header is rejected with 415. When true, the missing header
is accepted and the endpoint runs.

### N5 - Body-carrying HTTP methods

Default: POST, PUT, PATCH. Override: the BodyCarryingMethods property,
an ISet<string> with case-insensitive comparison. An application that
wants DELETE (or another method) treated as body-carrying adds it to
the set. The Content-Type check runs only for methods in this set.

## The Accept header check

The Accept header check runs first. When the header is absent, the
request is accepted. When the header is present, the middleware parses
the entries and accepts the request if any entry matches an acceptable
media type or the full wildcard.

### Parsing the header (RFC 9110 Section 12.5.1)

RFC 9110 Section 12.5.1 defines the Accept header as a comma-separated
list of media ranges, each optionally carrying parameters and a
quality value. The middleware splits the header on commas, trims each
entry, and extracts the media type from the segment before the first
semicolon. Parameters (including the quality value) are not part of
the media type.

### Quality parameters (;q=) - presence, not preference

RFC 9110 defines the q parameter as a preference weight. ApiPilot does
not honor the weight. The check asks whether a media type is present in
the Accept header, not how much the client prefers it. An application
that needs preference-based negotiation with multiple server-supported
media types is not using ApiPilot for that endpoint; ApiPilot serves
one response type per endpoint configuration.

### A missing Accept header is acceptable

RFC 9110 Section 12.5.1 states that a request without an Accept header
implies that the client accepts any media type. The middleware follows
this rule: no Accept header means no rejection. The request passes
through to the endpoint.

### The full wildcard */*

The literal media type */* is the wildcard. When AcceptWildcard is true
(the default), the presence of */* in the Accept header accepts the
request regardless of any other entry. When AcceptWildcard is false,
*/* is treated as a literal media type and must be a member of
AcceptableResponseMediaTypes to be accepted.

### Type-level wildcards (application/*) - out of scope

RFC 9110 permits type-level wildcards of the form type/* (for example,
application/*). ApiPilot does not recognize them. A request that names
only application/* in its Accept header is rejected with 406 when
application/json is the only acceptable response media type.

### Why application/* is out of scope

The check is presence, not preference. application/* expresses a
preference for any type under the application/ top-level type. Honoring
it would require the server to choose among multiple acceptable types,
which the library does not do. The full wildcard */* is honored because
it is unambiguous: the client accepts anything. application/* is not
honored because honoring it would require a preference algorithm the
library does not implement.

### What applications do when they need it

An application whose clients send application/* sets
AcceptableResponseMediaTypes to include application/*. The literal
string is then matched against the Accept header entries. The library
treats application/* as a literal media type, not as a wildcard. An
application that wants true wildcard behavior implements its own
middleware or replaces ApiPilot's.

## The Content-Type header check

The Content-Type header check runs only for methods in BodyCarryingMethods.
For other methods, the header is ignored and the request passes.

### Parsing the header (RFC 9110 Section 8.3)

RFC 9110 Section 8.3 defines the Content-Type header as a single media
type with optional parameters. The middleware extracts the media type
from the segment before the first semicolon. Parameters (charset,
boundary, and so on) are not part of the media type and are ignored.

### Header parameters (charset, boundary) ignored

A Content-Type of application/json; charset=utf-8 is parsed as
application/json. The charset parameter is not part of the media type
and does not affect the check. The middleware accepts the request when
application/json is in AcceptableRequestMediaTypes.

### Body-carrying methods (POST, PUT, PATCH by default)

The check runs only for methods in BodyCarryingMethods. The default set
is POST, PUT, PATCH. GET, HEAD, OPTIONS, TRACE, and DELETE are not in
the set by default. An application that wants DELETE treated as
body-carrying adds it to the set through the N5 override.

### A missing Content-Type is rejected by default

A request on a body-carrying method with no Content-Type header is
rejected with 415. The default behavior is fail-closed: the request
is rejected rather than accepted. An application that wants to accept
a body with no declared content type sets AcceptMissingContentType to
true through the N4 override.

## The envelope

When the middleware rejects a request, it writes the standard error
envelope. The envelope construction follows the discipline that every
envelope-writing component in ApiPilot follows.

### The correlation ID from ICorrelationIdAccessor

The middleware receives an optional ICorrelationIdAccessor through its
constructor. When one is supplied (because the application registered
the correlation service and the middleware was placed after
UseApiPilotCorrelation), the middleware reads the current request's
correlation ID from the accessor and uses it as meta.requestId.

### The TraceIdentifier fallback

When no accessor is supplied (because the application did not register
the correlation service), the middleware falls back to
HttpContext.TraceIdentifier. This preserves the works-without-registration
contract: the middleware functions in a minimal application that does
not use the correlation feature.

### The exact JSON for a 406 and a 415

Both rejections produce the same envelope with different code and
message. The 406 response:

    {
      "success": false,
      "error": {
        "code": "NOT_ACCEPTABLE",
        "message": "The requested media type is not supported."
      },
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

The 415 response:

    {
      "success": false,
      "error": {
        "code": "UNSUPPORTED_MEDIA_TYPE",
        "message": "The request content type is not supported."
      },
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

A missing Content-Type on a body-carrying method produces the second
shape with a slightly different message that names the missing header.

## The validator: ContentNegotiationOptionsValidator

ContentNegotiationOptionsValidator is one of the five options validators
added in Phase 1.8. It runs at startup through ValidateOnStart.

### The rules it enforces

- AcceptableResponseMediaTypes must not be empty, and every entry must
  be a valid media-type string (contains a slash, no whitespace).
- AcceptableRequestMediaTypes must not be empty, and every entry must
  be a valid media-type string.
- BodyCarryingMethods must not be empty, and every entry must be a
  valid HTTP method token (letters and hyphens only).
- If AcceptWildcard is false and AcceptableResponseMediaTypes is empty,
  the configuration is rejected because no Accept header could ever be
  satisfied.

When validation fails, the host refuses to start. The failure message
names the misconfigured property. See [error-contract.md](error-contract.md)
for the CONFIGURATION_ERROR code.

## Relationship to the SPEC.md content negotiation section

SPEC.md "Content negotiation" states the wire contract. This document
adds the operational detail: the reasoning behind each default, the
RFC section references, and the wildcard scope decision.

### Where the SPEC and this document agree

Both state the default acceptable response type (application/json), the
default acceptable request type (application/json), the wildcard
acceptance, the missing-Accept-is-acceptable rule, the missing-Content-Type
rejection, and the exclusion of type-level wildcards.

### Where this document adds operational detail

This document names the RFC sections, describes the parsing rules, and
explains the reasoning behind the scope decision. SPEC.md states the
rules; this document explains them.

### The RFC 7231 to RFC 9110 terminology update

An early draft of SPEC.md referenced RFC 7231 for the missing-Accept
rule. RFC 7231 was obsoleted by RFC 9110 (June 2022). The contract has
been updated to reference RFC 9110 Section 12.5.1 where applicable. The
rule itself is unchanged: a missing Accept header means the client
accepts any media type.

## Compatibility guarantees

- NOT_ACCEPTABLE and UNSUPPORTED_MEDIA_TYPE are stable codes.
- The 406 and 415 HTTP statuses for those codes are stable.
- The five transformations N1-N5 each have a stable default and a
  stable override point.
- The rejection that a type-level wildcard receives is a documented
  scope decision, not a bug. It will not change without a minor
  version bump and a changelog entry.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the error envelope
  shape shared by every rejection.
- [error-contract.md](error-contract.md) - the NOT_ACCEPTABLE and
  UNSUPPORTED_MEDIA_TYPE codes and their status mapping.
- [correlation.md](correlation.md) - the correlation contract that
  supplies meta.requestId.
- `../SPEC.md` - the normative content negotiation section.

