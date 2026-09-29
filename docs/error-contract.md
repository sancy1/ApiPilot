<!--
filepath: docs/error-contract.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot error code set, the HTTP status mapping, the exception-to-code mapping, and the reveal and logging policy.
-->

# The ApiPilot error contract

This document describes every standard ApiPilot error code, the HTTP
status each code maps to, how application exceptions become codes, and
what the library reveals or hides by default. The normative contract is
`../SPEC.md`; where this document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The fourteen standard error codes.
- The HTTP status mapping.
- The exception-to-code mapping and its override point.
- The safe-message and reveal policy.
- The logging policy.
- How errors become responses, and how the shape differs from ASP.NET
  Core ProblemDetails.

## The standard error code set

Every error response carries an error.code value. That value is a
stable, machine-readable, uppercase identifier. It is the primary key
for client-side error handling. The error.message is human-readable and
may change; the code does not.

### ApiErrorCode and the From(string) normalizer

ApiErrorCode is an immutable record with a single Code string property.
The static constructor is private. Instances are created either through
one of the fourteen standard static properties or through the From(string)
factory. From(string) trims the value and uppercases it with the invariant
culture, so "validation_error" and "VALIDATION_ERROR" produce the same
code. Null, empty, and whitespace inputs are rejected.

### Uppercase discipline

All standard codes are uppercase. Custom codes created through From(string)
are normalized to uppercase. The wire never carries a mixed-case code.

### The fourteen standard codes

From SPEC.md "Error code table":

| Code | HTTP status | Condition |
| --- | --- | --- |
| VALIDATION_ERROR | 400 or 422 | Validation failure |
| AUTHENTICATION_REQUIRED | 401 | Authentication missing |
| FORBIDDEN | 403 | Authorization failure |
| CSRF_HEADER_MISSING | 403 | Missing CSRF header |
| CSRF_TOKEN_INVALID | 403 | Invalid CSRF token |
| CSRF_TOKEN_EXPIRED | 403 | Expired CSRF token |
| CSRF_ORIGIN_REJECTED | 403 | Origin rejected |
| RESOURCE_NOT_FOUND | 404 | Resource not found |
| CONFLICT | 409 | Conflict |
| RATE_LIMITED | 429 | Rate limited |
| INTERNAL_ERROR | 500 | Unknown exception |
| CONFIGURATION_ERROR | startup | Configuration error (not a response code) |
| NOT_ACCEPTABLE | 406 | Unacceptable Accept header |
| UNSUPPORTED_MEDIA_TYPE | 415 | Unsupported request Content-Type |

## The HTTP status mapping

The adapter maps each code to an HTTP status through a built-in table
in `ApiPilot.AspNetCore/Results/ErrorResponseResult.cs`. Applications
that define their own codes supply the status through the same override
point described in "Application-defined codes" below.

### The full code-to-status table

The table above is the same table the adapter consults. Two entries
deserve note:

- VALIDATION_ERROR maps to 400 or 422. The default is 400. An application
  that prefers 422 configures the mapping explicitly.
- CONFIGURATION_ERROR has no response status. It is a startup signal.
  The application fails to start; no HTTP response is produced.

### Validation failure maps to 400 or 422

400 Bad Request is the historical default. 422 Unprocessable Entity is
the semantic default for syntactically valid but semantically invalid
input. The library defaults to 400. An application whose clients expect
422 configures the override.

### Configuration error is a startup error, not a response code

CONFIGURATION_ERROR is produced when an options validator fails during
ValidateOnStart. The host refuses to start. This code is never returned
over HTTP; it appears in the startup exception message and in the
server log.

## The code table in the adapter

The adapter's mapping lives in ErrorResponseResult.BuiltInMapErrorCode.
The method takes the wire code (a string) and returns an HTTP status. It
is a switch over the standard code strings. Two new arms were added in
Phase 1.7:

    "NOT_ACCEPTABLE"          => StatusCodes.Status406NotAcceptable,
    "UNSUPPORTED_MEDIA_TYPE"  => StatusCodes.Status415UnsupportedMediaType,

### How the wire code becomes an HTTP status

The flow is: the mapper produces an ApiError with a code. ErrorResponseResult
reads the code, looks it up in BuiltInMapErrorCode, and writes the HTTP
response with that status. The envelope is written to the response body
as JSON.

### Application-defined codes

An application defines a domain code with ApiErrorCode.From("ORDER_LOCKED")
and adds the corresponding status to the adapter by supplying its own
IApiErrorMapper implementation. The default mapper consults
ApiExceptionOptions.Mappings for exception-to-code mapping and the built-in
table for code-to-status mapping. Custom codes fall through to 500 unless
the mapper is replaced.

## Exception to error-code mapping

When an unhandled exception reaches ApiPilotExceptionMiddleware, the
middleware asks an IApiExceptionMapper to produce an ApiError. The
default mapper is DefaultApiExceptionMapper.

### The default mapping set

KnownExceptionTypes.Default is a curated list of common .NET exception
types mapped to error codes:

- ArgumentException and its derived types map to VALIDATION_ERROR.
- UnauthorizedAccessException maps to FORBIDDEN.
- KeyNotFoundException maps to RESOURCE_NOT_FOUND.
- OperationCanceledException maps to a specific client-cancellation code.
- Any other exception maps to INTERNAL_ERROR.

