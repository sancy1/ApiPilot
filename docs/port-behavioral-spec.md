<!--
filepath: docs/port-behavioral-spec.md
package:  n/a (repository root docs)
since:    v1.0.6
purpose:  Consolidated behavioral spec for language ports. Every rule a port must replicate, distilled from SPEC.md, the 24 per-concern docs, the API inventory, and the C# source.
-->

# ApiPilot port behavioral spec

This document is the **behavioral parity checklist** for porting ApiPilot
to other languages. It consolidates `SPEC.md` (normative wire contract),
the 24 per-concern docs, the API inventory (`api-inventory/`), and direct
reading of the C# source for the trickiest paths.

**Rule of precedence:** `SPEC.md` wins over everything. This document wins
over intuition. Where this document and the C# source disagree, the source
is the truth and this document must be corrected.

A port is conformant when it reproduces every rule below in its own
idiom. Framework-bound pieces (middleware, DI, HttpContext) map to each
ecosystem's equivalent; pure-logic pieces (envelopes, error codes,
pagination math, cookie validation, origin parsing, CSRF token format)
must be behavior-identical.

---

## 1. Response envelopes

### Success envelope (always)

```json
{ "success": true, "data": <any>, "message": <string|null>, "meta": {...} }
```

- `success` is always `true`. `data` is always present (object, array,
  scalar, or `null` — never omitted). `message` is always present
  (`null` when none). `meta` is always present.
- `meta` carries `requestId`, `timestamp`, `extra` (object, may be `{}`).

### Collection

Not a distinct shape: success envelope with `data` as an array.

### Paginated envelope

Success envelope plus `pagination` sibling of `data`:

```json
"pagination": { "page": 2, "pageSize": 20, "totalItems": 143,
                "totalPages": 8, "hasNext": true, "hasPrevious": true }
```

- All six fields required when present. `page`/`pageSize` are the values
  the **server used** (post-clamp/default), not what the client asked.
- `totalPages = ceil(totalItems / pageSize)`, minimum 1 when totalItems=0.
- `hasNext = page < totalPages`. `hasPrevious = page > 1`.
- `totalItems` is application-supplied; the library never queries data.

### Error envelope

```json
{ "success": false,
  "error": { "code": "VALIDATION_ERROR", "message": "...",
             "fields": { "email": ["Email is required."] } },
  "meta": { "requestId": "..." } }
```

- `error.code` stable machine string; `error.message` safe (never stack
  traces, crypto, SQL, paths, connection strings, internal notes).
- `error.fields` present **only** for `VALIDATION_ERROR`. Shape:
  object mapping field name → non-empty array of message strings.
  Field order not guaranteed; message order within a field preserved;
  no deduplication.
- `meta` optional on error envelopes but present whenever correlation
  is enabled.

### CSRF bootstrap response — the ONLY non-envelope success

`GET /api/csrf` (default path) returns a **bare object**, not the success
envelope:

```json
{ "token": "<csrf-request-token>" }
```

- `Cache-Control: no-store` on the response.
- Disabled via `CsrfBootstrapOptions.Enabled=false` → endpoint not mapped.
- This is the single exception to "every success uses the envelope".

---

## 2. Error codes and status mapping

### The 14 standard codes (SPEC.md table)

| Code | HTTP | Notes |
|---|---|---|
| VALIDATION_ERROR | 400 (422 configurable) | only code carrying `fields` |
| AUTHENTICATION_REQUIRED | 401 | |
| FORBIDDEN | 403 | also used by Fetch Metadata rejections |
| CSRF_HEADER_MISSING | 403 | |
| CSRF_TOKEN_INVALID | 403 | |
| CSRF_TOKEN_EXPIRED | 403 | |
| CSRF_ORIGIN_REJECTED | 403 | configurable via OriginPolicyOptions |
| RESOURCE_NOT_FOUND | 404 | |
| CONFLICT | 409 | |
| RATE_LIMITED | 429 | |
| INTERNAL_ERROR | 500 | |
| CONFIGURATION_ERROR | — | startup only, NEVER on wire |
| NOT_ACCEPTABLE | 406 | |
| UNSUPPORTED_MEDIA_TYPE | 415 | |

### ApiErrorCode semantics

- Immutable value; `From(string)` trims + uppercases (invariant culture).
  `"validation_error"` ≡ `"VALIDATION_ERROR"`. Null/empty/whitespace rejected.
- Custom codes via `From("ORDER_LOCKED")` → fall through to HTTP 500
  unless the app supplies its own `IApiErrorMapper`/status map.
