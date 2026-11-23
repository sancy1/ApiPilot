# ApiPilot.AspNetCore - API Inventory

Generated from XML documentation. This is the parity checklist for language ports.

| Type | Members |
|---|---|
| `ApiPilot.AspNetCore.Configuration.ApiExceptionOptions` | 8 |
| `ApiPilot.AspNetCore.Configuration.ApiExceptionOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions` | 9 |
| `ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.ApiPilotRateLimitOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions` | 1 |
| `ApiPilot.AspNetCore.Configuration.ContentNegotiationOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.CorrelationOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.PaginationOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.Configuration.PaginationOverrides` | 10 |
| `ApiPilot.AspNetCore.Configuration.PaginationResolver` | 3 |
| `ApiPilot.AspNetCore.DependencyInjection.ApiPilotBuilder` | 9 |
| `ApiPilot.AspNetCore.DependencyInjection.ApiPilotServiceCollectionExtensions` | 4 |
| `ApiPilot.AspNetCore.Diagnostics.ApiPilotActivitySource` | 5 |
| `ApiPilot.AspNetCore.Diagnostics.ApiPilotLogEvents` | 18 |
| `ApiPilot.AspNetCore.Diagnostics.ApiPilotRedactionPolicy` | 4 |
| `ApiPilot.AspNetCore.Diagnostics.ApiPilotSafeLogger` | 5 |
| `ApiPilot.AspNetCore.EndpointMetadata.PaginationEndpointExtensions` | 1 |
| `ApiPilot.AspNetCore.EndpointMetadata.PaginationEndpointFilter` | 6 |
| `ApiPilot.AspNetCore.EndpointMetadata.PaginationHttpContextExtensions` | 4 |
| `ApiPilot.AspNetCore.EndpointMetadata.PaginationMetadataAttribute` | 5 |
| `ApiPilot.AspNetCore.EndpointMetadata.TriState` | 3 |
| `ApiPilot.AspNetCore.ExceptionHandling.DefaultApiExceptionMapper` | 2 |
| `ApiPilot.AspNetCore.ExceptionHandling.IApiExceptionMapper` | 1 |
| `ApiPilot.AspNetCore.ExceptionHandling.KnownExceptionType` | 5 |
| `ApiPilot.AspNetCore.ExceptionHandling.KnownExceptionTypes` | 1 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotContentNegotiationExtensions` | 2 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotContentNegotiationMiddleware` | 2 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotCorrelationExtensions` | 2 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotCorrelationMiddleware` | 3 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotExceptionExtensions` | 2 |
| `ApiPilot.AspNetCore.Middleware.ApiPilotExceptionMiddleware` | 2 |
| `ApiPilot.AspNetCore.Middleware.CorrelationEnvelope` | 1 |
| `ApiPilot.AspNetCore.Middleware.HttpContextCorrelationIdAccessor` | 2 |
| `ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiExtensions` | 2 |
| `ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions` | 5 |
| `ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptionsValidator` | 1 |
| `ApiPilot.AspNetCore.OpenApi.ApiPilotSchemaDocument` | 2 |
| `ApiPilot.AspNetCore.OpenApi.ApiPilotSchemaEmitter` | 2 |
| `ApiPilot.AspNetCore.OpenApi.StandardErrorSchema` | 6 |
| `ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitExtensions` | 1 |
| `ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitOptions` | 3 |
| `ApiPilot.AspNetCore.RateLimiting.RateLimitDiagnosticsHook` | 1 |
| `ApiPilot.AspNetCore.RateLimiting.RateLimitReasonCodes` | 4 |
| `ApiPilot.AspNetCore.Results.ApiPilotResultsExtensions` | 5 |
| `ApiPilot.AspNetCore.Results.ApiResponseHttpMapper` | 1 |
| `ApiPilot.AspNetCore.Results.ApiResponseResult` | 3 |
| `ApiPilot.AspNetCore.Results.ApiResponseResult`1` | 3 |
| `ApiPilot.AspNetCore.Results.ErrorResponseResult` | 4 |
| `ApiPilot.AspNetCore.Results.PagedResponseResult`1` | 4 |
| `ApiPilot.AspNetCore.Serialization.DateSerializationMode` | 2 |
| `ApiPilot.AspNetCore.Serialization.EnumSerializationMode` | 2 |
| `ApiPilot.AspNetCore.Serialization.JsonSerializationExtensions` | 2 |
| `ApiPilot.AspNetCore.Serialization.JsonSerializerConfigurator` | 1 |
| `ApiPilot.AspNetCore.Serialization.UnixSecondsDateTimeConverter` | 2 |
| `ApiPilot.AspNetCore.Serialization.UnixSecondsDateTimeOffsetConverter` | 2 |
| `ApiPilot.AspNetCore.Validation.ApiPilotInvalidModelStateResponseFactory` | 1 |
| `ApiPilot.AspNetCore.Validation.ApiPilotValidateAttribute` | 1 |
| `ApiPilot.AspNetCore.Validation.ApiPilotValidationEndpointExtensions` | 2 |
| `ApiPilot.AspNetCore.Validation.ApiPilotValidationEndpointFilter` | 2 |
| `ApiPilot.AspNetCore.Validation.ApiPilotValidationFilter` | 3 |
| `ApiPilot.AspNetCore.Validation.FieldKeyNormalizer` | 1 |
| `ApiPilot.AspNetCore.Validation.ModelStateAdapter` | 2 |
| `ApiPilot.AspNetCore.Validation.ValidationKeyTransforms` | 1 |

---

## `ApiPilot.AspNetCore.Configuration`

### `ApiExceptionOptions`

Mutable configuration for the ApiPilot exception mapping middleware.             Controls which exception types map to which errors and what information        ...

| Member | Kind | Summary |
|---|---|---|
| `ErrorCodeToStatusMap` | property | Optional overrides for the HTTP status code produced for a given             error code. Keys are wire-format error codes (for example             "VALIDATION_E... |
| `IncludeExceptionMessageInLogs` | property | Whether to include the exception message in log output. Defaults to             true. Set to false for applications that process sensitive input where          ... |
| `IncludeExceptionTypeInLogs` | property | Whether to include the exception type name in log output for known             mappings. Defaults to true. Type names are safe for server-side logs             ... |
| `KnownExceptionLogLevel` | property | The log level used when the middleware logs an exception that has a             known mapping (for example, ArgumentException or KeyNotFoundException).         ... |
| `Mappings` | property | System.Xml.XmlElement |
| `RevealExceptionMessageInResponse` | property | Whether to reveal the exception message in the response body for             unknown exceptions. Defaults to false. When true, the response body             use... |
| `RevealExceptionTypeInResponse` | property | Whether to reveal the exception type name in the response body for             unknown exceptions. Defaults to false. When true, the response body             i... |
| `UnknownExceptionLogLevel` | property | The log level used when the middleware logs an exception that has no             known mapping. Defaults to Error. These exceptions are unexpected             f... |

### `ApiExceptionOptionsValidator`

Validates the ApiExceptionOptions configuration at startup. Fails             when the mapping list is empty, when any mapping carries a null             except...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.AspNetCore.Configuration.ApiExceptionOptions)` | method |  |

