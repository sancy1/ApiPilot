<!--
filepath: dotnet/verify/Verify.ApiPilot/FINDINGS.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  The authoritative record of every finding the verifier has produced
          against the published ApiPilot artifacts. Each finding carries an
          ID, the package and version it applies to, what was observed, what
          the shipped artifact documents, how to reproduce it, the impact,
          the status, and whether it blocks adoption.
relates:  Referenced by README.md. Produced by running the verifier against
          the pinned published packages. Not API authority; findings only.
-->

# Findings from the Published ApiPilot Artifacts

This file is the single authoritative record of every finding the verifier has
produced. The verifier consumes the published NuGet packages
(`ApiPilot.Core`, `ApiPilot.AspNetCore`, `ApiPilot.Security`) at pinned
version 1.0.3 and exercises documented behavior through real runtime flows.

Nothing in this file is derived from `dotnet/src`, `dotnet/tests`,
`dotnet/samples`, `SPEC.md`, the repository `docs/` files, or the repository
`CHANGELOG.md` as API authority. Where the repository CHANGELOG is mentioned,
it is mentioned only as non-authoritative context; it is not shipped to NuGet
consumers and therefore cannot resolve a package documentation gap.

Status legend:

- **open** - observed, recorded, not yet resolved. May or may not block adoption.
- **resolved** - a positive assertion: the shipped artifact behaves as documented. These are recorded so the reader can see what has been verified.
- **withdrawn** - recorded in error and corrected. The finding is left in the file with its correction so the record is honest.

---

## Functional deviations - documentation versus runtime behavior

A functional deviation is a case where the shipped artifact's runtime
behavior contradicts a specific claim in the shipped documentation. It is
not a documentation gap (the library works but the documentation is
incomplete), not a packaging gap, and not a positive assertion. It is a
library problem.

No open functional deviations remain. The two that were found against 1.0.3
- F-59 and F-65 - are fixed in 1.0.5. Their entries are in the Resolved
findings section, marked "resolved in 1.0.5". The focused action list is in
DEVIATIONS.md.


## Open findings

### F-14 - SPEC.md is referenced but does not ship inside the package

- **Package:** ApiPilot.Core, ApiPilot.AspNetCore, ApiPilot.Security at 1.0.3.
- **Observed:** The packaged README states that "the wire contract is documented in SPEC.md" and that "the single normative document is SPEC.md". SPEC.md is not present in any of the three restored packages. A consumer who installs the packages receives the DLL, the XML documentation, the `.nuspec`, and the README, but not SPEC.md.
- **Documented expectation:** The README names SPEC.md as the normative document for the wire contract.
- **Reproduction:** Restore ApiPilot.Core 1.0.3 and inspect `lib/net10.0/`. Or inspect the `.nuspec` `<readme>` entry.
- **Impact:** The documentation contract is not honored at the package boundary. A consumer cannot follow the README's normative reference.
- **Status:** open.
- **Blocks adoption:** no, but it is a real documentation gap.

### F-15 - The `.nuspec` description is narrower than the rendered README

- **Package:** ApiPilot.Core and ApiPilot.AspNetCore at 1.0.3.
- **Observed:** The `.nuspec` `<description>` for ApiPilot.Core describes only the framework-independent contracts, and for ApiPilot.AspNetCore describes only the ASP.NET Core integration. The rendered README describes the whole boundary (CSRF, cookies, Origin, Fetch Metadata, OpenAPI, rate limiting, browser client).
- **Documented expectation:** The `.nuspec` description accurately describes the specific package it belongs to.
- **Reproduction:** Inspect the restored `.nuspec` files.
- **Impact:** Low. The `.nuspec` text is accurate for its package; it is simply not the same breadth as the README.
- **Status:** open.

### F-30 - Undocumented default exception mapping for InvalidOperationException

