<!--
filepath: docs/serialization.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot JSON policy and every ApiPilotJsonOptions override.
-->

# The ApiPilot serialization contract

This document describes the JSON policy ApiPilot applies to every
response, the full ApiPilotJsonOptions surface, and the two integration
paths that carry the policy to MVC controllers and to Minimal API
endpoints. The normative wire contract is `../SPEC.md`; where this
document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The System.Text.Json policy and the exclusion of Newtonsoft.Json.
- The full ApiPilotJsonOptions property surface with defaults.
- The four transformation categories: property naming, enum mode, date
  mode, and depth/comment handling.
- Where the options apply: MVC controllers and Minimal API endpoints.
- The startup validator.

## The policy

ApiPilot uses System.Text.Json exclusively. It does not reference, wrap,
or depend on Newtonsoft.Json at any point. The policy is not configurable:
an application that wants Newtonsoft.Json is not using ApiPilot, it is
using a different library.

### System.Text.Json only

System.Text.Json is part of the .NET BCL. It requires no NuGet package
reference and no third-party assembly. Every serializer in the library
uses it through the standard JsonSerializerOptions type. Every
application-facing option maps to a property on JsonSerializerOptions.

### No Newtonsoft.Json, ever

The audit script scans the source tree for the Newtonsoft.Json identifier
and fails the build if it appears. The exclusion is permanent. An
application that has a legacy dependency on Newtonsoft.Json for its own
domain types configures a separate serializer for those types; ApiPilot
does not integrate with it.

### AddApiPilotJson configures both MVC JSON and Http.Json

AddApiPilotJson has two overloads. The IServiceCollection overload
configures the Http.Json.JsonOptions that the minimal API pipeline uses.
The IMvcBuilder overload configures both the MVC JSON options and the
Http.Json options. An application that uses controllers calls the
IMvcBuilder overload. An application that uses only minimal APIs calls
the IServiceCollection overload. A mixed application calls both or the
IMvcBuilder overload.

## ApiPilotJsonOptions - the full surface

ApiPilotJsonOptions carries every transformation the application can
configure. Each property has a sensible default and a first-class
override. The library applies the options when it builds the
JsonSerializerOptions instance it hands to the serializer.

### PropertyNamingPolicy (default CamelCase)

The policy for the property names on application-supplied DTOs. The
default is JsonNamingPolicy.CamelCase, which produces lowerCamelCase
property names on the wire.

### DictionaryKeyPolicy (default CamelCase)

The policy for the keys of dictionary-typed properties. The default is
JsonNamingPolicy.CamelCase. Dictionary keys are cased independently of
property names because a dictionary key is application data, not a
property name.

### EnumMode (default AsString)

How enum values are serialized. The default is EnumSerializationMode.AsString,
which writes the enum member name as a string. The alternative is
AsNumber, which writes the enum underlying integer value.

### DateMode (default Iso8601)

How DateTime and DateTimeOffset values are serialized. The default is
DateSerializationMode.Iso8601, which writes an ISO 8601 string. The
alternative is UnixSeconds, which writes the integer number of seconds
since the Unix epoch.

### MaxDepth

The maximum depth the serializer will read. The default is the
System.Text.Json default (64). The validator rejects a value below 1.

### ReadCommentHandling

Whether JSON comments are allowed in the request body. The default is
Disallow. The validator rejects an undefined value.

### DefaultIgnoreCondition

The condition under which properties are omitted from the output. The
default is Never, which writes every property including nulls. The
validator rejects an undefined value.

### The full property table with defaults

| Property | Type | Default |
| --- | --- | --- |
| PropertyNamingPolicy | JsonNamingPolicy? | CamelCase |
| DictionaryKeyPolicy | JsonNamingPolicy? | CamelCase |
| EnumMode | EnumSerializationMode | AsString |
| DateMode | DateSerializationMode | Iso8601 |
| MaxDepth | int | System.Text.Json default |
| ReadCommentHandling | JsonCommentHandling | Disallow |
| DefaultIgnoreCondition | JsonIgnoreCondition | Never |

## Property naming

Property names on application DTOs are governed by PropertyNamingPolicy.
The default is camelCase.

### Why camelCase is the default

JavaScript clients, the dominant consumer of an HTTP JSON API, use
lowerCamelCase as a de facto convention for object property names.
camelCase is what a JavaScript developer expects to see and what a
JavaScript serializer produces by default. The choice matches the
expectation of the widest audience.

### How to switch to PascalCase, snake_case, kebab-case

Set PropertyNamingPolicy to the corresponding JsonNamingPolicy instance.
PascalCase is JsonNamingPolicy null (the property name is written as-is).
snake_case and kebab-case require a custom JsonNamingPolicy implementation
that converts the property name.

### The distinction from FieldKeyNormalizer (validation)

PropertyNamingPolicy governs the names of DTO properties on the wire.
FieldKeyNormalizer governs the names of fields in the error.fields
object of a validation error. The two are independent. An application
that sets PropertyNamingPolicy to null but leaves the validation
default in place produces PascalCase DTO properties and camelCase
validation field names. The two are not linked.

## Enum serialization

Enum values on application DTOs are governed by EnumMode.

### AsString (the default)

