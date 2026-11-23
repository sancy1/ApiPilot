# ApiPilot.Security - API Inventory

Generated from XML documentation. This is the parity checklist for language ports.

| Type | Members |
|---|---|
| `ApiPilot.Security.Configuration.CookieProfileOptionsValidator` | 1 |
| `ApiPilot.Security.Configuration.CsrfOptionsValidator` | 1 |
| `ApiPilot.Security.Configuration.FetchMetadataOptionsValidator` | 1 |
| `ApiPilot.Security.Configuration.OriginPolicyOptionsValidator` | 1 |
| `ApiPilot.Security.Cookies.ApiPilotCookiesExtensions` | 1 |
| `ApiPilot.Security.Cookies.AuthenticationCookieProfile` | 2 |
| `ApiPilot.Security.Cookies.CookieProfile` | 6 |
| `ApiPilot.Security.Cookies.CookieProfileOptions` | 4 |
| `ApiPilot.Security.Cookies.CookieProfileValidator` | 1 |
| `ApiPilot.Security.Cookies.CookieSameSite` | 4 |
| `ApiPilot.Security.Cookies.CsrfCookieProfile` | 1 |
| `ApiPilot.Security.Cookies.HostPrefixValidator` | 4 |
| `ApiPilot.Security.Csrf.ApiPilotRequireCsrfAttribute` | 1 |
| `ApiPilot.Security.Csrf.ApiPilotSkipCsrfAttribute` | 1 |
| `ApiPilot.Security.Csrf.CsrfBindingFailureReason` | 5 |
| `ApiPilot.Security.Csrf.CsrfBindingProvider` | 2 |
| `ApiPilot.Security.Csrf.CsrfBootstrapEndpoint` | 1 |
| `ApiPilot.Security.Csrf.CsrfBootstrapExtensions` | 1 |
| `ApiPilot.Security.Csrf.CsrfBootstrapJsonContext` | 7 |
| `ApiPilot.Security.Csrf.CsrfBootstrapOptions` | 2 |
| `ApiPilot.Security.Csrf.CsrfBootstrapResponse` | 2 |
| `ApiPilot.Security.Csrf.CsrfEndpointMetadata` | 2 |
| `ApiPilot.Security.Csrf.CsrfEndpointPolicyResolver` | 1 |
| `ApiPilot.Security.Csrf.CsrfMiddleware` | 2 |
| `ApiPilot.Security.Csrf.CsrfMiddlewareExtensions` | 1 |
| `ApiPilot.Security.Csrf.CsrfOptions` | 10 |
| `ApiPilot.Security.Csrf.CsrfPolicy` | 3 |
| `ApiPilot.Security.Csrf.CsrfRotationPolicy` | 3 |
| `ApiPilot.Security.Csrf.CsrfRotationRequirement` | 2 |
| `ApiPilot.Security.Csrf.CsrfService` | 4 |
| `ApiPilot.Security.Csrf.CsrfServiceExtensions` | 1 |
| `ApiPilot.Security.Csrf.CsrfToken` | 3 |
| `ApiPilot.Security.Csrf.CsrfTokenFormat` | 3 |
| `ApiPilot.Security.Csrf.CsrfTokenOptions` | 4 |
| `ApiPilot.Security.Csrf.CsrfTokenParseResult` | 8 |
| `ApiPilot.Security.Csrf.CsrfTokenValidateOutcome` | 4 |
| `ApiPilot.Security.Csrf.CsrfTransitionExtensions` | 1 |
| `ApiPilot.Security.Csrf.CsrfValidationContext` | 5 |
| `ApiPilot.Security.Csrf.CsrfValidationResult` | 5 |
| `ApiPilot.Security.Csrf.DataProtectionCsrfTokenSigner` | 4 |
| `ApiPilot.Security.Csrf.DefaultCsrfTransitionListener` | 4 |
| `ApiPilot.Security.Csrf.ICsrfBindingProvider` | 1 |
| `ApiPilot.Security.Csrf.ICsrfRotationStore` | 3 |
| `ApiPilot.Security.Csrf.ICsrfService` | 3 |
| `ApiPilot.Security.Csrf.ICsrfTokenSigner` | 3 |
| `ApiPilot.Security.Csrf.ICsrfTransitionListener` | 3 |
| `ApiPilot.Security.DataProtection.ApiPilotDataProtectionExtensions` | 1 |
| `ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions` | 5 |
| `ApiPilot.Security.DataProtection.DataProtectionStartupDiagnostics` | 2 |
| `ApiPilot.Security.DataProtection.DataProtectionStartupHostedService` | 3 |
| `ApiPilot.Security.DataProtection.DevelopmentOnlyWarning` | 1 |
| `ApiPilot.Security.DataProtection.IDataProtectionStartupDiagnostics` | 1 |
| `ApiPilot.Security.DataProtection.InstanceSafetyValidator` | 1 |
| `ApiPilot.Security.DataProtection.SharedKeyConfiguration` | 1 |
| `ApiPilot.Security.Diagnostics.ApiPilotCsrfCounters` | 9 |
| `ApiPilot.Security.Diagnostics.ApiPilotCsrfObservabilityExtensions` | 1 |
| `ApiPilot.Security.Diagnostics.ApiPilotSecurityDiagnostics` | 3 |
| `ApiPilot.Security.Diagnostics.ApiPilotSecurityDiagnosticsExtensions` | 1 |
| `ApiPilot.Security.Diagnostics.DiagnosticCodes` | 4 |
| `ApiPilot.Security.Diagnostics.SecurityConfigurationValidator` | 2 |
| `ApiPilot.Security.Diagnostics.SecurityDiagnostic` | 4 |
| `ApiPilot.Security.Diagnostics.SecurityDiagnosticLevel` | 2 |
| `ApiPilot.Security.Diagnostics.SecurityDiagnosticsHostedService` | 3 |
| `ApiPilot.Security.Origin.FetchMetadataEvaluator` | 1 |
| `ApiPilot.Security.Origin.FetchMetadataMiddleware` | 2 |
| `ApiPilot.Security.Origin.FetchMetadataMiddlewareExtensions` | 2 |
| `ApiPilot.Security.Origin.FetchMetadataOptions` | 4 |
| `ApiPilot.Security.Origin.FetchMetadataProfile` | 3 |
| `ApiPilot.Security.Origin.OriginMatchMode` | 3 |
| `ApiPilot.Security.Origin.OriginMiddleware` | 2 |
| `ApiPilot.Security.Origin.OriginMiddlewareExtensions` | 2 |
| `ApiPilot.Security.Origin.OriginParts` | 4 |
| `ApiPilot.Security.Origin.OriginPolicyOptions` | 7 |
| `ApiPilot.Security.Origin.OriginValidator` | 3 |