- **Package:** ApiPilot.AspNetCore at 1.0.3.
- **Observed:** An `InvalidOperationException` escaping an endpoint with the default exception middleware produces `error.code = CONFLICT` and HTTP 409.
- **Documented expectation:** The shipped XML for `KnownExceptionTypes.Default` documents the existence and ordering of the default mapping table but does not enumerate the CLR exception-to-error-code mappings. The packaged README documents `CONFLICT` and HTTP 409 as an error condition but does not state that `InvalidOperationException` maps to it.
- **Reproduction:** Scenario 05, sub-check 2.
- **Impact:** Medium. Consumers cannot reliably predict the response classification for an unexpected `InvalidOperationException` from the shipped artifact documentation.
- **Status:** open.
- **Blocks adoption:** no.
- **Non-authoritative context:** The repository CHANGELOG identifies this behavior as designed (entry A-266), but the CHANGELOG is not shipped to NuGet consumers and therefore does not resolve the package documentation gap.

### F-31 - Undocumented default exception mapping for ArgumentNullException

- **Package:** ApiPilot.AspNetCore at 1.0.3.
- **Observed:** An `ArgumentNullException` escaping an endpoint with the default exception middleware produces `error.code = VALIDATION_ERROR` and HTTP 400.
- **Documented expectation:** The shipped XML documents only that `ArgumentNullException` precedes `ArgumentException` in the table; it does not state the code or the status for either. The packaged README describes `VALIDATION_ERROR` as a wire condition, not as the mapping for `ArgumentNullException`.
- **Reproduction:** Scenario 05, sub-check 1.
- **Impact:** Medium. An `ArgumentNullException` escaping a request handler is usually an application bug, not client-supplied malformed input; returning 400 signals to the client that their request was malformed, which may be misleading.
- **Status:** open.
- **Blocks adoption:** no.

### F-36 - The shipped XML does not name the type of `ApiExceptionOptions.ErrorCodeToStatusMap`

- **Package:** ApiPilot.AspNetCore at 1.0.3.
- **Observed:** The shipped XML documents `ErrorCodeToStatusMap` as "the Option D override point" and says "Applications ... populate this dictionary". It does not name the property's declared type. A consumer reading the shipped XML cannot determine whether the property is assignable as a whole, mutable in place, or read-only.
- **Documented expectation:** A public options property whose population is documented should name its declared type in the XML summary.
- **Reproduction:** Read the shipped `ApiPilot.AspNetCore.xml` member for `ApiExceptionOptions.ErrorCodeToStatusMap`.
- **Impact:** Low to medium. A consumer has to compile against the type or inspect the assembly to know how to populate the map. The type is `IReadOnlyDictionary<string, int>` with a public setter; the correct consumer pattern is a whole-value assignment. That pattern is not documented in the XML.
- **Discovery method:** A reflection probe of the shipped DLL using `MetadataLoadContext`, because the XML did not carry the type and the compiler reported the error `CS0200: Property or indexer 'IReadOnlyDictionary<string, int>.this[string]' cannot be assigned to`. The XML alone was not sufficient to write correct consumer code.
- **Status:** open.
- **Blocks adoption:** no.

### N-06 - The rendered README names documents that do not ship inside the package

- **Package:** ApiPilot.Core, ApiPilot.AspNetCore, ApiPilot.Security at 1.0.3.
- **Observed:** The packaged README names per-concern documents under `docs/`, the repository `CHANGELOG.md`, and the repository `audit.ps1` as consumer resources. None ship inside any package.
- **Documented expectation:** The README's documentation map is available to the consumer.
- **Reproduction:** Inspect the restored package contents.
- **Impact:** A consumer cannot follow the per-concern document links from inside the package.
- **Status:** open.

### N-13 - Same as F-14, recorded from the rendered page side

- **Package:** all three at 1.0.3.
- **Observed:** The nuget.org-rendered README states that the wire contract is documented in SPEC.md and that SPEC.md is the single normative document.
- **Documented expectation:** Same as F-14.
- **Status:** open.

### N-20 - Runnable Samples are referenced but live in the repository only

- **Package:** ApiPilot.Core, ApiPilot.AspNetCore at 1.0.3.
- **Observed:** The packaged README references two runnable Samples with `dotnet run` commands. The samples live in the repository, not in any package.
- **Documented expectation:** The README describes how a consumer can run the samples.
- **Impact:** Low. A consumer cannot run the samples without cloning the repository.
- **Status:** open.

### N-21 - The repository CHANGELOG ends at 1.0.2 while the live version is 1.0.3

