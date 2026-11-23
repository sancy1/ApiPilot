# ApiPilot.Core - API Inventory

Generated from XML documentation. This is the parity checklist for language ports.

| Type | Members |
|---|---|
| `ApiPilot.Core.Abstractions.IApiResponse` | 2 |
| `ApiPilot.Core.Abstractions.IApiResponseBuilder` | 4 |
| `ApiPilot.Core.Configuration.ContentNegotiationOptions` | 5 |
| `ApiPilot.Core.Errors.ApiError` | 5 |
| `ApiPilot.Core.Errors.ApiErrorCode` | 17 |
| `ApiPilot.Core.Errors.ApiErrorCodeJsonConverter` | 2 |
| `ApiPilot.Core.Errors.ApiErrorField` | 4 |
| `ApiPilot.Core.Errors.ApiErrorFieldsConverter` | 2 |
| `ApiPilot.Core.Errors.ErrorResponse` | 4 |
| `ApiPilot.Core.Errors.IApiErrorMapper` | 1 |
| `ApiPilot.Core.Metadata.CorrelationIdValidator` | 2 |
| `ApiPilot.Core.Metadata.CorrelationInvalidIdPolicy` | 3 |
| `ApiPilot.Core.Metadata.CorrelationOptions` | 6 |
| `ApiPilot.Core.Metadata.DefaultCorrelationIdGenerator` | 1 |
| `ApiPilot.Core.Metadata.ICorrelationIdAccessor` | 1 |
| `ApiPilot.Core.Metadata.ICorrelationIdGenerator` | 1 |
| `ApiPilot.Core.Metadata.RequestMetadata` | 6 |
| `ApiPilot.Core.Metadata.ResponseMetadata` | 5 |
| `ApiPilot.Core.Pagination.FilterRequest` | 3 |
| `ApiPilot.Core.Pagination.PagedResult`1` | 3 |
| `ApiPilot.Core.Pagination.PagedResultBuilder` | 1 |
| `ApiPilot.Core.Pagination.PageRequest` | 4 |
| `ApiPilot.Core.Pagination.PaginationMetadata` | 7 |
| `ApiPilot.Core.Pagination.PaginationOptions` | 10 |
| `ApiPilot.Core.Pagination.QueryParameterNames` | 5 |
| `ApiPilot.Core.Pagination.QueryValidationResult` | 7 |
| `ApiPilot.Core.Pagination.SortDirection` | 2 |
| `ApiPilot.Core.Pagination.SortDirectionExtensions` | 2 |
| `ApiPilot.Core.Pagination.SortRequest` | 3 |
| `ApiPilot.Core.Responses.ApiResponse` | 5 |
| `ApiPilot.Core.Responses.ApiResponse`1` | 6 |
| `ApiPilot.Core.Responses.ApiResponseBuilder` | 5 |
| `ApiPilot.Core.Responses.ApiResponseStatus` | 12 |
| `ApiPilot.Core.Responses.DefaultApiResponseBuilder` | 4 |
| `ApiPilot.Core.Responses.PagedApiResponse`1` | 5 |
| `ApiPilot.Core.Validation.IValidationErrorSource` | 1 |
| `ApiPilot.Core.Validation.ValidationErrors` | 7 |
| `ApiPilot.Core.Validation.ValidationResponseFactory` | 3 |

---

## `ApiPilot.Core.Abstractions`

### `IApiResponse`

Marker interface implemented by every ApiPilot response envelope,             successful or not. Exposes only the two members common to every             respon...

| Member | Kind | Summary |
|---|---|---|
| `Meta` | property | Non-business metadata attached to every response: the correlation             ID, the server timestamp, and an optional dictionary of extra values             t... |
| `Success` | property | True for a successful response; false for an error response. |

### `IApiResponseBuilder`

Contract for building ApiPilot success responses from application-owned             data. The builder attaches the standard envelope, response category,        ...

| Member | Kind | Summary |
|---|---|---|
| `Accepted``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a successful response representing work accepted for processing.             The category is Accepted (HTTP 202). |
| `Created``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a successful response representing a newly created resource.             The category is Created (HTTP 201). |
| `NoContent(ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a successful response without a payload. The typical use             is a 204 No Content outcome after a successful state change. |
| `Ok``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a successful response carrying a payload. The category             defaults to Ok (HTTP 200). |

## `ApiPilot.Core.Configuration`

### `ContentNegotiationOptions`

Mutable configuration for the ApiPilot content negotiation middleware.             The defaults match the content negotiation section of SPEC.md: JSON only,    ...

| Member | Kind | Summary |
|---|---|---|
| `AcceptableRequestMediaTypes` | property | The set of media types the middleware will accept in a request Content-Type             header on body-carrying methods. Defaults to a single entry, application... |
| `AcceptableResponseMediaTypes` | property | The set of media types the middleware will serve in a response. Defaults             to a single entry, application/json. Comparison is case-insensitive. |
| `AcceptMissingContentType` | property | Whether a request on a body-carrying method with no Content-Type header             is accepted. Defaults to false, which rejects such requests with HTTP 415.  ... |
| `AcceptWildcard` | property | Whether the full wildcard */* in the Accept header is treated as             accepting any media type. Defaults to true, which matches the RFC 7231             ... |
| `BodyCarryingMethods` | property | The HTTP methods that are considered to carry a body and therefore require             a Content-Type header. Defaults to POST, PUT, and PATCH. Comparison is   ... |

## `ApiPilot.Core.Errors`

### `ApiError`

An immutable error object carrying a machine-readable code, a safe             human-readable message, and an optional dictionary of field-level             err...

| Member | Kind | Summary |
|---|---|---|
| `Code` | property | The machine-readable error code. |
| `Create(ApiPilot.Core.Errors.ApiErrorCode,System.String)` | method | Creates an error without field-level details. |
| `Create(ApiPilot.Core.Errors.ApiErrorCode,System.String,System.Collections.Generic.IEnumerable{ApiPilot.Core.Errors.ApiErrorField})` | method | Creates an error, optionally carrying field-level details. The input             field values are copied into a read-only dictionary keyed by field             ... |
| `Fields` | property | Optional field-level errors, present only for validation failures.             Keyed by field name; each value is the array of messages for that             fie... |
| `Message` | property | A short, safe, human-readable summary of the error. Always non-null             and non-empty. |

### `ApiErrorCode`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `AuthenticationRequired` | property | Authentication is required but missing. HTTP 401 on the wire. |
| `Code` | property | The wire value of the code. Always non-null and non-empty. Uppercase             by construction. |
| `ConfigurationError` | property | Startup configuration error. Not a response code; fails closed at boot. |
| `Conflict` | property | Conflict with current resource state. HTTP 409 on the wire. |
| `CsrfHeaderMissing` | property | Missing CSRF header. HTTP 403 on the wire. |
| `CsrfOriginRejected` | property | Origin rejected by policy. HTTP 403 on the wire. |
| `CsrfTokenExpired` | property | Expired CSRF token. HTTP 403 on the wire. |
| `CsrfTokenInvalid` | property | Invalid CSRF token. HTTP 403 on the wire. |
| `Forbidden` | property | Authorization failure. HTTP 403 on the wire. |
| `From(System.String)` | method | Creates a code from the given string. Null, empty, and whitespace             values are rejected. The value is trimmed and uppercased so that             "vali... |
| `InternalError` | property | Unknown server-side failure. HTTP 500 on the wire. |
| `NotAcceptable` | property | The Accept header did not include any acceptable media type. HTTP 406 on the wire. |
| `RateLimited` | property | Rate limit exceeded. HTTP 429 on the wire. |
| `ResourceNotFound` | property | Resource not found. HTTP 404 on the wire. |
| `ToString` | method | Returns the wire value of the code. |
| `UnsupportedMediaType` | property | The request Content-Type on a body-carrying method was not acceptable. HTTP 415 on the wire. |
| `ValidationError` | property | Validation failure. HTTP 400 or 422 on the wire. |

### `ApiErrorCodeJsonConverter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Read(System.Text.Json.Utf8JsonReader@,System.Type,System.Text.Json.JsonSerializerOptions)` | method |  |
| `Write(System.Text.Json.Utf8JsonWriter,ApiPilot.Core.Errors.ApiErrorCode,System.Text.Json.JsonSerializerOptions)` | method |  |

### `ApiErrorField`

An immutable pairing of a field name and the array of validation             messages associated with that field. Used inside the fields dictionary             ...

| Member | Kind | Summary |
|---|---|---|
| `Create(System.String,System.Collections.Generic.IEnumerable{System.String})` | method | Creates a field error from a field name and a collection of messages.             The messages are copied into an immutable array. Null or empty             arg... |
| `Field` | property | The field name as it appears on the wire, for example "email" or             "orders[0].email". Always non-null and non-empty. |
| `Messages` | property | The validation messages for this field. Always non-null and             contains at least one message. The collection is an immutable             snapshot taken... |
| `WithMessage(System.String,System.String)` | method | Creates a field error carrying a single message. |

### `ApiErrorFieldsConverter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Read(System.Text.Json.Utf8JsonReader@,System.Type,System.Text.Json.JsonSerializerOptions)` | method |  |
| `Write(System.Text.Json.Utf8JsonWriter,System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IReadOnlyList{System.String}},System.Text.Json.JsonSerializerOptions)` | method |  |

### `ErrorResponse`

The ApiPilot error response envelope. Success is always false. The             Error property carries a stable, machine-readable code and a safe             hum...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Errors.ApiError,ApiPilot.Core.Metadata.ResponseMetadata)` | method | Creates an error response envelope. |
| `Error` | property | The error object: code, safe message, and optional field errors. |
| `Meta` | property | Non-business metadata attached to every response: the correlation             ID, the server timestamp, and optional extras. |
| `Success` | property | Always false. Error responses are the failure side of the envelope. |