- Codes are matched by clients; `message` text is NOT stable.

### Exception → code mapping (DefaultApiExceptionMapper)

Ordered list; **first `IsInstanceOfType` match wins** (most-derived first):

| Exception | Code | Status |
|---|---|---|
| ArgumentNullException | VALIDATION_ERROR | 400 |
| ArgumentException | VALIDATION_ERROR | 400 |
| KeyNotFoundException | RESOURCE_NOT_FOUND | 404 |
| UnauthorizedAccessException | FORBIDDEN | 403 |
| InvalidOperationException | CONFLICT | 409 |
| anything else | INTERNAL_ERROR | 500 |

- Matched entry message: `SafeMessage` unless `IncludeExceptionMessage`.
- Fallback message: `"An unexpected error occurred."` unless
  `RevealExceptionMessageInResponse` (default false). If
  `RevealExceptionTypeInResponse` (default false), appends
  `" (ExceptionTypeName)"`.
- `ApiExceptionOptions.Mappings` is the ordered override list;
  `ErrorCodeToStatusMap` overrides code→status.

---

## 3. Correlation

- Default header `X-Request-Id` (configurable `HeaderName`).
- Default generator: **GUIDv7** (time-ordered, unpredictable).
- `ValidationPattern` regex validates incoming ID (default matches GUIDv7).
- `CorrelationInvalidIdPolicy`: `Replace` (default — generate new),
  `Reject` (→ VALIDATION_ERROR response), `UseAsIs` (accept + warn).
- Echo: `EchoInResponseHeader` (default true) writes response header;
  `EchoInResponseBody` (default true) writes `meta.requestId`.
  When body echo is false, `meta.requestId` is `""` (empty string).
- **Envelope discipline (standing rule):** every envelope-writing
  component resolves ID via `ICorrelationIdAccessor?.RequestId`,
  falling back to `HttpContext.TraceIdentifier` when no accessor.
  Never bypass the accessor (defects A-040, A-118, A-120).
- Correlation ID is never a secret/credential/session id.

---

## 4. Pagination

### Defaults (PaginationOptions)

`DefaultPageSize=20`, `MaxPageSize=100`, `PageNumberBase=1`,
`SuccessStatusCode=200`, `StrictQueryValidation=false`,
param names `page`/`pageSize`/`sort`/`direction`.

### Query parsing rules (PaginationEndpointFilter)

- `page`/`pageSize` parsed via `IntegerParser` (default:
  `int.TryParse`, NumberStyles.Integer, invariant culture).
- Non-integer → VALIDATION_ERROR field error on that param name.
- `page < PageNumberBase` → error "must be at least {base}".
- `pageSize < 1` → error; `pageSize > MaxPageSize` → error.
- Wire page → internal: `internalPage = wirePage - PageNumberBase + 1`
  (so base=0 wire page 0 → internal page 1; PageRequest is always 1-based).
- Missing page/pageSize → defaults (page 1, DefaultPageSize).
- `PageRequest.FromQuery` fills nulls then validates: page≥1,
  pageSize≥1, pageSize≤MaxPageSize.

### Sort

- Default two-param model: `sort=<field>&direction=<asc|desc>`
  (direction case-insensitive via `SortDirectionExtensions.TryParseWireValue`).
- Sort present if EITHER `sort` or `direction` param non-empty.
- `SortParser` override: `Func<field, direction, SortRequest?>`; null →
  VALIDATION_ERROR "The sort expression is not valid."
- `SortDirectionParser` override for custom direction tokens.
- Empty `field` → sort parser returns null → error.

### Filters

- Any query param NOT named by ParameterNames is a candidate filter.
- `IsFilterParameter` predicate decides; when null, lenient mode treats
  all non-control params as filters.
- **Strict mode** (`StrictQueryValidation=true`): param rejected with
  VALIDATION_ERROR "This query parameter is not recognized." when
  `IsFilterParameter` returns false OR is null (null + strict → every
  non-control param rejected).
- FilterRequest carries raw key→value pairs; library never interprets.

### Three-layer precedence

Global `PaginationOptions` → fluent `PaginationOverrides` →
`PaginationMetadataAttribute`. Merge: attribute wins for primitives it
carries; fluent wins for delegates + ParameterNames (attribute can't
carry them). `PaginationResolver.Resolve` applies merged over global.

### Result

`PagedResultBuilder.From(items, page, pageSize, totalItems)` →
`PagedResult<T>`; `.ToPagedResult(meta)` reads effective options from
`HttpContext.Items` for `SuccessStatusCode` (defect A-108 fix).