- **Package:** all three.
- **Observed:** The repository `CHANGELOG.md` last version entry is 1.0.2. The live package version on nuget.org is 1.0.3.
- **Documented expectation:** A version history that names the current version.
- **Impact:** Low. Consumers reading only the repository CHANGELOG see an out-of-date history.
- **Status:** open.

### N-24 - Same as N-21, recorded from the nuget.org side

- **Package:** all three.
- **Observed:** nuget.org shows 1.0.0, 1.0.1, 1.0.3, with no 1.0.2 entry, while the repository CHANGELOG contains a 1.0.2 entry.
- **Status:** open.

### N-32 - The rendered README Releases link is written without a URL scheme

- **Package:** all three.
- **Observed:** In the packaged README, the Releases reference is written as `github.com/sancy1/ApiPilot/releases` without a scheme. The Issues reference in the same document includes the scheme.
- **Documented expectation:** Consistent link form.
- **Impact:** Low. Some Markdown renderers do not auto-link a scheme-less URL.
- **Status:** open.

### N-34 - nuget.org lists 1.0.0, 1.0.1, 1.0.3 with no 1.0.2

- **Package:** all three.
- **Observed:** nuget.org's version list for all three packages skips 1.0.2. The repository CHANGELOG contains a 1.0.2 entry describing a documentation-only repack of all three packages.
- **Documented expectation:** Consistent version history between nuget.org and the repository.
- **Impact:** Medium. A consumer reading the CHANGELOG would expect 1.0.2 to be installable and find no such version.
- **Status:** open.

---

### F-75 - The XML for AddApiPilotOpenApi does not document the AddEndpointsApiExplorer prerequisite

Observed: scenario 19. A host that registers AddApiPilotOpenApi and MapApiPilotOpenApi but not the framework AddEndpointsApiExplorer (for minimal APIs) or AddControllers (for MVC) produces HTTP 500 on every request to the OpenAPI document path. With AddApiPilotExceptions in the pipeline, the error is mapped to the opaque code CONFLICT (the default mapping for InvalidOperationException). The XML for AddApiPilotOpenApi does not state the prerequisite. A consumer who follows the XML gets a broken endpoint with an uninformative error code. Impact: medium. The fix is a documentation change: the XML for AddApiPilotOpenApi should name the AddEndpointsApiExplorer / AddControllers prerequisite. Status: open.
## Resolved findings (positive assertions)

### F-59 - The KeyTransform identity override is honored (resolved in 1.0.5)

Observed: scenario 09, sub-check 7 asserts the identity transform preserves the raw key on the wire. Originally reported as a functional deviation against 1.0.3; fixed in 1.0.5 by a property-scoped ApiErrorFieldsConverter on ApiError.Fields that overrides the global DictionaryKeyPolicy = CamelCase for that property. Status: resolved, positive.

### F-65 - The CSRF attributes work on minimal-API endpoints (resolved in 1.0.5)

Observed: scenario 11, sub-checks 4, 5, and 6 assert [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf] take effect on minimal-API endpoints via .WithMetadata(...). Originally reported as a functional deviation against 1.0.3; fixed in 1.0.5 by CsrfEndpointPolicyResolver, a shared internal resolver that reads both the canonical CsrfEndpointMetadata record and the attribute instances, and applies the documented precedence Require > Skip > UseGlobal. CsrfMiddleware, OriginMiddleware, and FetchMetadataMiddleware all delegate to it. Status: resolved, positive.

### F-16 - ApiPilot.Core has zero runtime dependencies

Observed: the restored `.nuspec` declares a dependency group with no child entries. Status: resolved, positive.

### F-17 - The success wire shape matches the packaged README

Observed: scenario 01, runtime. Top-level `success`, `data`, `message`, `meta`; `meta` carries `requestId`, `timestamp`, `extra`; `status` is absent. Status: resolved, positive.

### F-18 - The shipped XML signatures matched the published assemblies at compile time for the envelope types

This is compile-time evidence only. Runtime behavior is recorded separately by F-17, F-19, F-20, and F-21. Status: resolved, positive.

### F-19 - The error wire shape matches the packaged README

Observed: scenario 03, runtime. `success: false`, `error.code`, `error.message`, `error.fields` as a map of arrays, `meta.requestId`. Status: resolved, positive.

### F-20 - The non-generic success wire shape omits `data` and `status`

Observed: scenario 02, runtime. Status: resolved, positive.