---

## `ApiPilot.Security.Configuration`

### `CookieProfileOptionsValidator`

Validates the CookieProfileOptions configuration at startup. Fails              when the NamePrefix is not one of the defined prefixes, when the              ef...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.Cookies.CookieProfileOptions)` | method |  |

### `CsrfOptionsValidator`

Validates the CsrfOptions configuration at startup. Fails when the             header name is empty, when the bootstrap path is empty or does not             st...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.Csrf.CsrfOptions)` | method |  |

### `FetchMetadataOptionsValidator`

Validates the FetchMetadataOptions configuration at startup. When             the profile is Off the inner checks are skipped; the policy is             disable...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.Origin.FetchMetadataOptions)` | method |  |

### `OriginPolicyOptionsValidator`

Validates the OriginPolicyOptions configuration at startup. Fails             when the policy is enabled and no request could ever pass, when a             conf...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.Origin.OriginPolicyOptions)` | method |  |

## `ApiPilot.Security.Cookies`

### `ApiPilotCookiesExtensions`

Service collection extensions that register the cookie profile              options and the startup validator.

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotCookies(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Security.Cookies.CookieProfileOptions})` | method | Registers the cookie profile options and the startup validator. |

### `AuthenticationCookieProfile`

The default authentication and session cookie profiles. Both set             Secure, HttpOnly, and a narrow SameSite value. The authentication             profi...

| Member | Kind | Summary |
|---|---|---|
| `Default` | property | The default authentication cookie profile. Secure, HttpOnly,             SameSite=Lax, Path=/, no Domain (host-only). The base name is             "auth"; the e... |
| `SessionDefault` | property | The default session cookie profile. Secure, HttpOnly,             SameSite=Strict, Path=/, no Domain (host-only). The base name             is "session"; the ef... |

### `CookieProfile`

A value object describing a cookie profile. The profile carries the              base name without a prefix, the two security flags, the SameSite              v...

| Member | Kind | Summary |
|---|---|---|
| `Domain` | property | The cookie domain. When null, the cookie is host-only, which is             the strongest setting. When set, the value must be a valid             hostname with... |
| `HttpOnly` | property | Whether the cookie is marked HttpOnly. When true, browser             JavaScript cannot read the cookie value. Required for cookies             that carry a cre... |
| `Name` | property | The base cookie name, without any prefix. Must be non-empty and             must not contain any of the characters that RFC 6265 forbids in             a cookie... |
| `Path` | property | The cookie path. Must be non-empty and must start with a slash.             Defaults to "/" which is the widest possible scope. Choose the             narrowest... |
| `SameSite` | property | The SameSite attribute value. See CookieSameSite for the defined             values and their trade-offs. |
| `Secure` | property | Whether the cookie is marked Secure. When true, the browser sends             the cookie only over HTTPS. Required for cookies whose effective             name ... |

### `CookieProfileOptions`

Options that carry the cookie profiles the host validates at              startup. Every profile is validated against the security contract              in SPEC...

| Member | Kind | Summary |
|---|---|---|
| `AuthenticationProfile` | property | The authentication cookie profile. Defaults to Secure,             HttpOnly, SameSite=Lax, Path=/. An application that needs             different values replac... |
| `CsrfProfile` | property | The CSRF double-submit cookie profile. Defaults to Secure,             HttpOnly=false (readable by JavaScript), SameSite=Lax. The             profile is only me... |
| `NamePrefix` | property | The prefix applied to every cookie name. Defaults to the empty             string (no prefix). The defined values are "", "__Host-", and             "__Secure-"... |
| `SessionProfile` | property | The session cookie profile. Defaults to Secure, HttpOnly,             SameSite=Strict, Path=/. An application that needs different             values replaces t... |

### `CookieProfileValidator`

Validates a single cookie profile against the security contract in              SPEC.md. The validator is a pure function. It adds a failure message            ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.Cookies.CookieProfile,System.Collections.Generic.IList{System.String})` | method | Validates the profile against the effective name. The effective             name is the composed CookieProfileOptions.NamePrefix + the             profile Name. |

### `CookieSameSite`

The SameSite attribute value for a cookie profile. Mirrors the             three values defined by RFC 6265bis. ApiPilot defines its own             enum so the...

| Member | Kind | Summary |
|---|---|---|
| `Lax` | field | The browser sends the cookie on same-site requests and on             top-level cross-site navigations. The default for the             authentication cookie pr... |
| `None` | field | The browser sends the cookie on same-site and cross-site             requests. The value requires Secure=true. The weakest of the             three values and t... |
| `Strict` | field | The browser sends the cookie only on same-site requests. The             default for the session cookie profile. The strongest of the             three values, ... |
| `Unspecified` | field | The SameSite attribute is not set. Some older clients default             to Lax when the attribute is absent; others treat the cookie as             cross-site... |

### `CsrfCookieProfile`

The default CSRF double-submit cookie profile. The profile is              provided for applications whose legacy clients cannot hold a CSRF              reques...

