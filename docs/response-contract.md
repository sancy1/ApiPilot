<!--
filepath: docs/response-contract.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot success and error envelope shapes, the meta object, and the relationship to ASP.NET Core ProblemDetails.
-->

# The ApiPilot response contract

This document describes the wire shape of every ApiPilot response: the
success envelope, the error envelope, the paginated envelope, and the
meta object. The normative contract is `../SPEC.md`. Where this document
and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The success envelope (the two required keys, the two optional keys).
- The collection and paginated envelopes.
- The error envelope, including the fields object used by validation.
- The meta object and the requestId it carries.
- How the ApiPilot envelope differs from ASP.NET Core ProblemDetails.

## The success envelope

Every successful ApiPilot response is a JSON object with a required
success key and a required data key. The message and meta keys are
always present. From SPEC.md "Response envelope - success":

    {
      "success": true,
      "data": { "id": "ORD-10001", "status": "confirmed" },
      "message": "Order retrieved successfully.",
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2", "timestamp": "2026-09-29T00:00:00Z", "extra": {} }
    }

### The two required keys: success and data

- success is always true for a successful response.
- data carries the application payload. It may be an object, an array,
  a scalar, or null. The data key is never omitted.

### The optional message key

message is a short, safe, human-readable string. It is always present.
It is null when the application provides none. When present, it never
contains stack traces, cryptographic details, or internal implementation
notes.

### The meta key

meta is always present. It carries meta.requestId, meta.timestamp, and
meta.extra. See "The meta object" below.

## The collection envelope

A collection is not a distinct envelope. It is the success envelope with
data being an array. From SPEC.md "Response envelope - collection":

    {
      "success": true,
      "data": [
        { "id": 1, "name": "Laptop" },
        { "id": 2, "name": "Monitor" }
      ],
      "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
    }

### Why a collection is not a distinct envelope

The wire shape is identical to a single-object success response. Only
the data payload differs. A client deserializer that handles the success
envelope handles collections without any additional shape knowledge. The
pagination object is what makes a paginated collection different, and it
is described below.

## The paginated envelope

A paginated collection adds a pagination object next to data. From
SPEC.md "Response envelope - paginated collection":

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

### The pagination object

pagination is present only for paginated responses. It carries six
fields, all required when present: page, pageSize, totalItems, totalPages,
hasNext, and hasPrevious.

### The six pagination fields

- pagination.page and pagination.pageSize are the values the server used,
  not the values the client requested if the client requested an
  out-of-range value.
- pagination.totalItems is the total count across all pages, as computed
  by the application. ApiPilot does not query data sources.
- pagination.totalPages equals ceil(totalItems / pageSize), with a
  minimum of 1 when totalItems is 0.
- pagination.hasNext is true when page is less than totalPages.
- pagination.hasPrevious is true when page is greater than 1.

For the full pagination contract, including the ten override points, see
[pagination.md](pagination.md).

## The error envelope

Every ApiPilot error response is a JSON object with a required success
key of false, a required error object, and an optional meta object. From
SPEC.md "Response envelope - error":

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

### The error object: code, message, fields

- error.code is a stable, machine-readable identifier from the error
  code table in SPEC.md.
- error.message is a short, safe, human-readable string. It never
  contains stack traces, cryptographic details, or internal
  implementation notes.
- error.fields is present only for validation errors.

For the full error contract, including the fourteen standard codes and
the exception-to-code mapping, see [error-contract.md](error-contract.md).

### The fields object: field name to array of messages

error.fields is a JSON object mapping each field name to an array of
message strings. The shape is:

    "fields": {
      "email": [ "Email is required.", "Email must be unique." ],
      "items[0].price": [ "Price must be positive." ]
    }

A field with a single failure carries a one-element array. A field with
multiple failures carries a multi-element array. The field key format is
normalized by the adapter. See [validation.md](validation.md) for the
normalization rules and the override point.

### What error.message never contains

error.message is safe for the client to display. It never contains a
stack trace, a cryptographic value, a SQL fragment, a file path, a
connection string, or any internal implementation note. Detailed
diagnostics go to server-side logs only, correlated by meta.requestId.

### The generic message for unknown exceptions