### `IApiErrorMapper`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `TryMap(System.Exception,ApiPilot.Core.Errors.ApiError@)` | method | System.Xml.XmlElement |

## `ApiPilot.Core.Metadata`

### `CorrelationIdValidator`

Validates incoming correlation identifiers against a configured regular             expression pattern. The regex is compiled once at construction so that      ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.String)` | method | System.Xml.XmlElement |
| `IsValid(System.String)` | method | Returns true when the value matches the validation pattern. A null             value is always invalid. |

### `CorrelationInvalidIdPolicy`

The policy applied by the correlation middleware when an incoming             correlation ID is present but fails validation. The default is             Replace...

| Member | Kind | Summary |
|---|---|---|
| `Reject` | field | Reject the request with a VALIDATION_ERROR response. Use this             when a malformed incoming ID indicates a misconfigured client             and the requ... |
| `Replace` | field | Discard the invalid incoming ID and generate a new one. This is             the default and is safe for every deployment. |
| `UseAsIs` | field | Use the invalid incoming ID as-is. The middleware logs a             warning. Use this when the application has its own convention             for accepting IDs... |

### `CorrelationOptions`

Mutable configuration for correlation ID handling. Intended for binding             from application configuration. The default values reflect the ApiPilot     ...

| Member | Kind | Summary |
|---|---|---|
| `EchoInResponseBody` | property | Whether the correlation ID is included in the response envelope             meta.requestId. Defaults to true. |
| `EchoInResponseHeader` | property | Whether the correlation ID is echoed in a response header. Defaults             to true. |
| `HeaderName` | property | The header name read from incoming requests and echoed on responses.             Defaults to X-Request-Id. |
| `InvalidIncomingIdPolicy` | property | The policy applied when an incoming correlation ID is present but             fails validation. Defaults to Replace, which discards the invalid             valu... |
| `ValidateIncoming` | property | System.Xml.XmlElement |
| `ValidationPattern` | property | The regular expression pattern applied to incoming correlation IDs             when validation is enabled. Defaults to 8 to 128 alphanumeric,             unders... |

### `DefaultCorrelationIdGenerator`

Default correlation ID generator. Produces a version 7 GUID in the             standard hyphenated form. Version 7 GUIDs are time-ordered and             unpred...

| Member | Kind | Summary |
|---|---|---|
| `NewId` | method |  |

### `ICorrelationIdAccessor`

Read-only access to the correlation identifier for the current             request. Implementations read from an ambient context such as             HttpContext...

| Member | Kind | Summary |
|---|---|---|
| `RequestId` | property | The correlation identifier for the current context, or null when             no correlation ID has been established (for example, in code that             runs ... |

### `ICorrelationIdGenerator`

Produces new correlation identifiers. Implementations must be             thread-safe because the generator is invoked once per request and             requests...

| Member | Kind | Summary |
|---|---|---|
| `NewId` | method | System.Xml.XmlElement |

### `RequestMetadata`

Immutable metadata describing an incoming ApiPilot request. Populated             by the ASP.NET Core adapter from the HttpContext and carried through          ...

| Member | Kind | Summary |
|---|---|---|
| `Empty` | property | A request metadata instance with the current UTC timestamp and             empty string fields. Useful as a starting point in tests and as             an intern... |
| `Method` | property | The HTTP method (for example, GET, POST, PUT, PATCH, DELETE).             Uppercase by convention. Empty string until the adapter sets it. |
| `Path` | property | The request path without the query string. Empty string until the             adapter sets it. |
| `ReceivedAt` | property | The UTC instant at which the request was received by the server. |
| `RequestId` | property | The correlation identifier for this request. Read from the             configured header if present and valid, otherwise generated. |
| `WithRequestId(System.String)` | method | Returns a copy of this metadata with the request identifier replaced.             The original instance is unchanged. |

### `ResponseMetadata`

Non-business metadata attached to every ApiPilot response envelope.             Carries the request correlation ID, the server timestamp of response            ...

| Member | Kind | Summary |
|---|---|---|
| `Empty` | property | A metadata instance with the current UTC timestamp, an empty             request ID, and no extra values. Useful as a starting point in             tests and as... |
| `Extra` | property | Optional application-defined metadata. Never null; the empty             dictionary is returned when no extra values are provided so that             consumers ... |
| `RequestId` | property | System.Xml.XmlElement |
| `Timestamp` | property | The UTC instant at which the response was produced. Captured by             the middleware or builder that constructed the metadata. |
| `WithRequestId(System.String)` | method | Returns a copy of this metadata with the request identifier replaced.             The original instance is unchanged. |

## `ApiPilot.Core.Pagination`

### `FilterRequest`

An immutable set of filter criteria expressed as explicit key/value             pairs. The library does not parse arbitrary filter expressions and             d...

| Member | Kind | Summary |
|---|---|---|
| `Create(System.Collections.Generic.IEnumerable{System.Collections.Generic.KeyValuePair{System.String,System.String}})` | method | Creates a filter request from a sequence of key/value pairs. Keys             must be non-null and non-empty. Values may be empty strings.             Duplicate... |
| `Empty` | property | The canonical empty filter request. |
| `Pairs` | property | The filter pairs, keyed by filter name. Never null; may be empty. |

### `PagedResult`1`