| Member | Kind | Summary |
|---|---|---|
| `Default` | property | The default CSRF double-submit cookie profile. Secure,             HttpOnly=false (readable by JavaScript), SameSite=Lax,             Path=/, no Domain (host-on... |

### `HostPrefixValidator`

Validates the RFC 6265bis prefix rules on a cookie name. The              effective name is the NamePrefix from CookieProfileOptions composed              with ...

| Member | Kind | Summary |
|---|---|---|
| `HostPrefix` | field | The RFC 6265bis __Host- prefix. |
| `IsDefinedPrefix(System.String)` | method | Returns true when the given prefix is one of the defined             prefixes or the empty string. Used by the options validator to             reject unknown p... |
| `SecurePrefix` | field | The RFC 6265bis __Secure- prefix. |
| `Validate(System.String,ApiPilot.Security.Cookies.CookieProfile,System.Collections.Generic.IList{System.String})` | method | Validates the prefix rules on the effective name and the             profile attributes. Adds a failure message for each violated             rule to the failur... |

## `ApiPilot.Security.Csrf`

### `ApiPilotRequireCsrfAttribute`

Marks an endpoint as explicitly protected by CSRF validation. The             endpoint is enforced regardless of the request method, the global             Prot...

| Member | Kind | Summary |
|---|---|---|
| `PopulateMetadata(System.Reflection.MethodInfo,Microsoft.AspNetCore.Builder.EndpointBuilder)` | method |  |

### `ApiPilotSkipCsrfAttribute`

Marks an endpoint as exempt from CSRF protection. The endpoint             bypasses the middleware regardless of the request method and the             global P...

| Member | Kind | Summary |
|---|---|---|
| `PopulateMetadata(System.Reflection.MethodInfo,Microsoft.AspNetCore.Builder.EndpointBuilder)` | method |  |

### `CsrfBindingFailureReason`

The reason a CSRF binding could not be resolved for the current request.             A binding failure is a normal, expected outcome. The library never         ...

| Member | Kind | Summary |
|---|---|---|
| `NoConfiguredSource` | field | No binding source is available and no pre-auth source is configured.             This is the fail-closed default for an unauthenticated request when            ... |
| `None` | field | No binding failure occurred. This value is not returned by a             provider; it exists so that the enum has an explicit success value             and so t... |
| `NoSession` | field | The current request has no established session. A server-managed             session is a valid binding source only after the session has been             estab... |
| `NoSubject` | field | The current request is not authenticated and has no stable subject             claim. Pre-auth issuance is only possible when the application             config... |
| `ProviderFailure` | field | A configured binding source ran but failed to produce a value. This             includes a custom ICsrfBindingProvider that returned false and a             pre... |

### `CsrfBindingProvider`

The default CSRF binding provider. Resolves a stable, non-secret              binding from the current request using a fixed resolution order:              subj...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.Csrf.CsrfOptions)` | method | Creates the provider. |
| `TryGetBinding(Microsoft.AspNetCore.Http.HttpContext,System.String@,ApiPilot.Security.Csrf.CsrfBindingFailureReason@)` | method |  |

### `CsrfBootstrapEndpoint`

The handler logic for the CSRF bootstrap endpoint. The endpoint is a              GET that returns { "token": "..." } in the response body. It issues a         ...

| Member | Kind | Summary |
|---|---|---|
| `HandleAsync(Microsoft.AspNetCore.Http.HttpContext,ApiPilot.Security.Csrf.ICsrfService,ApiPilot.Security.Csrf.CsrfBootstrapOptions,ApiPilot.Core.Metadata.ICorrelationIdAccessor)` | method | Handles a GET request to the bootstrap path. |

### `CsrfBootstrapExtensions`

Endpoint routing extensions that register the CSRF bootstrap endpoint.              The endpoint is a GET that responds with a bare { "token": "..." }          ...

| Member | Kind | Summary |
|---|---|---|
| `MapApiPilotCsrf(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)` | method | Maps the CSRF bootstrap endpoint on the configured path. |

### `CsrfBootstrapJsonContext`

Source-generated JSON context for the bootstrap response. Avoids             reflection-based serialization on the hot path.

| Member | Kind | Summary |
|---|---|---|
| `#ctor` | method |  |
| `#ctor(System.Text.Json.JsonSerializerOptions)` | method |  |
| `CsrfBootstrapResponse` | property | Defines the source generated JSON serialization contract metadata for a given type. |
| `Default` | property | System.Xml.XmlElement |
| `GeneratedSerializerOptions` | property | The source-generated options associated with this context. |
| `GetTypeInfo(System.Type)` | method |  |
| `String` | property | Defines the source generated JSON serialization contract metadata for a given type. |

### `CsrfBootstrapOptions`

Bootstrap endpoint configuration. Two Option D points: the cache              control header value and whether the endpoint is enabled. The              respons...

| Member | Kind | Summary |
|---|---|---|
| `CacheControl` | property | The Cache-Control header value set on the bootstrap response.             Defaults to "no-store" so that intermediaries do not cache the             token. An a... |
| `Enabled` | property | Whether the bootstrap endpoint is enabled. Defaults to true.             When false, MapApiPilotCsrf registers nothing. |

### `CsrfBootstrapResponse`

The wire shape of the CSRF bootstrap response. A bare object with a             single token string. The shape is fixed by SPEC.md. The property name           ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.String)` | method | Creates a bootstrap response. |
| `Token` | property | The CSRF request token. Pinned to the lowercase wire key "token". |

### `CsrfEndpointMetadata`

The endpoint metadata attached by the CSRF protection attributes. The             middleware reads the metadata from the endpoint and applies the             po...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.Csrf.CsrfPolicy)` | method | The endpoint metadata attached by the CSRF protection attributes. The             middleware reads the metadata from the endpoint and applies the             po... |
| `Policy` | property | The endpoint CSRF policy. |

### `CsrfEndpointPolicyResolver`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `Resolve(Microsoft.AspNetCore.Http.HttpContext)` | method | System.Xml.XmlElement |

### `CsrfMiddleware`