### Startup validation (PaginationOptionsValidator)

DefaultPageSize≥1, MaxPageSize≥1, Default≤Max, PageNumberBase≥0,
SuccessStatusCode 2xx, ParameterNames non-null/no empty entries.
Failure → host refuses to start (CONFIGURATION_ERROR, never on wire).

---

## 5. Validation

- `VALIDATION_ERROR` is the only code carrying `error.fields`.
- Default HTTP 400; 422 configurable via status map.
- **FieldKeyNormalizer.Normalize** (default): strip leading `$.`,
  camelCase each dot-segment, preserve `[i]` bracket indices.
  `$.items[0].price`→`items[0].price`; `Items[0].Price`→`items[0].price`;
  `email`→`email`.
- `ApiPilotValidationOptions.KeyTransform` overrides; single resolver
  `ValidationKeyTransforms.Resolve` = `KeyTransform ?? Normalize`
  (prevents MVC/minimal-API drift, defect A-049).
- **MVC paths:** `[ApiController]` short-circuits before action filters →
  `ApiPilotInvalidModelStateResponseFactory` produces envelope.
  `[ApiPilotValidate]` action filter for non-ApiController controllers.
- **Minimal API:** `ApiPilotValidationEndpointFilter` /
  `WithApiPilotValidation` callback returning `ApiErrorField` list or
  raw key→messages dict (normalized via same resolver).
- Framework-neutral: app plugs in via `IValidationErrorSource`.
- PropertyNamingPolicy (serialization) and FieldKeyNormalizer (validation)
  are INDEPENDENT — do not conflate.

---

## 6. JSON serialization

- **System.Text.Json only.** Newtonsoft.Json forbidden (audit-enforced).
- Defaults: `PropertyNamingPolicy=CamelCase`,
  `DictionaryKeyPolicy=CamelCase`, `EnumMode=AsString`,
  `DateMode=Iso8601`, `MaxDepth=64` (STJ default),
  `ReadCommentHandling=Disallow`, `DefaultIgnoreCondition=Never`.
- `EnumMode`: `AsString` writes member name (`"Confirmed"`);
  `AsNumber` writes underlying int. Converter installed by configurator.
- `DateMode`: `Iso8601` string vs `UnixSeconds` integer (two converters:
  DateTime + DateTimeOffset; whole-second truncation, TZ normalization).
  Mode and converters ship together (defect A-027).
- **Envelope keys are PINNED** via `[JsonPropertyName]`:
  `success`, `data`, `message`, `meta`, `error`, `code`, `fields`,
  `pagination`, `requestId`, `timestamp`, `extra`, `page`, `pageSize`,
  `totalItems`, `totalPages`, `hasNext`, `hasPrevious`. Naming policy
  applies ONLY to app DTOs, never envelope keys.
- `AddApiPilotJson` two overloads: `IServiceCollection`→Http.Json;
  `IMvcBuilder`→both MVC + Http.Json. Mixed apps must configure both
  deliberately (defects A-028, A-083).
- Validator rejects: MaxDepth<1, undefined enum/date/comment/ignore values.

---

## 7. Content negotiation

Middleware `ApiPilotContentNegotiationMiddleware`; pipeline order:
Correlation → Exceptions → **ContentNegotiation** → rest.

### Accept check (406 NOT_ACCEPTABLE)

- Missing Accept header → accept (RFC 9110 §12.5.1).
- Parse comma list; media type = segment before first `;`.
- `;q=` weight IGNORED — presence check, not preference.
- `*/*` accepted iff `AcceptWildcard=true` (default); when false treated
  as literal type.
- **Type wildcards `application/*` NOT honored** — literal match only.
  App must add `application/*` to AcceptableResponseMediaTypes explicitly.
- Match against `AcceptableResponseMediaTypes` (default `{application/json}`,
  case-insensitive set).

### Content-Type check (415 UNSUPPORTED_MEDIA_TYPE)

- Runs ONLY for `BodyCarryingMethods` (default POST, PUT, PATCH;
  case-insensitive set).
- Media type = segment before first `;` (charset/boundary ignored).
- Missing Content-Type on body-carrying method → 415 by default;
  `AcceptMissingContentType=true` accepts.
- Match against `AcceptableRequestMediaTypes` (default `{application/json}`).

### Envelope + correlation

Rejection writes standard error envelope. RequestId from
`ICorrelationIdAccessor` ?? `HttpContext.TraceIdentifier` fallback
(works-without-registration). `EchoInResponseBody=false` → `""`.

