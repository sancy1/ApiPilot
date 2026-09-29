<!--
filepath: docs/pagination.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot pagination contract: the envelope, the three configuration layers, and the ten transformations.
-->

# The ApiPilot pagination contract

This document describes the pagination envelope, the three layers of
configuration, the ten transformations that each have a default and a
first-class override, and the endpoint filter that ties it together.
The normative contract is `../SPEC.md`; where this document and SPEC.md
disagree, SPEC.md wins.

## What this document covers

- The paginated envelope.
- The three configuration layers and their precedence.
- The ten transformations, each with its default and override.
- The request accessors used by endpoint handlers.
- The pagination endpoint filter and the pagination result.
- The startup validator.

## The paginated envelope

A paginated response is the success envelope with a pagination object
added next to data. From SPEC.md "Response envelope - paginated collection":

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

### The six pagination fields and their rules

- pagination.page and pagination.pageSize are the values the server used,
  not the values the client requested if the client requested an
  out-of-range value.
- pagination.totalItems is the total count across all pages, as computed
  by the application. ApiPilot does not query data sources.
- pagination.totalPages equals ceil(totalItems / pageSize), with a
  minimum of 1 when totalItems is 0.
- pagination.hasNext is true when page is less than totalPages.
- pagination.hasPrevious is true when page is greater than 1.

## The three layers of configuration

A pagination behavior can be configured at three levels. Each layer
overrides the layer below it. The resolution happens per request.

### Layer 1: global PaginationOptions

The global options are registered through AddApiPilotPagination with an
optional configure delegate. When the delegate is null, the library uses
the defaults: DefaultPageSize 20, MaxPageSize 100, PageNumberBase 1,
SuccessStatusCode 200, StrictQueryValidation false, and the standard
query parameter names (page, pageSize, sort, direction).

    builder.Services.AddApiPilotPagination(o =>
    {
        o.DefaultPageSize = 25;
        o.MaxPageSize = 200;
    });

### Layer 2: fluent PaginationOverrides

The fluent layer attaches per-endpoint overrides to a minimal API endpoint
through WithApiPilotPagination. The callback receives a PaginationOverrides
object. Any property left unset falls through to the global options.

    app.MapGet("/items", handler)
       .WithApiPilotPagination(o =>
       {
           o.MaxPageSize = 50;
           o.ParameterNames = new QueryParameterNames { Page = "p" };
       });

### Layer 3: attribute PaginationMetadataAttribute

The attribute layer attaches to a controller action or to a minimal API
endpoint through WithMetadata. It carries only primitive overrides:
DefaultPageSize, MaxPageSize, StrictQueryValidation, SuccessStatusCode,
and PageNumberBase. It cannot carry delegates or the ParameterNames
object. The TriState enum is used for StrictQueryValidation so that a
sentinel distinguishes "not set" from "set to false".

    [HttpGet]
    [PaginationMetadata(MaxPageSize = 50, PageNumberBase = 0)]
    public IActionResult List() { ... }

### The precedence chain

The resolution runs from lowest to highest: global -> fluent -> attribute.
For the primitive fields, the attribute wins because it is the most
specific declaration on the action. For the delegate fields
(SortDirectionParser, SortParser, IsFilterParameter, IntegerParser) and
for the ParameterNames object, the fluent is the only source that can
carry them.

### The merge rule

The resolver combines the three sources in two steps. First, the fluent
and attribute sources are merged through PaginationResolver.MergeOverrides.
For each primitive, the attribute value wins when set; otherwise the
fluent value is used. For each delegate and for ParameterNames, the fluent
value is used unconditionally because the attribute cannot carry them.
Second, the merged overrides are applied on top of the global options
through PaginationResolver.Resolve. For each property, the override wins
when set; otherwise the global value is used.

## The ten transformations

Each transformation has a sensible default and a first-class override.
The override point is one of the three mechanisms: a delegate parameter,
an options property, or a replaceable DI service. Every override has a
test that uses it, not only a test that asserts it exists.

### T1 - Query parameter names (ParameterNames)

Default: page, pageSize, sort, direction. Override: the ParameterNames
property, whose type is QueryParameterNames. An application whose
frontend uses pageNumber/perPage/orderBy/order sets those names on a new
QueryParameterNames instance and assigns it to PaginationOptions.

### T2 - Page size defaults and max (DefaultPageSize, MaxPageSize)

Default: 20 and 100. Override: the DefaultPageSize and MaxPageSize
properties. A request for a page size above MaxPageSize is rejected by
the endpoint filter with VALIDATION_ERROR.

### T3 - Sort direction parsing (SortDirectionParser)

Default: the library accepts asc and desc, case-insensitively. Override:
the SortDirectionParser property, a Func<string, SortDirection?>?. An
application whose frontend uses ascending/descending, +/-, or up/down
supplies a parser that maps its tokens to SortDirection values.

### T4 - Sort expression parsing (SortParser)

Default: the sort field and the sort direction arrive as two separate
query parameters. Override: the SortParser property, a
Func<string, string, SortRequest?>?. An application whose frontend
encodes the direction in the field (for example, -createdAt for
descending) supplies a parser that reads the sign and produces a
SortRequest.

### T5 - Page numbering base (PageNumberBase)

Default: 1, so the first page is page 1. Override: the PageNumberBase
property. An application whose frontend uses 0-based page numbers sets
it to 0.

### T6 - Filter parameter recognition (IsFilterParameter)