Enforces CSRF protection on state-changing requests. The middleware              runs after authentication so that the CSRF binding is the              authenti...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,ApiPilot.Security.Csrf.ICsrfService,Microsoft.Extensions.Options.IOptions{ApiPilot.Security.Csrf.CsrfOptions},Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Csrf.CsrfMiddleware},ApiPilot.Core.Metadata.ICorrelationIdAccessor,Microsoft.Extensions.Options.IOptions{ApiPilot.Core.Metadata.CorrelationOptions})` | method | Creates the middleware. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method | Executes the middleware. Reads the endpoint policy and the request             method; short-circuits when the request is exempt; validates the             toke... |

### `CsrfMiddlewareExtensions`

Application builder extensions that add the CSRF protection              middleware to the pipeline.

| Member | Kind | Summary |
|---|---|---|
| `UseApiPilotCsrfProtection(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the CSRF protection middleware to the pipeline. |

### `CsrfOptions`

Service-level configuration for the CSRF service and middleware. The              defaults match the security contract in SPEC.md. Every property is an         ...

| Member | Kind | Summary |
|---|---|---|
| `BootstrapPath` | property | The bootstrap endpoint path. Defaults to /api/csrf. The             middleware exempts this path from CSRF protection so the client             can obtain a tok... |
| `CodeMapping` | property | The mapping from an internal CsrfTokenParseResult value to the             public wire code the middleware writes. The defaults match the             wire contr... |
| `ExemptPaths` | property | Paths exempt from CSRF protection in addition to the bootstrap             path. Defaults to empty. The middleware evaluates the effective             exemption... |
| `ExemptPredicate` | property | An optional predicate that decides whether a request is exempt             from CSRF protection. When null, only ExemptPaths and the             bootstrap path ... |
| `HeaderName` | property | The header name the middleware reads for the CSRF token. Defaults             to X-CSRF-TOKEN. |
| `MissingHeaderCode` | property | The public wire code for a missing CSRF header. Defaults to             CSRF_HEADER_MISSING. This mapping is separate from CodeMapping             because a mis... |
| `PreAuthBindingSource` | property | System.Xml.XmlElement |
| `ProtectedMethods` | property | The HTTP methods the middleware protects. Defaults to POST, PUT,             PATCH, and DELETE. Comparison is case-insensitive. Safe methods             (GET, H... |
| `RotationPolicy` | property | The rotation policy. Defaults to OnBootstrap. |
| `RotationRequirement` | property | The rotation requirement. Defaults to Optional. When Required, the             host fails at startup unless an ICsrfRotationStore is registered. |

### `CsrfPolicy`

The endpoint-level CSRF policy. Set by the [ApiPilotSkipCsrf] and             [ApiPilotRequireCsrf] attributes and read by the middleware. The             prece...

| Member | Kind | Summary |
|---|---|---|
| `Require` | field | The endpoint is explicitly protected. Enforced regardless of the             request method, the global ProtectedMethods set, and any             [ApiPilotSkipC... |
| `Skip` | field | The endpoint is explicitly exempt. Bypassed regardless of the             request method and the global ProtectedMethods set. Ignored when             [ApiPilot... |
| `UseGlobal` | field | The endpoint uses the global policy. This is the default when no             attribute is present. A protected method is enforced, a safe             method pas... |

### `CsrfRotationPolicy`

The rotation policy for the CSRF service. The policy decides when a             new token is issued for the same binding without an explicit             applica...

| Member | Kind | Summary |
|---|---|---|
| `Never` | field | Tokens are never rotated automatically. Only an explicit             RotateAsync call rotates. Applications that control rotation             externally use thi... |
| `OnBootstrap` | field | A new bootstrap request issues a fresh token and records a             rotation marker. This is the default. Every call to the bootstrap             endpoint ro... |
| `OnWindow` | field | A new token is issued only when the previous token is within a             configurable window of expiry. Applications that want to avoid             rotating o... |

### `CsrfRotationRequirement`

The rotation requirement. Decides whether the host refuses to start             when no ICsrfRotationStore is registered.

| Member | Kind | Summary |
|---|---|---|
| `Optional` | field | Rotation is optional. When no store is registered, RotateAsync             issues a new token and reports that the previous token remains             valid unti... |
| `Required` | field | Rotation is required. The host fails at startup when no             ICsrfRotationStore is registered. An application that enables             this setting asser... |

### `CsrfService`

The default CSRF service. Composes the signer, the binding provider,              and an optional rotation store. The service is the public entry point         ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.Csrf.ICsrfTokenSigner,ApiPilot.Security.Csrf.ICsrfBindingProvider,ApiPilot.Security.Csrf.CsrfOptions,ApiPilot.Security.Csrf.CsrfTokenOptions,Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Csrf.CsrfService},ApiPilot.Security.Csrf.ICsrfRotationStore)` | method | Creates the service. |
| `IssueAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method |  |
| `RotateAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method |  |
| `ValidateAsync(Microsoft.AspNetCore.Http.HttpContext,System.String,System.Threading.CancellationToken)` | method |  |

### `CsrfServiceExtensions`

Service collection extensions that register the CSRF service stack: the              token options, the service options, the bootstrap endpoint options, the    ...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotCsrf(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Security.Csrf.CsrfOptions},System.Action{ApiPilot.Security.Csrf.CsrfTokenOptions},System.Action{ApiPilot.Security.Csrf.CsrfBootstrapOptions})` | method | Registers the CSRF service stack. |

### `CsrfToken`

The CSRF request token. The token is an opaque string on the wire. It              is produced by the configured signer and consumed by the validation          ...

| Member | Kind | Summary |
|---|---|---|
| `From(System.String)` | method | Creates a token from the given wire value. Null, empty, and             whitespace values are rejected. |
| `ToString` | method | Returns a fixed placeholder, never the wire value. This prevents             accidental logging of a CSRF token through string interpolation             or a de... |
| `Value` | property | The wire value of the token. Non-null, non-empty. This is the             string the client sends in the configured header. |

### `CsrfTokenFormat`

The plaintext payload format for a CSRF token. The payload is the              value handed to Data Protection for signing. It is a three-part              pipe...

| Member | Kind | Summary |
|---|---|---|
| `Delimiter` | field | The delimiter used between the three payload parts. The value is             a single pipe character. |
| `Encode(System.DateTimeOffset,System.String,System.String)` | method | Encodes the three payload parts into the plaintext string that             Data Protection will protect. The issuedAt value is written as a             decimal ... |
| `TryDecode(System.String,System.DateTimeOffset@,System.String@,System.String@)` | method | System.Xml.XmlElement |

### `CsrfTokenOptions`

Mutable configuration for CSRF token signing and validation. The              defaults match the security contract in SPEC.md. Every property is              an...

| Member | Kind | Summary |
|---|---|---|
| `ProtectionPurpose` | property | The Data Protection purpose string. Defaults to the v1 purpose             documented in SPEC.md. Two instances of an application must use             the same ... |
| `TimeProvider` | property | The clock used to determine the current time for issue and expiry             checks. Defaults to the system clock. Tests substitute a fixed or             adva... |
| `TokenEntropyBytes` | property | The number of random bytes placed in the token payload. Defaults             to 32. The bytes are produced by the platform cryptographic random             numb... |
| `TokenLifetime` | property | The token lifetime. A token whose issue time plus this lifetime is             before the current time is rejected as expired. Defaults to two             hours... |

### `CsrfTokenParseResult`

The outcome of a CSRF token validation attempt. A token either parses             and validates, or fails for one of five specific reasons. The reason          ...

| Member | Kind | Summary |
|---|---|---|
| `BindingMissing` | field | The current request has no resolvable binding for CSRF             validation. This is a pre-signature outcome: the request             could not produce a bind... |
| `Expired` | field | The token parsed and its signature verified, but its issue time             plus its lifetime is before the current time. The token is expired             and m... |
| `InvalidSignature` | field | The token's signature did not verify. This covers tampering, a             token from a different Data Protection key ring, an expired key,             and any ... |
| `Malformed` | field | The token could not be decoded. The signature may have been valid             but the plaintext did not carry the expected structure. This can             occur... |
| `Ok` | field | The token was well-formed, its signature verified, it was issued             within its lifetime, and its binding matched the current request. |
| `Rotated` | field | The token was well-formed and its signature verified, but a             rotation marker recorded for its binding is newer than the             token's issue tim... |
| `WrongSession` | field | The token parsed and its signature verified and it is within its             lifetime, but its binding does not match the current request. This             is t... |
| `WrongVersion` | field | The token was produced for a different version of the CSRF token             format. Reserved for a future version. In v1 this reason is not             produce... |

### `CsrfTokenValidateOutcome`

The outcome of a signer validation. Carries the internal parse reason             and, on success, the token issue time. The issue time is needed by            ...

| Member | Kind | Summary |
|---|---|---|
| `Failure(ApiPilot.Security.Csrf.CsrfTokenParseResult)` | method | Creates a failure outcome with the given reason and no issue time. |
| `IssuedAtUtc` | property | The token issue time, or null when the reason is not Ok. |
| `Reason` | property | The internal parse reason. |
| `Success(System.DateTimeOffset)` | method | Creates a success outcome with the given issue time. |

### `CsrfTransitionExtensions`

Service collection extensions that register the default CSRF              transition listener. Use this extension when the application wants              the de...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotCsrfTransitionListener(Microsoft.Extensions.DependencyInjection.IServiceCollection)` | method | Registers the default CSRF transition listener if one is not             already registered. |

### `CsrfValidationContext`

Per-request inputs for CSRF token validation. The context is              constructed by the caller (the middleware in Phase 2.4, or an              application...

| Member | Kind | Summary |
|---|---|---|
| `Binding` | property | The resolved binding for the current request, or null when the             binding could not be resolved. Exactly one of Binding and             BindingFailure ... |
| `BindingFailure` | property | The reason the binding could not be resolved, or None when the             binding was resolved. Exactly one of Binding and BindingFailure             is non-nu... |
| `RawHeaderValue` | property | The raw value of the CSRF header as received from the client, or             null when the header was absent. An empty string is a present             header wi... |
| `WithBinding(System.String,System.String)` | method | Creates a context with a resolved binding. |
| `WithoutBinding(ApiPilot.Security.Csrf.CsrfBindingFailureReason,System.String)` | method | Creates a context with an unresolved binding. |

### `CsrfValidationResult`

The outcome of a CSRF validation attempt. Pairs the internal parse              reason with the public wire code the middleware will emit. The two              ...

| Member | Kind | Summary |
|---|---|---|
| `Failure(ApiPilot.Security.Csrf.CsrfTokenParseResult,System.String)` | method | Creates a failure result with the given internal reason and public             code. |
| `IsSuccess` | property | True when the result represents a successful validation. |
| `PublicCode` | property | The public wire code the middleware will emit. Empty when the             result is Ok. A non-empty code is one of the values chosen by             CsrfOptions.... |
| `Reason` | property | The internal parse reason. Ok on success; a specific reason             otherwise. The reason is logged server-side and is never returned             to the cli... |
| `Success` | method | Creates a successful result. The public code is empty. |

### `DataProtectionCsrfTokenSigner`

The default CSRF token signer. Uses ASP.NET Core Data Protection as              the cryptographic authority. The library does not implement its own            ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.DataProtection.IDataProtectionProvider,ApiPilot.Security.Csrf.CsrfTokenOptions)` | method | Creates a signer that uses the given data protection provider and             options. The provider is asked for a protector with the configured             pur... |
| `Sign(System.String)` | method |  |
| `TryValidate(ApiPilot.Security.Csrf.CsrfToken,System.String)` | method |  |
| `ValidateWithMetadata(ApiPilot.Security.Csrf.CsrfToken,System.String)` | method |  |

### `DefaultCsrfTransitionListener`

The default CSRF transition listener. Composes over the CSRF service              and the binding provider. Login issues a fresh token for the new              ...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.Csrf.ICsrfService,ApiPilot.Security.Csrf.ICsrfBindingProvider,Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Csrf.DefaultCsrfTransitionListener},ApiPilot.Security.Csrf.ICsrfRotationStore)` | method | Creates the listener. |
| `OnLoginAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method |  |
| `OnLogoutAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method |  |
| `OnLogoutAsync(System.String,System.Threading.CancellationToken)` | method |  |

### `ICsrfBindingProvider`

Resolves the stable session binding for a CSRF token. The binding is              a non-secret identifier that is stable for the same security session          ...

| Member | Kind | Summary |
|---|---|---|
| `TryGetBinding(Microsoft.AspNetCore.Http.HttpContext,System.String@,ApiPilot.Security.Csrf.CsrfBindingFailureReason@)` | method | Attempts to resolve the stable binding for the current request. |

### `ICsrfRotationStore`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `ClearRotationMarkerAsync(System.String,System.Threading.CancellationToken)` | method | Clears the rotation marker for the given binding. Called on logout             or session teardown. A no-op when no marker is present. |
| `GetRotationMarkerAsync(System.String,System.Threading.CancellationToken)` | method | Reads the rotation marker for the given binding. |
| `SetRotationMarkerAsync(System.String,System.DateTimeOffset,System.Threading.CancellationToken)` | method | Records a rotation marker for the given binding. Any previously             recorded marker for the binding is replaced. |

### `ICsrfService`

The CSRF issuance, validation, and rotation service. The service is              the public entry point for CSRF operations. It resolves the binding            ...

| Member | Kind | Summary |
|---|---|---|
| `IssueAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method | Issues a fresh CSRF token bound to the current request's binding.             Returns null when no binding can be resolved for the request. |
| `RotateAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method | Rotates the token for the current request's binding. When a             rotation store is registered, records a rotation marker so that             previous tok... |
| `ValidateAsync(Microsoft.AspNetCore.Http.HttpContext,System.String,System.Threading.CancellationToken)` | method | Validates the given header value against the current request's             binding. Never throws for an invalid token; a failure is a normal             result. |

### `ICsrfTokenSigner`

The CSRF token signing and validation abstraction. The default              implementation uses ASP.NET Core Data Protection. An application              with i...

| Member | Kind | Summary |
|---|---|---|
| `Sign(System.String)` | method | Issues a new CSRF token bound to the given session binding. |
| `TryValidate(ApiPilot.Security.Csrf.CsrfToken,System.String)` | method | Validates a token against the given session binding. Returns a             result that identifies the outcome. Never throws for an invalid             token; a ... |
| `ValidateWithMetadata(ApiPilot.Security.Csrf.CsrfToken,System.String)` | method | Validates a token and returns both the reason and, on success,             the token issue time. The issue time is needed by services that             consult a... |

### `ICsrfTransitionListener`

A listener the application calls at authentication transitions to              keep the CSRF state consistent. The library never hooks ASP.NET Core             ...

| Member | Kind | Summary |
|---|---|---|
| `OnLoginAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method | Called after the application authenticates a user. Issues a fresh             CSRF token bound to the new authenticated subject. |
| `OnLogoutAsync(Microsoft.AspNetCore.Http.HttpContext,System.Threading.CancellationToken)` | method | Called before the application clears the authenticated session.             Resolves the binding from the current context and clears the             rotation ma... |
| `OnLogoutAsync(System.String,System.Threading.CancellationToken)` | method | Called at logout with the binding captured before the application             cleared session state. Clears the rotation marker for that binding             whe... |