### `ApiPilotJsonOptions`

Mutable configuration for JSON serialization of ApiPilot responses.             Intended for binding from application configuration and for programmatic        ...

| Member | Kind | Summary |
|---|---|---|
| `AllowTrailingCommas` | property | Whether trailing commas are allowed in incoming JSON. Defaults to             false for strict input parsing. |
| `DateMode` | property | How date and time values are serialized. Defaults to ISO 8601. |
| `DefaultIgnoreCondition` | property | System.Xml.XmlElement |
| `DictionaryKeyPolicy` | property | System.Xml.XmlElement |
| `EnumMode` | property | How enum values are serialized. Defaults to string form for             readability and stability. Set to Number for a strict numeric             wire contract. |
| `MaxDepth` | property | The maximum depth allowed when reading or writing JSON. Defaults             to 64, matching the System.Text.Json default. Set a positive             integer to... |
| `PropertyNamingPolicy` | property | System.Xml.XmlElement |
| `ReadCommentHandling` | property | System.Xml.XmlElement |
| `WriteIndented` | property | Whether JSON is written with indentation. Defaults to false for             compact output. Enable during development for readable response bodies. |

### `ApiPilotJsonOptionsValidator`

Validates the ApiPilotJsonOptions configuration at startup. Fails             when MaxDepth is less than 1 or when any enum-typed property carries             a...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions)` | method |  |

### `ApiPilotRateLimitOptionsValidator`

Validates the ApiPilotRateLimitOptions configuration at startup. Fails             when StatusCode is outside 400-599 or when Message is null, empty, or        ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitOptions)` | method |  |

### `ApiPilotValidationOptions`

Mutable configuration for ApiPilot validation behavior. Intended for             registration through the AddApiPilotValidation extension method. All           ...

| Member | Kind | Summary |
|---|---|---|
| `KeyTransform` | property | System.Xml.XmlElement |

### `ContentNegotiationOptionsValidator`

Validates the ContentNegotiationOptions configuration at startup.             Fails when the acceptable response or request media type sets are             empt...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Core.Configuration.ContentNegotiationOptions)` | method |  |

### `CorrelationOptionsValidator`

Validates the CorrelationOptions configuration at startup. Fails when             HeaderName is empty, when ValidationPattern is not a valid regular            ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Core.Metadata.CorrelationOptions)` | method |  |

### `PaginationOptionsValidator`

