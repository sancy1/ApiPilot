<!--
filepath: README.md
package:  n/a (repository root)
since:    v0.1.0-alpha.0
purpose:  Project identity, scope, the wire contract, and the documentation map for ApiPilot.
-->

# ApiPilot

[![ApiPilot.Core](https://img.shields.io/nuget/v/ApiPilot.Core.svg?label=ApiPilot.Core)](https://www.nuget.org/packages/ApiPilot.Core)
[![ApiPilot.AspNetCore](https://img.shields.io/nuget/v/ApiPilot.AspNetCore.svg?label=ApiPilot.AspNetCore)](https://www.nuget.org/packages/ApiPilot.AspNetCore)
[![ApiPilot.Security](https://img.shields.io/nuget/v/ApiPilot.Security.svg?label=ApiPilot.Security)](https://www.nuget.org/packages/ApiPilot.Security)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/)
[![Build](https://github.com/sancy1/ApiPilot/actions/workflows/ci.yml/badge.svg)](https://github.com/sancy1/ApiPilot/actions/workflows/ci.yml)

**A contract-first API boundary for ASP.NET Core.** Consistent JSON envelopes,
stable error codes, validation, pagination, correlation, CSRF issuance and
validation, secure cookie profiles, Origin and Fetch Metadata policy, a
repository-owned OpenAPI emitter, rate-limit integration, diagnostics, and a
zero-runtime-dependency browser client - in one library with a cross-language
wire specification.

**Target:** .NET 10 | **License:** MIT | **Runtime dependencies:** none | **Releases:** [github.com/sancy1/ApiPilot/releases](https://github.com/sancy1/ApiPilot/releases)

---

## Why ApiPilot

Every professional API re-invents the same boundary conventions - and each
service re-invents them differently.

- Response envelopes that differ per endpoint.
- Error codes that mean different things in different services.
- Validation responses that differ between MVC controllers and minimal APIs.
- Pagination conventions that drift (page vs pageNumber, pageSize vs perPage).
- Correlation IDs that appear in some responses and not others.
- CSRF protection that is hand-rolled, inconsistently, or missing.
- Browser clients that cannot share a stable parser across services.

The result: client SDKs cannot be reused, security defaults drift, and every
new service is a new integration.

ApiPilot standardizes the boundary once. Applications adopt the conventions
they want - the envelope, the error contract, pagination, correlation, the
security surfaces - and plug in the rest. The wire contract is documented in
SPEC.md and is shared across every language binding.

---

## The wire contract

Every response is a JSON object with a top-level `success` discriminator.

### Success

```json
{
  "success": true,
  "data": { "id": "ORD-10001", "status": "confirmed" },
  "message": "Order retrieved successfully.",
  "meta": {
    "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2",
    "timestamp": "2026-09-29T00:00:00Z",
    "extra": {}
  }
}
```

`success` and `data` are required. `message` and `meta` are always present -
`message` is `null` when the application provides none; `meta` always carries
`requestId`, `timestamp`, and `extra`.

### Error

```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "One or more values are invalid.",
    "fields": { "email": [ "Email is required." ] }
  },
  "meta": { "requestId": "01J8Z3K7X9Y2F0A6M3N5P7Q1R2" }
}
```

`error.code` is a stable, machine-readable identifier from the table below.

### Error codes

| Condition | HTTP status | Code |
| --- | --- | --- |
| Validation failure | 400 or 422 | `VALIDATION_ERROR` |
| Authentication missing | 401 | `AUTHENTICATION_REQUIRED` |
| Authorization failure | 403 | `FORBIDDEN` |
| Missing CSRF header | 403 | `CSRF_HEADER_MISSING` |
| Invalid CSRF token | 403 | `CSRF_TOKEN_INVALID` |
| Expired CSRF token | 403 | `CSRF_TOKEN_EXPIRED` |
| Origin rejected | 403 | `CSRF_ORIGIN_REJECTED` |
| Resource not found | 404 | `RESOURCE_NOT_FOUND` |
| Conflict | 409 | `CONFLICT` |
| Rate limited | 429 | `RATE_LIMITED` |
| Unknown exception | 500 | `INTERNAL_ERROR` |
| Configuration error | startup | `CONFIGURATION_ERROR` |
| Unacceptable Accept | 406 | `NOT_ACCEPTABLE` |
| Unsupported content type | 415 | `UNSUPPORTED_MEDIA_TYPE` |

### Paginated collections

```json
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
```

The full contract is in [SPEC.md](SPEC.md).

---

## Quick start

The three packages are published on NuGet. Install them directly:

```bash
dotnet add package ApiPilot.Core
dotnet add package ApiPilot.AspNetCore
dotnet add package ApiPilot.Security
```

### 1. Register ApiPilot services

```csharp
using ApiPilot.AspNetCore.Middleware;
using ApiPilot.AspNetCore.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiPilotJson();
builder.Services.AddApiPilotCorrelation();
builder.Services.AddApiPilotExceptions();
builder.Services.AddApiPilotPagination();
```

### 2. Add the middleware in the documented order

```csharp
var app = builder.Build();

// The pipeline order is required. Each component depends on the ones before it.
app.UseApiPilotCorrelation();  // establishes the request id first
app.UseApiPilotExceptions();   // maps a fault anywhere downstream
app.UseRouting();              // the CSRF middleware reads endpoint metadata
app.UseRateLimiter();          // the application-owned limiter (see below)

app.MapControllers();
app.Run();
```

### 3. Return the envelope from an endpoint

```csharp
using ApiPilot.AspNetCore.Results;
using ApiPilot.Core.Metadata;
using ApiPilot.Core.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

app.MapGet("/api/orders/{id}", (HttpContext context, string id) =>
{
    var accessor = context.RequestServices.GetService<ICorrelationIdAccessor>();
    var meta = new ResponseMetadata
    {
        RequestId = accessor?.RequestId ?? context.TraceIdentifier
    };

    var order = new { id, status = "confirmed" };
    return ApiResponseBuilder.Ok(order, meta).ToResult();
});
```

`ApiResponseBuilder.Ok<T>(data, meta, message?)` builds the success envelope;
`.ToResult()` writes it to the HTTP response with the correct status code.

### 4. Errors map automatically

An unhandled exception in an endpoint - with `UseApiPilotExceptions()` in the
pipeline - becomes the standard error envelope. The client always sees a
`success: false` body with a stable `error.code`, never a stack trace.

---

## Capability table

Explicit states: **Built in** - one call and it works. **Partial** - the
framework provides a piece; ApiPilot standardizes the rest. **Manual** - the
application writes it. **Not built in** - does not exist and is not attempted.

| Capability | ApiPilot | ASP.NET Core alone | Application work |
| --- | --- | --- | --- |
| Success envelope | **Built in** | Not standardized | Manual |
| Error codes & status mapping | **Built in** | Partial | Manual |
| Validation envelope | **Built in** | Partial | Manual |
| Pagination contract | **Built in** | Not built in | Manual |
| Filtering & sorting metadata | **Built in** | Not built in | Manual |
| Correlation IDs | **Built in** | Not standardized | Manual |
| Content negotiation (406/415) | **Built in** | Partial | Manual |
| CSRF issuance & validation | **Built in** | Not built in | Manual |
| Secure cookie profiles | **Built in** | Not standardized | Manual |
| Origin policy | **Built in** | Not built in | Manual |
| Fetch Metadata | **Built in** | Not built in | Manual |
| OpenAPI document | **Built in** (repository-owned emitter) | Partial metadata APIs | Configure endpoints |
| Rate-limit rejection envelope | **Built in** (adapter) | **Built in** (the limiter) | Configure the policy |
| Browser client | **Built in** (zero runtime deps) | Not built in | Manual |
| Retry / circuit breaker / idempotency | **Not included** | Not built in | Use a resilience library |
| Authentication / authorization | **Not included** | Built in (Identity, JwtBearer) | Application owns it |
| Database access / caching | **Not included** | Not built in | Application owns it |

**On rate limiting:** ApiPilot does **not** implement a limiter. It integrates
with the ASP.NET Core rate limiter through `RateLimiterOptions.OnRejected`.
The application configures the limiter and the policy; ApiPilot supplies the
rejection handler and the standard `RATE_LIMITED` envelope.

---

## Packages

| Package | Purpose |
| --- | --- |
| **[ApiPilot.Core](https://www.nuget.org/packages/ApiPilot.Core)** | framework-independent contracts: the envelope, the error model, validation, pagination, correlation abstractions. Depends only on the .NET BCL. |
| **[ApiPilot.AspNetCore](https://www.nuget.org/packages/ApiPilot.AspNetCore)** | the ASP.NET Core adapter: middleware, filters, results, serialization, content negotiation, OpenAPI emitter, rate-limit rejection. |
| **[ApiPilot.Security](https://www.nuget.org/packages/ApiPilot.Security)** | CSRF issuance and validation, secure cookie profiles, Origin policy, Fetch Metadata, Data Protection integration. |
| **[@apipilot/client](https://github.com/sancy1/ApiPilot/tree/main/javascript/ApiPilot.Client)** | zero-runtime-dependency browser fetch client. |

Install only the layers an application needs. A service that only needs the
envelope and error contract references `ApiPilot.Core` alone.

---

## Documentation

Each concern has a per-concern document. The full list is in
[docs/README.md](docs/README.md).

| Concern | Document |
| --- | --- |
| The success / error / paginated envelopes | [docs/response-contract.md](docs/response-contract.md) |
| The error model and code table | [docs/error-contract.md](docs/error-contract.md) |
| Validation, field normalization | [docs/validation.md](docs/validation.md) |
| Pagination and query parameters | [docs/pagination.md](docs/pagination.md) |
| Filtering and sorting | [docs/filtering-sorting.md](docs/filtering-sorting.md) |
| JSON policy | [docs/serialization.md](docs/serialization.md) |
| Content negotiation (406 / 415) | [docs/content-negotiation.md](docs/content-negotiation.md) |
| Correlation IDs | [docs/correlation.md](docs/correlation.md) |
| CSRF protection | [docs/csrf.md](docs/csrf.md) |
| Unsafe-method protection | [docs/unsafe-methods.md](docs/unsafe-methods.md) |
| Secure cookie profiles | [docs/cookies.md](docs/cookies.md) |
| Origin policy | [docs/origin-policy.md](docs/origin-policy.md) |
| Fetch Metadata | [docs/fetch-metadata.md](docs/fetch-metadata.md) |
| Data Protection and multi-instance | [docs/data-protection.md](docs/data-protection.md), [docs/multi-instance.md](docs/multi-instance.md) |
| OpenAPI emitter | [docs/openapi.md](docs/openapi.md) |
| Rate-limit integration | [docs/rate-limiting.md](docs/rate-limiting.md) |
| Observability | [docs/observability.md](docs/observability.md) |
| Threat model | [docs/threat-model.md](docs/threat-model.md) |
| Browser client | [docs/fetch-helper.md](docs/fetch-helper.md) |
| Supply chain (SBOM, signing, provenance) | [docs/supply-chain.md](docs/supply-chain.md) |
| Deployment | [docs/deployment.md](docs/deployment.md) |
| Troubleshooting a 403 | [docs/troubleshooting-403.md](docs/troubleshooting-403.md) |
| Migration | [docs/migration.md](docs/migration.md) |
| Resilience companion integration | [docs/resilience-integration.md](docs/resilience-integration.md) |

The single normative document is [SPEC.md](SPEC.md).

---

## Architecture

### The request boundary

```
HTTP request
    |
    v
ASP.NET Core pipeline
    |
    +--> UseApiPilotCorrelation   (assign / validate the request id)
    +--> UseApiPilotExceptions    (map a fault to the error envelope)
    +--> UseRouting
    +--> UseApiPilotCsrfProtection (validate CSRF on unsafe methods)
    +--> UseRateLimiter           (application-owned limiter)
    |
    v
application endpoint
    |
    +--> external calls (application-selected resilience library)
    |
    v
ApiPilot result  OR  mapped error
    |
    v
standard JSON envelope  +  diagnostics
```

### Package boundaries

```
ApiPilot.Core
    |
    +--> ApiPilot.AspNetCore
    |
    +--> ApiPilot.Security
    |
    +--> @apipilot/client (JavaScript; consumes the same wire contract)
```

`ApiPilot.Core` has no HTTP dependency. `ApiPilot.AspNetCore` and
`ApiPilot.Security` depend on ASP.NET Core. The client is standalone.

---

## Security surfaces

ApiPilot implements the defense-in-depth layers a cookie-authenticated API
needs. Each is documented in its per-concern doc.

### CSRF - the two-credential model

The authentication cookie is `HttpOnly` and `Secure` - browser-managed and
unreadable by JavaScript. CSRF uses a separate request token, held in
memory by the client and sent in a custom header on protected methods.

```csharp
// Server: register CSRF services and the pipeline component.
builder.Services.AddDataProtection();
builder.Services.AddApiPilotCsrf();

var app = builder.Build();
app.UseApiPilotCorrelation();
app.UseApiPilotExceptions();
app.UseRouting();
app.UseApiPilotCsrfProtection();
app.UseRateLimiter();

// Expose the bootstrap endpoint (GET /api/csrf by default).
app.MapApiPilotCsrf();
```

`POST`, `PUT`, `PATCH`, and `DELETE` require the CSRF header by default.
A `[ApiPilotSkipCsrf]` endpoint opts out; a `[ApiPilotRequireCsrf]` endpoint
opts in.

### Origin and Fetch Metadata

```csharp
builder.Services.AddApiPilotOriginPolicy();
builder.Services.AddApiPilotFetchMetadata();

app.UseApiPilotOriginPolicy();
app.UseApiPilotFetchMetadata();
```

Both run before the CSRF middleware. Origin policy is defense in depth; the
CSRF token is the primary protection.

### Secure cookie profiles

```csharp
builder.Services.AddApiPilotCookies();
```

The cookie profile validator rejects an insecure combination at startup:
`SameSite=None` requires `Secure=true`; `__Host-` requires `Secure`, no
`Domain`, and `Path=/`.

---

## Pagination

Register the pagination options once, then opt endpoints in:

```csharp
using ApiPilot.AspNetCore.EndpointMetadata;
using ApiPilot.Core.Pagination;

// Global configuration (application-wide).
builder.Services.AddApiPilotPagination(o =>
{
    o.DefaultPageSize = 25;
    o.MaxPageSize     = 200;
});

// Per-endpoint opt-in (minimal API).
app.MapGet("/api/items", (HttpContext context) =>
{
    var page = context.GetPageRequest();
    return Results.Ok();
})
.WithApiPilotPagination(o =>
{
    o.MaxPageSize = 50;  // fluent override
});
```

The precedence chain is global options -> fluent overrides -> the
`[PaginationMetadata]` attribute.

---

## Correlation

```csharp
builder.Services.AddApiPilotCorrelation(o =>
{
    o.HeaderName = "X-Correlation-Id";  // default is X-Request-Id
});

app.UseApiPilotCorrelation();
```

Every response envelope carries `meta.requestId`. The ID is read from the
configured header, validated against a pattern, and generated when absent or
malformed.

---

## OpenAPI

ApiPilot ships a repository-owned OpenAPI 3.0.3 emitter. It reads the
framework ApiExplorer descriptors and writes a document with
System.Text.Json. No Microsoft.OpenApi, Swashbuckle, NSwag, or Swagger
package is referenced.

```csharp
builder.Services.AddApiPilotOpenApi(o =>
{
    o.DocumentTitle   = "Orders API";
    o.DocumentVersion = "1.0.0";  // your API's version, not the library version
    o.DocumentPath    = "/openapi/v1.json";  // the default
});

var app = builder.Build();
app.MapApiPilotOpenApi();
```

---

## Rate limiting

ApiPilot does not implement a limiter. It integrates with the ASP.NET Core
limiter through the platform own extension point:

```csharp
builder.Services.AddApiPilotRateLimitRejection();

builder.Services.AddRateLimiter(o =>
{
    o.OnRejected = RateLimitDiagnosticsHook.HandleAsync;  // the ApiPilot handler
    o.AddFixedWindowLimiter("api", w =>
    {
        w.PermitLimit = 100;
        w.Window      = TimeSpan.FromSeconds(60);
    });
});

app.UseRateLimiter();
```

A rejection produces the standard envelope with `error.code = "RATE_LIMITED"`
and HTTP 429. The StatusCode, Message, and EmitRetryAfter are overridable
through `ApiPilotRateLimitOptions`.

---

## Browser client

`@apipilot/client` is a zero-runtime-dependency browser fetch client. It
speaks the same wire contract as the server.

```js
import { createApiPilotClient } from '@apipilot/client';

const client = createApiPilotClient({
    baseUrl: 'https://api.example.com',
    // C1-C7 overrides (all optional except baseUrl):
    csrfPath:         'csrf',
    credentials:      'same-origin',
    protectedMethods: ['POST', 'PUT', 'PATCH', 'DELETE'],
    csrfHeader:       'X-CSRF-TOKEN',
    onCsrfExpired:    (err) => console.warn('CSRF token expired', err),
    // fetch: myCustomFetch,
});

const response = await client.get('/api/orders/1');
console.log(response.data);

await client.post('/api/orders', { item: 'widget' });
```

The client holds the CSRF token in memory only - never in `localStorage` or
`sessionStorage`. It attaches the CSRF header only to protected methods on
the configured origin.

---

## Samples

Two standalone samples, both outside the solution, both zero-dependency.

### Samples.Api - the reference API

A single-host reference API exercising the full surface.

```bash
dotnet run --project dotnet/samples/Samples.Api/Samples.Api.csproj
```

Endpoints: `GET /api/ok`, `GET /api/fail`, `POST /api/validate`,
`GET /api/items`, `GET /api/csrf`, `POST /api/submit`, `GET /api/limited`.
See [dotnet/samples/Samples.Api/README.md](dotnet/samples/Samples.Api/README.md)
for run commands and how to exercise the CSRF and rate-limit paths.

### Samples.MultiInstance - shared Data Protection key ring

Two ApiPilot instances sharing a persisted key ring and application name. A
CSRF token issued by instance A validates on instance B. The two
requirements: a shared persisted key ring and a shared application name.

```bash
dotnet run --project dotnet/samples/Samples.MultiInstance/Samples.MultiInstance.csproj
```

See [dotnet/samples/Samples.MultiInstance/README.md](dotnet/samples/Samples.MultiInstance/README.md).

---

## Evidence and quality

Every number below is counted from an actual run at the v1.0.0 close.

| Gate | Result |
| --- | --- |
| Solution build (9 projects) | succeeded, 0 warnings, 0 errors |
| Core tests | 23 classes / 219 tests passed |
| AspNetCore tests | 52 classes / 377 tests passed |
| Security tests | 41 classes / 347 tests passed |
| Contract tests | 3 classes / 15 tests passed |
| Fuzz tests | 5 classes / 27 tests passed |
| Browser tests | 1 class / 6 tests passed / 1 fixture Unavailable (by design) |
| **.NET total** | **125 classes / 991 executed tests / 1 Unavailable** |
| JavaScript tests | 5 files / 60 tests passed |
| Audit | PASS (Sections 1-9, zero failures, zero warnings) |
| SBOM | generated per release (SPDX 2.3); attached to the release artifacts |
| Packages packed | 3 nupkg at 1.0.0 |
| Release workflow | tag-triggered via .github/workflows/release.yml |
| Independent security review | Not yet scheduled |

The browser suite drives a real headless Chrome/Edge over the Chrome
DevTools Protocol - no browser automation package is bundled. One fixture
(the HTTPS reverse-proxy scenario) is recorded as Unavailable by design; it
is never reported as a passing security assertion.

Run the entire suite:

```powershell
$repo = 'C:\Users\HP\Desktop\ApiPilot'
& dotnet build "$repo\dotnet\ApiPilot.slnx" -c Release --nologo

foreach ($p in @('ApiPilot.Core.Tests','ApiPilot.AspNetCore.Tests','ApiPilot.Security.Tests',
                 'ApiPilot.ContractTests','ApiPilot.FuzzTests')) {
    & dotnet run --project "$repo\dotnet\tests\$p\$p.csproj" -c Release --no-build
}

& dotnet run --project "$repo\dotnet\tests\ApiPilot.BrowserTests\ApiPilot.BrowserTests.csproj" `
    -c Release --no-build -- --allow-skip

Push-Location "$repo\javascript\ApiPilot.Client"
& node --test --test-reporter=tap "tests/**/*.test.js"
Pop-Location

& powershell -NoProfile -ExecutionPolicy Bypass -File "$repo\audit.ps1"
```

---

## Design principles

- **Zero third-party runtime dependencies.** The library builds on the .NET 10
  SDK, the .NET BCL, and the ASP.NET Core framework. The test harness is
  repository-local; the browser suite uses the Chrome DevTools Protocol
  directly. audit.ps1 enforces the boundary.
- **The API contract is frontend-agnostic.** HTTP + JSON + cookies + headers
  is the interoperability boundary. React, Vue, Angular, Svelte, Blazor,
  vanilla JS, mobile clients, and any HTTP consumer use the same contract.
- **The authentication credential is never exposed to JavaScript** to simplify
  frontend integration. CSRF uses a separate request token.
- **The core package is framework-independent.** ASP.NET Core integration and
  the security surfaces live in separate packages.
- **Every transformation has a sensible default and a first-class override.**
  Query-parameter names, sort syntax, page numbering, HTTP success status,
  correlation header, correlation ID format - all overridable through options
  or delegates without forking the library.
- **Fail-closed security.** An insecure configuration prevents the host from
  starting. There is no silent fallback to a default.
- **Published versions are immutable.** Any change, including documentation,
  gets a new version number.

---

## Roadmap and limitations

### Shipped in 1.0.0

The full boundary: envelope, error contract, validation, pagination, filtering
and sorting, correlation, serialization, content negotiation, CSRF, cookies,
Origin, Fetch Metadata, Data Protection, observability, rate-limit integration,
multi-instance, OpenAPI, supply-chain artifacts, and the browser client.

### Post-1.0.0 backfills

- **B.5** - consolidate two same-name ApiPilot Meter instances.
- **B.7** - derive the version from the assembly instead of hard-coding it in
  three diagnostics constants.
- **B.8** - correct the audit Section 9 output label.

### Planned (optional)

- **6.0** - additional framework examples.
- **6.1** - enterprise deployment enhancements.
- **6.2** - v2.0 synchronizer-token CSRF mode (optional).
- **6.3** - v2.1 sensitive-action token (optional).
- **6.4** - additional language roots (Python, Go) - structure only.

### Not included by design

Retry, circuit breaker, idempotency, payment processing, authentication,
authorization, JWT issuing, business logic, database access, and caching. These
belong to the application or to a companion resilience library.

---

## Project status - honest notes

- **Published on NuGet.** `ApiPilot.Core`, `ApiPilot.AspNetCore`, and
  `ApiPilot.Security` are live at `1.0.3`. Earlier versions remain on
  the versions tab.
- **No external production consumer yet.** The library is at v1.0.3; the
  sample and the test suite are the evidence. A case study will be added
  when one exists.
- **No independent security review scheduled.** The internal test suites
  (contract tests, fuzz tests, the browser security suite) have passed;
  no external reviewer has been engaged.
- **Package signing is not yet enabled.** The release workflow is
  fail-closed: it skips Authenticode signing when no certificate is
  configured and publishes with a NuGet repository signature. Adding a
  certificate is a one-secret change with no workflow edit.
---

## Contributing

ApiPilot is an open-source project. Contributions are welcome.

**Reporting a bug or requesting a feature:**

- Open an issue at https://github.com/sancy1/ApiPilot/issues
- Include the version, the observed behavior, and the expected behavior.

**Submitting a change:**

- **One concern per PR.** Keep each pull request focused.
- **Tests for every change.** Every new behavior needs a test; every fixed
  defect needs a regression test.
- **Docs updated in the same PR.** If the change affects the contract,
  update the relevant file under [docs/](docs/) and [SPEC.md](SPEC.md) in
  the same commit.
- **Zero build warnings.** The repository enforces
  `TreatWarningsAsErrors=true`. A PR that introduces a warning will not
  build.
- **`audit.ps1` must pass.** The audit enforces the boundary rules and the
  zero third-party dependency policy. A PR that introduces a forbidden
  symbol or a third-party production package reference fails the audit and
  will not be merged.
- **CI must be green.** Every push and pull request runs the full test
  suite (six .NET harnesses plus the JavaScript client) on GitHub Actions.

Before submitting a change, run `audit.ps1` locally and confirm CI passes
on your branch.

---

## License

ApiPilot is licensed under the MIT License. See [LICENSE](LICENSE).

---

## Author

**Alexander Sanchez Cyril** ([@sancy1](https://github.com/sancy1))

ApiPilot was built as a standalone API boundary library, extracted from the
author's portfolio of .NET services. It is the companion library to
[Portfolio.Resilience](https://www.nuget.org/packages/Portfolio.Resilience) -
the two compose at different layers:

- **Portfolio.Resilience** owns *how* an application safely executes an
  external or transiently unreliable operation.
- **ApiPilot** owns *what* the API sends back after the application has
  produced a result or a terminal failure.

For a production case study of the resilience companion, see **File-Ferry**
- a Windows desktop application that uses Portfolio.Resilience for every
filesystem operation.

The full history - every change and every finding, with sequential IDs - is
in [CHANGELOG.md](CHANGELOG.md).