## `ApiPilot.Security.DataProtection`

### `ApiPilotDataProtectionExtensions`

Registers and applies the ApiPilot Data Protection configuration.              The extension augments the application's existing Data Protection              se...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotDataProtection(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions})` | method | Registers the ApiPilot Data Protection configuration. |

### `ApiPilotDataProtectionOptions`

Configuration for the ApiPilot Data Protection layer. Separate from              CsrfTokenOptions: this type governs how the Data Protection keys              a...

| Member | Kind | Summary |
|---|---|---|
| `ApplicationName` | property | The Data Protection application name. Defaults to null, which             means the framework default (the entry assembly name) is used.             Two instanc... |
| `KeyLifetime` | property | The Data Protection key lifetime. Defaults to null, which means             the framework default (90 days) is used. This is NOT the CSRF             token life... |
| `KeyStorage` | property | The delegate that configures the application's Data Protection             builder for shared or persistent key storage. Defaults to null.             The deleg... |
| `KeyStorageConfigured` | property | Set by the ApiPilot extension when the KeyStorage delegate is             invoked. This records that the delegate ran; it does not record             whether th... |
| `MultiInstance` | property | Whether the application runs multiple instances behind a load             balancer. Defaults to false. When true, the application must             configure a s... |

### `DataProtectionStartupDiagnostics`