Validates the PaginationOptions configuration at startup. Fails when             DefaultPageSize is less than 1, when MaxPageSize is less than 1, when          ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Core.Pagination.PaginationOptions)` | method |  |

### `PaginationOverrides`

Partial overrides for the global PaginationOptions. Each property is             nullable; null means the global value is used unchanged. Non-null             v...

| Member | Kind | Summary |
|---|---|---|
| `DefaultPageSize` | property | The page size to use when the caller does not specify one. Null means             the global DefaultPageSize applies. |
| `IntegerParser` | property | Optional parser for integer query values. Null means the global             IntegerParser applies. |
| `IsFilterParameter` | property | Optional predicate for filter parameter recognition. Null means             the global IsFilterParameter applies. |
| `MaxPageSize` | property | The maximum allowed page size. Null means the global MaxPageSize applies. |
| `PageNumberBase` | property | The base for page numbering. Null means the global PageNumberBase             applies. |
| `ParameterNames` | property | The names of the four control query parameters. Null means             the global ParameterNames applies. |
| `SortDirectionParser` | property | Optional parser for sort direction values. Null means the global             SortDirectionParser applies (which may itself be null, in which             case th... |
| `SortParser` | property | Optional parser for the whole sort expression. Null means the             global SortParser applies. |
| `StrictQueryValidation` | property | Whether the pagination endpoint filter rejects unknown query parameters.             Null means the global StrictQueryValidation applies. |
| `SuccessStatusCode` | property | The HTTP status code used for a successful paginated response.             Null means the global SuccessStatusCode applies. |

### `PaginationResolver`

Resolves the effective pagination options for a request by layering             endpoint-specific overrides on top of the global configuration.

| Member | Kind | Summary |
|---|---|---|
| `FromAttribute(ApiPilot.AspNetCore.EndpointMetadata.PaginationMetadataAttribute)` | method | Converts a PaginationMetadataAttribute into a PaginationOverrides             instance. Returns null when the attribute is null or when none of             its ... |
| `MergeOverrides(ApiPilot.AspNetCore.Configuration.PaginationOverrides,ApiPilot.AspNetCore.Configuration.PaginationOverrides)` | method | Merges two sources of overrides into one. The fluent source carries             the fluent-declared overrides (attached to the endpoint via the             With... |
| `Resolve(ApiPilot.Core.Pagination.PaginationOptions,ApiPilot.AspNetCore.Configuration.PaginationOverrides)` | method | System.Xml.XmlElement |

## `ApiPilot.AspNetCore.DependencyInjection`

### `ApiPilotBuilder`

A thin fluent orchestration layer over the per-concern ApiPilot              registration extensions. Each Configure method delegates to the              corres...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.Extensions.DependencyInjection.IServiceCollection)` | method | Creates a builder around the given service collection. The             constructor is internal because the entry point is the             AddApiPilot extension ... |
| `ConfigureContentNegotiation(System.Action{ApiPilot.Core.Configuration.ContentNegotiationOptions})` | method | Registers the content negotiation options through the per-concern             extension. The callback may be null, in which case the library             default... |
| `ConfigureCorrelation(System.Action{ApiPilot.Core.Metadata.CorrelationOptions})` | method | Registers the correlation options through the per-concern extension.             The callback may be null, in which case the library defaults apply. |
| `ConfigureExceptions(System.Action{ApiPilot.AspNetCore.Configuration.ApiExceptionOptions})` | method | Registers the exception handling options through the per-concern             extension. The callback may be null, in which case the library             defaults... |
| `ConfigureJson(System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions})` | method | Registers the JSON serialization options through the per-concern             extension. The callback may be null, in which case the library             defaults... |
| `ConfigurePagination(System.Action{ApiPilot.Core.Pagination.PaginationOptions})` | method | Registers the pagination options through the per-concern extension.             The callback may be null, in which case the library defaults apply. |
| `ConfigureRateLimitRejection(System.Action{ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitOptions})` | method | Registers the rate-limit rejection options through the per-concern             extension. The callback may be null, in which case the library             defaul... |
| `ConfigureValidation(System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions})` | method | Registers the validation options through the per-concern extension.             The callback may be null, in which case the library defaults apply. |
| `Services` | property | The underlying service collection. Exposed so that a caller can             drop back to the IServiceCollection surface at any point in the             chain. |

### `ApiPilotServiceCollectionExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilot(Microsoft.Extensions.DependencyInjection.IServiceCollection)` | method | Returns a fluent builder that orchestrates the per-concern             ApiPilot registration extensions. Calling this method alone             registers nothing... |
| `AddApiPilotControllers(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions})` | method | Wires the ApiPilot validation envelope into MVC for controllers marked             with [ApiController]. Replaces the default ProblemDetails response with      ... |
| `AddApiPilotPagination(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Core.Pagination.PaginationOptions})` | method | Registers the ApiPilot pagination options. Call this to customize             the global PaginationOptions. Calling this is optional: the             [WithApiPi... |
| `AddApiPilotValidation(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions})` | method | System.Xml.XmlElement |

## `ApiPilot.AspNetCore.Diagnostics`

### `ApiPilotActivitySource`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Name` | field | The stable name of the ApiPilot activity source. |
| `Source` | property | The process-wide activity source. Created once at class             initialization and never disposed. |
| `StartCsrfValidation(System.String)` | method | System.Xml.XmlElement |
| `StartExceptionMapping(System.String)` | method | System.Xml.XmlElement |
| `Version` | field | The version of the ApiPilot activity source. |

### `ApiPilotLogEvents`

The stable event IDs ApiPilot uses for structured logging. Each             constant names the component that owns the ID and the event it             represent...

| Member | Kind | Summary |
|---|---|---|
| `ContentNegotiationUnacceptableAccept` | field | The Accept header did not include an acceptable response media type (406). |
| `ContentNegotiationUnsupportedMediaType` | field | The Content-Type header on a body-carrying method was not acceptable (415). |
| `CorrelationInvalidIdRejected` | field | An incoming correlation ID failed validation and the request was rejected. |
| `CorrelationInvalidIdWarning` | field | An incoming correlation ID failed validation and was replaced. |
| `CsrfRejected` | field | The CSRF middleware rejected a request. |
| `CsrfTransitionBindingUnresolved` | field | The CSRF transition listener could not resolve the previous binding at logout. |
| `CsrfTransitionNoStore` | field | The CSRF transition listener ran without a registered rotation store. |
| `ExceptionAfterResponseStarted` | field | An exception occurred after the response had already started. |
| `ExceptionMappedCritical` | field | Exception mapped to a response at LogLevel.Critical. |
| `ExceptionMappedDebug` | field | Exception mapped to a response at LogLevel.Debug. |
| `ExceptionMappedError` | field | Exception mapped to a response at LogLevel.Error. |
| `ExceptionMappedInformation` | field | Exception mapped to a response at LogLevel.Information. |
| `ExceptionMappedTrace` | field | Exception mapped to a response at LogLevel.Trace. |
| `ExceptionMappedWarning` | field | Exception mapped to a response at LogLevel.Warning. |
| `FetchMetadataRejected` | field | The Fetch Metadata middleware rejected a request. |
| `OriginRejected` | field | The Origin policy middleware rejected a request. |
| `RateLimitRejected` | field | The rate-limit rejection adapter rejected a request. |
| `SecurityDiagnosticsWarning` | field | The security diagnostics emitted a non-fatal startup warning. |

### `ApiPilotRedactionPolicy`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `RedactedNames` | property | The canonical set of names whose values are redacted. The set             is read-only and the comparison is case-insensitive. |
| `RedactedValue` | field | The literal value written in place of a redacted field. |
| `RedactHeader(System.String,System.String)` | method | System.Xml.XmlElement |
| `RedactValue(System.String,System.String)` | method | System.Xml.XmlElement |

### `ApiPilotSafeLogger`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.Extensions.Logging.ILogger)` | method | Creates a decorator over the supplied inner logger. |
| `BeginScope``1(``0)` | method |  |
| `IsEnabled(Microsoft.Extensions.Logging.LogLevel)` | method |  |
| `Log``1(Microsoft.Extensions.Logging.LogLevel,Microsoft.Extensions.Logging.EventId,``0,System.Exception,System.Func{``0,System.Exception,System.String})` | method |  |
| `Wrap(Microsoft.Extensions.Logging.ILogger)` | method | System.Xml.XmlElement |

## `ApiPilot.AspNetCore.EndpointMetadata`

### `PaginationEndpointExtensions`

Fluent extension method that attaches the ApiPilot pagination             endpoint filter to a minimal API endpoint and applies per-endpoint             overrid...

| Member | Kind | Summary |
|---|---|---|
| `WithApiPilotPagination``1(``0,System.Action{ApiPilot.AspNetCore.Configuration.PaginationOverrides})` | method | Attaches the pagination filter to the endpoint and applies the             supplied overrides on top of the global configuration. |

### `PaginationEndpointFilter`

A minimal API endpoint filter that reads pagination query parameters,             validates them against the effective options for the endpoint (global         ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Pagination.PaginationOptions})` | method | Creates the filter with the global pagination options. |
| `EffectiveOptionsKey` | field | Key under which the effective PaginationOptions is stored. |
| `FilterRequestKey` | field | Key under which the validated FilterRequest is stored. |
| `InvokeAsync(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext,Microsoft.AspNetCore.Http.EndpointFilterDelegate)` | method |  |
| `PageRequestKey` | field | Key under which the validated PageRequest is stored. |
| `SortRequestKey` | field | Key under which the validated SortRequest is stored. |

### `PaginationHttpContextExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `GetEffectivePaginationOptions(Microsoft.AspNetCore.Http.HttpContext)` | method | Returns the effective PaginationOptions resolved for the current             request (global defaults plus any endpoint overrides), or null when             the... |
| `GetFilterRequest(Microsoft.AspNetCore.Http.HttpContext)` | method | Returns the validated FilterRequest for the current request, or null             when the pagination filter did not run. |
| `GetPageRequest(Microsoft.AspNetCore.Http.HttpContext)` | method | Returns the validated PageRequest for the current request, or null             when the pagination filter did not run. |
| `GetSortRequest(Microsoft.AspNetCore.Http.HttpContext)` | method | Returns the validated SortRequest for the current request, or null             when no sort was requested or the pagination filter did not run. |