### F-21 - No internal exception detail leaks into the error envelope

Observed: scenario 03, runtime. The serialized payload contains no stack-trace frame, no exception type name, no `StackTrace`, no `InnerException`. Status: resolved, positive.

### F-22 - AddApiPilotCorrelation is public and callable

Observed: scenario 04. Status: resolved, positive.

### F-23 - UseApiPilotCorrelation is public and callable

Observed: scenario 04. Status: resolved, positive.

### F-24 - The default correlation header is X-Request-Id and is generated when absent

Observed: scenario 04, sub-check 1. Status: resolved, positive.

### F-25 - A valid incoming X-Request-Id is echoed unchanged

Observed: scenario 04, sub-check 2. Status: resolved, positive.

### F-26 - An invalid incoming X-Request-Id is replaced under the default policy

Observed: scenario 04, sub-check 3. Status: resolved, positive.

### F-27 - CorrelationOptions.HeaderName override is consumed

Observed: scenario 04, sub-check 4. Status: resolved, positive.

### F-28 - ApiPilotResultsExtensions.ToResult<T>(ApiResponse<T>) writes the envelope over HTTP

Observed: scenario 04, all sub-checks parsed the response body and read `meta.requestId`. Status: resolved, positive.

### F-29 - ApiPilot.AspNetCore depends on ApiPilot.Core exactly at 1.0.3

Observed: the restored `.nuspec` declares a dependency group for `net10.0` containing `ApiPilot.Core` 1.0.3 with `exclude="Build,Analyzers"`, and uses the `Microsoft.AspNetCore.App` framework reference. Status: resolved, positive.

### F-32 - AddApiPilotExceptions and UseApiPilotExceptions are public and callable

Observed: scenario 05. Status: resolved, positive.

### F-33 - ApiExceptionOptions.Mappings is IList<KnownExceptionType> with no setter

Observed: scenario 05, sub-check 3, and confirmed by reflection probe of the shipped DLL. Status: resolved, positive.

### F-34 - ApiExceptionOptions.ErrorCodeToStatusMap has a public setter

Declared type is `IReadOnlyDictionary<string, int>`. The correct consumer pattern is a whole-value assignment such as `ErrorCodeToStatusMap = new Dictionary<string, int> { ["CONFLICT"] = 503 }`. The override is consumed by the middleware. Observed: scenario 05, sub-check 4. Status: resolved, positive.

### F-35 - The exception middleware does not interfere with successful responses

Observed: scenario 05, sub-check 5. Status: resolved, positive.

### N-31 - The ApiPilot.Core `.nuspec` dependency group has no children

Observed: the restored `.nuspec`. Status: resolved.

### N-33 - All three ApiPilot packages are live at 1.0.3

Observed: the verifier's banner at every run, read from `project.assets.json`. Status: resolved.

### F-01, F-03, N-01, N-02, N-03, N-25 - The stale version and publication-status text in the rendered README was resolved by the 1.0.3 repack

Status: resolved.

---

### F-37 - AddApiPilotContentNegotiation and UseApiPilotContentNegotiation are public and callable

Observed: scenario 06, ten sub-checks across six separate in-process hosts. Status: resolved, positive.

### F-38 - The default content negotiation accepts a missing Accept, application/json, and */*

Observed: scenario 06, sub-checks 1 through 3. The default AcceptableResponseMediaTypes set is application/json, and AcceptWildcard defaults to true. Status: resolved, positive.

### F-39 - An unacceptable Accept is rejected with HTTP 406 and error.code NOT_ACCEPTABLE

Observed: scenario 06, sub-check 4. The response uses the standard error envelope; no internal exception detail leaks. Status: resolved, positive.

### F-40 - An unacceptable Content-Type on a body-carrying method is rejected with HTTP 415 and error.code UNSUPPORTED_MEDIA_TYPE

Observed: scenario 06, sub-checks 6 and 8. A POST with no Content-Type and a POST with Content-Type application/xml are both rejected with HTTP 415. Status: resolved, positive.

### F-41 - The ContentNegotiationOptions.AcceptWildcard = false override is consumed