The default startup diagnostics. Emits the development-only warning             when the application has not configured persistent key storage.

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions,Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.DataProtection.DataProtectionStartupDiagnostics},System.Collections.Generic.IEnumerable{Microsoft.Extensions.Options.IValidateOptions{ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions}})` | method | Creates the diagnostics component. |
| `Emit` | method |  |

### `DataProtectionStartupHostedService`

Emits the Data Protection startup diagnostics once at host startup.             The service is registered by AddApiPilotDataProtection. The             diagnost...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.DataProtection.IDataProtectionStartupDiagnostics)` | method | Creates the hosted service. |
| `StartAsync(System.Threading.CancellationToken)` | method |  |
| `StopAsync(System.Threading.CancellationToken)` | method |  |

### `DevelopmentOnlyWarning`

Emits a one-shot warning when the in-memory Data Protection key             ring is used. The warning is informational: the in-memory key ring             is ac...

| Member | Kind | Summary |
|---|---|---|
| `WarnOnce(Microsoft.Extensions.Logging.ILogger)` | method | Emits the warning if it has not already been emitted in this             process. |

### `IDataProtectionStartupDiagnostics`

A marker interface for the startup diagnostics component. The             diagnostics component is resolved at startup to emit the             development-only ...

| Member | Kind | Summary |
|---|---|---|
| `Emit` | method | Emits the startup diagnostics. Called once at host startup. |

### `InstanceSafetyValidator`

Validates the ApiPilotDataProtectionOptions configuration at startup. Fails              closed when MultiInstance is true and no KeyStorage delegate was       ...

| Member | Kind | Summary |
|---|---|---|
| `Validate(System.String,ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions)` | method |  |

### `SharedKeyConfiguration`

Applies the effective ApiPilot Data Protection options to an             existing IDataProtectionBuilder. The helper does not add the             Data Protectio...