An unhandled exception produces error.code INTERNAL_ERROR and a generic
error.message. The real exception detail is never returned to the
client unless the application explicitly enables it through
ApiExceptionOptions.RevealExceptionMessageInResponse or
RevealExceptionTypeInResponse. Both default to false.

## The meta object

meta carries non-business metadata. It has three standard keys: requestId,
timestamp, and extra.

### meta.requestId

meta.requestId is the correlation ID for the request. It is present on
every envelope whenever correlation is enabled. The value is either the
validated incoming ID from the configured header (default X-Request-Id)
or a generated ID when the incoming header is absent or invalid.

The correlation ID is never treated as a secret, an authorization
credential, or a session identifier. For the full correlation contract,
see [correlation.md](correlation.md).

## Relationship to ASP.NET Core ProblemDetails

ASP.NET Core MVC by default returns errors in the ProblemDetails shape,
standardized as RFC 9457 (which obsoleted RFC 7807). ApiPilot does not
emit ProblemDetails and does not depend on it. The two shapes are both
machine-readable. They are different contracts.

### What ProblemDetails is

ProblemDetails is a JSON object with keys type, title, status, detail,
and instance. There is no success discriminator: the presence of the
shape itself indicates an error. Validation failures use a derived shape,
ValidationProblemDetails, which adds an errors object mapping field
names to arrays of messages.

### What ApiPilot's envelope is

The ApiPilot envelope has a top-level success boolean. When success is
true, the payload is under data. When success is false, the error is
under error. There is a meta object on every envelope. A client can
discriminate success from failure by reading a single boolean instead
of by checking whether a particular key is present.

### The four structural differences

1. Discriminator. ApiPilot has success: boolean. ProblemDetails has none.
2. Error carrier. ApiPilot nests error under a single key. ProblemDetails
   spreads error data across the top level.
3. Field errors. ApiPilot uses error.fields with the same object-of-arrays
   shape as ValidationProblemDetails, but nested under error.
4. Metadata. ApiPilot has a first-class meta object with requestId.
   ProblemDetails has no standardized metadata slot.

### What replaces ProblemDetails in MVC hosts

When a controller is marked with [ApiController] and the application has
registered AddApiPilotControllers, the default MVC behavior of returning
ValidationProblemDetails is replaced by ApiPilotInvalidModelStateResponse
Factory. The factory produces an ApiPilot error envelope instead. See
[validation.md](validation.md) for the integration detail.

## The wire-format contract is SPEC.md

This document describes the shape and the intent. The normative contract
is `../SPEC.md`. Every JSON example above appears in SPEC.md verbatim.
The rules stated here are the rules SPEC.md freezes. A language binding,
a client library, or an independent implementation is compatible with
ApiPilot when it honors SPEC.md, not when it honors this document.

## Compatibility guarantees

The following are guaranteed not to change without a major version bump:

- The two required keys on the success envelope (success, data).
- The two required keys on the error envelope (success, error).
- The key names success, data, message, error, code, fields, pagination,
  and meta.
- The six pagination field names.
- The requestId, timestamp, and extra keys under meta.

The following may change in a minor or patch version:

- Additional keys under meta.
- Additional keys under pagination.
- Additional error codes.
- The error.message text for any given error (the code is stable, the
  message is not).

Published versions are immutable. Any change, including a documentation
change, gets a new version number. See `../SPEC.md` "Contract versioning"
for the full rule.

## Related documents

- [error-contract.md](error-contract.md) - the fourteen standard error
  codes, the HTTP status mapping, and the exception-to-code mapping.
- [validation.md](validation.md) - the validation envelope, the field-key
  normalization, and the MVC and Minimal API integration.
- [pagination.md](pagination.md) - the pagination contract and the ten
  override points.
- [filtering-sorting.md](filtering-sorting.md) - the sort and filter
  contract that feeds the paginated envelope.
- [serialization.md](serialization.md) - the JSON policy that produces
  the wire bytes.
- [correlation.md](correlation.md) - the correlation contract and the
  meta.requestId source.
- [content-negotiation.md](content-negotiation.md) - the 406 and 415
  rejections that use this same error envelope.
- `../SPEC.md` - the normative wire-format contract.