An immutable wrapper pairing a page of items with its pagination             metadata. Applications return a PagedResult from their query services;             ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.Collections.Generic.IReadOnlyList{`0},ApiPilot.Core.Pagination.PaginationMetadata)` | method | Creates a paginated result from a set of items and its pagination             metadata. The item collection is defensively copied. |
| `Items` | property | The items on this page. Never null; may be empty. An immutable             snapshot taken at construction. |
| `Pagination` | property | The pagination metadata describing the position of this page within             the full result set. |

### `PagedResultBuilder`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `From``1(System.Collections.Generic.IEnumerable{``0},System.Int32,System.Int32,System.Int32)` | method | System.Xml.XmlElement |

### `PageRequest`

A validated request for a page of results. Page numbers are 1-based.             Instances can only be created through the factories, which enforce             ...

| Member | Kind | Summary |
|---|---|---|
| `Create(System.Int32,System.Int32,ApiPilot.Core.Pagination.PaginationOptions)` | method | Creates a page request from explicit page and page size values,             validated against the given options. |
| `FromQuery(System.Nullable{System.Int32},System.Nullable{System.Int32},ApiPilot.Core.Pagination.PaginationOptions)` | method | System.Xml.XmlElement |
| `Page` | property | The requested 1-based page number. Always at least 1. |
| `PageSize` | property | System.Xml.XmlElement |

### `PaginationMetadata`

Immutable pagination metadata attached to a paginated response. Page             numbers are 1-based. Derived values (TotalPages, HasNext, HasPrevious)         ...

| Member | Kind | Summary |
|---|---|---|
| `Create(System.Int32,System.Int32,System.Int32)` | method | Creates pagination metadata. The arguments must satisfy: Page >= 1,             PageSize >= 1, TotalItems >= 0. |
| `HasNext` | property | True when there is a page after the current one. |
| `HasPrevious` | property | True when there is a page before the current one. |
| `Page` | property | The current page number, 1-based. |
| `PageSize` | property | The number of items per page. |
| `TotalItems` | property | The total number of items across all pages. |
| `TotalPages` | property | The total number of pages. Equals ceil(TotalItems / PageSize) with             a minimum of 1 so that an empty collection still has a page 1. |

### `PaginationOptions`

Mutable configuration for pagination defaults and limits. This is a             plain data holder intended for binding from application configuration           ...

| Member | Kind | Summary |
|---|---|---|
| `DefaultPageSize` | property | System.Xml.XmlElement |
| `IntegerParser` | property | System.Xml.XmlElement |
| `IsFilterParameter` | property | System.Xml.XmlElement |
| `MaxPageSize` | property | The maximum allowed page size. Requests for a larger page size are             rejected by the application endpoint filter. Defaults to 100. Must be            ... |
| `PageNumberBase` | property | The base for page numbering. 1 means the first page is page 1.             0 means the first page is page 0. Defaults to 1. |
| `ParameterNames` | property | System.Xml.XmlElement |
| `SortDirectionParser` | property | System.Xml.XmlElement |
| `SortParser` | property | System.Xml.XmlElement |
| `StrictQueryValidation` | property | When true, the ASP.NET Core pagination endpoint filter rejects             requests that contain query parameters it does not recognize.             Defaults to... |
| `SuccessStatusCode` | property | The HTTP status code used for a successful paginated response.             Defaults to 200 (OK). Applications that follow RFC 7233 range             semantics m... |

### `QueryParameterNames`

Configurable names for the four control query parameters used by the             pagination endpoint filter: the page number, the page size, the sort           ...

| Member | Kind | Summary |
|---|---|---|
| `Default` | property | The default parameter names. This instance is shared and should not             be mutated; applications that need different names create their own             ... |
| `Direction` | property | The name of the sort direction parameter. Defaults to "direction". |
| `Page` | property | The name of the page number parameter. Defaults to "page". |
| `PageSize` | property | The name of the page size parameter. Defaults to "pageSize". |
| `Sort` | property | The name of the sort field parameter. Defaults to "sort". |

### `QueryValidationResult`

A discriminated result representing either a successful query validation             or a validation failure. On success, carries the validated page, sort,     ...

| Member | Kind | Summary |
|---|---|---|
| `Errors` | property | System.Xml.XmlElement |
| `Failure(ApiPilot.Core.Validation.ValidationErrors)` | method | Produces a failed result carrying validation errors. |
| `Filters` | property | System.Xml.XmlElement |
| `IsSuccess` | property | True when the query was valid and the request objects are populated. |
| `Page` | property | System.Xml.XmlElement |
| `Sort` | property | System.Xml.XmlElement |
| `Success(ApiPilot.Core.Pagination.PageRequest,ApiPilot.Core.Pagination.SortRequest,ApiPilot.Core.Pagination.FilterRequest)` | method | Produces a successful result carrying the validated requests. |

### `SortDirection`

The direction of a sort. Transport-neutral.

| Member | Kind | Summary |
|---|---|---|
| `Ascending` | field | Ascending order. |
| `Descending` | field | Descending order. |

### `SortDirectionExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `ToWireValue(ApiPilot.Core.Pagination.SortDirection)` | method | Returns the wire value for the direction: "asc" or "desc". |
| `TryParseWireValue(System.String,ApiPilot.Core.Pagination.SortDirection@)` | method | Attempts to parse a wire value into a sort direction. Accepts             "asc" and "desc" case-insensitively. Returns false for any other             input. |

### `SortRequest`

A validated sort request describing the field to order by and the             direction. The field name is not validated against an application             allo...

| Member | Kind | Summary |
|---|---|---|
| `Create(System.String,ApiPilot.Core.Pagination.SortDirection)` | method | Creates a sort request from a field name and direction. |
| `Direction` | property | The sort direction. |
| `Field` | property | The field to sort by. Non-null and non-empty. |

## `ApiPilot.Core.Responses`

### `ApiResponse`

A successful ApiPilot response that carries no payload. Used for             NoContent (204) and any other success case where the server has             nothing...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Responses.ApiResponseStatus,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Creates a successful response with the given category and metadata. |
| `Message` | property | An optional human-readable message. Null when the application             does not provide one. |
| `Meta` | property | Non-business metadata attached to every response: the correlation             ID, the server timestamp, and optional extras. |
| `Status` | property | The transport-neutral category of the response. Used by the             ASP.NET Core adapter to select an HTTP status code. |
| `Success` | property | Always true for this type. Error responses are represented by a             separate type. |

### `ApiResponse`1`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(`0,ApiPilot.Core.Responses.ApiResponseStatus,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Creates a successful response carrying the given payload. |
| `Data` | property | The application payload. May be null when the caller explicitly             passes null or when the type argument itself is nullable. |
| `Message` | property | An optional human-readable message. Null when the application             does not provide one. |
| `Meta` | property | Non-business metadata attached to every response: the correlation             ID, the server timestamp, and optional extras. |
| `Status` | property | The transport-neutral category of the response. Used by the             ASP.NET Core adapter to select an HTTP status code. |
| `Success` | property | Always true for this type. Error responses are represented by a             separate type. |

### `ApiResponseBuilder`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Accepted``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | System.Xml.XmlElement |
| `Created``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | System.Xml.XmlElement |
| `Default` | field | The shared default builder instance. Stateless and safe to reuse             across threads and requests. |
| `NoContent(ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | System.Xml.XmlElement |
| `Ok``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | System.Xml.XmlElement |

### `ApiResponseStatus`

Transport-neutral category of an ApiPilot response. Core assigns a             category when it builds a response; the ASP.NET Core adapter (Phase 1)           ...

| Member | Kind | Summary |
|---|---|---|
| `Accepted` | field | The request was accepted for processing but not yet completed. |
| `BadRequest` | field | The request was rejected because it was malformed or failed validation. |
| `Conflict` | field | The request conflicts with the current state of the resource. |
| `Created` | field | A resource was successfully created. The payload is the created resource. |
| `Forbidden` | field | The authenticated caller was not permitted to perform the operation. |
| `NoContent` | field | The request succeeded and there is no payload to return. |
| `NotFound` | field | The requested resource was not found. |
| `Ok` | field | A successful response carrying a payload or a message. |
| `ServerError` | field | An unexpected server-side failure occurred. |
| `TooManyRequests` | field | The caller exceeded an allowed rate of requests. |
| `Unauthorized` | field | The request lacked valid authentication credentials. |
| `Unprocessable` | field | The request was well-formed but could not be processed. |

### `DefaultApiResponseBuilder`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Accepted``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method |  |
| `Created``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method |  |
| `NoContent(ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method |  |
| `Ok``1(``0,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method |  |

### `PagedApiResponse`1`

A successful ApiPilot response carrying a page of items and the             pagination metadata that describes the page's position within the             full r...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.Collections.Generic.IReadOnlyList{`0},ApiPilot.Core.Pagination.PaginationMetadata,ApiPilot.Core.Metadata.ResponseMetadata)` | method | Creates a paginated success response. |
| `Data` | property | The items on this page. Never null; may be empty. The wire shape             is a flat JSON array. |
| `Meta` | property | Non-business metadata attached to every response: the correlation             ID, the server timestamp, and optional extras. |
| `Pagination` | property | The pagination metadata describing the position of this page             within the full result set. |
| `Success` | property | Always true for this type. Error responses are represented by a             separate type. |

## `ApiPilot.Core.Validation`

### `IValidationErrorSource`

A framework-neutral source of validation field errors. Implementations             translate the results of an application validator (ASP.NET Core model        ...

| Member | Kind | Summary |
|---|---|---|
| `GetErrors` | method | Produces the validation field errors from this source. The returned             list may be empty. It must not contain null entries. |

### `ValidationErrors`

An immutable, deterministic collection of validation field errors.             Fields are sorted by name (ordinal, case-sensitive) and merged by field          ...

| Member | Kind | Summary |
|---|---|---|
| `Count` | property | The number of unique field names in the collection. |
| `Empty` | property | The canonical empty collection. |
| `Fields` | property | The sorted, deduplicated list of field errors. |
| `From(System.Collections.Generic.IEnumerable{ApiPilot.Core.Errors.ApiErrorField})` | method | Creates a collection from a sequence of field errors. Nulls and             null entries are rejected. Fields are merged by name and sorted. |
| `IsEmpty` | property | True when the collection contains no field errors. |
| `With(ApiPilot.Core.Errors.ApiErrorField)` | method | Returns a new collection with the additional field error merged in. |
| `WithRange(System.Collections.Generic.IEnumerable{ApiPilot.Core.Errors.ApiErrorField})` | method | Returns a new collection with the additional field errors merged in. |

### `ValidationResponseFactory`

Builds standard validation error responses. The factory aggregates the             supplied field errors into a deterministic ValidationErrors,             wrap...

| Member | Kind | Summary |
|---|---|---|
| `DefaultMessage` | field | The default message used when the caller does not supply one. |
| `FromFields(System.Collections.Generic.IEnumerable{ApiPilot.Core.Errors.ApiErrorField},ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a validation error response from a collection of field errors. |
| `FromSource(ApiPilot.Core.Validation.IValidationErrorSource,ApiPilot.Core.Metadata.ResponseMetadata,System.String)` | method | Builds a validation error response from a validation error source. |