### `PaginationMetadataAttribute`

Applies per-endpoint pagination overrides on top of the global             PaginationOptions. Apply to a controller action, a controller class,             or a...

| Member | Kind | Summary |
|---|---|---|
| `DefaultPageSize` | property | The page size to use when the caller does not specify one. -1 means             the global DefaultPageSize applies. |
| `MaxPageSize` | property | The maximum allowed page size. -1 means the global MaxPageSize applies. |
| `PageNumberBase` | property | The base for page numbering. 1 means the first page is page 1;             0 means the first page is page 0. -1 means the global             PageNumberBase appl... |
| `StrictQueryValidation` | property | Whether the pagination endpoint filter rejects unknown query             parameters. False (the default) leaves the global StrictQueryValidation             unc... |
| `SuccessStatusCode` | property | The HTTP status code used for a successful paginated response.             -1 means the global SuccessStatusCode applies. |

### `TriState`

A three-state value for attribute properties that must distinguish             between "not set" and an explicit boolean value. C# attributes cannot            ...

| Member | Kind | Summary |
|---|---|---|
| `False` | field | The property was explicitly set to false. Overrides a global true. |
| `True` | field | The property was explicitly set to true. Overrides a global false. |
| `Unset` | field | The property was not set. The global configuration value applies             unchanged. |

## `ApiPilot.AspNetCore.ExceptionHandling`

### `DefaultApiExceptionMapper`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.Extensions.Options.IOptions{ApiPilot.AspNetCore.Configuration.ApiExceptionOptions})` | method | Creates a mapper that uses the given options. |
| `Map(System.Exception,Microsoft.AspNetCore.Http.HttpContext)` | method |  |

### `IApiExceptionMapper`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Map(System.Exception,Microsoft.AspNetCore.Http.HttpContext)` | method | System.Xml.XmlElement |

### `KnownExceptionType`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.Type,ApiPilot.Core.Errors.ApiErrorCode,System.String,System.Boolean)` | method | System.Xml.XmlElement |
| `Code` | property | The error code to associate with this exception type. |
| `ExceptionType` | property | The CLR type to match. |
| `IncludeExceptionMessage` | property | Whether to use the exception message instead of the safe default. |
| `SafeMessage` | property | A safe default message used on the wire. |

### `KnownExceptionTypes`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Default` | property | System.Xml.XmlElement |

## `ApiPilot.AspNetCore.Middleware`

### `ApiPilotContentNegotiationExtensions`

Extension methods that register the ApiPilot content negotiation              middleware and its options.

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotContentNegotiation(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Core.Configuration.ContentNegotiationOptions})` | method | Registers the content negotiation options. |
| `UseApiPilotContentNegotiation(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the content negotiation middleware to the pipeline. Place this             after the correlation and exception middleware. |

### `ApiPilotContentNegotiationMiddleware`

Rejects requests whose Accept header does not include an acceptable              response media type with HTTP 406, and requests whose Content-Type             ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Configuration.ContentNegotiationOptions},Microsoft.Extensions.Logging.ILogger{ApiPilot.AspNetCore.Middleware.ApiPilotContentNegotiationMiddleware},ApiPilot.Core.Metadata.ICorrelationIdAccessor)` | method | Creates the middleware. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method |  |

### `ApiPilotCorrelationExtensions`

Extension methods that register the ApiPilot correlation middleware             and its supporting services.

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotCorrelation(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Core.Metadata.CorrelationOptions})` | method | Registers the correlation options, the default generator, the             HttpContext-based accessor, and the HttpContextAccessor if needed. |
| `UseApiPilotCorrelation(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the correlation middleware to the pipeline. Place this early,             before any middleware that needs the correlation ID. |

### `ApiPilotCorrelationMiddleware`

Reads the incoming correlation ID from the configured header,             validates it, generates a replacement when required, stores the             effective ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Metadata.CorrelationOptions},ApiPilot.Core.Metadata.ICorrelationIdGenerator,Microsoft.Extensions.Logging.ILogger{ApiPilot.AspNetCore.Middleware.ApiPilotCorrelationMiddleware})` | method | Creates the middleware. |
| `CorrelationIdKey` | field | The HttpContext.Items key under which the effective correlation             ID is stored for the duration of the request. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method | Executes the middleware. See the class summary for the exact             behavior at each step. |