### Validator

Acceptable sets non-empty + valid media-type strings (contain `/`, no
whitespace); BodyCarryingMethods non-empty + valid tokens; wildcard=false
+ empty response set → reject (no Accept could ever pass).

---

## 8. CSRF (the deep end)

### Two-credential model

- Auth/session credential: HttpOnly+Secure cookie (browser-managed).
- CSRF request credential: ApiPilot-issued token, client-held in memory,
  sent in custom header (default `X-CSRF-TOKEN`).
- Bootstrap: `GET {BootstrapPath}` (default `/api/csrf`) →
  `{"token":"..."}` + `Cache-Control: no-store`.

### Protected methods & endpoint policy

- `ProtectedMethods` default `{POST, PUT, PATCH, DELETE}` (case-insensitive).
- Safe methods (GET/HEAD/OPTIONS/TRACE) never protected by default.
- Precedence: `[ApiPilotRequireCsrf]` > `[ApiPilotSkipCsrf]` > global set.
  Require wins even on safe methods; Skip bypasses even on protected.
- Bootstrap path always exempt. `ExemptPaths` + `ExemptPredicate` extra.
- Origin policy + Fetch Metadata read SAME `CsrfEndpointMetadata` —
  Skip bypasses all three; Require enforces all three.

### Token format (CsrfTokenFormat)

Plaintext payload (inside Data Protection boundary):

```
{issuedAtUnixSeconds}|{randomBase64Url}|{bindingFingerprint}
```

- `issuedAt` = UTC DateTimeOffset → `ToUnixTimeSeconds()` invariant.
- `randomBase64Url` = base64url of `TokenEntropyBytes` (default 32)
  CSPRNG bytes; alphabet A-Za-z0-9-_ (no `=` padding, `+`→`-`, `/`→`_`).
- `bindingFingerprint` = base64url SHA-256 (see below).
- Delimiter `|`; parts must not contain `|` (Encode throws).
- Decode rejects: empty, missing/extra delimiters, empty parts,
  non-integer issuedAt, seconds outside [-62135596800, 253402300799].
- Wire token = `IDataProtector.Protect(payload)` output — opaque.
- Purpose string: `CsrfTokenOptions.ProtectionPurpose` default
  `"ApiPilot.Csrf.v1"` (must match across instances).
- `TokenLifetime` default **2 hours**; expiry check `issuedAt+lifetime <= now`.
- `TimeProvider` injectable (default system clock).

### Binding (CsrfBindingProvider)

Resolution order (first hit wins):

1. **Subject claim** — authenticated user, claim type `"sub"` first,
   then `ClaimTypes.NameIdentifier`. Value used raw.
2. **Session** — `HttpContext.Session.Id` (only if session available;
   `InvalidOperationException` → skip). Prefixed `"session:"`.
3. **PreAuthBindingSource** — configured delegate; non-empty result
   prefixed `"preauth:"`; configured-but-empty → `ProviderFailure`.

Fingerprint = `base64url(SHA256("ApiPilot.Csrf.Binding.v1|" + source))`.
Never logged, never client-visible, deterministic across restarts.

Fail-closed reasons when no binding: authenticated-but-no-subject →
`NoSubject`; no session+no user → `NoSession`; session present but no id
+ no preauth → `NoConfiguredSource`. Never generates anonymous binding.

### Validation pipeline (CsrfService.ValidateAsync)

Order of checks (first failure wins):

1. `rawHeaderValue is null` → `Malformed` reason + `MissingHeaderCode`
   (default `CSRF_HEADER_MISSING`).
2. No binding → `BindingMissing` reason + mapped code.
3. `Unprotect` throws `CryptographicException` → `InvalidSignature`.
4. `TryDecode` fails → `Malformed`.
5. `bindingFingerprint != binding` (ordinal) → `WrongSession`.
6. `issuedAt + lifetime <= now` → `Expired`.
7. Rotation store present AND `issuedAt <= marker` → `Rotated`.
8. Else → `Ok`.

Public wire codes via `CsrfOptions.CodeMapping` (default):
missing→`CSRF_HEADER_MISSING`, expired→`CSRF_TOKEN_EXPIRED`,
all others→`CSRF_TOKEN_INVALID`. Internal reasons (8: Ok, Malformed,
WrongVersion, InvalidSignature, WrongSession, Rotated, Expired,
BindingMissing) logged server-side, NEVER on wire.

### Rotation

- `CsrfRotationPolicy`: `Never` / `OnBootstrap` (default) / `OnWindow`.
- `CsrfRotationRequirement`: `Optional` (default) / `Required`
  (startup fails without `ICsrfRotationStore`).
