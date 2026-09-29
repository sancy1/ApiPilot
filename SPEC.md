<!--
filepath: SPEC.md
package:  n/a (repository root)
since:    v0.1.0-alpha.0
purpose:  Cross-language contract for the ApiPilot response envelope and CSRF flow.
-->

# ApiPilot - Specification

This document is the cross-language contract for ApiPilot. It defines the wire
format that every ApiPilot package emits and enforces. A language binding,
client library, or independent implementation is compatible when it honors
this contract.

The contract is versioned with the library. Breaking changes to the wire
format require a major version bump.

## Response envelope - success

Every successful ApiPilot response is a JSON object with success and data.
The message and meta keys are always present. message is null when the application provides none.

    {
      "success": true,
      "data": { "id": "ORD-10001", "status": "confirmed" },
      "message": "Order retrieved successfully.",
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2", "timestamp": "2026-09-29T00:00:00Z", "extra": {} }
    }

Rules:

- success is always true for a successful response.
- data carries the application payload. It may be an object, an array, a scalar,
  or null. The data key is never omitted.
- message is a short, safe, human-readable string, or null. The key is always present.
- meta is always present. It carries meta.requestId (present whenever correlation is
  enabled), meta.timestamp (the ISO 8601 response time), and meta.extra (an object,
  empty when no extras are supplied).


## Response envelope - collection

    {
      "success": true,
      "data": [
        { "id": 1, "name": "Laptop" },
        { "id": 2, "name": "Monitor" }
      ],
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

## Response envelope - paginated collection

    {
      "success": true,
      "data": [ ... ],
      "pagination": {
        "page": 2,
        "pageSize": 20,
        "totalItems": 143,
        "totalPages": 8,
        "hasNext": true,
        "hasPrevious": true
      },
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

Rules:

- pagination.page and pagination.pageSize are the values the server used,
  not the values the client requested if the client requested an out-of-range
  value.
- pagination.totalItems is the total count across all pages, as computed by
  the application. ApiPilot does not query data sources.
- pagination.totalPages equals ceil(totalItems / pageSize), with a minimum
  of 1 when totalItems is 0.
- pagination.hasNext is true when page is less than totalPages.
- pagination.hasPrevious is true when page is greater than 1.

## Response envelope - error

    {
      "success": false,
      "error": {
        "code": "VALIDATION_ERROR",
        "message": "One or more values are invalid.",
        "fields": {
          "email": [ "Email is required." ]
        }
      },
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

Rules:

- success is always false for an error response.
- error.code is a stable, machine-readable identifier from the table below.
- error.message is a short, safe, human-readable string. It never contains
  stack traces, cryptographic details, or internal implementation notes.
- error.fields is present only for validation errors. It maps a field name
  to an array of messages.
- Unknown exceptions produce a generic error.message. Detailed diagnostics
  go to server-side logs only, correlated by meta.requestId.

## Error code table

| Condition               | HTTP status  | Code                    |
| ----------------------- | ------------ | ----------------------- |
| Validation failure      | 400 or 422   | VALIDATION_ERROR        |
| Authentication missing  | 401          | AUTHENTICATION_REQUIRED |
| Authorization failure   | 403          | FORBIDDEN               |
| Missing CSRF header     | 403          | CSRF_HEADER_MISSING     |
| Invalid CSRF token      | 403          | CSRF_TOKEN_INVALID      |
| Expired CSRF token      | 403          | CSRF_TOKEN_EXPIRED      |
| Origin rejected         | 403          | CSRF_ORIGIN_REJECTED    |
| Resource not found      | 404          | RESOURCE_NOT_FOUND      |
| Conflict                | 409          | CONFLICT                |
| Rate limited            | 429          | RATE_LIMITED            |
| Unknown exception       | 500          | INTERNAL_ERROR          |
| Configuration error     | startup      | CONFIGURATION_ERROR     |
| Unacceptable Accept     | 406          | NOT_ACCEPTABLE          |
| Unsupported content type| 415          | UNSUPPORTED_MEDIA_TYPE  |

Error codes are part of the public contract. Renaming a code requires a
major version bump. Adding a code is a minor version change.

## CSRF bootstrap response

    {
      "token": "<csrf-request-token>"
    }

Rules:

- The bootstrap endpoint is a GET.
- The response contains only the CSRF request token. No authentication
  credential is ever serialized in the response.
- The token is held in memory by the client. It is never written to
  localStorage or sessionStorage.

## CSRF flow - two-credential model

ApiPilot separates the authentication credential from the CSRF request
credential.

- Authentication or session credential: an HttpOnly and Secure cookie,
  browser-managed, unreadable by JavaScript.
- CSRF request credential: a separate token issued by ApiPilot, held in
  memory by the client, sent on protected requests in a custom header.

Request sequence:

1. The browser authenticates through the application authentication system.
2. The browser requests the CSRF bootstrap endpoint.
3. ApiPilot returns the CSRF request token in the response body.
4. The browser keeps the token in memory.
5. For each protected request (POST, PUT, PATCH, DELETE by default), the
   browser sends the session cookie automatically and adds the CSRF token
   in the configured header.
6. ApiPilot validates method, endpoint policy, token, and Origin as
   configured, before the endpoint executes.

The CSRF token is signed and session-bound. It is validated by
constant-time comparison of the message authentication code. ApiPilot
delegates cryptographic protection to ASP.NET Core Data Protection. It
does not define its own cryptographic protocol.

## Cookie baseline

- Authentication or session cookies: Secure=true and HttpOnly=true.
- Prefer SameSite=Strict or SameSite=Lax when navigation permits.
- SameSite=None requires Secure=true.
- Keep Path narrow where practical.
- Avoid Domain unless cross-subdomain behavior is required.
- Prefer __Host- cookie naming where deployment permits.
- SameSite is not a complete replacement for CSRF validation.

Insecure combinations fail at startup with a configuration error. They
do not silently fall back to an insecure default.

## Correlation ID rules

- Every request carries a correlation ID, read from a configured header
  (default X-Request-Id) or generated if absent.
- Generated IDs use a time-ordered, unpredictable form.
- Incoming IDs are validated against a configured pattern before being
  echoed. Malformed incoming IDs are replaced with a generated ID.
- The correlation ID is echoed in meta.requestId and, when configured, in
  a response header.
- The correlation ID is never treated as a secret, an authorization
  credential, or a session identifier.

## Content negotiation

- ApiPilot defaults to application/json.
- An unacceptable Accept header is rejected with HTTP 406 and the standard
  error envelope.
- An unsupported request Content-Type is rejected with HTTP 415 and the
  standard error envelope.
- ApiPilot does not introduce a custom media-type ecosystem.
- The default acceptable response media type is application/json.
- The default acceptable request media type is application/json.
- The full wildcard */* is accepted in the Accept header by default.
- A missing Accept header is acceptable (RFC 7231: any media type is acceptable).
- A missing Content-Type header on a body-carrying method (POST, PUT, PATCH by default) is rejected with HTTP 415.
- Type-level wildcards such as application/* are not recognized. Applications that need them configure the acceptable media types explicitly.

## Contract versioning

This contract is versioned with the ApiPilot package. Patch and minor
releases preserve the wire format except where a change is additive and
backward compatible. Breaking changes to the wire format require a major
version bump and a migration note in CHANGELOG.md.

Published versions are immutable. Any change, including documentation-only
changes, gets a new version number.