### `ApiPilotExceptionExtensions`

Extension methods that register the ApiPilot exception middleware and             its supporting services.

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotExceptions(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.Configuration.ApiExceptionOptions})` | method | System.Xml.XmlElement |
| `UseApiPilotExceptions(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the exception middleware to the pipeline. Place this early in             the pipeline, immediately after any security middleware that must run            ... |

### `ApiPilotExceptionMiddleware`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,ApiPilot.AspNetCore.ExceptionHandling.IApiExceptionMapper,Microsoft.Extensions.Options.IOptions{ApiPilot.AspNetCore.Configuration.ApiExceptionOptions},Microsoft.Extensions.Logging.ILogger{ApiPilot.AspNetCore.Middleware.ApiPilotExceptionMiddleware},ApiPilot.Core.Metadata.ICorrelationIdAccessor,Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Metadata.CorrelationOptions})` | method | Creates the middleware. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method | Executes the middleware. If a downstream exception is thrown and the             response has not yet started, a mapped error response is written.             I... |

### `CorrelationEnvelope`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `ResolveRequestId(Microsoft.AspNetCore.Http.HttpContext,ApiPilot.Core.Metadata.ICorrelationIdAccessor,Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Metadata.CorrelationOptions})` | method | System.Xml.XmlElement |

### `HttpContextCorrelationIdAccessor`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.IHttpContextAccessor)` | method | Creates the accessor. |
| `RequestId` | property |  |

## `ApiPilot.AspNetCore.OpenApi`

### `ApiPilotOpenApiExtensions`

Registers the OpenAPI options and maps the OpenAPI document endpoint.

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotOpenApi(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions})` | method | Registers the OpenAPI options through the standard options             pipeline, wires startup validation, and returns the service             collection for ch... |
| `MapApiPilotOpenApi(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)` | method | Maps a GET endpoint that returns the OpenAPI document at the             configured path. The path is read from             ApiPilotOpenApiOptions.DocumentPath. |

### `ApiPilotOpenApiOptions`

The overridable surface for the repository-owned OpenAPI document.             Every property has a sensible default and may be replaced by the             appl...

| Member | Kind | Summary |
|---|---|---|
| `DocumentPath` | property | System.Xml.XmlElement |
| `DocumentTitle` | property | System.Xml.XmlElement |
| `DocumentVersion` | property | System.Xml.XmlElement |
| `IncludeErrorSchemas` | property | When true, the standard error and validation schemas are included             in the generated document. Defaults to true. Applications that do             not ... |
| `IncludePaginationSchema` | property | When true, the pagination schema is included in the generated             document. Defaults to true. |

### `ApiPilotOpenApiOptionsValidator`

Validates the ApiPilotOpenApiOptions configuration at startup. Fails             when the document title or version is null, empty, or whitespace, or           ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions)` | method |  |

### `ApiPilotSchemaDocument`

Builds the OpenAPI document skeleton: the openapi version string, the             info object, an empty paths object (the emitter fills it), and the            ...

| Member | Kind | Summary |
|---|---|---|
| `Build(ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions)` | method | Builds the document skeleton for the given options. The paths             object is empty; the emitter adds path items. |
| `OpenApiVersion` | field | The OpenAPI specification version emitted in the document. |

### `ApiPilotSchemaEmitter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `BuildDocument(Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionGroupCollectionProvider,ApiPilot.AspNetCore.OpenApi.ApiPilotOpenApiOptions)` | method | Builds the full OpenAPI document from the descriptors and options. |
| `s_supportedMethods` | field | The set of HTTP methods OpenAPI 3.0.3 recognizes as path-item             operations. A method outside this set is represented through a             vendor exte... |

### `StandardErrorSchema`

The canonical error-response schema for the ApiPilot wire contract,             derived from SPEC.md. Exposes the error envelope schema and its             nest...

| Member | Kind | Summary |
|---|---|---|
| `BuildEnvelopeSchema` | method | Builds the error envelope schema: success (always false), error,             and meta. Only success and error are required. |
| `BuildErrorObjectSchema` | method | Builds the nested error object schema: code, message, and an             optional fields map. Code and message are required. |
| `BuildFieldMapSchema` | method | Builds the field map schema: an object whose values are arrays of             message strings. Present only for validation failures. |
| `EnvelopeSchemaName` | field | The name under which the error envelope schema is published in             components.schemas. |
| `ErrorObjectSchemaName` | field | The name under which the nested error object schema is published             in components.schemas. |
| `FieldMapSchemaName` | field | The name under which the field map schema is published in             components.schemas. |