| Member | Kind | Summary |
|---|---|---|
| `Apply(Microsoft.AspNetCore.DataProtection.IDataProtectionBuilder,ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions)` | method | Applies the effective options to the builder. |

## `ApiPilot.Security.Diagnostics`

### `ApiPilotCsrfCounters`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `#ctor` | method | Creates the counters and the underlying meter. |
| `Dispose` | method |  |
| `MeterName` | field | The stable name of the ApiPilot meter. |
| `MeterVersion` | field | The version of the ApiPilot meter. |
| `RecordValidationDuration(System.Double)` | method | Records the duration of a validation call. |
| `TokenIssued` | method | Records a successful token issuance. |
| `TokenRotated` | method | Records a token rotation. |
| `ValidationFailed(System.String)` | method | Records a failed token validation with the internal reason as a             tag value. |
| `ValidationSucceeded` | method | Records a successful token validation. |

### `ApiPilotCsrfObservabilityExtensions`

System.Xml.XmlElement

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotCsrfObservability(Microsoft.Extensions.DependencyInjection.IServiceCollection)` | method | System.Xml.XmlElement |

### `ApiPilotSecurityDiagnostics`

The public security diagnostics surface. A DI service that              reports the state of the security configuration. The surface has              no endpoin...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.DataProtection.ApiPilotDataProtectionOptions)` | method | Creates the diagnostics surface. |
| `Emit(System.Action{ApiPilot.Security.Diagnostics.SecurityDiagnostic})` | method | Emits every non-fatal diagnostic through the logger. |
| `GetDiagnostics` | method | Returns the current non-fatal diagnostics. |

### `ApiPilotSecurityDiagnosticsExtensions`

Registers the ApiPilot security diagnostics. Call this after the             other AddApiPilot* security extensions so the diagnostics can             verify th...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotSecurityDiagnostics(Microsoft.Extensions.DependencyInjection.IServiceCollection)` | method | Registers the diagnostics surface, the registration checker, and             the startup hosted service. |

### `DiagnosticCodes`

Internal structured log identifiers for the security diagnostics.             The codes are used only in server-side log lines. They are never             expos...

| Member | Kind | Summary |
|---|---|---|
| `InMemoryKeyRing` | field | The Data Protection layer is using the in-memory key ring.             Non-fatal. Warning only. Emitted when MultiInstance is false             and no KeyStorag... |
| `OptionsWithoutValidator` | field | A security options type is registered but its validator is not.             Fatal. The host refuses to start. |
| `RotationStoreMissing` | field | The CSRF rotation requirement is Required but no             ICsrfRotationStore is registered. Fatal. The host refuses to             start. |
| `ValidatorMissing` | field | An expected security validator is not registered in the             container. Fatal. The host refuses to start, because a             missing validator means a... |

### `SecurityConfigurationValidator`

Verifies that the expected security validators are resolvable from             the container. The class does not run validation; the             ValidateOnStart...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.IServiceProvider)` | method | Creates the checker. |
| `GetDiagnostics` | method | Returns the diagnostics for the current registration state.             A missing validator produces a SecurityDiagnostic with the             ValidatorMissing ... |

### `SecurityDiagnostic`

A single security diagnostic.

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.String,ApiPilot.Security.Diagnostics.SecurityDiagnosticLevel,System.String)` | method | A single security diagnostic. |
| `Code` | property | The internal diagnostic code (SEC or SECW). |
| `Level` | property | The severity. |
| `Message` | property | The non-secret, human-readable description. |

### `SecurityDiagnosticLevel`

The severity of a security diagnostic.

| Member | Kind | Summary |
|---|---|---|
| `Fatal` | field | The condition is fatal. The host has not started. This level is             reserved for the fail-closed path; it is not produced by             ApiPilotSecurit... |
| `Warning` | field | The condition is a warning. The host has started. The operator             should review the configuration. |

### `SecurityDiagnosticsHostedService`

Emits the security diagnostics at host startup. The service runs             after ValidateOnStart has enforced every fatal option failure. Its             rema...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(ApiPilot.Security.Diagnostics.SecurityConfigurationValidator,ApiPilot.Security.Diagnostics.ApiPilotSecurityDiagnostics,Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Diagnostics.SecurityDiagnosticsHostedService})` | method | Creates the hosted service. |
| `StartAsync(System.Threading.CancellationToken)` | method |  |
| `StopAsync(System.Threading.CancellationToken)` | method |  |

## `ApiPilot.Security.Origin`

### `FetchMetadataEvaluator`

Evaluates the Fetch Metadata headers under the configured profile.              The evaluator is a pure function: it reads the two headers from the             ...

| Member | Kind | Summary |
|---|---|---|
| `IsRequestAcceptable(Microsoft.AspNetCore.Http.HttpContext,ApiPilot.Security.Origin.FetchMetadataOptions,System.String@)` | method | Applies the profile to the current request. Returns true when             the request should proceed. |

### `FetchMetadataMiddleware`