- `RotateAsync`: signs fresh token; with store → `SetRotationMarkerAsync`
  (binding, now); without store → fresh token + **unthrottled warning
  every call** (old token valid till expiry).
- `ICsrfRotationStore`: Get/Set/Clear marker per binding.

### Transition listener (app-called, never auto-hooked)

- `OnLoginAsync(ctx)` → issue fresh token bound to new subject.
- `OnLogoutAsync(ctx)` → resolve binding, clear marker (pre-logout).
- `OnLogoutAsync(previousBinding)` → clear marker by captured binding
  (post-logout ordering).

### Signer validation detail (DataProtectionCsrfTokenSigner)

`Sign`: issuedAt=now, random=CSPRNG(32B)→base64url, payload=Encode,
wire=Protect. `ValidateWithMetadata`: Unprotect (CryptographicException
→InvalidSignature) → TryDecode (fail→Malformed) → fingerprint ordinal
compare (mismatch→WrongSession) → expiry (→Expired) → Success(issuedAt).

---

## 9. Cookies

Library NEVER sets cookies — validates profiles at startup only.

### Three default profiles

| Profile | Secure | HttpOnly | SameSite | Path | Domain | Base name |
|---|---|---|---|---|---|---|
| Authentication | ✓ | ✓ | Lax | / | none | `auth` |
| Session | ✓ | ✓ | Strict | / | none | `session` |
| CSRF (double-submit) | ✓ | **false** | Lax | / | none | `csrf` |

### CookieProfile value object

`Name` (base, no prefix), `Secure`, `HttpOnly`, `SameSite`, `Path`,
`Domain?`. Effective name = `NamePrefix + Name`.
`CookieSameSite`: Lax/None/Strict/Unspecified.

### Prefix rules (RFC 6265bis, on EFFECTIVE name)