## `ApiPilot.AspNetCore.RateLimiting`

### `ApiPilotRateLimitExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotRateLimitRejection(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.RateLimiting.ApiPilotRateLimitOptions})` | method | System.Xml.XmlElement |

### `ApiPilotRateLimitOptions`

The overridable surface for the ApiPilot rate-limit rejection. Every             property has a sensible default and may be replaced by the application.

| Member | Kind | Summary |
|---|---|---|
| `EmitRetryAfter` | property | When true, a Retry-After header is emitted if (and only if) the             platform rate-limit lease supplies Retry-After metadata. ApiPilot             never ... |
| `Message` | property | The safe message written in the error envelope. Defaults to a fixed             ApiPilot string. SPEC.md defines no normative message for             RATE_LIMIT... |
| `StatusCode` | property | The HTTP status code written for a rate-limit rejection. Defaults to             429 (Too Many Requests), the SPEC.md mapping for RATE_LIMITED. Must be         ... |

### `RateLimitDiagnosticsHook`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `HandleAsync(Microsoft.AspNetCore.RateLimiting.OnRejectedContext,System.Threading.CancellationToken)` | method | System.Xml.XmlElement |

### `RateLimitReasonCodes`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `LimitExceeded` | field | The configured permit limit was exceeded. |
| `None` | field | No specific reason was supplied by the platform lease. |
| `QueueLimit` | field | The configured queue limit was exceeded. |
| `Unknown` | field | A denial reason that ApiPilot does not recognize. |

## `ApiPilot.AspNetCore.Results`

### `ApiPilotResultsExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `ToPagedResult``1(ApiPilot.Core.Pagination.PagedResult{``0},ApiPilot.Core.Metadata.ResponseMetadata)` | method | System.Xml.XmlElement |
| `ToResult(ApiPilot.Core.Errors.ErrorResponse)` | method | System.Xml.XmlElement |
| `ToResult(ApiPilot.Core.Responses.ApiResponse)` | method | System.Xml.XmlElement |
| `ToResult``1(ApiPilot.Core.Responses.ApiResponse{``0})` | method | System.Xml.XmlElement |
| `ToResult``1(ApiPilot.Core.Responses.PagedApiResponse{``0})` | method | System.Xml.XmlElement |

### `ApiResponseHttpMapper`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Map(ApiPilot.Core.Responses.ApiResponseStatus)` | method | Returns the HTTP status code for the given response category. |

### `ApiResponseResult`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Responses.ApiResponse)` | method | Creates a result from the given non-generic response. |
| `ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext)` | method |  |
| `Response` | property | The wrapped response. Exposed internally for test inspection. |

### `ApiResponseResult`1`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Responses.ApiResponse{`0})` | method | Creates a result from the given response. |
| `ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext)` | method |  |
| `Response` | property | The wrapped response. Exposed internally for test inspection. |

### `ErrorResponseResult`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Errors.ErrorResponse,ApiPilot.AspNetCore.Configuration.ApiExceptionOptions)` | method | Creates a result from the given error response. |
| `ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext)` | method |  |
| `ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext)` | method |  |
| `Response` | property | The wrapped error response. Exposed internally for test inspection. |

### `PagedResponseResult`1`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Core.Responses.PagedApiResponse{`0})` | method | Creates a result from the given paginated response. |
| `ExecuteAsync(Microsoft.AspNetCore.Http.HttpContext)` | method |  |
| `ExecuteResultAsync(Microsoft.AspNetCore.Mvc.ActionContext)` | method |  |
| `Response` | property | The wrapped response. Exposed internally for test inspection. |

## `ApiPilot.AspNetCore.Serialization`

### `DateSerializationMode`

How date and time values are serialized in ApiPilot JSON responses.

| Member | Kind | Summary |
|---|---|---|
| `Iso8601` | field | Dates are serialized as ISO 8601 strings, for example             "2026-09-21T14:30:00Z" for DateTimeOffset, or             "2026-09-21T14:30:00" for DateTime. ... |
| `UnixTimeSeconds` | field | Dates are serialized as Unix epoch seconds (integer). Some clients             and some legacy systems prefer this form. It is compact but less             read... |

### `EnumSerializationMode`

How enum values are serialized in ApiPilot JSON responses.

| Member | Kind | Summary |
|---|---|---|
| `AsString` | field | Enums are serialized as their names, for example "Confirmed".             This is the default. It is more readable and it is stable if             the underlyin... |
| `Number` | field | Enums are serialized as their underlying integer values, for             example 0. Use this when a strict numeric wire contract is             required. |

### `JsonSerializationExtensions`

