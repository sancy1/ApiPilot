<!--
filepath: docs/validation.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot validation envelope, the field-key normalization, and the MVC and Minimal API integration.
-->

# The ApiPilot validation contract

This document describes how validation failures reach the client: the
envelope shape, the field-key normalization, the two integration paths
(MVC and Minimal API), and the framework-neutral design that lets an
application plug in its own validator. The normative contract is
`../SPEC.md`; where this document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The validation envelope: VALIDATION_ERROR, the error.fields object,
  and the HTTP status.
- The field-key normalization and its override point.
- The MVC integration: [ApiPilotValidate], [ApiController], and the
  ApiPilotInvalidModelStateResponseFactory.
- The Minimal API integration: the ApiPilotValidationEndpointFilter.
- The framework-neutral design and how an application plugs in its own
  validator.
- The comparison to ProblemDetails ValidationProblemDetails.

## The validation envelope

A validation failure produces the standard ApiPilot error envelope with
error.code set to VALIDATION_ERROR and error.fields carrying the
per-field messages. From SPEC.md "Response envelope - error":

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

### VALIDATION_ERROR

VALIDATION_ERROR is one of the fourteen standard codes. It is the only
code that carries error.fields. Every other code omits the fields object
entirely. A client that sees error.fields present can rely on the code
being VALIDATION_ERROR.

### The fields object: field name to array of messages

error.fields is a JSON object. Each key is a field name. Each value is a
non-empty array of message strings. The shape is:

    "fields": {
      "email": [ "Email is required.", "Email must be unique." ],
      "items[0].price": [ "Price must be positive." ]
    }

A field with a single failure carries a one-element array. A field with
multiple failures carries a multi-element array. The order of the messages
within a field is the order the validator produced them. The order of the
fields within the object is not guaranteed.

### The HTTP status: 400 or 422

The default HTTP status for VALIDATION_ERROR is 400 Bad Request. Some
applications prefer 422 Unprocessable Entity, which is semantically
closer to "the syntax was fine, the values were not". The default is 400;
an application that wants 422 configures the mapping explicitly. See
[error-contract.md](error-contract.md) for the mapping override.

## Field-key normalization

The field names that appear in error.fields are not necessarily the
names the ASP.NET Core model binder produced. They are normalized before
the envelope is written.

### The problem: three ASP.NET Core binders produce three key formats

ASP.NET Core produces ModelState keys in different formats depending on
the binder that ran:

- [FromBody] JSON produces JSONPath: $.items[0].price
- Form-urlencoded POST produces bracket notation: Items[0].Price
- Query strings produce simple names: email

A client that receives $.items[0].price for one request and Items[0].Price
for another has to know both formats. The normalization converges all
three to one canonical form so the client sees one field name regardless
of how the request arrived.

### The default: FieldKeyNormalizer.Normalize

FieldKeyNormalizer.Normalize is the library default. It camelCases each
segment, preserves the bracket indices, and strips a leading $. The
canonical form for the three examples above is:

- $.items[0].price becomes items[0].price
- Items[0].Price becomes items[0].price
- email stays email

### The override: ApiPilotValidationOptions.KeyTransform

ApiPilotValidationOptions.KeyTransform is a Func<string, string>? property.
When it is null (the default), the library uses FieldKeyNormalizer.Normalize.
When it is set, the library calls the supplied delegate instead. An
application whose frontend expects Email or $.email or any other form
supplies its own transform.

### The single resolver: ValidationKeyTransforms.Resolve

ValidationKeyTransforms.Resolve is the one place the decision is made. It
returns options.KeyTransform ?? FieldKeyNormalizer.Normalize. Every code
path that normalizes a field key calls the resolver. This ensures the MVC
path and the Minimal API path cannot drift.

### Why the resolver exists (so both code paths cannot drift)

Before the resolver, the MVC action filter and the [ApiController]
response factory each decided independently how to normalize keys. The
action filter honored KeyTransform; the factory did not. A request that
took one path saw the override; a request that took the other did not.
This was recorded as A-049 and fixed by introducing the single resolver.
The resolver is the discipline: one decision point, one code path.

## The MVC integration

MVC controllers have two paths to the ApiPilot validation envelope. The
path taken depends on whether the controller is marked with [ApiController].

### [ApiPilotValidate] on a controller action

The [ApiPilotValidate] attribute is an action filter. When applied to a
controller action, it inspects ModelState after model binding. If
ModelState is invalid, the filter short-circuits the action and writes
the ApiPilot validation envelope. If ModelState is valid, the action runs.

The attribute is optional. A controller without [ApiController] and without
[ApiPilotValidate] falls back to the framework default: the action runs and
the controller is responsible for checking ModelState itself.

### The [ApiController] short-circuit

When a controller is marked with [ApiController], ASP.NET Core installs an
automatic model-state validation filter. That filter runs before action
filters. If ModelState is invalid, the filter short-circuits the request
and invokes the InvalidModelStateResponseFactory. This means [ApiPilotValidate]
never runs for a controller marked [ApiController]: the framework has
already handled the failure.

