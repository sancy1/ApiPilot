<!--
filepath: docs/filtering-sorting.md
package:  n/a (repository root docs)
since:    v0.2.0
purpose:  The ApiPilot filtering and sorting contract that feeds the paginated envelope.
-->

# The ApiPilot filtering and sorting contract

This document describes how a request declares a sort and a set of
filters, how the library validates those declarations, and how the
resulting SortRequest and FilterRequest reach the endpoint handler.
Filtering and sorting are query-time inputs, not wire-time outputs. The
output envelope carries the paginated page of data; the sort and the
filters are what the client sent in the query string. The normative
contract is `../SPEC.md`; where this document and SPEC.md disagree,
SPEC.md wins.

## What this document covers

- Where filtering and sorting live in the pipeline.
- The sort contract: the field, the direction, and the two override
  points that let an application change the wire syntax.
- The filter contract: the default lenient rule, the strict rule, and
  the override predicate.
- The query-validation result that carries the validated inputs.
- The wire shape of a sorted, filtered, paginated response.

## Where filtering and sorting live

Filtering and sorting are concerns of the query string, not concerns of
the response body. The response carries the page of data and the
pagination metadata. The query string carries the sort field, the sort
direction, and the filter parameters. The library parses and validates
the query string and hands the results to the handler.

### They are query metadata, not a separate envelope

There is no "sort" object and no "filter" object in the wire envelope.
The response envelope carries success, data, pagination, and meta. The
sort and the filters are inputs the client sent; they do not appear in
the response. A client that needs to know what sort was applied reads
the pagination object and its own request state, not a response field.

### The relationship to pagination

Sorting and filtering share the pagination options with pagination
itself. The SortDirectionParser, SortParser, IsFilterParameter, and
IntegerParser override points all live on PaginationOptions because
they are all part of the same query-validation step. The pagination
endpoint filter runs the sort and filter parsing before it stores the
PageRequest on HttpContext.Items. See [pagination.md](pagination.md) for
the full pipeline and the three-layer precedence rules.

## The sort contract

A sort declaration is a field name and a direction. The two-part model
is the library default.

### The two-part model: field + direction

By default, the client sends the sort field in one query parameter and
the sort direction in a second. The parameter names are configured by
PaginationOptions.ParameterNames: sort and direction by default. A
request that asks for the createdAt field descending looks like:

    GET /items?page=1&pageSize=20&sort=createdAt&direction=desc

### The default direction tokens: asc, desc (case-insensitive)

The library accepts asc and desc. The comparison is case-insensitive.
The direction token is translated to a SortDirection value.

### The override: PaginationOptions.SortDirectionParser

SortDirectionParser is a Func<string, SortDirection?>? property. When
null, the library uses the default asc/desc parser. When set, the
library calls the delegate with the direction token. The delegate
returns a SortDirection or null to indicate an unrecognized token.
An application whose frontend uses ascending/descending, +/-, or
up/down supplies a parser that maps its tokens.

### The override: PaginationOptions.SortParser

SortParser is a Func<string, string, SortRequest?>? property. When null,
the library uses the two-part model: it reads the sort field from one
parameter and the direction from another. When set, the library calls
the delegate with the field and the direction values. The delegate
returns a SortRequest or null. An application whose frontend encodes
the direction in the field supplies a parser that reads the sign or
the suffix.

### The direction-in-field convention (application-supplied)

A common convention is to prefix the field with a sign: -createdAt for
descending, createdAt or +createdAt for ascending. This convention is
not the default. An application that wants it supplies a SortParser
that strips the sign, reads the direction, and returns a SortRequest.

## The filter contract

A filter is a query parameter that is not a control parameter. The
library distinguishes controls from filters and either tolerates or
rejects the latter based on a mode.

### The default: any non-control query parameter is a filter

In lenient mode (the default), every query parameter that is not named
by PaginationOptions.ParameterNames is treated as a filter. The library
collects those parameters into a FilterRequest and stores the request
on HttpContext.Items. The library does not interpret the filter values;
it does not know what "status=active" means. The handler interprets.

### The override: PaginationOptions.IsFilterParameter

IsFilterParameter is a Func<string, bool>? property. When null, the
library uses the lenient rule: any key not named by ParameterNames is a
filter. When set, the library calls the delegate with the parameter key.
The delegate returns true if the parameter is a filter and false if it
is not recognized. An application that restricts filter keys to a known
prefix (for example, filter.) supplies a predicate.

### Strict mode: unknown parameters are rejected

When PaginationOptions.StrictQueryValidation is true (globally or per
endpoint), the endpoint filter rejects any query parameter that is not
a control parameter and not recognized as a filter. The response is
the standard error envelope with VALIDATION_ERROR and a message naming
the rejected parameter. Strict mode uses IsFilterParameter to decide
what counts as a filter; when IsFilterParameter is null and strict mode
is true, every non-control parameter is rejected because nothing is
classified as a filter.

### Lenient mode: unknown parameters are passed through

When StrictQueryValidation is false (the default), the endpoint filter
collects unknown parameters into the FilterRequest without rejecting
them. The handler receives the FilterRequest and can inspect it. A
parameter the handler does not understand is ignored by the handler.

## The query-validation result

The endpoint filter produces a QueryValidationResult internally and
stores the individual requests on HttpContext.Items. The handler reads
them through the accessor extensions.

### How a validated query reaches the handler

The filter runs before the endpoint. It parses the query string, builds
the PageRequest, the SortRequest, and the FilterRequest, and stores them
on HttpContext.Items under documented keys. The handler reads them
through the accessor extensions. The handler never parses the query
string itself.

### Retrieving the sort request: GetSortRequest

context.GetSortRequest() returns the SortRequest? when the request
included a sort parameter, or null otherwise. A SortRequest carries the
field name and the SortDirection.

### Retrieving the filter request: GetFilterRequest

context.GetFilterRequest() returns the FilterRequest? carrying the
recognized filter parameters, or null when the filter did not run. A
FilterRequest carries the parameter names and their raw string values.
The handler interprets the values.

## The wire shape of a sorted, filtered, paginated response

The response envelope carries only the page of data and the pagination
metadata. The sort and the filters do not appear in the response.

### The pagination object

The pagination object carries page, pageSize, totalItems, totalPages,
hasNext, and hasPrevious. These describe the page the server chose, not
the sort or the filters the client sent.

### The absence of sort and filter in the envelope

The sort field, the sort direction, and the filter values are not echoed
in the response. A client that needs to remember what it asked for keeps
that state on the client. This is deliberate: the response describes the
result, not the request. A response that echoed the request would double
the wire size for no client benefit.

## Compatibility guarantees

- The default two-part sort model is stable.
- The default direction tokens asc and desc, case-insensitive, are stable.
- The default lenient filter rule is stable.
- The SortDirectionParser, SortParser, and IsFilterParameter override
  points are stable.
- The sort and the filters do not appear in the wire envelope, and that
  will not change without a major version bump.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [pagination.md](pagination.md) - the pagination contract, the three
  configuration layers, and the endpoint filter that runs the sort and
  filter parsing.
- [response-contract.md](response-contract.md) - the success, collection,
  and paginated envelopes.
- [validation.md](validation.md) - the VALIDATION_ERROR envelope used
  when strict mode rejects an unknown parameter.
- `../SPEC.md` - the normative paginated envelope example and the
  "Response envelope - collection" shape.