Applies the Fetch Metadata policy to protected requests. The              middleware runs after the Origin policy and passes through on safe              method...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,Microsoft.Extensions.Options.IOptions{ApiPilot.Security.Origin.FetchMetadataOptions},Microsoft.Extensions.Options.IOptions{ApiPilot.Security.Csrf.CsrfOptions},Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Origin.FetchMetadataMiddleware},ApiPilot.Core.Metadata.ICorrelationIdAccessor)` | method | Creates the middleware. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method | Executes the middleware. Resolves the endpoint policy and the             request method; passes through when the request is not enforced;             evaluates... |

### `FetchMetadataMiddlewareExtensions`

Service collection and application builder extensions for the Fetch              Metadata policy. The service registration wires the options and the            ...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotFetchMetadata(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Security.Origin.FetchMetadataOptions})` | method | Registers the Fetch Metadata options and the startup validator. |
| `UseApiPilotFetchMetadata(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the Fetch Metadata middleware to the pipeline. |

### `FetchMetadataOptions`

Configuration for the Fetch Metadata policy. The policy is              defense-in-depth. It supplements the Origin policy and the CSRF              check. It i...

| Member | Kind | Summary |
|---|---|---|
| `AllowedModeValues` | property | The Sec-Fetch-Mode values that are permitted on protected             methods when the header is present. Defaults to cors,             same-origin, and navigat... |
| `AllowedSiteValues` | property | The Sec-Fetch-Site values that are permitted on protected             methods. Defaults to same-origin, same-site, and none. The value             cross-site is... |
| `AllowMissingHeaders` | property | Whether a missing or malformed Sec-Fetch-Site header is             permitted. Defaults to true. The setting is only consulted when             the header is ab... |
| `Profile` | property | The enforcement profile. Defaults to Off. See             FetchMetadataProfile for the three values and their behavior. |

### `FetchMetadataProfile`

The Fetch Metadata enforcement profile. Off is the default and the             compatibility path. Compat allows a missing Sec-Fetch-Site header             so ...

| Member | Kind | Summary |
|---|---|---|
| `Compat` | field | The policy enforces the Sec-Fetch-Site check when the header is             present and well-formed. A missing or malformed header is             permitted (sub... |
| `Off` | field | The Fetch Metadata policy is disabled. The middleware passes             every request through regardless of the Sec-Fetch-Site header.             This is the ... |
| `Strict` | field | The policy enforces the Sec-Fetch-Site check strictly. A             disallowed value is always rejected. A missing or malformed             header is rejected ... |

### `OriginMatchMode`

The origin comparison mode for the Origin policy. The default is             Exact. AnyPortSameHost relaxes the port check. SameSite is not             implemen...

| Member | Kind | Summary |
|---|---|---|
| `AnyPortSameHost` | field | The request Origin must match an allowed origin by scheme and             host (case-insensitive). The port is ignored. Applications             whose clients a... |
| `Exact` | field | The request Origin must match an allowed origin by scheme,             host (case-insensitive), and port. Missing ports are             normalized to the scheme... |
| `SameSite` | field | The request Origin must share a registrable domain (eTLD+1)             with an allowed origin. This mode is NOT implemented in this             phase. The vali... |

### `OriginMiddleware`

Applies the defense-in-depth Origin policy to protected requests.              The middleware runs before CSRF protection and passes through on              saf...

| Member | Kind | Summary |
|---|---|---|
| `#ctor(Microsoft.AspNetCore.Http.RequestDelegate,Microsoft.Extensions.Options.IOptions{ApiPilot.Security.Origin.OriginPolicyOptions},Microsoft.Extensions.Options.IOptions{ApiPilot.Security.Csrf.CsrfOptions},Microsoft.Extensions.Logging.ILogger{ApiPilot.Security.Origin.OriginMiddleware},ApiPilot.Core.Metadata.ICorrelationIdAccessor)` | method | Creates the middleware. |
| `InvokeAsync(Microsoft.AspNetCore.Http.HttpContext)` | method | Executes the middleware. Resolves the endpoint policy and the             request method; passes through when the request is not enforced;             checks th... |

### `OriginMiddlewareExtensions`

Service collection and application builder extensions for the              Origin policy. The service registration wires the options and the              startu...

| Member | Kind | Summary |
|---|---|---|
| `AddApiPilotOriginPolicy(Microsoft.Extensions.DependencyInjection.IServiceCollection,System.Action{ApiPilot.Security.Origin.OriginPolicyOptions})` | method | Registers the Origin policy options and the startup validator. |
| `UseApiPilotOriginPolicy(Microsoft.AspNetCore.Builder.IApplicationBuilder)` | method | Adds the Origin policy middleware to the pipeline. |

### `OriginParts`

The parsed parts of an origin: scheme, host, and normalized port.             The port is 0 when the scheme has no defined default.

| Member | Kind | Summary |
|---|---|---|
| `#ctor(System.String,System.String,System.Int32)` | method | The parsed parts of an origin: scheme, host, and normalized port.             The port is 0 when the scheme has no defined default. |
| `Host` | property | The lowercased host. |
| `Port` | property | The normalized port, or 0 for an undefined default. |
| `Scheme` | property | The lowercased scheme. |

### `OriginPolicyOptions`

Configuration for the defense-in-depth Origin policy. The policy              runs on protected methods and rejects a request whose Origin does              not...

| Member | Kind | Summary |
|---|---|---|
| `AllowedOrigins` | property | The set of allowed origins. Each entry is an origin string of             the form scheme://host[:port]. Comparison is case-insensitive             on the schem... |
| `AllowMissingOrigin` | property | Whether a missing Origin header is permitted. Defaults to true.             The default matches the practice of modern browsers that omit             the header... |
| `AllowRefererFallback` | property | Whether the Referer fallback is enabled. Defaults to false. When             true and the Origin header is absent, the Referer is parsed and             its ori... |
| `AllowSameOrigin` | property | Whether a present Origin that matches the request's own origin             is permitted without being in AllowedOrigins. Defaults to true.             This is i... |
| `Enabled` | property | Whether the Origin policy is enabled. Defaults to true. When             false, the middleware passes every request through and the             startup validato... |
| `MatchMode` | property | The origin comparison mode. Defaults to Exact (scheme, host,             and normalized port must match). See OriginMatchMode for the             defined values... |
| `RejectionCode` | property | The public wire code for a rejected origin. Defaults to             CSRF_ORIGIN_REJECTED, matching SPEC.md. Applications that use             a different code o... |

### `OriginValidator`

The RFC 6454 origin comparison. Parses an origin string of the              form scheme://host[:port] into a normalized tuple and compares              two orig...

| Member | Kind | Summary |
|---|---|---|
| `IsRequestOriginAcceptable(Microsoft.AspNetCore.Http.HttpContext,ApiPilot.Security.Origin.OriginPolicyOptions,System.String@)` | method | Applies the Origin policy to the current request. Returns true             when the request should proceed. Returns false with a reason             string suita... |
| `OriginMatches(System.String,System.String,ApiPilot.Security.Origin.OriginMatchMode)` | method | Compares two origin strings using the given mode. Returns false             when either string is malformed. |
| `TryParseOrigin(System.String,ApiPilot.Security.Origin.OriginParts@)` | method | Attempts to parse an origin string into its normalized parts. |