- Defined prefixes: `""`, `"__Host-"`, `"__Secure-"` only.
- `__Host-` ⇒ Secure=true AND Domain=null AND Path=`/`.
- `__Secure-` ⇒ Secure=true.
- `SameSite=None` ⇒ Secure=true (else startup fail).
- Path: non-empty, must start with `/`.
- Name: non-empty, no RFC 6265-forbidden chars (`,` `;` space `"`
  `\` control chars).
- Domain: valid hostname, no scheme/path/port/whitespace.
- SameSite must be defined value.

### Startup validation (CookieProfileOptionsValidator + HostPrefixValidator)

All rules above fail-closed at `ValidateOnStart`. CSRF cookie
HttpOnly=false is deliberate for double-submit; recommended pattern is
in-memory token (skip cookie). CSRF cookie never carries auth credential.

---

## 10. Origin policy

RFC 6454 origin = `scheme://host[:port]`; scheme+host lowercased;
missing port → scheme default (https/wss→443, http/ws→80).
Malformed origin → non-match, never throws.

### Match modes (OriginMatchMode)

- `Exact` (default): scheme+host+normalized port all equal.
- `AnyPortSameHost`: scheme+host equal, port ignored.
- `SameSite`: **NOT IMPLEMENTED** — treated as non-match; startup
  validator fails closed if configured.

### Options (OriginPolicyOptions)

- `Enabled` default true.
- `AllowedOrigins`: set of `scheme://host[:port]` strings.
- `AllowSameOrigin` default true — present Origin matching request's
  own origin passes without being in AllowedOrigins.
- `AllowMissingOrigin` default true — absent Origin passes.
- `AllowRefererFallback` default false — opt-in; parse Referer origin
  when Origin absent; unparseable → non-match.
- `MatchMode` default Exact.
- `RejectionCode` default `CSRF_ORIGIN_REJECTED`.

### Fail-closed startup

Enabled + `AllowSameOrigin=false` + empty `AllowedOrigins` → no request
could ever pass → startup fail. Also rejects: malformed origin entry,
undefined mode, SameSite mode, empty rejection code.

### Rejection

Standard error envelope, configurable code, offending Origin NEVER
echoed; reason logged server-side only.

---

## 11. Fetch Metadata policy

Opt-in, `Profile` default `Off` (compatibility path, SPEC 6.17).

### Profiles

- `Off`: pass everything.
- `Compat`: enforce Sec-Fetch-Site when present+well-formed; missing/
  malformed permitted (subject to AllowMissingHeaders).
- `Strict`: disallowed value always rejected; missing/malformed rejected
  unless `AllowMissingHeaders=true`.

### Headers

- `Sec-Fetch-Site` values: `same-origin`, `same-site`, `none` permitted
  by default; `cross-site` rejected by default. Other values = malformed.
- `Sec-Fetch-Mode` checked ONLY when present; default allowed
  `{cors, same-origin, navigate}`; `no-cors`/`websocket` absent.
- `AllowMissingHeaders` (default true) governs ONLY missing/malformed —
  NEVER overrides a present disallowed value (explicit correction).

### Config (FetchMetadataOptions)

`Profile`, `AllowedSiteValues`, `AllowedModeValues`,
`AllowMissingHeaders`. Rejection → `FORBIDDEN` envelope; reason
(MissingSiteHeader/MalformedSiteHeader/DisallowedSiteValue/
DisallowedModeValue) logged only.

### Startup validation

Off → skip inner checks. Enabled → allowed sets non-empty. Strict +
`cross-site` in AllowedSiteValues → fail (ineffective Strict = defect).

---

## 12. Data Protection & multi-instance

- ApiPilot AUGMENTS app's Data Protection; never replaces, never
  inspects internals. Crypto authority = `IDataProtector`.
- `ApiPilotDataProtectionOptions`: `ApplicationName` (default framework),
  `KeyLifetime` (default framework 90d — SEPARATE from TokenLifetime),
  `KeyStorage` delegate, `MultiInstance` (default false),
  `KeyStorageConfigured` (set when delegate ran).
- **Multi-instance contract:** shared persisted key ring + SAME
  ApplicationName across all instances. Failure mode: token from
  instance A → `CSRF_TOKEN_INVALID`/`InvalidSignature` on instance B.
- `InstanceSafetyValidator`: `MultiInstance=true` + no KeyStorage via
  ApiPilot path → startup fail (fail-closed).
- In-memory key ring → `SECW001` diagnostic (Warning) + one-shot log
  event `9001` per process. Never includes key material/paths.
- Diagnostic codes internal only, never on wire.

---

## 13. Rate limiting

- ApiPilot implements NO limiter/policy/partition (audit-enforced).
- Integration = platform `RateLimiterOptions.OnRejected` callback →
  `RateLimitDiagnosticsHook.HandleAsync`.
- Rejection → `RATE_LIMITED`/429 envelope via `ErrorResponseResult`
  (single serialization seam). RequestId via `CorrelationEnvelope` seam
  (accessor ?? TraceIdentifier; EchoInResponseBody=false → `""`).
- `ApiPilotRateLimitOptions`: `StatusCode` (default 429, must be 400-599),
  `Message` (fixed safe default; SPEC defines none → overridable),
  `EmitRetryAfter` (default true).
- Retry-After: ONLY when lease supplies `MetadataName.RetryAfter`
  TimeSpan; positive → integer delta-seconds ceiling (invariant);
  zero/negative → nothing; absent → nothing. Never invented.
- Diagnostics: counter `apipilot.ratelimit.rejected` (tag `reason`),
  log event `4001` (code+reason only — NEVER path/query/partition/
  identity/IP). Reason codes: `LimitExceeded`, `QueueLimit`,
  `Unknown`, `None`.
- Registration `AddApiPilotRateLimitRejection` fail-closed (bad status
  or empty message → no start).

---

## 14. OpenAPI emitter

- Repository-owned emitter; NO Microsoft.OpenApi/Swashbuckle/NSwag.
- Input: `IApiDescriptionGroupCollectionProvider` (consumed, not registered).
- Output: OpenAPI **3.0.3** (fixed, not overridable).
- Top-level: `openapi`, `info` (title+version from options), `paths`,
  `components.schemas`.
- Path keys: relative, `/`-prefixed. Method keys lowercase.
- Supported methods: get/put/post/delete/options/head/patch/trace.
  Unsupported → `x-apipilot-unsupported-methods` vendor extension.
- Duplicate path+method → `x-apipilot-duplicate-operation` extension
  (never silent overwrite).
- Schemas: `ApiPilotErrorResponse` (success=false+error+meta),
  `ApiPilotError` (code+message+optional fields), `ApiPilotErrorFields`
  (object→string[]). Parity test serializes real envelope vs schema.
- Options (`AddApiPilotOpenApi`, fail-closed): `DocumentPath`
  (default `/openapi/v1.json`, must be absolute route), `DocumentTitle`
  (default `ApiPilot API`), `DocumentVersion` (default `1.0.0`),
  `IncludeErrorSchemas`, `IncludePaginationSchema`.
- `MapApiPilotOpenApi()` takes no path arg — single override mechanism.
- Compatibility contract: version string, schema names, vendor extension
  names frozen for this version.

---

## 15. Observability

### Log event IDs (stable, never renumbered)

| Range | Component |
|---|---|
| 1001-1007 | ExceptionMiddleware (per-level mapping, response-started) |
| 2001-2002 | CorrelationMiddleware (replaced/rejected incoming id) |
| 3001-3002 | ContentNegotiation (406/415) |
| 4001 | RateLimitDiagnosticsHook |
| 5001 | CsrfMiddleware rejection |
| 6001-6002 | CsrfTransitionEvents (binding unresolved, no rotation store) |
| 7001 | OriginMiddleware rejection |
| 8001 | FetchMetadataMiddleware rejection |
| 9001 | DataProtectionStartupDiagnostics (in-memory keyring, once) |
| 9100 | SecurityDiagnosticsHostedService (non-fatal startup warning) |

### Redaction (ApiPilotSafeLogger + ApiPilotRedactionPolicy)

`ILogger` decorator redacting VALUES of state entries whose KEYS match
(case-insensitive): `Authorization`, `Cookie`, `Set-Cookie`,
`X-CSRF-TOKEN`, `X-XSRF-TOKEN`, `Proxy-Authorization`,
`WWW-Authenticate` → literal `[redacted]`.
Preserves OriginalFormat, non-redacted entries, exception. Scope is
exact: key-name only; does NOT scrub opaque strings/exception messages/
scope payloads. Set is fixed, not runtime-overridable.

### Metrics (meter `ApiPilot`)

- `apipilot.csrf.issued` Counter<long>
- `apipilot.csrf.validated.ok` Counter<long>
- `apipilot.csrf.validated.failed` Counter<long> (tag `reason`)
- `apipilot.csrf.rotated` Counter<long>
- `apipilot.csrf.validation.duration` Histogram<double> (unit ms)
- `apipilot.ratelimit.rejected` Counter<long> (tag `reason`)

CSRF metrics opt-in via `AddApiPilotCsrfObservability`. Two separate
Meter instances share name `ApiPilot` (CSRF counters + rate-limit hook);
consolidation is scheduled backfill.

### Tracing

`ApiPilotActivitySource` static, never disposed, name `ApiPilot`.
Start methods set ONLY HTTP method + error code — NEVER path, query
string, or exception type name. App attaches own ActivityListener.

---

## 16. Browser client (@apipilot/client equivalent)

Framework-agnostic, zero runtime deps. `createApiPilotClient(config)` →
`{get, post, put, patch, delete, request, csrf}`.

### Request pipeline (10 steps)

1. **URL**: `new URL(path, baseUrl)`; relative appends, absolute replaces
   path; malformed → `ApiPilotConfigurationError`.
2. **Origin**: compare `.origin` (normalizes scheme/host/port).
3. **Protected?** = method ∈ protectedMethods AND method ∉ safe
   {GET,HEAD,OPTIONS,TRACE} AND request origin == API origin. All 3.
4. **CSRF header**: protected → read token from store (lazy bootstrap
   fetch if absent) → attach to `csrfHeader` (default `X-CSRF-TOKEN`).
5. **Headers**: caller headers copied first, CSRF added; body present +
   no caller Content-Type → `application/json` (caller always wins).
6. **fetch**: `config.fetch` ?? `globalThis.fetch`.
7. **Body**: `response.text()`; failure → `ApiPilotHttpError` (.cause).
8. **Parse**: JSON parse fail OR non-object OR `success` not boolean →
   `ApiPilotProtocolError` (.rawBody, .expected, .status).
9. **success:false** → `ApiPilotEnvelopeError` (.wireError, .status);
   on `CSRF_TOKEN_EXPIRED` fire `onCsrfExpired` (notification only —
   return ignored, exception contained on .cause, NO retry/replay).
10. **success:true** → return response object.

### Response classification — by BODY, not response.ok

Transport fail → HttpError; body read fail → HttpError; non-JSON →
ProtocolError; JSON w/o boolean success → ProtocolError; success:false →
EnvelopeError; success:true → return.

### Response object (8 client-controlled fields AFTER spread — always win)

`ok`, `status`, `success`, `data`, `message`, `error`, `meta`, `raw`.
Unknown envelope fields preserved via spread but client fields override
(server cannot forge `ok`/`status`/`raw`).

### CSRF bootstrap contract (client side)

Bare `{token}` OR `{success:true, token}` accepted; `success:false` →
EnvelopeError; else → ProtocolError. Token in closure variable ONLY —
never localStorage/sessionStorage/cookie/logs/client object.
Concurrent `csrf.get()` coalesce to one fetch; `refresh()`/`clear()`
during flight → generation counter discards stale result.

### Minimal response contract

Custom fetch needs only `{status:number, ok:boolean, text():Promise<string>}`.
Never uses `.json()`, `.headers`, `.clone()`, `.body`.

### CSP-safe

No eval/new Function/Function()/document.write/innerHTML/string
setTimeout/setInterval.

### Config overrides (C1-C7)

`baseUrl` (required), `csrfPath` (default baseUrl+"/csrf"),
`credentials` ("same-origin"), `protectedMethods`
([POST,PUT,PATCH,DELETE]), `csrfHeader` ("X-CSRF-TOKEN"),
`onCsrfExpired` (null), `fetch` (globalThis.fetch).

### Error classes (4, stable)

`ApiPilotHttpError`, `ApiPilotEnvelopeError`, `ApiPilotProtocolError`,
`ApiPilotConfigurationError`.

### Runtimes

Browser ~2019+ (fetch+URL+globalThis+AbortController): Chrome 71+,
Firefox 65+, Safari 12.1+, Edge 79+. Node ≥18.

---

## 17. Pipeline order (ASP.NET Core equivalent)

```
1. UseApiPilotCorrelation()      — first, establishes request id
2. UseApiPilotExceptions()       — second, catches downstream faults
3. UseRouting()                  — resolves endpoint + metadata
4. (app authentication)
5. UseApiPilotOriginPolicy()     — after auth, before CSRF
6. UseApiPilotFetchMetadata()    — after Origin
7. UseApiPilotCsrfProtection()   — before endpoint dispatch
8. UseApiPilotContentNegotiation() — if used
9. (app authorization)
10. endpoints
```

Rate limiter is app-owned (`UseRateLimiter` with ApiPilot OnRejected).
Origin/FetchMetadata/CSRF all require post-routing (endpoint metadata).

---

## 18. Cross-cutting invariants (never violate in a port)

1. **Zero third-party runtime deps** — audit-enforced in C#; ports should
   match (stdlib only).
2. **Fail-closed startup** — every options validator runs at boot;
   insecure/ambiguous config prevents start. `CONFIGURATION_ERROR` is
   startup-only, never a wire code.
3. **Safe messages** — error.message never leaks internals; reveal flags
   default false and are dev-only.
4. **Internal reasons never on wire** — CSRF parse reasons, Origin
   rejection detail, Fetch Metadata reason all log-only.
5. **Envelope keys pinned** — naming policy never touches envelope.
6. **Correlation discipline** — accessor ?? TraceIdentifier everywhere;
   EchoInResponseBody=false → `""` not omission.
7. **SPEC.md is normative** — wire shapes frozen; message text not.
8. **Published versions immutable** — any change = new version.
9. **Client fields win** — response object spread-then-override.
10. **No silent drops** — unsupported OpenAPI methods and duplicate
    operations get explicit vendor extensions, never silent loss.

---

## Port mapping quick-reference

| C# concept | Portable core | Framework-bound equivalent |
|---|---|---|
| Envelopes/errors | ✅ pure logic | serializer per language |
| Pagination math | ✅ pure logic | query parsing per framework |
| FieldKeyNormalizer | ✅ pure logic | ModelState equivalent |
| CookieProfile validation | ✅ pure logic | startup validation hook |
| OriginValidator/FetchMetadata | ✅ pure logic | middleware/filter equivalent |
| CSRF token format | ✅ pure logic | crypto primitive per language |
| CSRF binding | ⚠️ needs auth/session model | framework auth integration |
| DataProtection signer | ❌ DP-specific | per-language AEAD/HMAC + keyring |
| Exception mapper | ⚠️ exception taxonomy differs | per-language exception model |
| Content negotiation | ✅ pure logic | middleware equivalent |
| Correlation | ✅ pure logic | middleware + context accessor |
| OpenAPI emitter | ❌ ApiExplorer-specific | per-framework route metadata |
| Rate limit hook | ❌ platform-specific | per-framework rate limiter |
| Observability | ⚠️ meter/activity concepts | OpenTelemetry per language |
| Browser client | ✅ already TS/JS | — (reference impl) |

**Hardest port pieces:** DataProtection-equivalent signer (key ring,
purpose isolation, multi-instance), CSRF binding (auth model coupling),
OpenAPI emitter (framework descriptor source), exception taxonomy mapping.