Extension methods that configure ApiPilot JSON conventions on the             ASP.NET Core JSON serializers. Two overloads are provided: one for             app...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotJson(Microsoft.Extensions.DependencyInjection.IMvcBuilder,System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions})` | method | System.Xml.XmlElement |
| `AddApiPilotJson(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions})` | method | System.Xml.XmlElement |

### `JsonSerializerConfigurator`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Configure(System.Text.Json.JsonSerializerOptions,ApiPilot.AspNetCore.Configuration.ApiPilotJsonOptions)` | method | Applies the given options to the target serializer options. The target             is mutated in place. Converters are registered only if an instance of        ... |

### `UnixSecondsDateTimeConverter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Read(System.Text.Json.Utf8JsonReader@,System.Type,System.Text.Json.JsonSerializerOptions)` | method |  |
| `Write(System.Text.Json.Utf8JsonWriter,System.DateTime,System.Text.Json.JsonSerializerOptions)` | method |  |

### `UnixSecondsDateTimeOffsetConverter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Read(System.Text.Json.Utf8JsonReader@,System.Type,System.Text.Json.JsonSerializerOptions)` | method |  |
| `Write(System.Text.Json.Utf8JsonWriter,System.DateTimeOffset,System.Text.Json.JsonSerializerOptions)` | method |  |

## `ApiPilot.AspNetCore.Validation`

### `ApiPilotInvalidModelStateResponseFactory`

Produces the standard ApiPilot validation envelope when MVC has already             determined that ModelState is invalid for a controller marked with          ...

| Member | Kind | Summary |
|---|---|---|
| `Create(Microsoft.AspNetCore.Mvc.ActionContext)` | method | Builds the validation envelope for the given ActionContext. This is             the method that is assigned to ApiBehaviorOptions.InvalidModelStateResponseFacto... |

### `ApiPilotValidateAttribute`

Applies the ApiPilot validation filter to a controller action or to every             action in a controller. The filter checks ModelState before the action    ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor` | method | Creates the attribute. The framework resolves the underlying filter             from DI on each request. |

### `ApiPilotValidationEndpointExtensions`

Extension methods that attach the ApiPilot validation filter to a             minimal API route handler. Two forms are provided: a raw callback that            ...

| Member | Kind | Summary |
|---|---|---|
| `WithApiPilotValidation``1(``0,System.Func{Microsoft.AspNetCore.Http.HttpContext,System.Collections.Generic.IReadOnlyDictionary{System.String,System.Collections.Generic.IReadOnlyList{System.String}}})` | method | Attaches a validation filter using a callback that returns raw key to             messages pairs. The keys are normalized using the KeyTransform             con... |
| `WithApiPilotValidation``1(``0,System.Func{Microsoft.AspNetCore.Http.HttpContext,System.Collections.Generic.IReadOnlyList{ApiPilot.Core.Errors.ApiErrorField}})` | method | Attaches a validation filter using a callback that returns ApiErrorField             values. The callback receives the HttpContext and returns field errors,    ... |

### `ApiPilotValidationEndpointFilter`

A minimal API endpoint filter that runs an application-supplied validation             callback before the endpoint handler. If the callback returns one or more...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.Func{Microsoft.AspNetCore.Http.HttpContext,System.Collections.Generic.IReadOnlyList{ApiPilot.Core.Errors.ApiErrorField}})` | method | Creates a filter with the given validation callback. |
| `InvokeAsync(Microsoft.AspNetCore.Http.EndpointFilterInvocationContext,Microsoft.AspNetCore.Http.EndpointFilterDelegate)` | method |  |

### `ApiPilotValidationFilter`

MVC action filter that checks ModelState before the action runs. When             the model state is invalid, the filter short-circuits the pipeline and        ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.Extensions.Options.IOptions{ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions},Microsoft.Extensions.Options.IOptions{ApiPilot.AspNetCore.Configuration.ApiExceptionOptions},Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Metadata.CorrelationOptions})` | method | Creates the filter, resolving the effective key transform once. |
| `OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext)` | method |  |
| `OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext)` | method |  |

### `FieldKeyNormalizer`

Default key normalizer for validation field names. Converts the various             key formats produced by ASP.NET Core model binding into a single canonical  ...

| Member | Kind | Summary |
|---|---|---|
| `Normalize(System.String)` | method | Normalizes a field key into camelCase with bracket-preserved indices. |

### `ModelStateAdapter`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `ToErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary)` | method | Converts the model state to a list of field errors using the default             key normalizer. This is equivalent to calling the two-argument             over... |
| `ToErrors(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary,System.Func{System.String,System.String})` | method | System.Xml.XmlElement |

### `ValidationKeyTransforms`

Resolves the effective field key transform from ApiPilotValidationOptions.             This is the single source of truth for the precedence rule:             t...

| Member | Kind | Summary |
|---|---|---|
| `Resolve(ApiPilot.AspNetCore.Configuration.ApiPilotValidationOptions)` | method | Returns the effective key transform for the given options. |