This was recorded as A-053. The fix is not to fight the framework but to
replace the factory.

### ApiPilotInvalidModelStateResponseFactory

ApiPilotInvalidModelStateResponseFactory is the replacement for the MVC
default. It produces the ApiPilot validation envelope instead of
ValidationProblemDetails. The factory is registered by AddApiPilotControllers.

### Why the factory exists (replaces ProblemDetails ValidationProblemDetails)

The MVC default is ValidationProblemDetails. ApiPilot replaces it so that
clients see one error shape across every failure path: model binding,
manual validation, action filters, and unhandled exceptions. A client
deserializer that understands the ApiPilot error envelope understands
every error the API produces.

## The Minimal API integration

Minimal API endpoints do not have action filters. ApiPilot offers an
endpoint filter instead.

### ApiPilotValidationEndpointFilter

The endpoint filter inspects the endpoint's parameters for validation
attributes and produces the ApiPilot validation envelope when a parameter
fails validation. The filter is added to an endpoint through the endpoint's
metadata or by a convention. The exact registration shape is in the
source and in the XML documentation on ApiPilotValidationEndpointFilter.

### Endpoint filter vs action filter (the difference)

An action filter runs inside MVC. It has access to the ActionContext, the
controller, and the ModelStateDictionary. An endpoint filter runs inside
the minimal API pipeline. It has access to the endpoint's arguments but
not to a ModelStateDictionary. The two paths produce the same envelope
but through different integration points.

## The framework-neutral contract

The validation contract is framework-neutral. ApiPilot does not depend on
a specific validation library.

### Why ApiPilot does not depend on FluentValidation or DataAnnotations extensions

A validation library is an application choice. Some applications use
DataAnnotations, some use FluentValidation, some use a hand-written
validator, some use a domain-driven approach with its own result type.
Choosing one for the application would fight the applications that chose
a different one. ApiPilot accepts whatever the application already uses
and normalizes the result.

### How an application plugs in its own validation

The adapter consumes IValidationErrorSource. An application that uses a
custom validator implements the interface and registers the implementation.
The adapter reads the errors through the interface and writes the envelope.
The interface is the only contract between ApiPilot and the validation
library. See the XML documentation on IValidationErrorSource and
ValidationErrors for the exact shape.

## Multi-error responses

A validation failure can carry multiple field errors. The library does
not limit the count.

### The order of the fields

The order of messages within a field is the order the validator produced
them. The order of fields within the object is not guaranteed. A client
that cares about field order must sort client-side.

### Duplicate field handling

If a validator produces two messages for the same field, both appear in
the field's array. The library does not deduplicate messages.

## Relationship to ProblemDetails ValidationProblemDetails

ASP.NET Core MVC by default returns validation failures as
ValidationProblemDetails, a shape derived from RFC 9457 ProblemDetails.

### What ValidationProblemDetails looks like

ValidationProblemDetails has type, title, status, detail, instance, and
errors keys. The errors key maps field names to arrays of messages. There
is no success discriminator and no meta object.

### What ApiPilot's validation envelope looks like

The ApiPilot validation envelope has success: false, an error object with
code, message, and fields, and a meta object with requestId. The fields
object has the same object-of-arrays shape as ValidationProblemDetails'
errors, but it is nested under error and the surrounding envelope differs.

### Why the difference is deliberate

The ApiPilot envelope carries success and requestId. ValidationProblemDetails
carries neither. A client that needs to distinguish success from failure
reads a boolean in the ApiPilot envelope versus inferring from the status
code in the ProblemDetails world. A client that needs to correlate an
error with a server log line reads meta.requestId in the ApiPilot
envelope versus relying on a separate correlation header in the
ProblemDetails world.

## The validator: ApiPilotValidationOptionsValidator is not present

Phase 1.8 added five options validators: Pagination, Correlation,
ContentNegotiation, ApiException, and ApiPilotJson. There is no
ApiPilotValidationOptionsValidator. The reason is that ApiPilotValidationOptions
has one property (KeyTransform) that is a delegate, and one property
(the field-key format) that is descriptive rather than prescriptive. There
is nothing to validate: any delegate is a valid transform, and any string
is a valid key format. The validation options type accepts whatever the
application sets.

## Compatibility guarantees

- VALIDATION_ERROR is a stable code.
- The error.fields object shape is stable: object of field names to
  non-empty arrays of strings.
- The default field-key normalization (camelCase, bracket indices preserved,
  leading $ stripped) is stable.
- The KeyTransform override point is stable.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the error envelope shape
  and the ProblemDetails comparison.
- [error-contract.md](error-contract.md) - the fourteen codes and the
  code-to-status mapping.
- [serialization.md](serialization.md) - the JSON policy that produces
  the wire bytes for the fields object.
- `../SPEC.md` - the normative validation error example and the error
  code table.