Default: any query parameter not named by ParameterNames is treated as a
filter. Override: the IsFilterParameter property, a Func<string, bool>?.
An application that restricts filter keys to a known prefix (for example,
filter.) supplies a predicate.

### T7 - Integer parsing (IntegerParser)

Default: int.TryParse with NumberStyles.Integer and the invariant culture.
Override: the IntegerParser property, a Func<string, int?>?. An
application whose frontend sends hex or thousand-separated integers
supplies a parser.

### T8 - Response envelope shape (not overridable - the wire contract)

The paginated envelope shape is fixed by SPEC.md. The six field names,
the pagination object nesting, and the surrounding success envelope are
not overridable. An application that needs a different shape is not
configuring ApiPilot; it is not using ApiPilot.

### T9 - Success HTTP status (SuccessStatusCode)

Default: 200 OK. Override: the SuccessStatusCode property. An application
that follows RFC 7233 range semantics sets it to 206 Partial Content.

### T10 - Strict-mode query validation (StrictQueryValidation)

Default: false (lenient). A request with an unrecognized query parameter
passes through. Override: the StrictQueryValidation property. When true,
the endpoint filter rejects any query parameter not named by
ParameterNames and not classified as a filter with VALIDATION_ERROR.

## Retrieving the request in a handler

The endpoint filter stores the validated PageRequest, SortRequest, and
FilterRequest on HttpContext.Items. The handler retrieves them through
four extension methods.

### GetPageRequest

context.GetPageRequest() returns the PageRequest? produced by the filter,
or null when the filter did not run.

### GetSortRequest

context.GetSortRequest() returns the SortRequest? when the request
included a sort parameter, or null otherwise.

### GetFilterRequest

context.GetFilterRequest() returns the FilterRequest? carrying the
recognized filter parameters, or null when the filter did not run.

### GetEffectivePaginationOptions

context.GetEffectivePaginationOptions() returns the PaginationOptions
that the filter resolved for this request after applying the three
layers. The handler can read the effective page size or max page size
without re-resolving.

## The pagination endpoint filter

The endpoint filter runs on every request to an endpoint that carries
the pagination metadata. It performs the following steps:

- Reads the effective options through the three-layer resolution.
- Parses the page and page size query parameters through IntegerParser
  or the default parser.
- Parses the sort parameter through SortParser or the default parser.
- Recognizes filter parameters through IsFilterParameter or the default
  lenient rule.
- In strict mode, rejects unrecognized parameters with VALIDATION_ERROR.
- Stores the PageRequest, SortRequest, and FilterRequest on HttpContext.Items.

### What it does on every request

The filter does not query data. It does not produce the response. It
validates the query string, produces the request objects, and stores them.
The handler reads them and produces the response.

### Strict mode vs lenient mode

Lenient mode (the default) tolerates unknown query parameters. An
application that treats unknown parameters as filters leaves the default
in place. Strict mode rejects them. An application that wants the strict
behavior sets StrictQueryValidation to true globally or per endpoint.

### The MergeOverrides step

The filter reads the PaginationOverrides attached to the endpoint as
metadata and, if a PaginationMetadataAttribute is also present, merges
the two. The merge logic lives in PaginationResolver.MergeOverrides.
The result is applied on top of the global options through
PaginationResolver.Resolve.

## The pagination result

The handler produces the paginated envelope through two types.

### PagedResultBuilder.From

PagedResultBuilder.From(items, page, pageSize, totalItems) produces a
PagedResult<T> carrying the items and the derived pagination fields.
totalPages, hasNext, and hasPrevious are computed from the inputs.

### .ToPagedResult(meta)

The PagedResult<T>.ToPagedResult(ResponseMetadata meta) extension produces
an IResult that the minimal API pipeline writes. The extension reads the
effective options from HttpContext.Items to determine the success status
code (T9). This means a per-endpoint SuccessStatusCode override is
honored by the result, not only by the filter.

### How the result reads SuccessStatusCode (A-108)

Before Phase 1.5, the result hardcoded 200 and ignored the effective
SuccessStatusCode. The defect was recorded as A-108 and fixed by reading
the effective options from HttpContext.Items.

## The validator: PaginationOptionsValidator

PaginationOptionsValidator is one of the five options validators added in
Phase 1.8. It runs at startup through ValidateOnStart.

### The rules it enforces

- DefaultPageSize must be at least 1.
- MaxPageSize must be at least 1.
- DefaultPageSize must not exceed MaxPageSize.
- PageNumberBase must be non-negative.
- SuccessStatusCode must be a 2xx status code.
- ParameterNames must not be null and must not have any empty entry.

### What happens when validation fails

When ValidateOnStart runs the validator and the validator fails, the
host refuses to start. The failure message names the misconfigured
property. This is the fail-closed startup property. See
[error-contract.md](error-contract.md) for the CONFIGURATION_ERROR
code.

## Compatibility guarantees

- The paginated envelope shape is fixed by SPEC.md.
- The six field names page, pageSize, totalItems, totalPages, hasNext,
  and hasPrevious are stable.
- The three configuration layers and their precedence are stable.
- The ten transformations each have a default and an override point that
  is stable for the current major version.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [response-contract.md](response-contract.md) - the success and error
  envelopes that wrap the pagination object.
- [filtering-sorting.md](filtering-sorting.md) - the sort and filter
  contract that feeds the paginated envelope.
- [error-contract.md](error-contract.md) - the VALIDATION_ERROR code
  used for a page-size violation and for a strict-mode rejection.
- [correlation.md](correlation.md) - the meta.requestId on every
  paginated response.
- `../SPEC.md` - the normative pagination envelope example.