Observed: scenario 06, sub-check 5. With AcceptWildcard false, Accept: */* is rejected with 406 NOT_ACCEPTABLE. Status: resolved, positive.

### F-42 - The ContentNegotiationOptions.AcceptableResponseMediaTypes and AcceptMissingContentType overrides are consumed

Observed: scenario 06, sub-checks 9 and 10. The AcceptableResponseMediaTypes collection is mutable and supports Clear() and Add(). After clearing and adding application/vnd.example+json, that media type is accepted and application/json is rejected with 406. With AcceptMissingContentType true, a POST with no Content-Type is accepted. Status: resolved, positive.
### F-43 - The verifier working recipe is now documented for the next developer

Observed: during the initial build, several classes of avoidable cost were
incurred and not recorded in a reusable form. These include (a) five failed
attempts at reflection-probing the shipped DLL before a working recipe was
found, (b) PowerShell 5.1 constructs that abort scripts (if in expression
position, -- inside an XML comment, unclosed here-strings, reserved
automatic variable names), (c) the discipline of asserting only what the
shipped artifact documents and recording the rest as observations, and (d)
the append-only discipline for growing documents.

Recorded so the next developer does not rediscover them: DEVELOPER_GUIDE.md
in the same folder. Status: resolved, positive. This is a finding about the
verifier's own maintainability, recorded alongside the library findings for
transparency. It does not reflect a defect in any ApiPilot package.
### F-44 - A draft fix exists for the three open documentation findings F-30, F-31, F-36

Observed: the open documentation findings against ApiPilot.AspNetCore 1.0.3
- F-30 (undocumented InvalidOperationException mapping), F-31 (undocumented
ArgumentNullException mapping), and F-36 (unnamed type of ErrorCodeToStatusMap)
- share a single underlying documentation gap: the shipped XML does not
enumerate the default exception mapping table, and it does not name the
declared type of the options property that carries the error-code-to-status
override. A single draft fix that addresses all three is now available at
PROPOSED_FIXES.md in the same folder. Status: resolved, positive. This is a
finding about the completeness of the handover, recorded alongside the
library findings for transparency. It does not reflect a defect in any
ApiPilot package.
### F-45 - AddApiPilotJson(IServiceCollection, ...) is public and callable

Observed: scenario 07. Registers the options through the standard pipeline and configures the minimal-API JSON serializer. Status: resolved, positive.

### F-46 - Default PropertyNamingPolicy and DictionaryKeyPolicy are camelCase

Observed: scenario 07, sub-check 1. The wire body contains camelCase keys and does not contain PascalCase. Status: resolved, positive.

### F-47 - Default EnumMode is AsString

Observed: scenario 07, sub-check 2. Enum values appear as their names on the wire. Status: resolved, positive.

### F-48 - Default DateMode is Iso8601

Observed: scenario 07, sub-check 3. DateTimeOffset values appear as ISO 8601 strings on the wire. Status: resolved, positive.

### F-49 - Default DefaultIgnoreCondition is Never

Observed: scenario 07, sub-check 4. Null values are written explicitly on the wire, not omitted. Status: resolved, positive.

### F-50 - The EnumMode, DateMode, and PropertyNamingPolicy overrides are consumed; JsonSerializerConfigurator.Configure is idempotent

Observed: scenario 07, sub-checks 5 through 8. With EnumMode = Number, the enum appears as a number. With DateMode = UnixTimeSeconds, the date appears as Unix epoch seconds. With PropertyNamingPolicy = null, the property appears in PascalCase. Calling Configure twice on the same JsonSerializerOptions instance leaves the converter count unchanged. Status: resolved, positive.

### F-51 - The shipped XML documents the default of each ApiPilotJsonOptions property but does not name the declared types of PropertyNamingPolicy, DictionaryKeyPolicy, DefaultIgnoreCondition, or ReadCommentHandling

Observed: reading the shipped ApiPilot.AspNetCore.xml for ApiPilotJsonOptions. The XML summary for each of these four properties describes what the property does and what its default is, but does not name the property type. A consumer must compile against the type or inspect the assembly to know the exact declared type. Impact: low. The properties are settable and their effects are documented; only the type names are omitted. Same class as F-36. Status: open.
### F-52 - The pagination surface is public and callable

Observed: scenario 08. AddApiPilotPagination, WithApiPilotPagination, PaginationMetadataAttribute, GetPageRequest, GetSortRequest, GetFilterRequest, GetEffectivePaginationOptions, and ToPagedResult are all public and callable from a consumer. Status: resolved, positive.

### F-53 - The documented pagination defaults are on the wire

Observed: scenario 08, sub-checks 1 and 2. DefaultPageSize is 20, MaxPageSize is 100, SuccessStatusCode is 200, PageNumberBase is 1. A request for pageSize 200 is rejected with HTTP 400 and error.code VALIDATION_ERROR. Status: resolved, positive.

### F-54 - The three-layer precedence chain holds

Observed: scenario 08, sub-checks 3, 4, 5, and 6. The global options, the fluent WithApiPilotPagination overrides, and the [PaginationMetadata] attribute are all consumed. When both the fluent extension and the attribute set MaxPageSize, the attribute wins. Status: resolved, positive.

### F-55 - The paginated envelope shape matches the packaged README

Observed: scenario 08, sub-check 9. Top-level success, data as an array, pagination as an object with page, pageSize, totalItems, totalPages, hasNext, hasPrevious, and meta with requestId. No status key. Status: resolved, positive.

### F-56 - GetPageRequest and GetEffectivePaginationOptions return the effective values in the handler

Observed: scenario 08, sub-checks 7 and 8. GetPageRequest returns the validated PageRequest. GetEffectivePaginationOptions returns the merged options (global plus endpoint overrides). Status: resolved, positive.

### F-57 - The SuccessStatusCode override is consumed at the pipeline level

Observed: scenario 08, sub-check 10. With global SuccessStatusCode = 206, the response carries HTTP 206. Status: resolved, positive.

### F-58 - The shipped XML does not name the declared types of the pagination parser delegates

Observed: reading the shipped ApiPilot.Core.xml for PaginationOptions. The XML documents what ParameterNames, SortDirectionParser, SortParser, IsFilterParameter, and IntegerParser do and what their defaults are, but does not name the declared type of any of them. Same class as F-36 and F-51. Impact: low. The properties are settable and their effects are documented; only the type names are omitted. Status: open.
### F-66 - The CSRF attributes work on controller actions

Observed: scenario 12. [ApiPilotSkipCsrf] on a POST controller action bypasses the middleware (HTTP 200). [ApiPilotRequireCsrf] on a GET controller action enforces protection (HTTP 403, error.code CSRF_HEADER_MISSING). Both attributes on the same action: Require wins. This path was not tested by scenario 11; the 1.0.5 fix covers it and the verifier confirms it. Status: resolved, positive.
### F-67 - The Origin policy honors the CSRF attributes on minimal-API endpoints

Observed: scenario 13. [ApiPilotSkipCsrf] bypasses the Origin policy on a minimal-API endpoint sent with a hostile Origin header (HTTP 200). [ApiPilotRequireCsrf] enforces it on a GET endpoint sent with a hostile Origin header (HTTP 403, error.code CSRF_ORIGIN_REJECTED). The default AllowMissingOrigin=true permits a request with no Origin header and lets the CSRF middleware produce the rejection. The AllowMissingOrigin=false override rejects the missing header before CSRF runs. Status: resolved, positive.
### F-68 - The Fetch Metadata middleware honors the Off and Strict profiles

Observed: scenario 14. The Off profile (the default) is inert; a cross-site Sec-Fetch-Site passes the Fetch Metadata middleware and the CSRF middleware rejects with CSRF_HEADER_MISSING. The Strict profile rejects a cross-site Sec-Fetch-Site with FORBIDDEN (HTTP 403). Strict with AllowMissingHeaders=true permits a missing header and lets CSRF reject; Strict with AllowMissingHeaders=false rejects the missing header with FORBIDDEN. Status: resolved, positive.
### F-69 - AddApiPilotDataProtection and the MultiInstance startup rule are honored

Observed: scenario 15. AddApiPilotDataProtection is public and callable. The single-instance default (MultiInstance=false) starts without key storage. MultiInstance=true without a KeyStorage delegate fails at startup (the host does not start), confirming the documented fail-closed contract. MultiInstance=true with a KeyStorage delegate that persists keys to the file system starts. Two hosts that share an application name and a key ring validate each other CSRF tokens: host A issues a token via its bootstrap endpoint and host B accepts it in a protected POST. Status: resolved, positive.
### F-70 - The rate-limit rejection handler is honored

Observed: scenario 16. AddApiPilotRateLimitRejection is public and callable. The default rejection produces HTTP 429 with error.code RATE_LIMITED and the standard error envelope. The StatusCode override (503) is consumed. The Message override is consumed. EmitRetryAfter=false suppresses the Retry-After header. An invalid StatusCode (200, outside the documented 400-599 range) fails the host at startup, confirming the fail-closed validator. Status: resolved, positive.
### F-71 - The cookie profile validator is honored

Observed: scenario 17. AddApiPilotCookies is public and callable. The default profiles (Authentication, Session, CSRF) start the host. The startup validator fails the host for SameSite=None without Secure, for __Host- with a non-root Path, and for an invalid NamePrefix. The validator passes for __Host- with Secure, no Domain, and Path=/. The library never sets a cookie; it validates the profiles the application applies. Status: resolved, positive.
### F-72 - The security diagnostics surface is strict-composition and reports SECW001

Observed: scenario 18. AddApiPilotSecurityDiagnostics is a strict-composition extension: it requires all five security validators (CsrfOptions, CookieProfileOptions, OriginPolicyOptions, FetchMetadataOptions, ApiPilotDataProtectionOptions) to be registered. When the composition is complete, the diagnostics service resolves and GetDiagnostics returns the SECW001 in-memory-key-ring warning by default. The warning is suppressed when MultiInstance=true with KeyStorage. ApiPilotLogEvents exposes at least 18 distinct non-zero constants. Status: resolved, positive.

### F-73 - The fail-closed startup failures throw OptionsValidationException

Observed: scenarios 15, 16, and 17. Every documented fail-closed startup failure throws Microsoft.Extensions.Options.OptionsValidationException. Confirmed at runtime for MultiInstance=true without KeyStorage (scenario 15), for an invalid RateLimit StatusCode (scenario 16), and for SameSite=None without Secure, __Host- with a non-root Path, and an invalid NamePrefix (scenario 17). The shipped XML did not name the exception type; the verifier observed it. Status: resolved, positive.

### F-74 - The default RATE_LIMITED message string is "Too many requests. Retry later."

Observed: scenario 16. The default error.message for a rate-limit rejection is the exact string "Too many requests. Retry later.". The shipped XML did not name the exact string; the verifier observed it. Status: resolved, positive.
### F-76 - The OpenAPI emitter is public and callable

Observed: scenario 19 (with AddEndpointsApiExplorer registered). The document is served at the default path /openapi/v1.json with the documented defaults (openapi 3.0.x, info.title ApiPilot API, info.version 1.0.0). The DocumentPath, DocumentTitle, DocumentVersion, and IncludeErrorSchemas overrides are consumed. The three error schemas (ApiPilotErrorResponse, ApiPilotError, ApiPilotErrorFields) are present by default; IncludeErrorSchemas=false omits them. An invalid DocumentPath fails the host at startup. Status: resolved, positive.
### F-77 - Sort and filter parsing are honored

Observed: scenario 20. Sort parsing for ?sort=name&direction=asc produces a SortRequest with the field and ascending direction; ?sort=name&direction=desc produces a descending SortRequest. Filter parsing in lenient mode produces a FilterRequest with the passed-through pair (status -> active). StrictQueryValidation=true rejects an unknown query parameter with VALIDATION_ERROR (HTTP 400). The default lenient mode passes it through (HTTP 200). Status: resolved, positive.
## Withdrawn findings

### F-13 - Withdrawn

A first read of the shipped XML incorrectly reported empty `<summary>` entries for a large fraction of the public surface. The XML has zero empty members out of 201. The original finding was the result of a reading error in the inspection script, not a documentation defect. Status: withdrawn.

---

## Notes on the discovery method

Some findings required a reflection probe of the shipped DLL because the shipped XML did not carry the type of a member. The probe used `System.Reflection.MetadataLoadContext` in a temporary .NET 10 console project, reading the DLL from the NuGet cache without executing it. The probe produced the exact declared type of `ErrorCodeToStatusMap` and confirmed it has a public setter. The XML alone was not sufficient. That gap is recorded as F-36 above.

No local source files were read at any point. No `ProjectReference` was added. The verifier consumes only the published packages.