The exact set is in `ApiPilot.AspNetCore/ExceptionHandling/KnownExceptionTypes.cs`.

### The override: ApiExceptionOptions.Mappings

ApiExceptionOptions.Mappings is a mutable list of KnownExceptionType
entries. Each entry carries an ExceptionType, a Code, a SafeMessage, and
an IncludeExceptionMessage flag. An application adds its own entries to
map its domain exceptions to domain codes. The matching algorithm
considers the exception type and its base types, so a mapping for
Exception catches everything.

### The matching order (most specific first)

The mapper walks the exception type hierarchy from the most derived type
to the base Exception. The first matching entry wins. This means a mapping
for MyDomainException beats a mapping for Exception. An application that
wants to override a default mapping places its entry before the default
in the list.

### The fallback when no mapping matches: INTERNAL_ERROR

If no entry in Mappings matches the exception type, the mapper produces
INTERNAL_ERROR with a generic safe message. The exception detail is not
returned to the client. It is logged server-side.

## The safe-message discipline

error.message is safe for the client to display. It never contains a
stack trace, a cryptographic value, a SQL fragment, a file path, a
connection string, or any internal implementation note.

### error.message never contains stack traces, crypto details, internal notes

The rule is enforced at two layers. The mapper produces safe messages by
construction. The serializer does not include any property the mapper
did not set. A custom mapper that returned unsafe content would violate
the contract; the library does not protect against a custom mapper that
deliberately bypasses the discipline.

### RevealExceptionMessageInResponse (default false)

When true, the mapper includes the real exception.Message in the
error.message field instead of the safe message. This is intended for
development environments only. It is never enabled by default and
should not be enabled in production.

### RevealExceptionTypeInResponse (default false)

When true, the mapper includes the exception type name as a separate
field in the error object. This is intended for development environments
only. It is never enabled by default and should not be enabled in
production.

## The logging discipline

Every exception that reaches the middleware is logged. The log entry
carries the correlation ID so an operator can correlate the client-side
error with the server-side detail.

### IncludeExceptionTypeInLogs

When true (default), the log line includes the exception type name.

### IncludeExceptionMessageInLogs

When true (default), the log line includes the exception message.

### The correlation ID on every log line

The log line carries the correlation ID from the current request. This
ID matches meta.requestId in the response envelope, which lets an
operator find the server-side detail for a reported client error.

## How errors become responses

The middleware constructs an ApiError, wraps it in an ErrorResponse with
a ResponseMetadata object, and passes the ErrorResponse to ErrorResponseResult
for serialization and writing.

### ErrorResponseResult writes the envelope

ErrorResponseResult serializes the ErrorResponse through System.Text.Json
using the same options the application configured through AddApiPilotJson.
The envelope shape is the error envelope described in
[response-contract.md](response-contract.md).

### Why ApiPilot does not emit ProblemDetails

ProblemDetails is a fine shape for a specific style of API. ApiPilot
standardizes on a single envelope for both success and error so that
clients handle success and failure with the same deserializer. Mixing
ProblemDetails into the error path would force clients to know two
shapes. The library chooses one. See
[response-contract.md](response-contract.md) "Relationship to ASP.NET
Core ProblemDetails" for the full comparison.

## Relationship to ProblemDetails

ASP.NET Core MVC by default returns errors in ProblemDetails (RFC 9457,
which obsoleted RFC 7807). ApiPilot does not emit ProblemDetails.

### What ProblemDetails looks like

A ProblemDetails response has type, title, status, detail, and instance
keys. A validation error uses ValidationProblemDetails, which adds an
errors object mapping field names to arrays of messages.

### What ApiPilot's error envelope looks like

The ApiPilot error envelope has success: false, an error object containing
code, message, and fields, and a meta object containing requestId.

### Why ApiPilot replaced the MVC ValidationProblemDetails default

When [ApiController] is applied to a controller, ASP.NET Core short-circuits
the request before action filters run and produces a ValidationProblemDetails
response by default. ApiPilot replaces that default through
ApiPilotInvalidModelStateResponseFactory, registered by AddApiPilotControllers.
The factory produces the ApiPilot error envelope so clients see one shape
across every error. See [validation.md](validation.md) for the integration
detail.

## Compatibility guarantees

- The fourteen standard code strings are stable. Renaming a code requires
  a major version bump.
- Adding a new standard code is a minor version change.
- The HTTP status for each standard code is stable for the default adapter.
  An application that maps a code to a different status is exercising the
  override, not changing the contract.
- The error.message text for any given code is not stable. Clients match
  on the code, never on the message.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the envelope shapes and
  the ProblemDetails comparison.
- [validation.md](validation.md) - the validation error, the fields object,
  and the [ApiController] integration.
- [correlation.md](correlation.md) - the meta.requestId that ties a client
  error to a server log line.
- [content-negotiation.md](content-negotiation.md) - the 406 and 415
  rejections that use the NOT_ACCEPTABLE and UNSUPPORTED_MEDIA_TYPE codes.
- `../SPEC.md` - the normative error code table.