The enum member name is written as a JSON string. A value of
OrderStatus.Confirmed is written as "Confirmed". The choice is
readable and stable across refactoring that changes the underlying
integer values.

### AsNumber

The enum underlying integer value is written as a JSON number. A value
of OrderStatus.Confirmed is written as 1 (or whatever the underlying
value is). The choice is compact and preserves ordering.

### The custom EnumConverter

The converter that implements the two modes is JsonSerializerConfigurator
internal to the library. An application does not implement its own; it
sets EnumMode and the library installs the appropriate converter.

## Date serialization

Date values on application DTOs are governed by DateMode.

### Iso8601 (the default)

DateTime and DateTimeOffset values are written as ISO 8601 strings. The
choice is interoperable with every language and every client library.

### UnixSeconds

DateTime and DateTimeOffset values are written as the integer number of
seconds since the Unix epoch (1970-01-01T00:00:00Z). The choice matches
systems that already use Unix time internally.

### The two UnixSeconds converters

The UnixSeconds mode is implemented by two converters:
UnixSecondsDateTimeConverter for DateTime and
UnixSecondsDateTimeOffsetConverter for DateTimeOffset. Both are
internal to the library. The converters handle the timezone normalization
and the truncation to whole seconds.

### A-027: the mode shipped before the converters

In an early phase, DateSerializationMode.UnixSeconds was documented and
exposed before the two converters existed. An application that set the
mode to UnixSeconds produced ISO 8601 output because the converters were
not installed. The defect was recorded as A-027 and fixed by shipping
the converters in the same phase as the mode. The lesson: a mode and
its implementation ship together, never the mode first.

## Depth and comments

The serializer reads a request body up to MaxDepth levels of nesting.
The default matches the System.Text.Json default. A body deeper than
MaxDepth is rejected with a JsonException that the exception middleware
maps to a generic error.

### MaxDepth

MaxDepth is an int. It is not nullable. The default is the
System.Text.Json default (64). The validator rejects a value below 1.

### ReadCommentHandling

ReadCommentHandling controls whether JSON comments (// and /* */) are
allowed in the request body. The default is Disallow. A client that
sends a body with comments receives an error. An application that wants
to accept comments sets ReadCommentHandling to Allow. The validator
rejects an undefined value.

## Where the options apply

The options apply to every JSON serialization the library performs.
That includes the response envelope, the request body deserialization
performed by the minimal API pipeline, and the MVC pipeline.

### MVC controllers

For MVC controllers, the options apply through the MVC JSON options
that AddApiPilotJson configures. The MVC input formatter and output
formatter both use the configured options.

### Minimal API endpoints

For minimal API endpoints, the options apply through the Http.Json
options that AddApiPilotJson configures. The minimal API pipeline reads
and writes JSON through those options.

### The double configuration path

Before the fix recorded as A-028 and A-083, AddApiPilotJson configured
only one of the two paths depending on which overload the application
called. An application that called the IServiceCollection overload
configured Http.Json but not MVC JSON. An application that called the
IMvcBuilder overload configured MVC JSON but not Http.Json. A mixed
application saw different serialization on the two paths. The fix is
that the IMvcBuilder overload configures both, and the IServiceCollection
overload configures Http.Json as documented. The two paths can be
configured independently, but only deliberately.

## The validator: ApiPilotJsonOptionsValidator

ApiPilotJsonOptionsValidator is one of the five options validators added
in Phase 1.8. It runs at startup through ValidateOnStart.

### The rules it enforces

- MaxDepth must be at least 1.
- EnumMode must be a defined EnumSerializationMode value.
- DateMode must be a defined DateSerializationMode value.
- ReadCommentHandling must be a defined JsonCommentHandling value.
- DefaultIgnoreCondition must be a defined JsonIgnoreCondition value.

When validation fails, the host refuses to start. The failure message
names the misconfigured property. See [error-contract.md](error-contract.md)
for the CONFIGURATION_ERROR code.

## Relationship to the wire contract

The serializer writes the envelope keys success, data, error, message,
pagination, and meta. Those keys are fixed regardless of the
PropertyNamingPolicy the application sets. The envelope types carry
JsonPropertyName attributes that pin the wire names. PropertyNamingPolicy
applies only to application DTO properties, not to the envelope itself.

### How naming choices affect the envelope keys

The envelope keys do not change. An application that sets
PropertyNamingPolicy to snake_case still sees success, data, error,
message, pagination, and meta in the response envelope. The application
DTO properties inside data are written in snake_case.

### Why the envelope keys are fixed

The wire contract in SPEC.md specifies the envelope key names. A change
to those keys is a breaking change to the contract. Pinning them with
JsonPropertyName ensures the application cannot accidentally break the
contract through a serializer option.

## Compatibility guarantees

- System.Text.Json is the only serializer.
- The four transformation categories (property naming, enum mode, date
  mode, depth/comment handling) each have a stable default and a stable
  override point.
- The envelope keys success, data, error, message, pagination, and meta
  are fixed regardless of PropertyNamingPolicy.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the envelope shapes
  the serializer produces.
- [error-contract.md](error-contract.md) - the error codes and the
  error envelope.
- [validation.md](validation.md) - the fields object and the
  FieldKeyNormalizer distinction.
- `../SPEC.md` - the normative wire-format examples.

