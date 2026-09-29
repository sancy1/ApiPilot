<!--
filepath: CHANGELOG.md
package:  n/a (repository root)
since:    v0.1.0-alpha.0
purpose:  Version history for ApiPilot, following Keep a Changelog and SemVer.
-->

# Changelog

All notable changes to ApiPilot are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

ApiPilot follows strict semver discipline:

- **Patch** bump for documentation, non-breaking fixes, and doc-only XML comment additions.
- **Minor** bump for backward-compatible new features.
- **Major** bump for breaking changes.

Published versions are immutable. Any change, including documentation-only changes,
requires a new version number. Published tags are never force-pushed, deleted, or amended.

## [Unreleased]

## [1.0.2] - 2026-09-29

### Changed

- README: split the merged "Contributing and license" section into three
  sections separated by horizontal rules: **Contributing**, **License**, and
  **Author**. The Contributing section now describes how ApiPilot actually
  enforces quality: the six repository-local test harnesses, `audit.ps1`,
  and the GitHub Actions CI pipeline. The Author section names the author
  and clarifies the composition with the companion Portfolio.Resilience
  library. Also corrected the "Project status - honest notes" section: the
  previous text stated the packages were not yet published to NuGet, which
  is no longer true. Documentation-only change: no code, no wire-contract,
  and no API-surface changes. The three packages repack at 1.0.2 so the
  nuget.org README renders the corrected content.

## [1.0.1] - 2026-09-29

### Changed

- README: added NuGet version badges for the three packages (ApiPilot.Core,
  ApiPilot.AspNetCore, ApiPilot.Security), an MIT license badge, a .NET 10
  badge, and a CI build-status badge. The package names in the Packages
  table are now clickable links to their nuget.org pages. An install-command
  block was added under the table. Documentation-only change: no code, no
  wire-contract, and no API-surface changes. The three packages repack at
  1.0.1 so the nuget.org README renders the updated content.

## [1.0.0] - 2026-09-29

### Added (Phase 5.5)

- The v1.0.0 milestone. The repository VERSION is 1.0.0. The three .NET packages pack at 1.0.0; the client package.json, src/version.js, and the generated dist artifacts are 1.0.0. Every current-state version literal in the repository reads 1.0.0; every historical literal is preserved. The [Unreleased] section is promoted to [1.0.0] with this date.

- The wire contract is frozen. SPEC.md and docs/response-contract.md now describe the actual serialized shape: the success envelope always carries message (null when the application provides none) and meta (meta.requestId, meta.timestamp, and meta.extra, always present). This closes A-072b/c by freezing the shape the code produces rather than changing the code.

- The documentation index rows for the Phase 5.0 through 5.4 docs are promoted to Shipped at 1.0.0.

- Status files updated at the milestone close: README.md, PLANNING.md (correcting the stale version rows, F-001), and IMPLEMENTATION_STATE.md.

- Post-1.0.0 backfill items recorded in IMPLEMENTATION_STATE.md with explicit block plans: the meter consolidation, the audit label correction A-202, and the derive-version-from-assembly refactor that prevents the hard-coded-version-literal class.

- Independent security review: Not yet scheduled. The library internal test suites (contract tests, fuzz tests, and the browser security suite) have passed; no external reviewer has been engaged. Recorded honestly per the discipline.

### Findings from the build (Phase 5.5)

- A-273 (test-code): two client skeleton tests pinned the old client version 0.4.0. The 1.0.0 bump changed the version deliberately, so the pins were updated to 1.0.0. The since header was preserved.

- A-274 (process): a verification line used an if expression as a value inside a parenthesized subexpression, a PowerShell 5.1 syntax error. The check failed; no repository file was touched.

- A-275 (product): three production constants carried a hard-coded version literal of 0.4.0 when the assembly is 1.0.0: ApiPilotActivitySource.Version, the RateLimitDiagnosticsHook meter version, and ApiPilotCsrfCounters.MeterVersion. The design error: telemetry metadata was treated as a compile-time constant instead of runtime build metadata; ActivitySource and Meter accept a runtime version string. Caught by the pre-release version convergence scan. Corrected to 1.0.0. The recurrence-prevention refactor is scheduled as Backfill B.7: a shared internal provider that reads AssemblyInformationalVersionAttribute from the library assembly (never the entry assembly, which would report the consuming application version), falling back to Assembly.GetName().Version, with four tests including a source scan that fails on a hard-coded version literal in a diagnostics constructor.

- A-276 (test-code): two version-pinning tests asserted the old version. Corrected to 1.0.0. Same class as A-273.

- A-277 (process): the convergence-scan script placed else on its own line after a closing brace, a PowerShell 5.1 parse error. The scan itself ran and reported no hits; no repository file was affected. Same class as A-129.

### Added (Phase 5.3)

- The pure shared outcome model in dotnet/tests/ApiPilot.TestHarness/TestHarness.cs. It defines TestOutcomeKind (Passed, Failed, Skipped, Unavailable), a TestOutcome record carrying a kind and an optional message, a pure TestOutcomeClassifier.Classify function, and a pure TestExitPolicy.ExitCode function. The model carries no state and has no side effects. It is linked into all six test harnesses through a Compile Include in each project file. There is one definition and one behavior across the repository.

- The exit-code policy. The runner reads --allow-skip from the command line. The policy is: 1 when any test failed; 2 when no test failed but a skip or unavailability is present and --allow-skip was not supplied; 0 otherwise. A run with an unaccepted skip or unavailability is reported as SKIP-NOT-ACCEPTED and returns 2, a distinct non-success result.

- Every existing harness reports the same summary fields: Test classes, Tests run, Passed, Skipped, Unavailable, Failed, Duration, and the TEST RESULT line. A project with no skips or unavailability prints Skipped: 0 and Unavailable: 0. All existing tests keep their behavior: a void or Task test that completes normally is Passed and one that throws is Failed. All five existing harnesses remained green with zero regressions: Core 211, AspNetCore 377, Security 347, ContractTests 15, FuzzTests 27.

- TestHarnessContractTests in the Core test project, 8 tests. It calls the pure classifier and the pure exit policy directly. It proves a Passed outcome classifies as Passed, a Skipped outcome as Skipped and not Passed, an Unavailable outcome as Unavailable and not Passed, a null or unknown return as Passed, and the exit policy returns 1 on failure, 2 on an unaccepted skip or unavailability, and 0 on a clean or accepted-skip run. The test uses the classifier directly, so the harness run itself stays at zero skips and exit 0.

- The ApiPilot.BrowserTests project, added to ApiPilot.slnx as the ninth project. It references neither the ApiPilot libraries nor the ASP.NET Core framework. It uses only the base .NET runtime: HttpClient, Process, ClientWebSocket, JSON, and filesystem APIs. This keeps the browser harness isolated from the library under test and prevents compile-time coupling.

- SampleHost.cs and BrowserHost.cs. SampleHost builds and launches the standalone Samples.Api on a free loopback port. BrowserHost detects a Chromium or Edge executable in the standard locations and in PATH, with an APIPILOT_BROWSER_PATH override; launches it headless with a temporary profile and a free remote-debugging port; polls the debugging endpoint until it answers; filters the target list to type page; and exposes the page-level webSocketDebuggerUrl. When no browser is present, IsBrowserAvailable is false and SkipReasonNoBrowser is the visible text SKIPPED: no supported Chromium/Edge executable found.

- The repository-owned Chrome DevTools Protocol client. CdpClient is a minimal transport over ClientWebSocket: one command at a time, an incrementing id, the reply whose id matches, and event messages discarded. CdpConnection wraps the methods the fixtures need: Target.getTargets, Page.navigate, Runtime.evaluate, and Network.getCookies. There is no browser automation package.

- BrowserSecurityTests, 7 tests in one class. Every test returns Task<TestOutcome>, so a missing browser or an unavailable fixture is reported as a skip or unavailability, never as a silent pass. Six tests execute against the real running Samples.Api and a real headless browser: the HTTP-only auth cookie is unreadable through document.cookie; the CSRF token never appears in localStorage or sessionStorage; a POST to /api/submit without the CSRF header is rejected with 403; a POST with a hostile Origin header is rejected with 403; and the bootstrap endpoint issues a non-empty token. The seventh test, the HTTPS reverse-proxy fixture, returns Unavailable with the visible text UNAVAILABLE: HTTPS reverse-proxy fixture unavailable. It is recorded as an environment-dependent acceptance item for a future phase or external CI, not a passing security assertion.

- The browser suite result at the Phase 5.3 close: one class, six tests executed and passed, zero skipped, one unavailable, zero failed. Without --allow-skip the suite reports SKIP-NOT-ACCEPTED and returns 2. With --allow-skip the suite reports PASS and returns 0. The unavailability is never folded into the pass count.

### Findings from the build (Phase 5.3)

- A-269 (process): the ApiPilot.FuzzTests project file received the Compile Include link twice, producing a duplicate Compile item and NETSDK1022. No write corrupted the other projects; only FuzzTests was affected. Fixed by rewriting the project file to exactly one Compile Include. Lesson: a file this small is safer rewritten in full than edited by an anchored insert, because a rewrite is idempotent by construction.

- A-270 (process, repeated): a dotnet run --no-build was issued after a build that had failed, so the run reported the previous binary, including a PASS for zero tests. The PASS was meaningless. A run after a failed build is not valid evidence. The rule applied: build, confirm exit 0, then run.

- A-271 (process): a run command was issued alongside a build that was expected to fail at an intermediate step, producing a confusing file-not-found secondary error. The correct form at an intermediate step is the build alone; the run is added only when the build is green.

- A-272 (test-code): BrowserSecurityTests declared six fixtures as async Task but contained return TestOutcome values, producing twelve CS1997 errors. The correct declaration is async Task<TestOutcome>. Fixed by the rewrite. Lesson: a method that returns an outcome must declare the outcome-returning type.

### Added (Phase 5.2)

- The ApiPilot.FuzzTests project. A repository-local executable fuzz and abuse harness, added to ApiPilot.slnx as the eighth project. It carries its own TestRunner, TestAssert, Program, and SampleHost, copied from the ContractTests harness with the namespace changed, matching the per-project harness pattern. It references ApiPilot.AspNetCore and ApiPilot.Security and the ASP.NET Core shared framework. It has no third-party dependencies.

- The curated fuzz set, 27 tests across 5 classes. The set is deterministic, not random. Each case is a specific hostile shape named by the plan.

- CsrfTokenFuzzTests.cs (7 tests). It calls the CSRF plaintext codec directly. It rejects an empty payload, a payload with no delimiter, a payload with adjacent delimiters, a payload with a third delimiter, a payload whose issued-at part is not numeric, and a payload whose issued-at part is out of range. A positive case round-trips a valid payload so a codec that rejected everything would fail.

- OriginFuzzTests.cs (6 tests). It calls the RFC 6454 origin validator directly. It rejects an empty string, the literal null origin, a non-absolute string, and an origin carrying a path or query. It proves SameSite match mode returns false. A positive case parses a well-formed origin.

- FetchMetadataFuzzTests.cs (5 tests). It calls the Fetch Metadata evaluator directly. It proves the Off profile passes a cross-site value, Compat allows a missing header, Strict rejects a present cross-site value with the reason DisallowedSiteValue, Strict accepts same-origin, and Strict rejects a malformed header with the reason MalformedSiteHeader when missing headers are not allowed.

- QueryFuzzTests.cs (5 tests). It drives the running Samples.Api. A page size above MaxPageSize, a page below the minimum, and a non-numeric page value are each rejected without a 5xx. An unexpected request content type is rejected with a 4xx. A hostile query leaks no stack-trace frame and no namespace prefix. The internal-detail check scans the actual body bytes.

- HeaderFuzzTests.cs (4 tests). It drives the running Samples.Api. A duplicate CSRF header, an OPTIONS request, a PATCH to a GET-only endpoint, and an oversized Origin header are each handled without a 5xx.

- Test totals at the Phase 5.2 close: ApiPilot.FuzzTests is 27 tests across 5 classes (7 CSRF, 6 origin, 5 fetch metadata, 5 query, 4 header). The pure-validator cases (18) run in-process; the HTTP cases (9) drive the real sample through SampleHost.

### Findings from the build (Phase 5.2)

- A-268 (process): the ApiPilot.slnx insertion block was truncated by the paste channel mid for-loop, producing a parse error and a partial execution. No write occurred; the file was untouched. Fixed by replacing the loop-based splice with a single string Replace before the closing tag, a form short enough that it cannot truncate. Same class as A-062, A-123, A-128, and A-203.

- The three verification checks that close this phase: the file list shows every planned file; the counted [TestClass] and [Test] figures match the runner summary; the run is green. Source count 5 classes and 27 tests equals the runner count 5 classes and 27 tests.

### Added (Phase 5.1)

- The ApiPilot.ContractTests project. A repository-local executable contract-test harness, added to ApiPilot.slnx as the seventh project. It launches the standalone Samples.Api as a child process, builds it before launching so a stale binary is never exercised, waits for readiness with a bounded timeout, captures stdout and stderr for diagnostics, and kills the process tree on dispose.

- SampleHost.cs (new). The build-and-launch helper. It chooses a free loopback port, sets ASPNETCORE_URLS, runs dotnet build on the sample project, starts the sample, polls a known endpoint for readiness, and tears the process down. It fails with the captured output when the build or the readiness check fails.

- EnvelopeContractTests.cs (6 tests). It sweeps the running sample endpoint surface: GET /api/ok (success envelope), GET /api/fail (error envelope with a stable code), POST /api/validate (validation error envelope), GET /api/items (success envelope whose data carries pagination), the CSRF bootstrap endpoint (the documented bare token shape), and POST /api/submit without a token (403 error envelope). Each test reads the actual response bytes.

- StatusMappingContractTests.cs (5 tests). It proves the error-code-to-status mapping by exercising ErrorResponseResult directly (the A-250 pattern), not by copying the table. It asserts VALIDATION_ERROR maps to 400, RESOURCE_NOT_FOUND to 404, RATE_LIMITED to 429, INTERNAL_ERROR to 500, and an unrecognized wire code falls back to 500.

- NoCredentialLeakTests.cs (4 tests). It scans actual response bodies for Set-Cookie, Cookie, Authorization, and X-CSRF-TOKEN. The scan is case-insensitive and asserts the body is non-empty before scanning, so it cannot pass vacuously (A-187, A-188).

- The standalone Samples.Api is now the contract-test target. Three defects in the sample were found and fixed by this phase: it did not register Data Protection before AddApiPilotCsrf (A-265); its /api/validate endpoint returned a bare object rather than the validation envelope; and its /api/items endpoint returned a bare object rather than the success envelope. Every envelope-writing endpoint now builds its response through ApiResponseBuilder and ErrorResponseResult, and resolves its correlation id through ICorrelationIdAccessor with a TraceIdentifier fallback.

- A-185 resolution. Full TypeScript compiler validation is recorded as an Accepted limitation. Full compiler validation is unavailable under the enforced zero-dependency client policy: tsc is supplied by the TypeScript package, and audit.ps1 Section 7a fails any client development dependency. Declaration/runtime name parity remains the strongest repository-supported check. Reopen only if the policy changes or an approved external CI tool is introduced. No typescript devDependency and no tsconfig.json were added.

- Test totals at the Phase 5.1 close: ApiPilot.ContractTests is 15 tests across 3 classes (6 envelope, 5 status mapping, 4 leak).

### Findings from the build (Phase 5.1)

- A-264 (process): a PowerShell array literal contained a backslash before the class-opening brace in SampleHost.cs, producing a broken string. Caught before the block was sent; fixed to the plain opening brace. Same class as A-193 and A-257 (quote and escape defects in array literals).

- A-265 (product): Samples.Api called AddApiPilotCsrf without registering Data Protection. The host failed at startup with InvalidOperationException: No service for type Microsoft.AspNetCore.DataProtection.IDataProtectionProvider has been registered, thrown from CsrfServiceExtensions.AddApiPilotCsrf. Every contract test failed because the sample never became ready. Found by the Phase 5.1 sweep, which captured the sample stderr. Fixed by adding builder.Services.AddDataProtection() before the CSRF registration. This is a product defect in the sample, discovered by the contract test that was built to find it.

- A-266 (test-code): EnvelopeContractTests.Fail_ReturnsErrorEnvelope hardcoded an expected status of 500 for GET /api/fail. The library default exception map sends InvalidOperationException to CONFLICT (409), so the sample correctly returned 409. The test asserted an implementation detail rather than the contract. Fixed by asserting the error envelope shape and a stable, non-empty code. Lesson: a contract test asserts the contract shape and a stable code, not a status the application map chooses.

- A-267 (test-code): EnvelopeContractTests.Items_ReturnsPaginatedEnvelope read the pagination key at the envelope root. After the sample was corrected to return the success envelope, the pagination object moved under data. The test still expected the pre-envelope shape. Fixed by reading root data pagination. Lesson: when a fix moves a key under the envelope, any test that read it at the root is a stale assertion.

- Test totals at the Phase 5.1 close: ApiPilot.ContractTests is 15 tests across 3 classes. The new tests are 15 across 3 classes. The repository test total is recomputed at the phase gate.

### Added (Phase 5.0)

- The repository-owned OpenAPI emitter in ApiPilot.AspNetCore/OpenApi/. It reads the framework ApiExplorer descriptors and writes an OpenAPI 3.0.3 document as System.Text.Json nodes. No Microsoft.OpenApi, Swashbuckle, NSwag, or Swagger reference. The files are ApiPilotOpenApiOptions.cs, ApiPilotOpenApiOptionsValidator.cs, StandardErrorSchema.cs, ApiPilotSchemaDocument.cs, ApiPilotSchemaEmitter.cs, and ApiPilotOpenApiExtensions.cs. A reader who wants a different OpenAPI version is out of scope for this emitter.

- The error schemas are published in components.schemas: ApiPilotErrorResponse, ApiPilotError, and ApiPilotErrorFields. The shapes are derived from SPEC.md. The IncludeErrorSchemas option gates their inclusion. The IncludePaginationSchema option is reserved for the emitter pagination path.

- The document path is configured through ApiPilotOpenApiOptions.DocumentPath (default /openapi/v1.json). The mapping extension MapApiPilotOpenApi takes no path parameter; it reads the configured path. There is one coherent override mechanism.

- The standalone reference API dotnet/samples/Samples.Api/. A single-host sample (not in ApiPilot.slnx) that exercises the success, error, validation, pagination, CSRF bootstrap and protection, correlation, and platform rate-limit rejection paths. It has no third-party dependencies; it builds separately from the solution. The host URL is not hard-coded; it defaults to http://127.0.0.1:5090 when neither ASPNETCORE_URLS nor --urls is supplied.

- The OpenAPI tests (ApiPilot.AspNetCore.Tests/OpenApi/, 18 tests across 4 classes). ApiPilotOpenApiOptionsTests (4) proves the options defaults and mutability. ApiPilotSchemaEmitterTests (6) proves the document structure, the OpenAPI version, the path-key form, the method mapping, the error-schema publication, and the IncludeErrorSchemas override. OpenApiSchemaParityTests (5) is the A-235 proof: it serializes a real error envelope through camelCase options and asserts the generated schema declares exactly the serialized keys, in both directions. ApiPilotOpenApiIntegrationTests (3) serves the document over real HTTP and proves the DocumentPath override is consulted.

- A plan correction, recorded per the honest-findings rule: the implementation plan named Phase 4.4 as the Phase 5.0 dependency. There is no Phase 4.4; Phase 4 ends at 4.3. The real dependency is the completed Phase 4.3 (v0.5.0). The plan also named docs/enterprise-evidence-pack.md; the repository uses docs/supply-chain.md. The repository name wins.

### Findings from the build (Phase 5.0)

- A-252 (process): the first write of Samples.Api/Program.cs failed with three errors: a missing using Microsoft.AspNetCore.Hosting for the UseUrls extension, a guessed PageRequest constructor that does not exist, and a CA1050 top-level type. Two were flagged as uncertain before the write; both should have been read first. The build caught them; no file corruption.

- A-253 (process): the first corrective to Program.cs did not land. The file on disk remained the original broken version because the corrective was not written as a self-verifying block and its interactive-scope guard created a false already-applied path. Corrected by the discipline own rule: a corrective to a known-bad file uses no guard, writes unconditionally, and proves itself with markers in the same block.

- A-254 (process): the second corrective put SampleDto in namespace Samples.Api, which is invisible to the top-level statements that reference it (CS0246). Fixed by the global-namespace declaration with a scoped pragma warning disable CA1050 and an explanatory comment, the documented exception the discipline allows.

- A-255 (process): a C# probe body was emitted as raw lines in a PowerShell instruction rather than as a write block, so PowerShell parsed the C# and failed. Same class as A-062. Fixed by writing every C# file through a PowerShell array-literal write block.

- A-256 (product): ApiPilotOpenApiOptions.IncludeErrorSchemas was declared but not consulted. ApiPilotSchemaDocument.Build unconditionally added the three error schemas. This is the A-098/A-108/A-109 override-declared-but-not-consulted class. Fixed by gating the schemas block on options.IncludeErrorSchemas. The override is now consulted and the IncludeErrorSchemas test uses it.

- A-257 (process): a PowerShell array-literal quote-escaping defect produced a two-character char literal in ApiPilotSchemaEmitter.cs, yielding CS1012. Same class as A-193/A-194/A-195. Fixed by using the char overloads (TrimStart(char), Replace(char, char)) throughout, which have no ambiguous quoting.

- A-258 (process): a verification line used a backslash-quote sequence inside a double-quoted PowerShell string. PowerShell 5.1 does not use backslash as an escape, so the block aborted before its write ran. Same class as A-044/A-193. Fixed by removing the sequence and using char-based comparison in the verification.

- A-259 (test-code): a verification check searched for the two-character sequence slash-quote to detect a defective char literal, but the sequence legitimately occurs in correct code (string literals ending in a slash). The check produced a false positive; the build proved the code correct. Same class as A-242/A-246.

- A-260 (process, recurring): a missing using for a referenced type. This occurred three times in Phase 5.0: Microsoft.AspNetCore.Hosting for UseUrls, ApiPilot.Core.Pagination for PageRequest, and Microsoft.AspNetCore.Http for HttpContext. The behavioral rule adopted: the pre-write checklist must write out the complete using list as it will appear in the file and point at the using that resolves every type named in the body, before the write, not deferred to the build.

- A-261 (process): a write block was sent with a knowingly malformed verification line and a note to edit it before running. A block must be runnable exactly as written; requiring manual editing risks a partial paste. Fixed by keeping the clean count line used in the other test blocks.

- A-262 (process): a dotnet run used --no-build after a new test file was added, so the run executed a stale DLL and reported 374 tests when the source contained 377. Same class as A-091/A-093/A-094. Fixed by building fresh before running, and by comparing the DLL timestamp to the source timestamp before trusting a run.

- A-263 (process): a helper with an optional second parameter does not convert to a one-parameter Action<T> as a method group. CS1503. Fixed by providing a one-parameter overload and passing a lambda at the override call site. Related to the A-107 init-vs-Action<T> mismatch: the delegate conversion is not permissive.

- Test totals at the Phase 5.0 close: 935 .NET tests across 115 test classes (211 Core across 22 classes, 377 AspNetCore across 52 classes, 347 Security across 41 classes). 60 JavaScript tests across 5 files. The new AspNetCore tests are 18 across 4 classes.

## [0.5.0] - 2026-09-28

### Added (Phase 4.2 and 4.3)

- Multi-instance operational support and the v0.5.0 milestone. Phase 4.2 proves ApiPilot across two application instances sharing a Data Protection key ring; Phase 4.3 is the version milestone. The repository VERSION is bumped from 0.4.0 to 0.5.0; the .NET packages repack at 0.5.0 through Directory.Build.props.

- The CSRF middleware now honors CorrelationOptions.EchoInResponseBody. Before this phase, CsrfMiddleware.WriteErrorAsync resolved the request id from the accessor with a TraceIdentifier fallback but ignored the echo flag, unlike the exception middleware and the rate-limit hook. The middleware now routes its error envelope through the shared CorrelationEnvelope seam, completing the A-120 response-envelope contract across all three envelope writers.

- CorrelationEnvelope is now public. The shared error-envelope seam (CorrelationEnvelope.ResolveRequestId) was internal and lived in ApiPilot.AspNetCore. The CSRF middleware is in ApiPilot.Security, a different assembly, so the seam was not reachable. It is promoted to public, with its existing XML documentation intact.

- The Data Protection startup validation now runs. ApiPilotDataProtectionExtensions registered the InstanceSafetyValidator but did not surface it at startup. The DataProtectionStartupDiagnostics now resolves the registered IValidateOptions<ApiPilotDataProtectionOptions> validators, runs them against the bridged options instance, aggregates failures, and throws OptionsValidationException through the DataProtectionStartupHostedService. The host fails closed when MultiInstance is true and no KeyStorage delegate was configured. The inert AddOptions<T>().ValidateOnStart() wiring is removed; it did not fire in this registration shape.

- Multi-instance tests (ApiPilot.Security.Tests). MultiInstanceCsrfIntegrationTests now proves: two hosts sharing a key ring validate each other tokens in both directions; two hosts with separate key rings reject with the exact CSRF_TOKEN_INVALID wire code; and a MultiInstance host without key storage fails closed at startup. Each test creates a unique temporary key-ring directory and removes it in finally with a cleanup assertion.

- The sample dotnet/samples/Samples.MultiInstance/. A standalone project (not in ApiPilot.slnx) that starts two instances on loopback ports with a shared key ring and application name, issues a CSRF token from instance A, and sends a protected request to instance B. It has no third-party dependencies; it builds separately from the solution.

- docs/multi-instance.md (created). The multi-instance contract: the two requirements (the shared persisted key ring and the same application name), the failure mode (CSRF_TOKEN_INVALID), the startup validation, and the two diagnostic signals.

- docs/observability.md (created). The observability contract: the stable log event catalogue, the redaction policy and its exact scope, the metric instruments, the process-wide ActivitySource, and the Option D surface.

- docs/rate-limiting.md (promoted). The document was created in Phase 4.1; its index row is promoted to Shipped.

- docs/data-protection.md (corrected). The startup-validation wording now states that the InstanceSafetyValidator runs at startup through the DataProtectionStartupHostedService, matching the implementation.

- docs/README.md (changed). The observability.md, rate-limiting.md, and multi-instance.md index rows are promoted to Shipped | 0.5.0.

- Test totals at the v0.5.0 close: the Security suite passes at 347 tests. The full repository totals are recorded by the final verification run.

### Findings from the build (Phase 4.2 and 4.3)

- A-239 (product defect): the CSRF middleware did not honor CorrelationOptions.EchoInResponseBody. CsrfMiddleware.WriteErrorAsync resolved the request id from the accessor with a TraceIdentifier fallback but ignored the echo flag, unlike the exception middleware and the rate-limit hook. Fixed by routing the CSRF error envelope through the shared CorrelationEnvelope seam. This is the same A-120 response-envelope class paid for at A-040 and A-118.

- A-240 (process, the A-079 splice class): the Block 1 corrective that added the correlationOptions constructor parameter appended the new parameter line without replacing the preceding parameter closing paren. The result was a malformed parameter list (28 cascade compiler errors). The read-back of the file caught the exact one-line defect; the fix was a single character, the closing paren to a comma.

- A-241 (design, cross-assembly visibility): CorrelationEnvelope was internal and lived in ApiPilot.AspNetCore. The CSRF middleware is in ApiPilot.Security, a different assembly with no InternalsVisibleTo grant. The seam was not reachable (CS0122). Promoted to public (class and method) so the shared seam can be used across the two production assemblies. The public surface grew by one well-documented static helper.

- A-242, A-246, A-249 (process, the check-pattern and anchor class; consolidated): the close cycle produced repeated check-and-anchor defects: a PowerShell -match with a ^ anchor that, without (?m), matched only the start of the whole file rather than the line (a false negative); an index map derived from line numbers in a previously read file rather than from the exact bytes at the moment of the write (an off-by-one); and an anchor that checked a blank line for a code line. In every case the guard fired and no corrupt write occurred, and the correction was to read the exact index:content pair immediately before the write and assert on the content, never the line number. This class is the same as A-021, A-168, A-191, A-216, A-226, A-236.

- A-243 and A-236 (process, the count-estimate class; consolidated): the expected test count in a verification block was estimated rather than counted from the pre-write read, producing an off-by-one. The content was correct; the expectation was wrong. The rule holds: the expected test count is derived by counting the markers in the write content, never estimated.

- A-244 and A-250 (test-design defect, reverted): the initial fix for the missing ApiPilotSecurityDiagnostics registration added AddApiPilotSecurityDiagnostics() to the minimal multi-instance test hosts. That extension is a strict composition extension: its SecurityConfigurationValidator requires all five security validators to be registered and fails the host with SEC002 when any is absent. The minimal host registers only DataProtection and Csrf, so the addition turned three failures into six. The fix was reverted; the two redundant diagnostics tests were removed (the SECW001 behavior is covered by the existing ApiPilotSecurityDiagnosticsTests unit tests). The lesson: read what a registration extension does at startup before adding it to a host.

- A-245 (product defect): the Data Protection startup validation did not run. ApiPilotDataProtectionExtensions registered the InstanceSafetyValidator via TryAddEnumerable but did not surface it at startup; the documented claim in docs/data-protection.md (the validator runs at startup through ValidateOnStart) was not realized. A probe proved that a ValidateOnStart wiring added to the extension did not fire in this registration shape (the IOptions<T> OptionsWrapper bridge). Fixed by running the registered validators in DataProtectionStartupDiagnostics.Emit, invoked by the existing DataProtectionStartupHostedService, throwing OptionsValidationException on any failure. The inert ValidateOnStart call was removed. The documented fail-closed contract is now real.

- A-247 (process, the A-240 recurrence): the multi-slice rebuild that added the validators constructor parameter and assignment inserted the new parameter and the new assignment alongside the copied lines, without replacing the logger parameter terminator and without placing the assignment inside the constructor body. The brace count balanced but the structure was wrong; the structural read before the build caught it. The lesson: a splice that adds a parameter replaces the preceding parameter terminator, and an added statement goes before the block closing brace, not after.

- A-248 (process, the doc-tag companion; the A-068 and A-059 class): the DataProtectionStartupDiagnostics constructor gained the validators parameter but the matching param tag was omitted from the XML documentation; CS1573 fired. Fixed by adding the param tag. The lesson: a signature change that adds a parameter adds the matching param tag in the same edit.

- A-251 (test-code defect): the sample Program.cs omitted using Microsoft.AspNetCore.Hosting (for UseUrls) and using Microsoft.AspNetCore.DataProtection (for PersistKeysToFileSystem); the platform extension namespaces were missing though the ApiPilot security namespace was imported. The explicit sample build gate caught both. The lesson: every extension method containing namespace is imported; an ApiPilot namespace does not provide the platform extensions.

- Note (plan-versus-repository corrections, not defects): the Phase 4.2 plan named dotnet/tests/ApiPilot.AspNetCore.Tests/MultiInstanceTests.cs; the actual multi-instance test surface is dotnet/tests/ApiPilot.Security.Tests/Integration/MultiInstanceCsrfIntegrationTests.cs, which was extended. The plan said docs/multi-instance.md would be expanded; the file did not exist and was created. The plan named the sample at samples/Samples.MultiInstance/; the repository convention places it at dotnet/samples/Samples.MultiInstance/.


### Added (Phase 4.1)

- Rate-limit integration. Six new source files plus one refactor, one audit extension, one document, and four test files. ApiPilot integrates with the ASP.NET Core platform rate limiter through the platform OnRejected callback; it implements no limiter, no policy, and no partition.

- dotnet/src/ApiPilot.AspNetCore/RateLimiting/RateLimitReasonCodes.cs: the stable diagnostic reason-code constants (none, limit_exceeded, queue_limit, unknown) used as the reason tag on the metric.

- dotnet/src/ApiPilot.AspNetCore/RateLimiting/ApiPilotRateLimitOptions.cs: the Option D override surface. StatusCode (default 429), Message (default a fixed safe ApiPilot string), and EmitRetryAfter (default true). The wire error code RATE_LIMITED is fixed and not overridable.

- dotnet/src/ApiPilot.AspNetCore/RateLimiting/RateLimitDiagnosticsHook.cs: the platform OnRejected handler. It writes the standard RATE_LIMITED error envelope through ErrorResponseResult (the single serialization seam), emits the apipilot.ratelimit.rejected metric, emits a sanitized structured log event (id 4001), and passes through a platform-supplied Retry-After value. The configured StatusCode is made effective through a per-response clone of ApiExceptionOptions whose ErrorCodeToStatusMap overlays the RATE_LIMITED entry; the registered options instance is never mutated.

- dotnet/src/ApiPilot.AspNetCore/RateLimiting/ApiPilotRateLimitExtensions.cs: the opt-in AddApiPilotRateLimitRejection extension. Registers ApiPilotRateLimitOptions through the standard options pipeline with ValidateOnStart and the ApiPilotRateLimitOptionsValidator. It does not register the platform limiter and does not set OnRejected; the application owns both.

- dotnet/src/ApiPilot.AspNetCore/Configuration/ApiPilotRateLimitOptionsValidator.cs: the fail-closed startup validator. Rejects a StatusCode outside 400-599 and a null, empty, or whitespace Message.

- dotnet/src/ApiPilot.AspNetCore/Middleware/CorrelationEnvelope.cs (new) and dotnet/src/ApiPilot.AspNetCore/Middleware/ApiPilotExceptionMiddleware.cs (refactored): the shared ResolveRequestId seam. The two response-envelope correlation concerns (the accessor value with TraceIdentifier fallback; the empty id when EchoInResponseBody is false) are now implemented once and used by both the exception middleware and the rate-limit hook. The exception middleware was refactored to call the seam; the 303 AspNetCore tests passed before and after, proving the behavior preserved.

- dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotLogEvents.cs (changed): RateLimitRejected = 4001 added to the catalogue (the 4xxx block). The catalogue is now 18 constants, pairwise distinct.

- dotnet/src/ApiPilot.AspNetCore/DependencyInjection/ApiPilotBuilder.cs (changed): ConfigureRateLimitRejection added, delegating to AddApiPilotRateLimitRejection. The builder now covers seven concerns.

- audit.ps1 (changed): the forbidden-symbols list is extended from 26 to 32 entries with the rate-limiter construction types (TokenBucketRateLimiter, SlidingWindowRateLimiter, FixedWindowRateLimiter, ConcurrencyLimiter, RateLimiter, PartitionedRateLimiter). The integration seam types (RateLimiterOptions, OnRejectedContext, RateLimitLease, AddRateLimiter, UseRateLimiter) remain permitted. Each symbol was verified to match its forbidden construction form with zero false positives on the seam before being added.

- docs/rate-limiting.md: the per-concern rate-limiting contract. The file is created in this phase; its docs/README.md index row remains Planned | 4.3 and flips to Shipped | 0.5.0 at Phase 4.3.

- Test files: RateLimitReasonCodesTests, ApiPilotRateLimitOptionsTests, ApiPilotRateLimitOptionsValidatorTests, ApiPilotRateLimitExtensionsTests, ApiPilotBuilderRateLimitTests, RateLimitDiagnosticsHookTests, RateLimitIntegrationTests (ApiPilot.AspNetCore.Tests). The integration test runs a real loopback pipeline through InProcessHost and asserts a 429 with the RATE_LIMITED envelope and a non-empty request id.

- Test totals at the Phase 4.1 close: 910 .NET tests across 111 test classes (211 Core across 22 classes, 359 AspNetCore across 48 classes, 340 Security across 41 classes). 60 JavaScript tests across 5 files. Audit PASS (Sections 1-9).

### Findings from the build (Phase 4.1)

- A-220 (process, self-caught): the Phase 4.1 design initially proposed deferring the Option D override surface to a later phase. That would have violated the standing discipline (an override is implemented and tested in the phase that introduces the transformation). Corrected to complete the full surface in-phase. The finding is a process correction, not a product defect.

- A-221 (drift prevention): the two response-envelope correlation concerns existed in ApiPilotExceptionMiddleware.ResolveRequestId. The rate-limit hook needed the same logic. To prevent the A-040/A-118 defect class (the override declared but not consulted; the same rule paid for twice), the logic was extracted to a shared internal seam, CorrelationEnvelope.ResolveRequestId, and the exception middleware was refactored to call it. The 303 AspNetCore tests passed before and after the refactor, proving the behavior preserved.

- A-222 (platform fact): the .NET rate-limiting surface spans two namespaces. RateLimiterOptions and OnRejectedContext are in Microsoft.AspNetCore.RateLimiting; RateLimitLease and MetadataName are in System.Threading.RateLimiting. A component that integrates with the limiter references both. Recorded so the next developer does not assume a single namespace.

- A-223 (process): a PowerShell reflection probe against the shared-framework assembly could not reliably resolve the platform member signatures. The compiler is the authority for platform member signatures; a probe is not. The OnRejected delegate and the metadata types were confirmed by compiling the code, not by reflection.

- A-224 (process, dependency order): the rate-limit hook was written before the ApiPilotLogEvents.RateLimitRejected constant it references. The compiler named the missing constant (CS0117). The catalogue edit was performed out of sequence to satisfy the dependency, then the build went green. Lesson: a consumer and its constant are one unit; write the producer first or in the same step.

- A-225 (process, anchor from a mental model): the first event-catalogue insertion attempt anchored on the assumption that the 5001 constant immediately followed the 3002 constant. The file has the 5001 constant XML doc comment between them, so the anchor failed and the guard prevented a corrupt write. The anchor was re-derived from a fresh read of the exact bytes. Lesson: an insertion anchor is written from the target region exact bytes, including doc-comment lines, never from the constants visible sequence.

- A-226 (process, check pattern too strict): a verification check expected the explicit form new Meter(...); the file used the target-typed form new(...). The content was correct; the check pattern was too strict. Lesson: a check asserts the property, not a particular syntactic spelling.

- A-227 (process, corrective not folded into the write): the missing using ApiPilot.AspNetCore.RateLimiting was identified in the same message that described the ApiPilotBuilder write, but the write block ran against the original using list. The compiler reported CS0246. The using was added in a follow-up corrective. Lesson: a corrective identified before a write must be folded into that write; do not send a write whose own accompanying message names a mandatory preceding correction.

- A-228 (real defect in test code): ApiPilotRateLimitOptionsValidatorTests called result.Failures.Any(...), but ValidateOptionsResult.Failures is IEnumerable<string>? (nullable). The nullable analyzer rejected it (CS8604). Fixed by capturing the value into a local and null-guarding with an explicit throw. Lesson: a framework property can be nullable even when the semantic path implies non-null; the correct response is an explicit null check, not a null-forgiving operator.

- A-229 (real defect in test code): the fail-closed tests for the rate-limit options resolved IOptions<T>.Value after registering invalid options, expecting to then invoke the validator manually. The options pipeline validated on Create and threw OptionsValidationException. The source is correct; the test was wrong about when validation fires. Corrected to assert the exception at the point of resolution. Lesson: a test of a fail-closed options registration asserts the exception at resolution, not a manual validator call.

- A-230 (process): ApiPilotLogEventsTests hardcoded the catalogue size (17) and was not updated when RateLimitRejected = 4001 was added. The tests correctly failed and were updated to 18. Lesson: adding a catalogued constant requires a tree-wide check for tests that pin the catalogue size.

- A-231 (platform fact): MetadataName is an abstract sealed type with no public constructors. The retry metadata is a static instance whose string name routes through the lease TryGetMetadata(string, out object?) method. A test lease controls the return at that method. Recorded so the next developer does not re-probe.

- A-232 (platform fact): OnRejectedContext is directly constructible (a public parameterless constructor with public HttpContext and Lease setters). RateLimitLease is abstract with a protected parameterless constructor and requires a derived test double. Recorded for the test harness.

- A-233 (real defect in test code): the TestRateLimitLease test double declared protected override TryGetMetadata; the platform member is public (CS0507). Corrected to public override. Lesson: an override access modifier must match the base member; confirm each abstract or virtual member exact access level when deriving from a platform type.

- A-234 (real defect in test code): RateLimitDiagnosticsHookTests omitted using ApiPilot.AspNetCore.Serialization, so AddApiPilotJson did not resolve (CS1061). Corrected. Lesson: a test helper that registers the JSON options imports the namespace that declares the extension.

- A-235 (real product defect): the RateLimitDiagnosticsHook set httpContext.Response.StatusCode = options.StatusCode and then called ErrorResponseResult.ExecuteAsync, which unconditionally overwrote the status from its built-in RATE_LIMITED mapping (429). The Option D StatusCode override was dead code. This is the override-declared-but-not-consulted defect class (A-098/A-108/A-109/A-118). The test HandleAsync_StatusOverride_IsWritten caught it. Fixed by building a per-response clone of ApiExceptionOptions whose ErrorCodeToStatusMap overlays the RATE_LIMITED entry with the configured StatusCode; the registered options instance is never mutated. The failing test was preserved unchanged (it correctly expected 503) and now passes. This is the only product defect recorded this phase.

- A-236 (process): the expected test count in the diagnostics append was estimated at 20; the file contained 21. The content was correct; the expectation was wrong. Lesson reinforced: the expected test count is derived by counting the markers in the write content, never estimated.

- A-237 (real defect in test code): the structured-log test used FakeLogger<RateLimitDiagnosticsHook>, but the hook is a static class and a static type cannot be a generic argument (CS0718). Corrected to type the fake to the test class. Lesson: a FakeLogger<T> requires a non-static T; type it to the test class when the component under test is static.

- A-238 (real defect in test code): the integration test omitted using ApiPilot.AspNetCore.Serialization (CS1061 for AddApiPilotJson) and used unqualified Results.Ok(...), which bound to the test project own Results namespace instead of Microsoft.AspNetCore.Http.Results (CS0234). Both corrected. Lesson: a test project folder named Results creates a namespace that shadows the framework Results class for unqualified use; fully qualify, and import the serialization extension.

### Follow-up and scheduled (Phase 4.1)

- Scheduled release-status update: the docs/README.md rate-limiting.md index row remains Planned | 4.3 and changes to Shipped | 0.5.0 at Phase 4.3. The document itself was created in Phase 4.1.

- Scheduled architectural backfill: the ApiPilot meter name is shared by more than one Meter instance (the CSRF counters meter and the rate-limit hook meter are separate instances with the same public name ApiPilot). A listener keyed on the name may observe both. This is a known state, not a defect. The consolidation (shared-meter injection, a central meter factory, or another design) is to be evaluated in a numbered backfill phase; it is not scheduled to a version in this changelog entry.


### Added (Phase 4.0)

- Safe observability baseline. Six new source files and six new test files, all using the .NET platform observability APIs (System.Diagnostics.Metrics, System.Diagnostics.ActivitySource) with no third-party telemetry SDK.

- dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotLogEvents.cs: the event-ID catalogue. Names the existing component-scoped event IDs (1001-1007 exception middleware, 2001-2002 correlation, 3001-3002 content negotiation, 5001 CSRF, 6001-6002 CSRF transition, 7001 Origin, 8001 Fetch Metadata, 9100 security diagnostics) for documentation and programmatic reference. It does not renumber shipped IDs; the IDs are a component-scoped log-consumer contract.

- dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotRedactionPolicy.cs: the named-field redaction set (Authorization, Cookie, Set-Cookie, X-CSRF-TOKEN, X-XSRF-TOKEN, Proxy-Authorization, WWW-Authenticate) and the two redaction methods. Case-insensitive comparison, null-safe. The scope is named-field values only; opaque string content is not redacted.

- dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotSafeLogger.cs: an ILogger decorator that redacts the values of structured state entries whose keys match the redaction policy. It preserves the original message template ({OriginalFormat}), the non-redacted structured entries, and the exception instance. It uses a dedicated internal redacted state type and a matching formatter. The pass-through path applies when no redacted key is present (zero modification). The formatter is a documented contract of the decorator; it is not a claim of strict LoggerMessage equivalence.

- dotnet/src/ApiPilot.AspNetCore/Diagnostics/ApiPilotActivitySource.cs: the process-wide ActivitySource named "ApiPilot". A static class with no disposal. The Start methods set only http.method and apipilot.error.code; they never set the request path, query string, or exception type name, because those values can carry sensitive content.

- dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotCsrfCounters.cs: the CSRF counters and histograms. Meter named "ApiPilot". Instruments: apipilot.csrf.issued, apipilot.csrf.validated.ok, apipilot.csrf.validated.failed (reason tag), apipilot.csrf.rotated, apipilot.csrf.validation.duration (ms). Rejects a null, empty, or whitespace reason. IDisposable, disposing the meter.

- dotnet/src/ApiPilot.Security/Diagnostics/ApiPilotCsrfObservabilityExtensions.cs: the opt-in DI extension AddApiPilotCsrfObservability, registering ApiPilotCsrfCounters as a singleton via TryAddSingleton. Idempotent; an application registration is preserved.

- Test files: ApiPilotLogEventsTests, ApiPilotRedactionPolicyTests, ApiPilotSafeLoggerTests, ApiPilotActivitySourceTests (ApiPilot.AspNetCore.Tests); ApiPilotCsrfCountersTests, ApiPilotCsrfObservabilityExtensionsTests (ApiPilot.Security.Tests).

- audit.ps1 (changed): the $forbiddenSymbols list is extended from 18 to 26 entries with eight external telemetry SDK identifiers (OpenTelemetry, OpenTelemetry.Extensions.Hosting, ApplicationInsights, Datadog, NewRelic, Sentry, AppMetrics, Prometheus). Boundary hardening, not relaxation. The identifiers appear only in the audit script; no source file references them.

- No new package or framework reference. The observability APIs are provided by the existing Microsoft.AspNetCore.App framework reference. No docs file is written at Phase 4.0; docs/observability.md and its docs/README.md row are deferred to Phase 4.3.

- Test totals at the Phase 4.0 close: 854 .NET tests across 104 test classes (211 Core across 22 classes, 303 AspNetCore across 41 classes, 340 Security across 41 classes). 60 JavaScript tests across 5 files (carried from the Phase 3.2 close; not re-run this phase). Version remains 0.4.0; the bump to 0.5.0 occurs at Phase 4.3.

### Findings from the build (Phase 4.0)

- A-208 (design discovery): ApiPilotLogEvents is a catalogue, not an owner. The read of ApiPilotExceptionMiddleware.LogMessages.cs revealed IDs 1001-1007 are already in use by the exception middleware, and the CHANGELOG named the full component-scoped numbering. The event IDs are a shipped contract for log consumers; renumbering them would break application log filters. The design is corrected: the class catalogues the existing IDs; the owning components keep their IDs. Lesson: a design that proposes to own a numeric range must first read the range current occupants.

- A-209 (boundary hardening): the audit forbidden-symbols list did not include any external telemetry SDK identifier. The observability concern is new at Phase 4.0; the boundary must be extended to cover it. Eight identifiers added (listed above). Lesson: a boundary enforced by an allowlist must be reviewed whenever a new concern touches the boundary.

- A-210 (real defect in the design): the ApiPilotSafeLogger design proposed calling the inner Log with a redacted state object of a different type than the formatter TState. The call does not compile. Fixed by introducing a dedicated redacted state type and a matching formatter. Lesson: a decorator that transforms the state cannot reuse the original formatter; the decorator owns both the redacted state and its formatter. The test asserts the redacted value is absent from the state the inner logger receives, not merely from the formatted output.

- A-211 (documentation correction): the initial design implied the redaction discipline covered all secrets. That claim is false: a redaction decorator works on named fields; exception messages, scope state, and values under generic field names are outside its reach. Documented prominently on the type and in the file header. Lesson: a redaction claim broader than its implementation is worse than no claim, because it produces false confidence.

- A-212 (design correction): the initial ApiPilotActivitySource design combined a static source with an IDisposable wrapper, leaving the lifetime model unclear. Corrected to a pure static class with a process-wide source and no disposal. The correction also removed http.path and the exception type name from the activity tags, because both can carry sensitive content. Lesson: a process-wide instrumentation resource has process-wide lifetime; and when adding activity tags, ask whether the value could carry a secret.

- A-213 (boundary and lifetime discipline): the additional Phase 4.0 checklist requirements (event-ID pairwise distinctness within the current catalogue, MeterListener cleanup between tests, ValidationFailed input handling, extension idempotence, names-as-contract tests, audit-only identifiers, no duplicate registrations) are captured as permanent contract items. Lesson: a new observability surface has three families of contract - the numeric IDs, the string names, and the DI registration shape - and each family has a test that locks it.

- A-214 (invariant precision): the original checklist claimed the event-ID tests would assert global uniqueness. That claim is imprecise: the scheme is component-scoped and future components may legally allocate new IDs. The corrected test asserts pairwise distinctness across the complete current catalogue and does not claim closure. Lesson: an invariant must state exactly what it asserts and exactly what it does not.

- A-215 (formatter semantics): the initial ApiPilotSafeLogger design proposed reconstructing the message from key/value pairs joined by commas, which is not LoggerMessage-equivalent and would change the message shape for consumers. The corrected design preserves the {OriginalFormat} entry, the non-redacted structured state, and the exception; the redacted values are substituted into the original template. Four tests assert the contract. Lesson: a decorator that transforms the state of a framework service must either preserve the framework own semantics exactly or explicitly document what it does not preserve.

- A-216 (process, demonstrated by Block 4 output): the Block 4 verification check "No IDisposable" returned False because the file XML doc comment contains the phrase <see cref="IDisposable"/> in a sentence that explains what the class is not. The content is correct; the check pattern was too strict. Lesson: a "does not contain X" check must exclude comments if the property it tests is about code structure, not doc text.

- A-217 (process, demonstrated by Block 11 output): the Block 11 verification expected 12 [Test] attributes but the file contains 11. The helper BuildListener was included in the estimate but does not carry [Test]. The content is correct; the expectation was miscounted. Lesson: the expected test count in a verification block is derived by counting the [Test] markers in the write content, not by an estimate.

- A-218 (real defect, demonstrated by the build): the build failed with 18 CS1591 errors in the two new Security test files. Root cause: ApiPilot.AspNetCore.Tests.csproj sets GenerateDocumentationFile=false; ApiPilot.Security.Tests.csproj does not, so it inherits true from Directory.Build.props. The two test projects have different documentation contracts. Fixed by adding XML doc to the two Security test files. Lesson: when writing new test files, check the target test project GenerateDocumentationFile setting first.

- A-219 (real defect in the test, demonstrated by the Security test run): the ApiPilotCsrfCountersTests tests TokenIssued_IncrementsIssuedCounter and ValidationSucceeded_IncrementsOkCounter asserted that the listener would receive a single aggregated measurement with the total value. In fact, MeterListener receives one measurement per instrument call. Three TokenIssued() calls produce three listener callbacks, not one callback with value 3. The source is correct. Fixed by summing the measurements. Lesson: a MeterListener sees individual measurements, not aggregated totals.

## [0.4.0] - 2026-09-27

### Added (Phase 3.4)

- The v0.4.0 milestone. The repository VERSION is bumped from 0.3.0 to 0.4.0. All three .NET packages (ApiPilot.Core, ApiPilot.AspNetCore, ApiPilot.Security) repack at 0.4.0 via Directory.Build.props. The client package (@apipilot/client) ships at 0.4.0. Both language roots converge on one repository version.

- This is a coordinated release-alignment decision, not a claim that major .NET functionality changed. Phase 3 added no .NET behavior. The .NET packages carry the repository version because VERSION is the repository's single version source. The client is already at 0.4.0. The two language roots now ship at the same version.

- The existing 0.3.0 artifacts in artifacts/ are preserved as historical build outputs. The 0.4.0 packages are produced alongside them.

- HANDOVER.md (extended): the Phase 1.6 through 3.4 append. The document grows from 425 lines to 735 lines. The append covers the security-specific disciplines (the three cross-cutting envelope concerns, fail-closed startup, secrets-never-in-logs, the CSRF origin rule, the cookie prefix rules, rotation honesty, the pipeline order), the client-specific disciplines (the namespace pattern, the public surface whitelist, C1 through C7, the minimal response contract, the response spread with client precedence, the onCsrfExpired containment, the documented bootstrap shape, pathToFileURL, node --check, ReadAllBytes/WriteAllBytes, the examples boundary), the expanded defect-class memory (sideEffects, symbol-in-prose, case-insensitive scans, wholesale namespace leak, the array-literal apostrophe, the missing closing brace, the double-quote in a double-quoted string, plan-value-not-file-value, the vacuous pass), the version contract update with the convergence decision, the PowerShell trap additions, the extended findings registry A-133 through A-204, and the extended reference table.

- VERSION (changed): 0.3.0 -> 0.4.0. The .NET packages and the client now ship at the same version.

- dotnet pack (run): the three .NET packages at 0.4.0 are produced in artifacts/. The 0.3.0 packages remain.

- Test count: 803 .NET tests (211 Core + 268 AspNetCore + 324 Security) and 60 JavaScript tests (3 skeleton + 8 errors + 14 csrf + 29 client + 6 declarations). Both counts are unchanged by the version bump.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 324 (unchanged).
  - JavaScript tests: 60 (unchanged).

### Findings from the build (Phase 3.4)

- **A-204** (Phase 3.4, process): The Phase 3.4 verification pack, .NET build, and JavaScript tests were run before the six Phase 3.4 writes. Nothing was broken: the incremental pack did not overwrite the 0.3.0 artifacts (they remain with their Phase 3.2 timestamps), and the JavaScript tests passed against unchanged code. But the order of operations was wrong. The verification runs after the writes. **Lesson:** the pre-write checklist's write-order is the order of execution. Do not run the final verification block until every write block has completed. The verification is a check on the post-write state, not the pre-write state. Add to the phase-closing ritual: the four-gate verification block is the last block of the phase, after all writes and after any build steps whose outputs the verification consumes.

### Added (Phase 3.3)

- Client documentation and framework examples. The client is now documented by a per-concern contract file and demonstrated by seven framework integration examples. No client runtime behavior changed. The 803 .NET tests and the 60 JavaScript tests are preserved.

  - `docs/fetch-helper.md` (new): the per-concern contract for the client. Covers the client lifecycle, the request pipeline, the response classification tree, the four failure categories, the response object and the client-precedence rule, the CSRF bootstrap contract, the onCsrfExpired callback, the minimal response contract, the CSP-safe guarantee, the supported runtimes, the Option D surface, and the compatibility guarantees.
  - `docs/README.md` (extended): the `fetch-helper.md` row moves from Planned | 3.3 to Shipped | 0.4.0. No other row changes.
  - `javascript/ApiPilot.Client/examples/vanilla-js/` (new): the plain browser example. Two files: `index.html` (markup) and `app.js` (the module that imports the built client). No package manager, no build step.
  - `javascript/ApiPilot.Client/examples/react/` (new): the React 19 example. A `useApiPilot(baseUrl)` hook that memoizes the client on `baseUrl`. Runnable with Vite 8.
  - `javascript/ApiPilot.Client/examples/nextjs/` (new): the Next.js 16 App Router example. A Server Component that renders a Client Component. The `"use client"` directive marks the boundary. Runnable with Next.js 16.
  - `javascript/ApiPilot.Client/examples/vue/` (new): the Vue 3 example. A `useApiPilot(baseUrl)` composable with a browser-safe factory (`ensure()`). Under SSR, `ensure()` returns null. Runnable with Vite 8.
  - `javascript/ApiPilot.Client/examples/svelte/` (new): the Svelte 5 example. A `createApiPilotStore(baseUrl)` readable store. The client is non-reactive; reactive request state belongs to the consumer. Runnable with Vite 8.
  - `javascript/ApiPilot.Client/examples/angular/` (new): the Angular 22 source-level integration illustration. The service file uses the standalone `@Injectable({ providedIn: "root" })` pattern. The Angular project scaffolding is the reader's responsibility.
  - `javascript/ApiPilot.Client/examples/blazor-interop/` (new): the Blazor WASM source-level integration illustration. A JS module that Blazor loads via `IJSRuntime`. Documents that interop is asynchronous and the module must be loaded before invocation.
  - `javascript/ApiPilot.Client/examples/README.md` (new): the index of the seven examples, with a table naming each framework and its pattern.

- `audit.ps1` (extended): a new Section 9, Examples boundary scan. It enforces five rules: (9a) every example `package.json` is valid JSON; (9b) no example `package.json` declares `@apipilot/client` in dependencies or devDependencies; (9c) every example directory with a `package.json` has a local `.gitignore` containing `node_modules/`; (9d) no example contains a `.tgz` and no example `package.json` ships `dist/` or `*.tgz` in its files array; (9e) every example relative import of the client resolves to the real `dist/apipilot-client.esm.js`. Section 9 runs on every audit from Phase 3.3 onward. See A-201 for the real defect Section 9 caught on its first run.

- The examples do not belong to the published client package. They are not in the client's `files` array and not in the tarball. They do not depend on `@apipilot/client`. Every example imports the client from a relative path to the built artifact. This is the boundary that keeps the client framework-agnostic.

- Test count: 803 .NET tests unchanged. 60 JavaScript tests unchanged. Phase 3.3 adds no tests to the client package or the .NET side. The examples verification is mechanical (Section 9 of the audit) rather than test-based.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 324 (unchanged).
  - JavaScript tests: 60 (3 skeleton + 8 errors + 14 csrf + 29 client + 6 declarations).

- The repository VERSION stays at 0.3.0. The client package carries its own version 0.4.0. The repository VERSION bumps at Phase 3.4 (the v0.4.0 milestone).

### Findings from the build (Phase 3.3)

- **A-196** (Phase 3.3, real defect): The client README line 135 read an import whose specifier was wrapped in redundant single quotes inside the double-quoted string. The defect was introduced by a PowerShell-layer quote doubling in Phase 3.2 and was not caught by the Phase 3.2 verification. Fixed in Phase 3.3 by rewriting the line with a clean specifier. **Lesson:** a PowerShell array literal that writes a JavaScript code block in a Markdown file must be checked for shell-quote artifacts. Spot-check every import line in a written Markdown file. Same class as A-186 and A-198.
- **A-197** (Phase 3.3, real defect in a write): The first write of `examples/vanilla-js/index.html` inlined the JavaScript in a script tag. The JavaScript was not checked by node --check (it was embedded in HTML). Fixed by moving the JavaScript to `app.js` and having `index.html` import it with a src attribute. **Lesson:** an example with a build-free structure still separates markup from code. A script tag with a src attribute is the plain-browser equivalent of an ESM entry. The check for the module syntax runs on the separate `.js` file.
- **A-198** (Phase 3.3, process): The Block 42 verification used a regex that did not match the file's actual byte form. The file was correct; the check was wrong. **Lesson:** when checking for the presence of a JavaScript string literal, the check pattern must match the file's actual form. If the file uses single quotes, the check pattern uses single quotes. The PowerShell-layer escape is not the file. Add to the phase pre-write checklist: any check on content written into a source file must include a spot-read of the actual written file.
- **A-199** (Phase 3.3, real defect in the write script): The Block 58 audit.ps1 Section 9 script was missing its final closing brace. The script's outer if block was not closed. PowerShell refused to execute the block. Fixed by appending the missing closing brace. **Lesson:** a script's brace balance must be counted before the script is sent. For a script with nested control flow, count opening braces and closing braces and confirm they match. Same class as A-129 and A-146.
- **A-200** (Phase 3.3, real defect): The Section 9 regex pattern in audit.ps1 used a double-quoted PowerShell string containing an unescaped double-quote character. The double quote terminated the string early. The audit failed at parse time; the file on disk was broken. Fixed by rewriting the regex pattern as a single-quoted PowerShell string using character escapes for the quote characters. **Lesson:** in a PowerShell double-quoted string, every literal double quote must be escaped. A regex pattern that contains either quote character should always be written as a PowerShell single-quoted string using regex character escapes to avoid the quoting entirely. Same class as A-044, A-129, A-136, A-146, A-193, A-194, A-195.
- **A-201** (Phase 3.3, real defect): The file `examples/blazor-interop/wwwroot/apiPilotInterop.js` imported the client from four levels up. The file's actual depth is three levels up. The import did not resolve to the real client artifact. Caught by the Section 9 boundary scan on its first run. Fixed by correcting the path to three levels. **Lesson:** the pre-write checklist published a path for this file, but the correct depth is three, not four. A published import-path table is a plan, not a verification. Only a resolution check against the actual file on disk proves the path is right. Same class as A-186 and A-198.
- **A-202** (Phase 3.3, process - label imprecision in the audit output): The Section 9 counter line reports the number of imports found, not the number successfully resolved. The failure is reported separately and correctly. The label should be "Found" not "Resolved". **Lesson:** an audit output label must describe the fact the counter holds. A counter incremented before a check is a "found" counter, not a "passed" counter. Not a defect in the check itself; the check catches the failure correctly.
- **A-203** (Phase 3.3, critical process defect): The Block 60 CHANGELOG entry script contained array-literal strings with unescaped apostrophes in the prose. PowerShell parsed the array literal until the first apostrophe, then treated the rest of the line as a PowerShell expression. The array literal failed to parse; the block did not execute as a unit. Fragments of the block ran individually. Variables from prior blocks were still in the session scope. No file was modified by the fragments because the write line was inside the unexecuted else block. **Lesson:** a PowerShell array literal that contains prose with apostrophes must double every apostrophe before the block is sent. Count the apostrophes in the content. Additionally: before running any block that uses a variable that was set in a prior block, re-set the variable explicitly at the top of the block. The terminal session is not isolated between blocks. Same class as A-012, A-101, A-104, A-193, A-194, A-195.

### Added (Phase 3.2)

- Client package hardening. The client package is now installable from a tarball and consumable through its exports map. No runtime behavior changed. The 803 .NET tests and the 54 JavaScript tests are preserved; the JavaScript count grows to 60 with the new parity check.

  - `package.json` (extended): an `engines` block declares `"node": ">=18.0.0"`. Node 18 is the first release that provides global `fetch` and the built-in `node:test` runner that this package uses for its own tests. See A-184 and A-185 for the phase decisions.
  - `package.json` (unchanged otherwise): the four consumption forms (ESM, IIFE, TypeScript declarations, npm) declared by `main`, `module`, `browser`, `types`, and `exports`. The `files` array is `[dist/, src/, README.md, LICENSE]`. No runtime or development dependencies.
  - `.gitignore` (extended): the client `.gitignore` gains `*.tgz` so `npm pack` artifacts are not committed.
  - `tests/declarations.test.js` (new, 6 tests): a declaration/runtime parity check. It extracts declared symbol names from `src/types.d.ts` by regex and asserts they match the runtime exports of the built ESM artifact. This is a NAME-LEVEL check. It does not compile TypeScript and does not prove the declarations are valid TypeScript. True compiler validation is deferred to Phase 5.1. The test file uses the term "declaration/runtime parity" and does not claim "validation". See A-185.
  - `README.md` (extended): three new sections. "Supported runtimes" states the browser floor as the four required APIs (`fetch`, `URL`, `globalThis`, `AbortController`) with the API-derived browser versions (Chrome 71+, Firefox 65+, Safari 12.1+, Edge 79+) and the Node floor (`>=18.0.0`). No year-based browser claim is made. "Content Security Policy" states the CSP-safe guarantee and lists the seven forbidden patterns that were grepped. The "How to build" section gains a note that Phase 3.2 finalized Path A: no minification, no bundling, no tree-shaking.
  - `src/iife-entry.js` (corrected): the public IIFE global is now published from an explicit whitelist of the eight intended symbols. Previously the entry assigned `globalThis.__ApiPilotClient` wholesale to `globalThis.ApiPilot`, which leaked the internal `createCsrfStore` symbol onto the public global. The ESM entry whitelists eight symbols; the IIFE entry now whitelists the same eight. The two consumption forms agree. See A-189.

- Verification performed in Phase 3.2 (four checks):
  - CSP static scan across `src/` and `dist/` for the seven forbidden patterns (`eval(`, `new Function(`, `Function(`, `document.write(`, `.innerHTML =`, string-argument `setTimeout`, string-argument `setInterval`). Zero hits. The scan uses a case-sensitive match (`-cmatch` in PowerShell) so it does not false-positive on the lowercase `function` keyword. See A-188.
  - `npm pack --dry-run`: 13 files. Contents: `dist/apipilot-client.esm.js` (29.7 kB), `dist/apipilot-client.iife.js` (29.5 kB), `dist/apipilot-client.d.ts` (9.3 kB), all seven `src/` files, `README.md` (10.4 kB), `LICENSE` (1.1 kB), `package.json` (980 B). No `tests/`, no `scripts/`, no `.gitignore`, no `.tgz`. Package size 17.0 kB compressed, 120.3 kB unpacked.
  - Tarball install smoke test: `npm pack` produces `apipilot-client-0.4.0.tgz`; the tarball installs cleanly in a scratch project; eight content assertions pass; the ESM module resolves through the `exports` map and exposes exactly the eight intended symbols; the IIFE global exposes the same eight; no tests or scripts ship; no runtime dependencies; version consistency confirmed. The scratch directory and tarball are cleaned up. See A-189 (the smoke test detected a real defect on its first run).
  - Four-gate verification: `.NET build` exit 0; Core 211/211, AspNetCore 268/268, Security 324/324; JS build exit 0 with three artifacts; JS tests 60/60; audit exit 0.

- Test count: 803 at Phase 3.1 close (the .NET suites are unchanged). 60 JavaScript tests at Phase 3.2 close. Delta from Phase 3.1: +6 JavaScript (the new `tests/declarations.test.js` parity check).
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 324 (unchanged).
  - JavaScript tests: 60 (3 skeleton + 8 errors + 14 csrf + 29 client + 6 declarations).

- The repository VERSION stays at 0.3.0. The client package carries its own version 0.4.0 in package.json. The repository VERSION bumps at Phase 3.4 (the v0.4.0 milestone).

### Findings from the build (Phase 3.2)

- **A-184** (Phase 3.2, decision): The minification decision is Path A: no minification, no bundling, no tree-shaking. The unminified ESM output is 29.7 kB; the unminified IIFE output is 29.5 kB. Both are small. A production minifier (esbuild, terser) would be a third-party dev dependency, which conflicts with the zero-dependency policy. A consumer that wants a minified artifact can produce one with their own build pipeline. The decision is documented in the README "How to build" section. This is a decision, not a defect.
- **A-185** (Phase 3.2, decision): Compiler-level TypeScript validation is deferred to Phase 5.1 (contract tests). Phase 3.2 does not install `typescript`; the environment does not have `tsc` available and installing it would add a dev dependency. Instead, Phase 3.2 adds a declaration/runtime parity check: a name-level check that every symbol declared in `src/types.d.ts` is exported by the built ESM artifact, and vice versa. The check is honest about its scope: it does not compile TypeScript, it does not prove the declarations are valid TypeScript, and it does not prove the interface members are correctly typed. It catches the "declared but not exported" and "exported but not declared" defect classes only. The README and the test file use the term "declaration/runtime parity" and never use "validation". Add to Phase 5.1 pre-write checklist: install `typescript` as a test-only dev dependency with the audit allowlist, run a real `tsc` against a scratch consumer, publish the check as part of the contract test suite. This is a decision, not a defect.
- **A-186** (Phase 3.2, real defect): The declaration/runtime parity check used four backslashes inside the PowerShell array literal that wrote `tests/declarations.test.js`. PowerShell single-quoted strings pass backslashes literally, so the JavaScript file received four backslashes, which the JavaScript parser turned into two, which the regex interpreted as a literal backslash rather than a word-character escape. Every extractor returned an empty array; four of the six parity tests failed. Fixed by using two backslashes in the array literal, which produces the two backslashes in the JavaScript file that the parser turns into one backslash in the string value that the regex reads as the start of the word-character escape. **Lesson:** in a PowerShell single-quoted array literal that will be written verbatim to a JavaScript file, the backslash escape for a JavaScript regex is two backslashes -- exactly two. Four is wrong. Add to the phase pre-write checklist: for any regex that will be embedded in a written file, count the backslashes by working backward from the file content.
- **A-187** (Phase 3.2, process): Two of the six declaration/runtime parity tests passed over empty arrays without a non-empty guard. The vacuous-pass defect class. A test that iterates over an extractor output should assert the extractor produced a non-empty result. Fixed by adding a non-empty assertion to each extractor-dependent test before the loop. **Lesson:** an assertion loop over a collection that could be empty must assert a non-empty length first. This is the mechanical prevention of "the test passed because it did nothing".
- **A-188** (Phase 3.2, process): The CSP static scan used PowerShell -match operator, which is case-insensitive. The pattern for the capital-F constructor therefore matched the lowercase function keyword in the IIFE wrappers present in five of the six source fragments and propagated to both dist outputs. The scan reported seven hits, all false positives. Fixed by using -cmatch (case-sensitive) so the pattern matches only the capital-F constructor form. **Lesson:** a scan for a case-sensitive API must use a case-sensitive match. PowerShell -match is case-insensitive by default; -cmatch is the case-sensitive form. Same class as A-021 and A-183. The scan tool must be correct for the pattern it is looking for.
- **A-189** (Phase 3.2, real defect): The IIFE build published `createCsrfStore` on `globalThis.ApiPilot` alongside the eight intended symbols. The ESM module correctly exposed exactly eight symbols because `src/index.js` explicitly whitelists them. The IIFE entry assigned `globalThis.__ApiPilotClient` wholesale to `globalThis.ApiPilot`, so any symbol the fragments attached to the internal namespace became public. `createCsrfStore` is an internal implementation detail used only by `src/client.js`; it was never intended to be part of the public API. The two consumption forms disagreed on the surface: ESM had eight, IIFE had nine. Detected by the Phase 3.2 tarball install smoke test (Assertion 4). Fixed by changing `src/iife-entry.js` to publish a whitelisted subset of the internal namespace, matching the ESM entry export list exactly. **Lesson:** a wholesale namespace assignment to a public global exposes every internal symbol. The public global must be built from an explicit whitelist. The two consumption forms (ESM and IIFE) must share the same eight-symbol surface, and the smoke test asserts this. Add to the phase pre-write checklist: any public global assignment must be whitelisted; the smoke test must assert ESM and IIFE surface parity.
- **A-190** (Phase 3.2, process): A corrective block was pasted into PowerShell starting from the middle of the block, not the first line. PowerShell parsed the JavaScript content lines as PowerShell expressions, producing a parse error. No file was modified because the script first executable line never ran. **Lesson:** when pasting a long corrective block, verify the first line is the variable assignment. If the terminal shows leftover content, press Enter once to get a fresh prompt before pasting. The corrective content-aware guard makes a partial-paste or a mid-block paste safe.
- **A-191** (Phase 3.2, process): The Step 1 state-check regex matched the symbol name inside the A-189 comment on the whitelist in `src/iife-entry.js`. The symbol is not in the whitelist itself. The check was imprecise. **Lesson:** a check for "symbol X is not in a whitelist" must match the actual assignment form, not the bare symbol name, when a comment could legitimately reference the symbol. Same class as A-021 and A-188.
- **A-192** (Phase 3.2, process): The Step 1 state check expected nine ns references. The actual count is eight. The expectation was wrong: the ns prefix appears once per whitelisted symbol, not once for the variable declaration. The check own expectation was miscounted, not the file. **Lesson:** when writing an expected count into a check, count the actual pattern occurrences in the file being written, not an estimated figure. Same class as A-177.
- **A-193** (Phase 3.2, real defect in the write script): The Block 18 CHANGELOG corrective script used a backslash followed by a single quote inside PowerShell single-quoted strings in two places. PowerShell does not use backslash as an escape character. A single quote inside a single-quoted string must be doubled (two single quotes for one). The backslash-quote sequence terminated the string early, and PowerShell parsed the remainder as a new expression. The script failed at parse time; no file was modified. Fixed by replacing every backslash-quote sequence with doubled single quotes in the affected lines. **Lesson:** in a PowerShell single-quoted string, the ONLY escape is two single quotes for one. Backslash is not an escape. Anywhere an apostrophe appears inside a single-quoted PowerShell string, it must be doubled. Same class as A-044, A-129, A-136, A-146.
- **A-194** (Phase 3.2, process): A script containing the text of a CHANGELOG entry that itself contains apostrophes is a double-quoting problem. The CHANGELOG text uses apostrophes. The CHANGELOG text is embedded in PowerShell single-quoted strings. Every apostrophe in the content must be doubled in the script. **Lesson:** when a corrective writes text that contains apostrophes, enumerate every apostrophe in the text and double it. A pre-write grep for the apostrophe in the target content, followed by a manual verification that every occurrence is either doubled or moved to a double-quoted string, catches the entire class.
- **A-195** (Phase 3.2, process): The A-193 finding text described the defect using the literal two-character backslash-quote sequence. That literal sequence, embedded in the PowerShell single-quoted string that writes the CHANGELOG, reproduces the exact defect A-193 documents. The script fails to parse. **Lesson:** when documenting a defect about a specific escape sequence, do not use the escape sequence literally in the prose. Describe it in words. This is the same as A-021 and A-136. The rule is now explicit: any text describing an escape sequence must describe it in words, not reproduce it. Add to the phase pre-write checklist: a finding that documents a string-escape defect must not contain the escape sequence.

### Added (Phase 3.1)

- Fetch helper core and CSRF integration in `javascript/ApiPilot.Client/`. The client is now a functional framework-agnostic CSRF-aware fetch wrapper. Every transformation has a sensible default and a first-class override (the Option D surface C1 through C7). Every override has a test that uses it.

  - `src/csrf.js` (new): the CSRF token store. In-memory only, per-client. The token never leaves the closure. A generation counter guards against stale in-flight bootstrap results repopulating the store after refresh() or clear(). Concurrent get() calls coalesce into a single bootstrap fetch. The bootstrap response is classified by its body, not by response.ok: a transport failure is an HttpError, a non-JSON body is a ProtocolError, a body with success:false is an EnvelopeError (the mandatory correction), and the documented bootstrap shape `{ "token": "..." }` is a success. See A-180.
  - `src/client.js` (new): the `createApiPilotClient(config)` factory and the request pipeline. `get`, `post`, `put`, `patch`, `delete`, `request`, and the `csrf` store accessor. The pipeline resolves URLs via `new URL()`, detects protected methods (C4) at the API origin only, attaches the CSRF header (C5) on protected same-origin requests, serializes the body to JSON when appropriate, and preserves unknown envelope fields on the response with the eight client-controlled fields winning on any name collision. The `onCsrfExpired` callback (C6) is invoked only on CSRF_TOKEN_EXPIRED; its return value is ignored and a thrown exception is contained on `.cause`. The `fetch` implementation (C7) is the only transport.
  - `src/errors.js` (extended): the `ApiPilotHttpError`, `ApiPilotEnvelopeError`, and `ApiPilotProtocolError` constructors now accept a `.cause`. The `ApiPilotWireError` factory preserves unknown fields via spread on top of the three known fields (code, message, fields), with the normalized known fields winning on collision.
  - `src/index.js` (extended): one new runtime export -- `createApiPilotClient`. The ESM entry now exports exactly eight runtime symbols: `VERSION`, the six error symbols, and `createApiPilotClient`.
  - `src/types.d.ts` (extended): the four new public interfaces (`ApiPilotClientConfig`, `RequestOptions`, `ApiPilotResponse<T>`, `ApiPilotClient`) and the `createApiPilotClient` factory declaration. Both `declare global` blocks now carry `createApiPilotClient`. The Phase 3.0 "no premature 3.1 symbols" comment is retired and rewritten -- the phase it anticipated has arrived.
  - `scripts/manifest.json` (extended): the `sources` array grows from two to four entries -- `version.js`, `errors.js`, `csrf.js`, `client.js` -- in dependency order.

- Tests added in Phase 3.1 (51 new JavaScript tests across 4 test files):
  - `tests/errors.test.js` (new, 8 tests): the five error classes are constructors; the base class extends Error; each subclass sets its own name and is a subclass of the base; `ApiPilotWireError` normalizes non-object and typed-incorrect sources; `ApiPilotWireError` preserves unknown fields via spread; the wire error is a fresh object each call; `ApiPilotEnvelopeError` and `ApiPilotProtocolError` carry their extended fields.
  - `tests/csrf.test.js` (new, 14 tests): get fetches and caches; cached get does not re-fetch; concurrent get calls coalesce; refresh clears and re-fetches; clear empties the store; the generation guard discards in-flight results after refresh or clear; a non-2xx response with a valid `success:false` envelope throws `ApiPilotEnvelopeError` (the mandatory correction); a 200 without a token throws `ApiPilotProtocolError`; a non-JSON body throws `ApiPilotProtocolError`; the documented bootstrap shape `{ "token": "..." }` is accepted; a JSON body with neither a `success` nor a `token` throws `ApiPilotProtocolError`; a network failure throws `ApiPilotHttpError` with `.cause`; the token is never written to localStorage or sessionStorage.
  - `tests/client.test.js` (new, 29 tests): construction guards (C1, C7); the success path; the CSRF header is attached to protected same-origin requests (C4, C5); the header is not attached to cross-origin requests (the security-critical test); the header is not attached for methods not in protectedMethods (C4 override); the header name is configurable (C5 override); GET is never protected even when listed (safe-method invariant); custom credentials flow through (C3 override); custom fetch is used instead of the global (C7 override); non-string body serialized to JSON; caller Content-Type preserved; unknown envelope fields preserved (correction 1); client-controlled fields win over server fields with the same name (correction 1); a 401 with a valid `success:false` envelope throws `ApiPilotEnvelopeError`, not HttpError; a 500 with a valid envelope also throws `ApiPilotEnvelopeError`; non-JSON body throws `ApiPilotProtocolError`; JSON without a boolean success throws `ApiPilotProtocolError`; network failure throws `ApiPilotHttpError` with `.cause`; a response whose `text()` rejects throws `ApiPilotHttpError` with `.cause` (correction 4); a response whose `ok` and `status` disagree uses `ok` as returned (correction 4); `onCsrfExpired` fires on `CSRF_TOKEN_EXPIRED` (C6 override); does not fire on `CSRF_HEADER_MISSING` or any other code; does not fire on the success path; a throwing callback does not replace the `ApiPilotEnvelopeError` (correction 3); a throwing callback attaches `.cause` (correction 3); the public surface is exactly the eight runtime symbols.
  - `tests/skeleton.test.js` (updated, 3 tests): the seven-symbol test is renamed to eight and the surface array gains `createApiPilotClient`. The first two tests remain frozen. See A-179.

- Test count: 803 at Phase 3.0 close (the .NET suites are unchanged). 54 JavaScript tests at Phase 3.1 close (3 skeleton + 8 errors + 14 csrf + 29 client). The .NET count is 803; the JavaScript count is 54. They are counted separately because they are different test harnesses.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 324 (unchanged).
  - JavaScript tests: 54 (3 skeleton + 8 errors + 14 csrf + 29 client).

- The repository VERSION stays at 0.3.0. The client package carries its own version 0.4.0 in package.json. The repository VERSION bumps at Phase 3.4 (the v0.4.0 milestone).

### Findings from the build (Phase 3.1)

- **A-176** (Phase 3.1, process): The Block 3B append pre-flight check verified the last line of `src/client.js` was `credentials: credentials`. The actual last line is `        });`, the closing of the `createCsrfStore` construction. The guard refused to append, which was correct. The fix is to match the actual last line of the file produced by Block 3A. **Lesson:** a pre-flight check must verify the state the script will act on, derived from the actual write that preceded it, not from an assumed final line. Same class as A-175. Add to the phase pre-write checklist: any append block must verify the actual last line of the file it is appending to, not a line chosen from memory of the prior write.
- **A-177** (Phase 3.1, process): The Block 9B verification script asserted an expected test count of 36 for `tests/client.test.js`. The actual count is 29 (14 in the first half, 15 in the second). The count 36 was a loose estimate from the pre-write checklist, not a count of the `test(` declarations in the two array literals. The guard fired on the mismatch, which was correct -- the check exists to catch count drift. The fix is to update the assertion to 29, matching the file. **Lesson:** the expected test count in a verification script must be derived from the actual `test(` declarations in the source, not from an estimate in the checklist. The pre-write checklist estimated "~25 tests" for `client.test.js`; that estimate was the origin of the bad number. Same class as A-095 and A-152.
- **A-178** (Phase 3.1, process): The pre-write checklist for Phase 3.1 asserted "~25 tests" for `tests/client.test.js`. The actual count is 29. The checklist estimate is not a defect, but it did not converge with the actual count. **Lesson:** when a phase produces test files, the pre-write checklist estimated count is a planning figure; the post-write verification expected count must be derived from the write block. The two are not the same number.
- **A-179** (Phase 3.1): The Phase 3.0 skeleton test `tests/skeleton.test.js` includes a test asserting the ESM module exported symbols are exactly the seven Phase 3.0 symbols (`VERSION` plus the six error symbols). Phase 3.1 added an eighth export (`createApiPilotClient`), so the test failed by design -- the assertion was correct and the file was correct; the surface had legitimately grown. The Phase 3.1 pre-write checklist decision to "keep the skeleton test locked at seven symbols" was correct for the first two tests (they assert existence) but wrong for the third (it asserts exclusion). Fixed by updating the third test expected array to the eight Phase 3.1 symbols and updating its name to "eight documented symbols". The first two tests remain unchanged. **Lesson:** a test that asserts "the surface is exactly N symbols" is a phase-bound assertion. When a later phase legitimately grows the surface, the assertion must be updated, not preserved. Distinguish "asserts a symbol exists" (frozen) from "asserts no other symbol exists" (phase-bound). Add to the pre-write checklist: any test that asserts an exact set of exports must be revisited at every phase that adds or removes an export.
- **A-180** (Phase 3.1, real defect): The bootstrap classifier in `src/csrf.js` required the response body to have a boolean `success` field, based on the standard ApiPilot envelope from SPEC.md. But the CSRF bootstrap endpoint is documented in SPEC.md L126-L138 and docs/csrf.md as returning `{ "token": "<csrf-request-token>" }` -- a bare object, the only successful response in ApiPilot that does not use the standard envelope. The classifier therefore rejected the correct response. Every CSRF test failed because of this. Fixed by accepting the documented bootstrap shape as a success: the classifier accepts (1) a body with `success: false` as an `ApiPilotEnvelopeError`, (2) a body with `success: true` and a non-empty `token` string as a success, (3) a body with no `success` field but a non-empty `token` string as a success (the documented shape), and (4) any other body as an `ApiPilotProtocolError`. **Lesson:** the bootstrap endpoint response shape is a documented exception to the standard envelope. The classifier must accept `{ "token": "..." }` as a valid bootstrap response. The mandatory correction (classify by body, not by response.ok) still applies for the error case. Same class as A-168 (a scan/assertion that does not distinguish the documented exception from the standard contract).
- **A-181** (Phase 3.1, real defect): The test `a JSON response without a boolean success throws ApiPilotProtocolError` in `tests/csrf.test.js` was written under the assumption that the bootstrap endpoint follows the standard ApiPilot envelope (with a `success` discriminator). That assumption is wrong: SPEC.md L126-L138 documents the bootstrap response as a bare `{ "token": "..." }` object. When the classifier was corrected (A-180) to accept the documented shape, this test became a false positive -- it asserted that a valid response is rejected. The test is rewritten: the corrected test suite has two tests -- one asserts that the documented shape is accepted, and one asserts that a body with neither `success` nor a valid `token` throws `ApiPilotProtocolError`. **Lesson:** a test that asserts a body `{ "token": "x" }` is a protocol error contradicts SPEC.md. The correct test is: a bootstrap response with a non-empty `token` and no other recognizable field is a success. A response with neither a boolean `success` nor a non-empty string `token` is a protocol error. This finding is the test-side counterpart of A-180.


### Added (Phase 3.0)

- Browser client skeleton in a new JavaScript language root at `javascript/ApiPilot.Client/`. The client is framework-agnostic and has zero runtime dependencies. npm is one distribution channel among several, not an architectural dependency. The design supports plain browser JavaScript, CDN script tags, TypeScript projects, non-npm bundlers, import maps, Blazor JavaScript interop, and server-rendered applications.

  - `javascript/ApiPilot.Client/package.json` (new): the package manifest. `type: module`. `main`, `module`, and `browser` all point to the ESM output. `exports` declares the ESM entry, the IIFE entry, and the package.json. `dependencies` and `devDependencies` are both empty objects. The `sideEffects` field is intentionally omitted: the source files populate a shared namespace at load time, which is a side effect; claiming `sideEffects: false` would mislead bundlers. See A-167.
  - `javascript/ApiPilot.Client/src/version.js` (new): the version constant. Attaches `ns.VERSION` to the internal namespace.
  - `javascript/ApiPilot.Client/src/errors.js` (new): the error hierarchy. `ApiPilotClientError` (base, extends Error) with four subclasses (`ApiPilotConfigurationError`, `ApiPilotHttpError`, `ApiPilotEnvelopeError`, `ApiPilotProtocolError`), plus the `ApiPilotWireError` plain-object factory that normalizes the `{ code, message, fields }` shape from SPEC.md and preserves unknown fields.
  - `javascript/ApiPilot.Client/src/types.d.ts` (new): hand-authored TypeScript declarations. Declares only the seven symbols the skeleton actually exports (VERSION and the six error symbols). The declaration file carries the internal-namespace wording verbatim and declares both the internal `globalThis.__ApiPilotClient` namespace and the public `globalThis.ApiPilot` IIFE global.
  - `javascript/ApiPilot.Client/src/iife-entry.js` (new): the IIFE entry. Publishes the internal namespace as the public global `globalThis.ApiPilot`. The internal namespace remains an implementation detail.
  - `javascript/ApiPilot.Client/src/index.js` (new): the ESM entry. Declares the seven named exports of the ESM output, all read from `globalThis.__ApiPilotClient`. Written after the manifest and build script so the export list is established first.
  - `javascript/ApiPilot.Client/scripts/manifest.json` (new): the source concatenation order. The build script reads it to know which fragments to concatenate and where the entry files go. It is the single source of truth for the build order.
  - `javascript/ApiPilot.Client/scripts/build.js` (new): the build script. Plain Node.js, standard library only. Reads the manifest, concatenates the sources, appends the ESM entry to the ESM output and the IIFE entry to the IIFE output, copies the declarations file to dist/. No minification, no bundling, no parser, no regex over arbitrary JavaScript. Path A for Phase 3.0.
  - `javascript/ApiPilot.Client/tests/skeleton.test.js` (new): the skeleton test. Node.js built-in test runner (`node:test`, `node:assert/strict`). Three tests: (1) the built ESM entry imports and exports exactly the seven Phase 3.0 symbols; (2) the internal namespace `globalThis.__ApiPilotClient` is populated after the import; (3) the public surface is exactly the seven documented symbols (the mechanical proof of the boundary).
  - `javascript/ApiPilot.Client/README.md` (new): the package README. States that npm is one distribution channel, not an architectural dependency. Lists the four consumption forms (ESM, IIFE, TypeScript declarations, npm) with the first three explicitly marked "No package manager required." Carries the internal-namespace wording and the bundler-notes explanation verbatim.
  - `javascript/ApiPilot.Client/LICENSE` (new): the MIT license, byte-for-byte identical to the repository root LICENSE.
  - `javascript/ApiPilot.Client/.gitignore` (new): ignores `dist/`, `node_modules/`, and `*.log`.

- `audit.ps1` (extended): two new sections. Section 7 (Client dependency scan) reads the client package.json and asserts the dependency and devDependency blocks are empty; scans the client source for any import or require whose specifier is not a relative path or a `node:` built-in. Section 8 (Client source scan) applies the same forbidden-symbol list from Section 5c to the client source and ASCII-checks the client .js files and the client package.json. The script grows from 316 to 440 lines.

- The client is not published to npm in Phase 3.0. The build produces `dist/apipilot-client.esm.js`, `dist/apipilot-client.iife.js`, and `dist/apipilot-client.d.ts`. The repository VERSION stays at 0.3.0. The client package carries its own version 0.4.0 in package.json; the repository VERSION bumps at Phase 3.4 (the v0.4.0 milestone).

- The four consumption forms ship from one source: ESM (primary), IIFE (public global `globalThis.ApiPilot`), TypeScript declarations, and the npm package. The ESM and IIFE outputs are generated from the same fragments by the same build script. Behavioral divergence between the two is impossible because the build is a single concatenation.

- Test count: 803 at Phase 2.11 close. 803 at Phase 3.0 close. Delta: 0. The .NET suites are unchanged. The three JavaScript tests are new but counted separately from the .NET test count.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 324 (unchanged).
  - JavaScript skeleton tests: 3 (new).

### Findings from the build (Phase 3.0)

- **A-167** (Phase 3.0): The initial Phase 3 design proposed `"sideEffects": false` in the client package.json while the build strategy mutates `globalThis.__ApiPilotClient` during namespace initialization. Namespace mutation is a side effect. Advertising `sideEffects: false` would have told bundlers they could tree-shake or reorder module initialization, which would break the client. Fixed by omitting the field. The README gains a "Bundler notes" section stating the omission is deliberate. **Lesson:** the package.json `sideEffects` field is a contract with bundlers, not a decoration. Any module that mutates a global at load time has a side effect and must not claim otherwise.
- **A-168** (Phase 3.0): The first write of `javascript/ApiPilot.Client/src/types.d.ts` named three Phase 3.1 symbols (`createApiPilotClient`, `RequestOptions`, `ApiPilotResponse`) in a prose comment explaining that those symbols are not part of the 3.0 surface. Guard 3 (the "no premature 3.1 declarations" check) uses word-boundary matching and correctly flagged the comment text. Fixed by rewording the comment to describe the deferred symbols without naming them. The guard was not weakened. **Lesson:** a scan for symbol presence must include prose. A comment that names a symbol is on the same declaration surface as a `declare`. The correct response to a prose false positive is to rewrite the prose, not to weaken the scan. Same family as A-021.
- **A-169** (Phase 3.0, environment): The JavaScript runtime version was not verified before the first JavaScript toolchain invocation. The build ran under Node.js v24.19.0, discovered when the first `node` command executed. The .NET toolchain had been verified at Phase 3.0 start (`dotnet --version` = 10.0.302), but no equivalent check existed for the JavaScript runtime. The build script uses only stable APIs and is compatible with Node 18+, so no defect resulted. **Lesson:** the Phase 3.0 pre-write checklist must include a JavaScript runtime check (`node --version`) alongside the .NET toolchain check. Add to the environment verification section of every JavaScript phase. The minimum supported Node version should be stated explicitly and checked. A formal `engines` constraint in package.json arrives in Phase 3.2.
- **A-170** (Phase 3.0): The first write of `scripts/build.js` had a syntax error at line 55. The intent was to build a JavaScript string whose value contains a nested `'use strict';`. The line used single-quote delimiters on both the outer string and the embedded fragment. JavaScript terminated the outer string at the first inner single quote, producing `SyntaxError: Unexpected identifier 'use'`. The build script never executed on first attempt. Fixed by writing the fragment as a template literal (backticks), which safely contains single quotes and newlines. **Lesson:** when a JavaScript string literal must contain the same quote character it is delimited with, use a template literal (backtick) or an escape, not the same quote character twice.
- **A-171** (Phase 3.0, process): The Block 7 verification for `scripts/build.js` checked four structural properties of the file content (node built-in imports, no require, no regex manipulation, non-zero exit on failure). It did not execute the script. The script had a fatal syntax error that only surfaced when the script ran, a full block later. The write was declared verified on incomplete evidence. **Lesson:** a source file that is a standalone executable (a build script, a test runner, a CLI entry) must be executed or at minimum parsed with `node --check <file>` before the write is declared verified. Structural string guards are not substitutes for a parse. Add `node --check` to the verification of every new `.js` file. This is the JavaScript analogue of "always build fresh before running tests" (A-091/A-093/A-094).
- **A-172** (Phase 3.0): Two related defects on the JavaScript test path. (1) The first write of `tests/skeleton.test.js` used a Windows filesystem path (`join(clientRoot, 'dist', 'apipilot-client.esm.js')`) as the argument to dynamic `import()`. On Windows, that path is rejected by Node's ESM loader with `ERR_UNSUPPORTED_ESM_URL_SCHEME` because the drive letter is parsed as a URL scheme. Fixed by converting the path to a `file://` URL with `pathToFileURL` from `node:url` before passing it to `import()`. (2) The `test` script in `package.json` used `node --test ./tests/`, which Node 24 rejects (bare directory name). Fixed by using the TAP-reporter glob form `node --test --test-reporter=tap "tests/**/*.test.js"`, quoted so Node receives the literal glob and expands it internally. **Lessons:** (1) on Windows, any path passed to a Node API expecting a URL must go through `pathToFileURL`. (2) A `package.json` script that invokes a tool is a program, not a declaration; it must be executed at least once before the write is declared verified. (3) The `test` script value must be a direct `node --test ...` invocation that a developer can also run without any package manager, so the client's testing does not depend on npm.
- **A-173** (Phase 3.0): The write of `javascript/ApiPilot.Client/LICENSE` did not reproduce the root LICENSE byte-for-byte on first attempt. Root: 1077 bytes, LF-only line endings. Client copy: 1101 bytes, CRLF line endings, plus a trailing empty array element adding one blank line. The byte mismatch was 24 bytes. Guard fired; byte-level diagnostic identified both causes. Fixed by reading the root LICENSE with `ReadAllBytes` and writing the client LICENSE with `WriteAllBytes` -- a faithful byte-level copy that does not normalize line endings. **Lesson:** a byte-for-byte copy of an existing file cannot rely on `WriteAllLines` in Windows PowerShell 5.1, because the platform default newline differs from a source file that was written with LF-only. The correct pattern for a copy is `ReadAllBytes` then `WriteAllBytes`. The distinction is now explicit: authoring writes use `WriteAllLines`; copying writes use `ReadAllBytes`/`WriteAllBytes`.
- **A-174** (Phase 3.0): The root `README.md` contained stale phase-state prose and a stale project count that were not updated at any Phase 2 sub-phase close or at the Phase 2.11 milestone. (1) The prose paragraph claimed "Phases 0 through 1.9 are complete" and "Phase 2 (security core) is next" -- both wrong since Phase 2.0. (2) The repository layout block described the solution as "(four projects)" -- wrong since Phase 2.0, which added ApiPilot.Security and ApiPilot.Security.Tests, bringing the total to six. Both were corrected during the Phase 3.0 close, along with the addition of the new `javascript/ApiPilot.Client/` subtree to the layout and the extension of the test-count row to mention the three JavaScript skeleton tests. **Lesson:** the phase-close ritual for documentation updates must include ALL phase-state prose in the target file, not just the status table. A status table row and a prose paragraph can drift independently. Add to the phase-closing checklist: grep the target file for every phrase that describes phase state (for example "Phases 0 through", "is next", "is complete", the project count), and update each one or confirm it is still accurate. The Phase 2.11 close updated the table but not the paragraph, and the drift went unnoticed until the Phase 3.0 close.
- **A-175** (Phase 3.0, process): The corrective script that appends A-174 to `CHANGELOG.md` had a pre-flight check that assumed `### Added (Phase 2.10)` was the immediate successor of the A-173 bullet. The actual successor is the `## [0.3.0] - 2026-09-26` header (the boundary between the `[Unreleased]` section and the `[0.3.0]` version). The guard refused to write, which was correct. The insertion point for A-174 is between the A-173 bullet and the `## [0.3.0]` header. **Lesson:** a pre-flight check should verify the state the script will act on, not a state inferred from a different structure. When the pre-flight fails, re-read the surrounding context and re-derive the insertion point rather than guess. This finding is about the closing-sequence script, not the artifact; it is a process finding recorded for completeness.
## [0.3.0] - 2026-09-26

### Added (Phase 2.10)

- Security diagnostics and fail-closed startup validation in ApiPilot.Security.Diagnostics. The diagnostics surface reports non-fatal configuration warnings. The registration checker verifies that every expected security validator is resolvable from the container. A missing validator prevents the host from starting.

  - `Configuration/CsrfOptionsValidator.cs` (new): the missing startup validator for CsrfOptions. Validates HeaderName, BootstrapPath, MissingHeaderCode, ProtectedMethods, and the CodeMapping coverage of every non-Ok CsrfTokenParseResult member. The CodeMapping check is the important one: an unmapped failure reason would produce a rejection with an empty error code.
  - `Diagnostics/DiagnosticCodes.cs` (new): the internal structured log identifiers. SEC prefix marks fatal, SECW prefix marks warnings. The codes are never exposed on the wire and are not part of the error code table in SPEC.md.
  - `Diagnostics/ApiPilotSecurityDiagnostics.cs` (new): the public diagnostics surface. A DI service with no endpoint. Reports the current non-fatal diagnostics. The only warning in this phase is the in-memory Data Protection key ring (SECW001). The surface never contains token values, bindings, key paths, cookie values, or any other secret material.
  - `Diagnostics/SecurityConfigurationValidator.cs` (new): the registration checker. Takes IServiceProvider (the built container). Resolves IEnumerable<IValidateOptions<T>> for each of the five security options types and reports a missing validator as a Fatal diagnostic with code SEC002. The class does not call IValidateOptions<T>.Validate; validation remains the framework responsibility. This is not an orchestrator.
  - `Diagnostics/SecurityDiagnosticsHostedService.cs` (new): the IHostedService that runs at host startup. It throws when a Fatal diagnostic is present, preventing the host from starting. Non-fatal warnings are emitted through the logger via a LoggerMessage.Define delegate (EventId 9100). The service runs after ValidateOnStart, so it does not cause duplicate fatal failures.
  - `Diagnostics/ApiPilotSecurityDiagnosticsExtensions.cs` (new): AddApiPilotSecurityDiagnostics. Registers the SecurityConfigurationValidator (transient), the ApiPilotSecurityDiagnostics (singleton), and the SecurityDiagnosticsHostedService (via TryAddEnumerable for IHostedService).

- The library does not expose a diagnostic endpoint. The spec 6.26 boundary is honored. The diagnostics surface is a DI service; applications read it programmatically or the hosted service emits it through the structured logger.

- The SecurityConfigurationValidator does not reimplement validation. It reports registration completeness only. Fatal validation remains the ValidateOnStart responsibility. This distinction was required by the reviewer and is enforced by the constructor signature change from IServiceCollection to IServiceProvider.

- Tests added in Phase 2.10 (17 new tests across 3 new test classes):
  - `Configuration/CsrfOptionsValidatorTests.cs` (8): defaults pass, empty HeaderName fails, empty BootstrapPath fails, BootstrapPath without a leading slash fails, empty MissingHeaderCode fails, empty ProtectedMethods fails, incomplete CodeMapping fails, null options throws.
  - `Diagnostics/ApiPilotSecurityDiagnosticsTests.cs` (6): in-memory key ring produces the InMemoryKeyRing warning, MultiInstance=true suppresses the warning, KeyStorageConfigured=true suppresses the warning, Emit calls the sink once per diagnostic, a fully-configured container produces no fatal diagnostics, a container with no validators produces five fatal diagnostics.
  - `Integration/SecurityDiagnosticsIntegrationTests.cs` (3): a fully-configured host starts successfully, a host with a missing validator refuses to start with the SEC002 code in the failure message, the in-memory key ring produces the SECW001 warning that the diagnostics surface reports.

- `Csrf/CsrfServiceExtensions.cs` (extended): registers the CsrfOptionsValidator via TryAddEnumerable in AddApiPilotCsrf.
- `tests/ApiPilot.Security.Tests/InProcessHost.cs` (extended): exposes a public IServiceProvider Services property so integration tests can resolve registered services after the host starts.

- Test count: 792 at Phase 2.9 close. 816 at Phase 2.10 close. Delta: +24.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 307 -> 324.

### Findings from the build (Phase 2.10)

- **A-162** (Phase 2.10): SecurityConfigurationValidator was written to take IServiceCollection but the test expected IServiceProvider. The IServiceCollection shape cannot resolve IEnumerable<IValidateOptions<T>> after the container is built. Fixed by rewriting the class to take IServiceProvider and resolve validators through GetServices<IValidateOptions<T>>. **Lesson:** a diagnostics or coordination class that runs after container build must take IServiceProvider, not IServiceCollection. The pre-write checklist must confirm the container-build lifecycle position of any new diagnostics service.
- **A-163** (Phase 2.10, process): The initial corrective rewrite of SecurityConfigurationValidator had an `if Test-Path { skip }` guard which skipped because the file existed. The fix never landed. This is the third instance of the same class of defect (A-148, A-158). **The rule is now absolute:** corrective writes to known-bad files must use no existence guard or a content-aware guard that checks for the specific fix being present.
- **A-164** (Phase 2.10): InProcessHost did not expose Services. The integration test needed to resolve ApiPilotSecurityDiagnostics from the built host. Fixed by adding a public IServiceProvider Services property that returns _app.Services. **Lesson:** the test infrastructure type must expose the built host services so integration tests can resolve registered services after startup.
### Added (Phase 2.9)

- Data Protection and multi-instance integration in ApiPilot.Security.DataProtection. The library augments the application's existing ASP.NET Core Data Protection configuration; it never replaces it. The multi-instance flag and the KeyStorage delegate are the contract.

  - `DataProtection/ApiPilotDataProtectionOptions.cs` (new): the options type. ApplicationName (string?), KeyLifetime (TimeSpan?), MultiInstance (bool, default false), KeyStorage (Action<IDataProtectionBuilder>?), plus an internal KeyStorageConfigured flag.
  - `DataProtection/SharedKeyConfiguration.cs` (new): the Apply helper. Sets the application name and key lifetime when the corresponding option is set, invokes the KeyStorage delegate when present, and records that the delegate ran by setting the KeyStorageConfigured flag on the same options instance.
  - `DataProtection/InstanceSafetyValidator.cs` (new): the IValidateOptions<ApiPilotDataProtectionOptions> startup validator. Fails closed when MultiInstance is true and KeyStorageConfigured is false. The failure message names MultiInstance and AddApiPilotDataProtection.
  - `DataProtection/DevelopmentOnlyWarning.cs` (new): a one-shot startup warning. Fires when the in-memory Data Protection key ring is in use (MultiInstance false, no KeyStorage). Emits once per process via Interlocked.CompareExchange. Never includes key material, file paths, or filesystem secrets.
  - `DataProtection/ApiPilotDataProtectionExtensions.cs` (new): AddApiPilotDataProtection. Reads the effective options at registration time, applies the SharedKeyConfiguration helper to the builder, registers the concrete options instance as a singleton, registers IOptions<ApiPilotDataProtectionOptions> via OptionsWrapper so the same instance reaches the validator, registers the InstanceSafetyValidator via TryAddEnumerable, and registers the startup diagnostics and hosted service.
  - `DataProtection/DataProtectionStartupHostedService.cs` (new): an IHostedService that resolves the IDataProtectionStartupDiagnostics and calls Emit once at host startup.

- The options type is named ApiPilotDataProtectionOptions, not DataProtectionOptions. The shorter name collides with the public Microsoft.AspNetCore.DataProtection.DataProtectionOptions. The ApiPilot prefix matches the convention already set by ApiPilotJsonOptions, ApiPilotValidationOptions, and ApiPilotContentNegotiationOptions.

- The library never inspects Microsoft.AspNetCore.DataProtection internals and never uses reflection. The multi-instance safety check is declaration-based. The library knows that the KeyStorage delegate ran; it cannot verify that the delegate configured a persistent store. The contract is documented explicitly: MultiInstance = true requires the application to configure shared or persistent key storage through the approved ApiPilot registration path.

- The Data Protection KeyLifetime is separate from the CSRF token lifetime (CsrfTokenOptions.TokenLifetime). Key rotation does not invalidate an existing CSRF token unless the Data Protection configuration itself revokes the old key.

- The extension does not add Data Protection services redundantly. services.AddDataProtection() is idempotent; the application's provider, application name, and key lifetime are preserved unless ApiPilot options explicitly override them.

- Tests added in Phase 2.9 (12 new tests across 3 new test classes):
  - `DataProtection/DataProtectionOptionsTests.cs` (6): defaults are null/false, ApplicationName/KeyLifetime/MultiInstance are mutable, Apply with a null KeyStorage does not set the flag, Apply with a KeyStorage sets the flag and invokes the delegate.
  - `DataProtection/InstanceSafetyValidatorTests.cs` (6): MultiInstance=false passes, MultiInstance=true with KeyStorageConfigured=true passes, MultiInstance=true with KeyStorageConfigured=false fails, the failure names MultiInstance, the failure names AddApiPilotDataProtection, null options throws.
  - `Integration/MultiInstanceCsrfIntegrationTests.cs` (2): two hosts sharing a key ring and application name validate each other's tokens (host A issues, host B passes a protected POST), two hosts with separate key rings do not (host A issues, host B rejects the POST with 403). Both tests use a temporary on-disk key ring directory.

- Test count: 778 at Phase 2.8 close. 790 at Phase 2.9 close. Delta: +12.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 293 -> 307. Wait, correction: 305 was confirmed after the DataProtectionOptions and InstanceSafetyValidator tests, then +2 for the MultiInstance tests = 307.

### Findings from the build (Phase 2.9)

- **A-159** (Phase 2.9): The new options type DataProtectionOptions collided with Microsoft.AspNetCore.DataProtection.DataProtectionOptions. CS0104 at every use site where both namespaces are in scope. Renamed to ApiPilotDataProtectionOptions, matching the prefix convention. **Lesson:** every new public options type must be checked against the framework's public types. The prefix rule is now a pre-write checklist item.
- **A-160** (Phase 2.9): MultiInstanceCsrfIntegrationTests was missing the using ApiPilot.AspNetCore.Middleware directive. The AddApiPilotCorrelation and UseApiPilotCorrelation extension methods live in that namespace. CS1061 at two call sites. Fixed by adding one using. Same family as A-155.
- **A-161** (Phase 2.9): The Data Protection options must be registered as a concrete singleton plus an OptionsWrapper bridge rather than through the standard options pipeline. The KeyStorage delegate must run at registration time to configure the builder; the pipeline cannot defer the delegate. The bridge ensures the validator sees the same instance the builder was configured with. This is a legitimate exception to the no-OptionsWrapper rule established in Phase 1.9.5. **Lesson:** when a configuration delegate must run at registration time, the OptionsWrapper bridge is required; the A-122 defect does not apply because the pipeline is not asked to construct a fresh default.
### Added (Phase 2.8)

- Optional Fetch Metadata policy in ApiPilot.Security.Origin. A Sec-Fetch-Site-based defense-in-depth layer. The policy is opt-in and disabled by default. This is the compatibility path required by SPEC.md 6.17.

  - `Origin/FetchMetadataProfile.cs` (new): the three-member enum (Off, Compat, Strict). Off is the default.
  - `Origin/FetchMetadataOptions.cs` (new): the S28 through S31 options. Profile (default Off), AllowedSiteValues (default same-origin, same-site, none), AllowedModeValues (default cors, same-origin, navigate), AllowMissingHeaders (default true).
  - `Origin/FetchMetadataEvaluator.cs` (new): the evaluation logic. Applies the profile to the two headers. The AllowMissingHeaders correction is applied: the setting governs the missing-or-malformed case only. It never overrides an explicitly present disallowed value. A cross-site value under Strict with AllowMissingHeaders=true is still rejected.
  - `Origin/FetchMetadataMiddleware.cs` (new): the middleware. Reads the same CsrfEndpointMetadata that the CSRF middleware reads. The precedence rule is Require > Skip > global ProtectedMethods set. Rejects with ApiErrorCode.Forbidden (403). Resolves the correlation ID from ICorrelationIdAccessor with TraceIdentifier fallback.
  - `Origin/FetchMetadataMiddleware.LogMessages.cs` (new): the LogRejectedDelegate (EventId 8001).
  - `Origin/FetchMetadataMiddlewareExtensions.cs` (new): AddApiPilotFetchMetadata and UseApiPilotFetchMetadata.
  - `Configuration/FetchMetadataOptionsValidator.cs` (new): the IValidateOptions<FetchMetadataOptions> startup validator. When the profile is Off, inner checks are skipped. When the profile is enabled, AllowedSiteValues and AllowedModeValues must not be empty. Under Strict, cross-site must not be in AllowedSiteValues; the validator fails closed so an ineffective Strict policy is rejected at startup.

- The Fetch Metadata rejection uses ApiErrorCode.Forbidden. There is no dedicated Fetch-Metadata-rejected code in SPEC.md. The log reason distinguishes the case.

- Malformed Sec-Fetch-Site values (values outside the four-member spec set same-origin, same-site, cross-site, none) are treated as missing. The AllowMissingHeaders policy applies.

- Tests added in Phase 2.8 (44 new tests across 5 new test classes):
  - `Origin/FetchMetadataProfileTests.cs` (4): three members, Off is the default, each member defined, distinct numeric values.
  - `Configuration/FetchMetadataOptionsValidatorTests.cs` (7): Off passes, Compat with defaults passes, Strict with defaults passes, Strict with cross-site fails, empty site values fails, empty mode values fails, undefined profile fails.
  - `Origin/FetchMetadataEvaluatorTests.cs` (16): Off with cross-site passes, Compat with same-origin passes, Compat with missing header passes, Compat with cross-site is rejected, Strict with cross-site and AllowMissingHeaders=true is rejected (the Q8.2 correction), Strict with cross-site and AllowMissingHeaders=false is rejected, Strict missing header with AllowMissingHeaders=true passes, Strict missing header with AllowMissingHeaders=false is rejected, Strict malformed with AllowMissingHeaders=true passes, Strict malformed with AllowMissingHeaders=false is rejected, Strict same-site passes, Strict none passes, Strict allowed mode passes, Strict disallowed mode is rejected, Strict missing mode passes, Strict cross-site explicitly added to AllowedSiteValues passes.
  - `Origin/FetchMetadataMiddlewareTests.cs` (11): GET passes regardless of profile, Off passes, Compat missing header passes, Strict cross-site returns 403 FORBIDDEN, failure envelope carries the correlation ID, Skip policy bypasses, Require policy on GET enforces, Strict same-site passes, Strict disallowed mode returns 403, Strict missing header disallowed returns 403, custom ProtectedMethods can add a safe method.
  - `Integration/FetchMetadataIntegrationTests.cs` (6): Off passes through (the compatibility path), GET is unaffected, Strict cross-site returns 403, Strict same-origin passes, Compat missing header passes, Strict disallowed mode returns 403.

- Test count: 734 at Phase 2.7 close. 778 at Phase 2.8 close. Delta: +44.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 249 -> 293.

### Findings from the build (Phase 2.8)

- **A-157** (Phase 2.8, process): A property declaration ended with a comma instead of a semicolon. The comma is valid inside a collection initializer but not as a property-declaration terminator. The compiler reported CS1002 and CS1519 with the exact line and column. Fixed by changing the comma to a semicolon. Lesson: when writing a new class, every property declaration ends with a semicolon. Trailing commas are only valid inside initializer blocks. Same family as the trailing-comma defect in a PowerShell assignment statement from an earlier phase.
- **A-158** (Phase 2.8, process): A PowerShell single-quoted string that contains a backtick-quoted inline-code fragment (for example, a code span with a dollar-sign variable assignment) causes the PowerShell parser to treat the backtick as an interpolation marker. The fragment then terminates the outer string early and the parse fails with UnexpectedToken. Fixed by rewriting the text to avoid the backtick and the embedded dollar-sign. Lesson: PowerShell single-quoted strings cannot carry backtick-quoted inline-code fragments verbatim. Paraphrase or escape.
### Added (Phase 2.7)

- Origin policy in ApiPilot.Security.Origin. A defense-in-depth layer that rejects a request whose Origin does not match an allowed origin. The policy is not a replacement for CSRF protection, not an authorization mechanism, and not a CORS implementation.

  - `Origin/OriginMatchMode.cs` (new): the three-member enum (Exact, AnyPortSameHost, SameSite). Exact is the default. SameSite is present in the surface but not implemented; the validator treats it as a non-match and the startup validator fails closed when it is configured.
  - `Origin/OriginPolicyOptions.cs` (new): the S23 through S27 options. Enabled (default true), AllowedOrigins (default empty), AllowSameOrigin (default true), AllowMissingOrigin (default true), AllowRefererFallback (default false), MatchMode (default Exact), RejectionCode (default CSRF_ORIGIN_REJECTED).
  - `Origin/OriginValidator.cs` (new): the RFC 6454 origin comparison. TryParseOrigin normalizes scheme, host, and port (with the scheme default applied to a missing port). OriginMatches compares two origin strings under the given mode. IsRequestOriginAcceptable applies the full policy: same-origin allowance, missing-origin behavior, referer fallback, and allow-list membership. Malformed origins are treated as non-matches; no exception is thrown.
  - `Origin/OriginMiddleware.cs` (new): the middleware. Reads the endpoint policy from the same CsrfEndpointMetadata that the CSRF middleware reads. The precedence rule is Require > Skip > global ProtectedMethods set. Resolves the correlation ID from ICorrelationIdAccessor with TraceIdentifier fallback (A-120 discipline). Never echoes the offending Origin in the response envelope.
  - `Origin/OriginMiddleware.LogMessages.cs` (new): the LogRejectedDelegate (EventId 7001).
  - `Origin/OriginMiddlewareExtensions.cs` (new): AddApiPilotOriginPolicy and UseApiPilotOriginPolicy.
  - `Configuration/OriginPolicyOptionsValidator.cs` (new): the IValidateOptions<OriginPolicyOptions> startup validator. The fail-closed rule: when Enabled is true and AllowSameOrigin is false, AllowedOrigins must contain at least one entry, because otherwise no request would ever pass.

- AllowSameOrigin and AllowMissingOrigin are independent decisions. AllowSameOrigin governs a present origin that matches the request own origin. AllowMissingOrigin governs an absent Origin. Neither implies the other.

- The Origin middleware reuses the CSRF endpoint policy metadata. An endpoint marked [ApiPilotSkipCsrf] also bypasses the Origin policy, because an endpoint that opts out of CSRF protection is opting out of the CSRF-adjacent Origin policy. An endpoint marked [ApiPilotRequireCsrf] enforces the Origin policy even on a safe method. Documented on the middleware XML remarks.

- SameSite matching mode is not implemented in this phase. A public suffix list is required for correct registrable-domain comparison. The mode exists in the enum so a future phase can implement it without changing the surface. The startup validator fails closed when SameSite is configured.

- Tests added in Phase 2.7 (43 new tests across 5 new test classes):
  - `Origin/OriginMatchModeTests.cs` (3): three members, Exact is the default, each member is defined.
  - `Origin/OriginValidatorTests.cs` (16): parsing well-formed origins, explicit ports, uppercase host lowercasing, the null literal, empty strings, paths, exact-mode matches on scheme and host, exact-mode port mismatch, exact-mode scheme mismatch, exact-mode host mismatch, default-port normalization, AnyPortSameHost matching and rejection, SameSite returning false, malformed request origin, malformed allowed origin.
  - `Configuration/OriginPolicyOptionsValidatorTests.cs` (7): defaults pass, disabled policy skips inner checks, empty AllowedOrigins with no same-origin fails, empty AllowedOrigins with same-origin passes, malformed origin fails, SameSite mode fails, empty RejectionCode fails.
  - `Origin/OriginMiddlewareTests.cs` (12): GET passes, POST with missing Origin passes by default, POST with missing Origin returns 403 when disallowed, POST with matching same-origin passes, POST with hostile Origin returns 403, failure envelope carries the correlation ID, custom RejectionCode honored, Skip policy bypasses, Require policy on GET enforces, Enabled=false passes, allowed-origin entry matches.
  - `Integration/OriginPolicyIntegrationTests.cs` (6): GET unaffected, POST same-origin passes, POST with no Origin passes, POST with hostile Origin returns 403 CSRF_ORIGIN_REJECTED, allowed origin entry passes, POST with no Origin returns 403 when AllowMissingOrigin is false.

- Test count: 685 at Phase 2.6 close. 734 at Phase 2.7 close. Delta: +43.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 206 -> 249.

### Findings from the build (Phase 2.7)

- **A-155** (Phase 2.7): A test file that exercises the error envelope via ErrorResponseResult must register AddApiPilotJson() in its ServiceCollection, and it must import ApiPilot.AspNetCore.Serialization where the extension lives. The OriginMiddlewareTests file was missing the using. Fixed by adding one using. Same class as A-150 (Phase 2.4). **Lesson:** the pre-write checklist for any test file that exercises an error envelope must include (a) the services.AddApiPilotJson() call in the Context helper and (b) the using ApiPilot.AspNetCore.Serialization directive. Grep the test file for both before running.
- **A-156** (Phase 2.7): Microsoft.AspNetCore.Http.HostString throws ArgumentException("The value must be greater than zero") when constructed with a port of 0 or negative. The HostString(string, int) overload requires a positive port. The OriginMiddlewareTests Context helper used new HostString(host, port ?? 0), which threw when no port was passed. Fixed by using the HostString(string) overload when no port is intended. **Lesson:** when a helper conditionally sets a value, use the overload that omits the value rather than passing a zero default. The compiler does not catch this; the runtime does.

### Added (Phase 2.6)

- Secure cookie profiles in ApiPilot.Security.Cookies. The library validates cookie configurations at startup and fails closed when a profile is insecure. The library never sets a cookie; the profiles are a declaration the application applies to its own cookie configuration.

  - `Cookies/CookieSameSite.cs` (new): the four-member enum (Unspecified, Lax, Strict, None). Separate from Microsoft.AspNetCore.Http.SameSiteMode so the profile value object does not depend on the ASP.NET Core type.
  - `Cookies/CookieProfile.cs` (new): the value object. Carries the base name, Secure, HttpOnly, SameSite, Path, and optional Domain. The `required` modifier on Name, Secure, HttpOnly, and SameSite makes the compiler catch profile constructions that omit a security-relevant field. The base name does not carry a prefix; the effective name is NamePrefix + Name.
  - `Cookies/HostPrefixValidator.cs` (new, file `__HostPrefixValidator.cs`): the RFC 6265bis prefix rules. __Host- requires Secure, no Domain, Path=/. __Secure- requires Secure. IsDefinedPrefix recognizes the empty string and the two prefixes.
  - `Cookies/CookieProfileValidator.cs` (new): the per-profile validator. Checks the effective name for RFC 6265 forbidden characters, the Path for a leading slash, the Domain for hostname syntax, the SameSite value for definedness, SameSite=None for Secure, and delegates to HostPrefixValidator.
  - `Cookies/CookieProfileOptions.cs` (new): carries NamePrefix and the three profiles (AuthenticationProfile, SessionProfile, CsrfProfile). Path and Domain live on each profile, not on the options.
  - `Cookies/AuthenticationCookieProfile.cs` (new): the auth default (Secure + HttpOnly + SameSite=Lax + Path=/ + no Domain) and the session default (Secure + HttpOnly + SameSite=Strict + Path=/ + no Domain).
  - `Cookies/CsrfCookieProfile.cs` (new): the CSRF double-submit cookie default (Secure + HttpOnly=false + SameSite=Lax). The XML doc states explicitly that HttpOnly=false increases XSS exposure and is necessary only when browser JavaScript must read the cookie. The recommended pattern is to hold the CSRF token in memory and skip the cookie.
  - `Configuration/CookieProfileOptionsValidator.cs` (new): the IValidateOptions<CookieProfileOptions> startup validator. Fails closed on an invalid prefix, an invalid profile, or an undetected RFC 6265 violation. The failure messages name the profile and the property.
  - `Cookies/ApiPilotCookiesExtensions.cs` (new): AddApiPilotCookies(IServiceCollection, Action<CookieProfileOptions>?). Registers through the standard options pipeline with ValidateOnStart, plus TryAddEnumerable for the validator.

- The Path property is documented as the configured scope, not as "narrow". "/" is required by RFC 6265bis for __Host- cookies and is the widest scope for ordinary cookies. The XML doc states both facts and recommends the narrowest path that serves the cookie purpose.

- The NamePrefix is validated against the defined set (empty, __Host-, __Secure-) and against the profile attributes. The prefix rules apply to the effective name (NamePrefix + Name), not to the prefix or the name in isolation.

- CONFIGURATION_ERROR is a specification-level classification (SPEC.md L119). It is not a wire-visible marker in the startup-failure path. The actual startup exception is Microsoft.Extensions.Options.OptionsValidationException, thrown by ValidateOnStart. No HTTP response is produced because the host does not start.

- Tests added in Phase 2.6 (46 new tests across 5 new test classes):
  - `Configuration/CookieProfileOptionsValidatorTests.cs` (7): defaults pass, null prefix fails, undefined prefix fails, null auth profile fails, SameSite=None without Secure fails, __Host- with non-root Path fails, custom valid profile passes.
  - `Cookies/CookieProfileTests.cs` (6): auth default attributes, session default attributes, csrf default attributes, structural equality, inequality, CookieSameSite enum members.
  - `Cookies/HostPrefixValidatorTests.cs` (9): __Host- valid combination, __Host- no Secure, __Host- with Domain, __Host- with non-root Path, __Secure- with Secure, __Secure- no Secure, no prefix no rules, the three defined prefixes are recognized, unknown prefixes are rejected.
  - `Cookies/CookieProfileValidatorTests.cs` (20): the three defaults pass, empty effective name, name with space, name with semicolon, name with comma, empty Path, Path without slash, empty Domain, Domain with scheme, Domain with path, Domain with port, Domain with leading dot, undefined SameSite, SameSite=None without Secure, SameSite=None with Secure, HttpOnly=false passes, valid domain passes, valid subdomain passes.
  - `Integration/CookieProfileIntegrationTests.cs` (4): valid configuration starts the host, SameSite=None without Secure fails startup, __Host- with non-root Path fails startup, unknown prefix fails startup. Each failure test asserts that the exception message names the offending property.

- Test count: 639 at Phase 2.5 close. 685 at Phase 2.6 close. Delta: +46.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 160 -> 206.

### Findings from the build (Phase 2.6)

- **A-153** (Phase 2.6): The first write of CookieProfileValidator.cs triggered three defects. (1) IsValidHostname had a duplicated and nonsensical IndexOf condition. (2) StartsWith(string, StringComparison) with a one-character string triggered CA1865, and the recommended fix StartsWith(char, StringComparison) does not exist in net10.0. (3) IndexOf used as a boolean check triggered CA2249. Fixed by whole-file rewrite: StartsWith(char) for single-character checks (the char overload has no comparison-type parameter and uses ordinal by default), Contains for boolean string checks, and a foreach loop for the hostname character check. **Lesson:** the char overload of StartsWith/EndsWith/Contains does not accept a StringComparison argument. When an analyzer recommends a char overload, drop the comparison argument.
- **A-154** (Phase 2.6): CA1859 on the ValidateProfile private helper in CookieProfileOptionsValidator. The analyzer recommends the concrete List<string> over the interface IList<string> for the parameter. Fixed by changing the private method parameter type. **The public method CookieProfileValidator.Validate keeps IList<string>** because an interface is the correct contract for a public method. **Lesson:** private methods take concrete collection types; public methods take interface types. Add to the pre-write checklist for any new validator or helper.

### Added (Phase 2.5)

- Authentication transition hooks in ApiPilot.Security.Csrf. The library never hooks ASP.NET Core Identity, SignInManager, or any authentication framework directly. The application calls the listener after its authentication flow completes and before its session state ends.

  - `Csrf/ICsrfTransitionListener.cs` (new): the listener interface. Three methods: OnLoginAsync(HttpContext) returns Task<CsrfToken?>, OnLogoutAsync(HttpContext) returns Task, OnLogoutAsync(string previousBinding) returns Task. The two logout overloads cover both orderings: the context overload works when called before the application clears session state; the binding overload works in both orderings and is the recommended pattern.
  - `Csrf/CsrfTransitionEvents.cs` (new): the DefaultCsrfTransitionListener. Composes over ICsrfService, ICsrfBindingProvider, an optional ICsrfRotationStore, and ILogger. OnLoginAsync issues a fresh token bound to the new authenticated subject. OnLogoutAsync clears the rotation marker for the previous binding when a store is registered, logs a structured warning when the binding cannot be resolved, and logs a separate warning when no store is registered.
  - `Csrf/CsrfTransitionEvents.LogMessages.cs` (new): two LoggerMessage.Define delegates (EventId 6001 for binding unresolved at logout, 6002 for no store at logout). Follows the A-139 pattern.
  - `Csrf/CsrfTransitionExtensions.cs` (new): AddApiPilotCsrfTransitionListener(IServiceCollection). Registers the default listener with TryAddSingleton. The listener is not auto-registered by AddApiPilotCsrf; the application opts in explicitly. The lifetime follows the dependencies: the ICsrfService and ICsrfRotationStore registrations are singletons, so the listener is a singleton. The registration comment names the coupling.

- The Q5.2 design decision: OnLogoutAsync captures the binding from the current context when possible. Because the CSRF binding is derived from the authenticated subject (through CsrfBindingProvider), the binding is only resolvable before the application clears session state. The explicit-binding overload exists so the logout handler can pass the binding captured before logout. The XML doc on the interface states both orderings and recommends the explicit-binding pattern.

- Tests added in Phase 2.5 (14 new tests in 1 new test class):
  - `Csrf/CsrfTransitionListenerTests.cs` (14): login with an authenticated subject issues a token, login without a binding returns null, logout with a context before session clear clears the marker, logout with an anonymous context logs a warning and does not clear, logout with an explicit binding clears the marker for that binding, logout without a store logs a warning, null context rejected on login, null context rejected on logout, null binding rejected, constructor null service/binding provider/logger rejections, the assembly does not reference ASP.NET Core Identity, and a custom listener can replace the default through the interface.

- Test count: 625 at Phase 2.4 close. 639 at Phase 2.5 close. Delta: +14.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 146 -> 160.

- No findings in Phase 2.5. The phase ran cleanly: the interface shape, the default listener, the LogMessages companion, the DI registration, and the tests all compiled and passed on first write. The Q5.2 correction was a design adjustment applied before the first write, not a defect discovered during the build.

### Added (Phase 2.4)

- Unsafe-method protection middleware in ApiPilot.Security.Csrf. Enforces CSRF on state-changing methods by default and supports per-endpoint overrides through attributes.

  - `Csrf/CsrfEndpointMetadata.cs` (new): the CsrfPolicy enum (UseGlobal, Require, Skip) and the CsrfEndpointMetadata record.
  - `Csrf/CsrfAttributes.cs` (new): [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf], both implementing IEndpointMetadataProvider so minimal API endpoints and controller actions automatically attach the metadata.
  - `Csrf/CsrfMiddleware.cs` (new): the middleware. Reads the endpoint policy via GetOrderedMetadata, applies the precedence rule Require > Skip > global ProtectedMethods, reads the header, calls ICsrfService.ValidateAsync, writes the standard error envelope on failure. Resolves the correlation ID from ICorrelationIdAccessor with TraceIdentifier fallback (A-120 discipline).
  - `Csrf/CsrfMiddleware.LogMessages.cs` (new): the LogRejectedDelegate (LoggerMessage.Define, EventId 5001). Follows the A-139 pattern.
  - `Csrf/CsrfMiddlewareExtensions.cs` (new): UseApiPilotCsrfProtection(IApplicationBuilder). The service registration comes from AddApiPilotCsrf (Phase 2.2).
  - `Csrf/CsrfOptions.cs` (extended): added ProtectedMethods (default POST, PUT, PATCH, DELETE), ExemptPaths (empty list, resolved at request time together with BootstrapPath), and ExemptPredicate (optional Func<HttpContext, bool>).

- The precedence rule for endpoint policy is Require (from [ApiPilotRequireCsrf]) > Skip (from [ApiPilotSkipCsrf]) > global ProtectedMethods. Require overrides the safe-method bypass, so [ApiPilotRequireCsrf] on a GET endpoint enforces protection.

- Effective exemptions are computed per request: the current BootstrapPath plus the configured ExemptPaths, plus the predicate when set. The bootstrap path is read at request time, so a later change to BootstrapPath is honored.

- Pipeline placement: after UseApiPilotCorrelation, after UseApiPilotExceptions, after authentication, and after UseRouting. The middleware reads endpoint metadata, so it must sit after routing has resolved the endpoint. Place before UseApiPilotContentNegotiation and before the endpoint dispatch.

- Tests added in Phase 2.4 (39 new tests across 4 new test classes, plus 1 promoted infrastructure file):
  - `TestInfrastructure/FakeLogger.cs` (new): promoted from the inline copies in CsrfServiceTests and CsrfMiddlewareTests because two files in the same namespace cannot each declare FakeLogger<T> (A-149). The shared file lives in namespace ApiPilot.Security.Tests.
  - `Csrf/CsrfEndpointMetadataTests.cs` (5): the CsrfPolicy enum members, default value, record equality.
  - `Csrf/CsrfAttributesTests.cs` (4): AttributeUsage on both attributes, IEndpointMetadataProvider implementation, distinct types.
  - `Csrf/CsrfMiddlewareTests.cs` (23): the method x policy matrix, the precedence rule, the exemptions, the custom header name, the envelope correlation ID, the constructor null guards, and the custom ProtectedMethods set.
  - `Integration/CsrfProtectionIntegrationTests.cs` (7): end-to-end through a real InProcessHost. No token on POST returns 403 CSRF_HEADER_MISSING, valid token passes, GET is unaffected, Skip policy bypasses, Require policy on GET enforces, the bootstrap path is exempt even on POST, the failure envelope carries the correlation ID.

- `Csrf/CsrfServiceTests.cs` (modified): removed the inline FakeLogger declaration; uses the shared one.
- `Csrf/CsrfMiddlewareTests.cs` (modified): removed the inline FakeLogger declaration; uses the shared one via using ApiPilot.Security.Tests.

- Test count: the actual Security test run went from 107 at Phase 2.3 close to 146 at Phase 2.4 close. Delta: +39.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).
  - `Security.Tests`: 107 -> 146.
  - Total: 586 at Phase 2.3 close. 625 at Phase 2.4 close.

- **Count reconciliation note:** the README.md test count values written at Phase 2.2 close (609) and Phase 2.3 close (620) were both higher than the actual test run. The actual totals were 575 at Phase 2.2 close and 586 at Phase 2.3 close. The drift is recorded as A-152. The Phase 2.4 update sets the correct value and the ritual is corrected: the README count is taken from the last test run output, not from a carried-forward estimate.

### Findings from the build (Phase 2.4)

- **A-147** (Phase 2.4): The first write of `CsrfAttributes.cs` used `Microsoft.AspNetCore.Http.Metadata.MethodInfo` in the `PopulateMetadata` signature. `MethodInfo` lives in `System.Reflection`, not in the ASP.NET Core namespace. The compiler reported CS0234 and two cascade CS0535 (interface not implemented). Fixed by using `MethodInfo` with `using System.Reflection;` and adding `using Microsoft.AspNetCore.Builder;` for `EndpointBuilder`.
- **A-148** (Phase 2.4, process): The corrective block for A-147 had an existence-only guard (`if Test-Path { skip }`) which caused the block to skip because the original file was already on disk. The guard prevented the fix. **Lesson:** when a corrective needs to overwrite a known-bad file, the write must not be gated by an existence check. Use no guard or a content-aware guard that checks for the specific fix being present.
- **A-149** (Phase 2.4, process): The A-143 pattern (declare FakeLogger<T> inline in the test file that needs it) works only for one file per namespace. A second file in the same namespace with its own FakeLogger<T> produces CS0101 and CS0111. Fixed by promoting FakeLogger<T> and LogEntry to a shared `TestInfrastructure/FakeLogger.cs` file in namespace ApiPilot.Security.Tests, and removing the inline copies. **Lesson:** when a test helper is used by more than one file in the same namespace, promote it to shared infrastructure. This mirrors the pattern already established for TestAssert, TestRunner, and InProcessHost.
- **A-150** (Phase 2.4): The CsrfMiddlewareTests.Context helper built a bare ServiceProvider with no ApiPilot services. When a test triggered the 403 error path, ErrorResponseResult could not resolve IOptions<Http.Json.JsonOptions> and the test failed. Nine tests failed for the same missing registration. Fixed by adding services.AddApiPilotJson() to the test context. Same class as A-117 (the Phase 1.6 test context needed AddApiPilotJson()): a test context that exercises the error envelope must register the JSON options the envelope serialization depends on.
- **A-151** (Phase 2.4): The CSRF protection middleware reads endpoint metadata to determine the per-endpoint policy. It must sit after UseRouting() so the endpoint is set on the HttpContext, and before the endpoint dispatch. The integration test placed UseApiPilotCsrfProtection before UseRouting, so GetEndpoint returned null and every request used the global policy. Two tests failed: a Skip endpoint was enforced, a Require endpoint was not. **Lesson:** any middleware that reads endpoint metadata must be placed after UseRouting. Add to the pre-write checklist for any new endpoint-metadata-reading middleware. Document the pipeline order in the per-concern doc.
- **A-152** (Phase 2.4, process): The README test count drifted from the actual test run count across multiple phases. The values written at Phase 2.2 close (609) and Phase 2.3 close (620) were both higher than the actual counts (575 and 586). **Lesson:** the README test count must be updated from the actual Tests run line of the test harness output, not from a value carried forward by hand. Add to the phase-closing ritual: read the actual count from the last test run before writing the README.

### Added (Phase 2.3)

- CSRF bootstrap endpoint in ApiPilot.Security.Csrf. A GET endpoint that returns a bare { "token": "..." } body and nothing else. This is the only place in ApiPilot where a successful response is not the standard success envelope; the wire shape is mandated by SPEC.md.

  - `Csrf/CsrfBootstrapOptions.cs` (new): the S10 and S11 bootstrap endpoint options. CacheControl (default "no-store") and Enabled (default true).
  - `Csrf/CsrfBootstrapEndpoint.cs` (new): the static HandleAsync method. Reads the effective options, calls ICsrfService.IssueAsync, writes the success response on non-null, writes the standard error envelope on null. The response Cache-Control header is set on every response. The endpoint never sets a cookie and never echoes the binding.
  - `Csrf/CsrfBootstrapExtensions.cs` (new): MapApiPilotCsrf(IEndpointRouteBuilder). Reads the path from CsrfOptions.BootstrapPath. Reads the bootstrap options from DI, defaulting to a new CsrfBootstrapOptions if not registered. When CsrfBootstrapOptions.Enabled is false, registers nothing.
  - `Csrf/CsrfBootstrapEndpoint.cs` also carries the wire DTO CsrfBootstrapResponse. The record is an expanded type whose Token property is pinned to the lowercase key "token" with [JsonPropertyName]. A source-generated JsonSerializerContext avoids reflection on the hot path.

- `Csrf/CsrfServiceExtensions.cs` (extended): AddApiPilotCsrf takes a third optional parameter, configureBootstrap, and registers CsrfBootstrapOptions through the standard options pipeline. ValidateOnStart is called for the bootstrap options type.

- `ApiPilot.Security.csproj` (extended): the project now references ApiPilot.AspNetCore. This is a deliberate architectural decision. The security package composes on the ASP.NET Core adapter for the error-envelope construction path. The dependency is one-directional: Core <- AspNetCore <- Security.

- `audit.ps1` (extended): Section 5g-bis allowlist now includes ApiPilot.AspNetCore. Without this, the new reference would fail the audit.

- Tests added in Phase 2.3 (11 new tests across 2 test classes, plus infrastructure):
  - `tests/InProcessHost.cs` (new): copied from ApiPilot.AspNetCore.Tests with the namespace changed to ApiPilot.Security.Tests. The file is byte-for-byte identical to the source otherwise. Both test projects now declare their own InProcessHost, matching the pattern established for FakeLogger and TestAssert.
  - `Csrf/CsrfBootstrapOptionsTests.cs` (4): S10 default no-store, S11 default true, both properties mutable.
  - `Integration/CsrfBootstrapIntegrationTests.cs` (7): 200 with a token, response has exactly one property (token), no auth material leaks in the raw JSON, unauthenticated without a pre-auth source returns 403 with CSRF_TOKEN_INVALID, a custom bootstrap path is honored and the default path returns 404, the default Cache-Control is no-store, a custom Cache-Control value is honored. Uses a real InProcessHost with Data Protection registered.

- Test count: 609 at Phase 2.2 close. 620 at Phase 2.3 close. Delta: +11.
  - `ApiPilot.Security.Tests`: 130 -> 141 tests across 13 classes.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).

### Findings from the build (Phase 2.3)

- **A-144** (Phase 2.3, process): The anchor-based splice for adding a `<param>` tag and a method-body statement to `CsrfServiceExtensions.cs` corrupted the XML doc block and threw the method body out of balance. Thirty-one cascade errors resulted. The recovery was a whole-file rewrite. **Lesson:** when a splice touches both an XML doc block and the method body, the risk of anchor mismatch is high. For edits that touch more than one region of a file, use a whole-file rewrite from the start. Same class as A-105 and A-106 (splice routines) and A-124 (skip-count off-by-one).
- **A-145** (Phase 2.3): The source-generated JsonSerializerContext for the bootstrap response used the default JsonSerializerOptions, which produces PascalCase. The wire shape `{ "token": "..." }` mandated by SPEC.md requires lowercase. Two integration tests failed with "key was not present in the dictionary". Fixed by pinning the property name with `[JsonPropertyName("token")]` on the DTO. **Lesson:** when a wire key is fixed by the contract, pin it with `[JsonPropertyName]` on the source-generated DTO. Do not rely on a naming policy that the serializer context does not apply. Same class as A-045 (ApiErrorCode serialized as a nested object) and A-054 (ApiError.Fields serialized as nested objects).
- **A-146** (Phase 2.3, process): A PowerShell `$out += 'x',` line with a trailing comma is a parse error. The comma is only valid inside `@(...)` array literals, not as a statement terminator. The parse error prevented the corrective block from executing; the two failing tests were not fixed by the first attempt. **Lesson:** use `$out += 'x'` (no comma) for statement-style assignment. Record in the PowerShell trap list. Same family as A-129 (if/else on separate lines) and A-136 (apostrophe inside single-quoted string).

### Added (Phase 2.2)

- CSRF service and rotation in ApiPilot.Security.Csrf. Public API for issuing, validating, and rotating tokens. Composes over the Phase 2.1 signer, a binding provider, and an optional rotation store.

  - `Csrf/CsrfBindingFailureReason.cs` (new): the five-member enum of reasons a binding could not be resolved (None, NoSubject, NoSession, NoConfiguredSource, ProviderFailure). None is the default. A binding failure is a normal, expected outcome; the library never silently generates an anonymous binding.
  - `Csrf/CsrfOptions.cs` (new): the S6 through S9 service-level options plus the two enums CsrfRotationPolicy (OnBootstrap, OnWindow, Never) and CsrfRotationRequirement (Optional, Required). Contains the CodeMapping dictionary (7 default entries) and the PreAuthBindingSource delegate.
  - `Csrf/ICsrfRotationStore.cs` (new): the async rotation-store interface. GetRotationMarkerAsync, SetRotationMarkerAsync, ClearRotationMarkerAsync. Async because a real store is likely Redis or a database. Optional; not registered by default.
  - `Csrf/ICsrfBindingProvider.cs` (new): the binding provider interface. TryGetBinding(HttpContext, out binding, out failureReason).
  - `Csrf/CsrfBindingProvider.cs` (new): the default provider. Resolves in the corrected Q2 order: subject claim (sub, then ClaimTypes.NameIdentifier), then server-managed session, then the configured PreAuthBindingSource. Fails closed otherwise. Fingerprints every source through SHA-256 with a purpose prefix; the original source value is never used as the binding.
  - `Csrf/CsrfValidationContext.cs` (new): the per-request inputs (Binding or BindingFailure, plus RawHeaderValue). Immutable. Two factories: WithBinding and WithoutBinding.
  - `Csrf/CsrfValidationResult.cs` (new): the outcome type. Pairs the internal CsrfTokenParseResult with the public wire code the middleware will emit. Success() and Failure(reason, code) factories.
  - `Csrf/ICsrfService.cs` (new): the service interface. IssueAsync, ValidateAsync, RotateAsync. Async.
  - `Csrf/CsrfService.cs` (new): the default implementation. Composes signer, binding provider, options, and optional rotation store. When no store is registered, RotateAsync issues a fresh token and emits a structured warning on EVERY call (not throttled, not first-call-only) that the previous token remains valid until natural expiry.
  - `Csrf/CsrfService.LogMessages.cs` (new): the LoggerMessage.Define delegate for the rotation warning. Matches the A-042 pattern from ApiPilotExceptionMiddleware.
  - `Csrf/CsrfServiceExtensions.cs` (new): the DI registration. AddApiPilotCsrf registers both options types through the standard pipeline, the signer, the binding provider, and the service. The rotation store is left to the application; the service accepts a null store and operates in expiry-only mode.
  - `Csrf/CsrfTokenValidateOutcome.cs` (new): a small record carrying the reason and, on success, the token issue time. Needed so CsrfService can compare against the rotation marker. Exposed through the new ICsrfTokenSigner.ValidateWithMetadata method.

- `Csrf/CsrfTokenParseResult.cs` (extended): added BindingMissing and Rotated, growing the enum from six to eight members. Both are internal parse reasons; the public wire code is selected by CsrfOptions.CodeMapping.

- `Csrf/ICsrfTokenSigner.cs` (extended): added ValidateWithMetadata(CsrfToken, string). Existing TryValidate delegates to it and discards the issue time. No breaking change to the interface contract.

- `Csrf/DataProtectionCsrfTokenSigner.cs` (extended): implements ValidateWithMetadata. TryValidate becomes a thin wrapper.

- Tests added in Phase 2.2 (19 new tests across 6 test classes):
  - `Csrf/CsrfBindingFailureReasonTests.cs` (4): five-member enum, None as default, distinct values, every member defined.
  - `Csrf/CsrfValidationContextTests.cs` (6): WithBinding and WithoutBinding factories, rejection of invalid inputs.
  - `Csrf/CsrfValidationResultTests.cs` (6): Success and Failure factories, IsSuccess semantics.
  - `Csrf/CsrfOptionsTests.cs` (14): S6 through S9 defaults and overrides, both new enums, CodeMapping contents and mutability, PreAuthBindingSource default and assignment.
  - `Csrf/CsrfBindingProviderTests.cs` (13): subject claim path, NameIdentifier path, session path, pre-auth source path (with and without a value), fail-closed default, fingerprint stability, cross-source distinctness, null context rejection, null options rejection. Uses TestSession and TestSessionFeature doubles.
  - `Csrf/CsrfServiceTests.cs` (16): issue round-trip, issue with no binding returns null, issue with pre-auth source, validate success, validate each failure reason mapping, validate with custom code mapping, rotate round-trip, rotate without a store emits a warning on every call (two calls, two warnings), rotate with a store writes a marker, marker newer than token rejects as Rotated, marker older than token accepts. Uses FakeLogger, FixedSigner, InMemoryRotationStore doubles.

- Test count: 513 at Phase 2.1 close. 609 at Phase 2.2 close. Delta: +96.
  - `ApiPilot.Security.Tests`: 34 -> 130 tests across 11 classes.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).

### Findings from the build (Phase 2.2)

- **A-138** (Phase 2.2): `HttpContext.Session` throws `InvalidOperationException` when no `ISessionFeature` is registered. The initial fix wrapped `Session.Id` but not `Session` itself. In a DefaultHttpContext with no session feature, `context.Session` throws before `.Id` is ever reached. Four tests failed with "Session has not been configured for this application or request." Fixed by extracting a `TryGetSessionId(HttpContext)` helper that wraps the entire session access in try/catch and returns null on `InvalidOperationException`. Used at both the resolution call site and the fail-closed reason call site. **Extended lesson:** when an accessor is documented to throw conditionally, wrap the outermost access, not the innermost member read.
- **A-139** (Phase 2.2): CA1848 on `_logger.LogWarning(...)` in CsrfService.RotateAsync. Fixed by converting CsrfService to a partial class and adding `CsrfService.LogMessages.cs` with a LoggerMessage.Define delegate. Third instance of this class in the project (after A-042 in ApiPilotExceptionMiddleware and its Phase 1.3 follow-up). **Pre-write checklist addition:** grep for `_logger.Log(Information|Warning|Error|Debug|Trace|Critical)` in any new class that logs, before the build.
- **A-140** (Phase 2.2): CS1573 on the new `tokenOptions` constructor parameter. The parameter was added to CsrfService but the XML doc `<param>` tag was not. Fixed by adding one `<param>` tag. Same class as A-059 (missing `<paramref>` target).
- **A-141** (Phase 2.2): A test that asserts the size of a mutable default dictionary must be updated when a new entry is added to the dictionary. `CsrfOptions.CodeMapping` gained a `BindingMissing` entry in a prior block; the count-asserting test went red. **Lesson:** when a default collection gains an entry, tree-grep for tests asserting its `Count`. Same class as A-095 (pre-write test count must match the write block).
- **A-142** (Phase 2.2): `ISessionFeature` lives in `Microsoft.AspNetCore.Http.Features`, a separate namespace from `Microsoft.AspNetCore.Http`. Same class as A-126 (missing `using ApiPilot.AspNetCore.Configuration;`): a type referenced across namespaces without importing its namespace. The compiler reports CS0246 with the exact type name. Fix is one `using` in alphabetical position.
- **A-143** (Phase 2.2, process): A test that references `FakeLogger<T>` in a new test project will not compile unless the class is declared in that project. In this repository, `FakeLogger<T>` is not a shared infrastructure file; it is declared inline in the test file that needs it (e.g., `ApiPilotExceptionMiddlewareTests.cs`). The pattern for a new test file is to declare its own `FakeLogger<T>` at the bottom of the file. Fixed by copying the 22-line class into `CsrfServiceTests.cs` alongside a `LogEntry` record.

### Added (Phase 2.1)

- CSRF token format and signing in ApiPilot.Security.Csrf. The token is a Data Protection protected payload. Data Protection is the cryptographic authority: the library performs no HMAC, no MAC comparison, and no key management of its own. The signer builds a plaintext payload and hands it to IDataProtector.Protect; validation hands the wire value to IDataProtector.Unprotect.

  - `Csrf/CsrfTokenOptions.cs` (new): the S2 through S5 override surface. ProtectionPurpose (default "ApiPilot.Csrf.v1"), TokenEntropyBytes (default 32), TokenLifetime (default 2 hours), TimeProvider (default TimeProvider.System).
  - `Csrf/CsrfTokenParseResult.cs` (new): the six named validation outcomes. Ok, Malformed, WrongVersion, InvalidSignature, Expired, WrongSession. Ok is the default value. WrongVersion is reserved for a future purpose-versioned signer; the v1 signer maps a purpose mismatch to InvalidSignature because Data Protection reports it as a CryptographicException from Unprotect.
  - `Csrf/CsrfTokenFormat.cs` (new): the plaintext payload codec. Encode writes `issuedAtUnixSeconds|randomBase64Url|bindingFingerprint`. TryDecode parses that shape and returns a boolean. The codec performs no cryptography; it prepares and interprets the bytes that Data Protection protects.
  - `Csrf/CsrfToken.cs` (new): the opaque value object. Wraps the wire string. From rejects null, empty, and whitespace. ToString returns the fixed placeholder "[csrf-token]" so a token never appears in a log line by accident.
  - `Csrf/ICsrfTokenSigner.cs` (new): the signing abstraction. Sign(binding) produces a CsrfToken. TryValidate(token, binding) returns a CsrfTokenParseResult. The interface knows nothing about HTTP, headers, or the response envelope.
  - `Csrf/DataProtectionCsrfTokenSigner.cs` (new): the default signer. Resolves an IDataProtector with the configured purpose. On validation, catches CryptographicException from Unprotect and maps it to InvalidSignature; maps a payload decode failure to Malformed; maps a binding mismatch to WrongSession; maps an elapsed lifetime to Expired. Returns Ok on success.

- The token wire format is Data Protection's own serialized output. The library treats it as opaque. The semantic payload inside the protection boundary is defined by CsrfTokenFormat.

- The purpose string is the versioning mechanism. Two instances of an application must share the purpose to validate each other's tokens. A future v2 signer uses a different purpose and can run alongside v1 during a migration. This behavior is tested.

- Tests added in Phase 2.1 (34 new tests across 5 test classes):
  - `Csrf/CsrfTokenOptionsTests.cs` (5): each S2 through S5 default, and mutability of the settable properties.
  - `Csrf/CsrfTokenParseResultTests.cs` (4): the six named members exist and are distinct, Ok is the default value, each member has a distinct numeric value, WrongVersion is defined.
  - `Csrf/CsrfTokenTests.cs` (5): From preserves the wire value, From rejects null and empty, ToString redacts, structural equality holds for matching values.
  - `Csrf/CsrfTokenFormatTests.cs` (8): encode/decode round-trip, non-UTC rejected, pipe in random rejected, pipe in binding rejected, empty payload rejected, missing part rejected, non-numeric issued-at rejected, extra pipe rejected.
  - `Csrf/DataProtectionCsrfTokenSignerTests.cs` (12): sign-then-validate round-trip, opaque wire value, uniqueness, tampered token rejected as InvalidSignature, cross-key-ring token rejected, wrong binding rejected as WrongSession, expired token rejected as Expired, not-yet-expired accepted, purpose isolation (v1 signer cannot validate v2 tokens), null token rejected, empty binding rejected, null binding rejected. Uses a test-local FakeTimeProvider for deterministic clock control.

- Test count: 479 at Phase 2.0 close. 513 at Phase 2.1 close. Delta: +34.
  - `ApiPilot.Security.Tests`: new project, 34 tests in 5 classes.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 268 (unchanged).

### Findings from the build (Phase 2.1)

- **A-137** (Phase 2.1): CA2249 on `string.IndexOf(char) >= 0` in a bounds-check context. The analyzer prefers `string.Contains(char)` for readability. Two sites in CsrfTokenFormat.cs. Fixed by swapping to Contains. Same class as A-125 (CA2263 on Enum.IsDefined) and A-135 (regex trailing backslash): a modern BCL overload or a syntactic form the analyzer prefers over the older one. The tree-wide grep discipline would have caught both sites before the build if they had not been written in the same block; a single build caught both.

### Added (Phase 2.0)

- ApiPilot.Security project skeleton. A new library package alongside ApiPilot.Core and ApiPilot.AspNetCore. The package targets net10.0, references the Microsoft.AspNetCore.App framework, and project-references ApiPilot.Core. It has no PackageReference entries; the boundary policy is enforced by the audit.

  - `src/ApiPilot.Security/ApiPilot.Security.csproj` (new): the source project. AssemblyName and RootNamespace are ApiPilot.Security. The Microsoft.AspNetCore.App FrameworkReference is present. The ApiPilot.Core ProjectReference is present.
  - `src/ApiPilot.Security/Csrf/.gitkeep` (new): the CSRF feature folder, reserved for Phase 2.1 onward.
  - `src/ApiPilot.Security/Cookies/.gitkeep` (new): the cookie profile feature folder, reserved for Phase 2.6.
  - `src/ApiPilot.Security/Origin/.gitkeep` (new): the Origin policy feature folder, reserved for Phase 2.7.
  - `src/ApiPilot.Security/DataProtection/.gitkeep` (new): the Data Protection feature folder, reserved for Phase 2.9.
  - `src/ApiPilot.Security/FetchMetadata/.gitkeep` (new): the Fetch Metadata feature folder, reserved for Phase 2.8.
  - `src/ApiPilot.Security/Configuration/.gitkeep` (new): the options validator feature folder, mirroring the ApiPilot.AspNetCore Configuration folder.
  - `tests/ApiPilot.Security.Tests/ApiPilot.Security.Tests.csproj` (new): the test project. OutputType Exe. Project-references ApiPilot.Security and ApiPilot.Core.
  - `tests/ApiPilot.Security.Tests/Program.cs` (new): the harness entry point.
  - `tests/ApiPilot.Security.Tests/TestAssert.cs` (new): the assertion library, copied from ApiPilot.Core.Tests with the namespace changed.
  - `tests/ApiPilot.Security.Tests/TestRunner.cs` (new): the reflection discovery runner, copied from ApiPilot.Core.Tests with the namespace changed.

- `dotnet/ApiPilot.slnx` (extended): the two new projects are listed. The solution now contains six projects.

- `audit.ps1` (extended): three changes. Section 5a automatically includes ApiPilot.Security.csproj in the no-PackageReference scan. A new Section 5g-bis checks ApiPilot.Security.dll references against the platform allowlist. A new Section 5h checks ApiPilot.Security.csproj declares the ASP.NET Core FrameworkReference. The script grows from 280 to 316 lines.

- Test count: 479 at Phase 1.9.5 close. 479 at Phase 2.0 close. Delta: 0. The ApiPilot.Security test project exists but contains no test classes; the runner discovers and runs 0 tests, which is a valid PASS.

### Findings from the build (Phase 2.0)

- **A-135** (Phase 2.0): The securityAllowPattern regex string in the new audit Section 5g-bis ended with a trailing backslash because a mis-escaped single quote in the PowerShell array literal produced a bad terminal character instead of the intended closing parenthesis. The PowerShell regex parser reported Illegal backslash at end of pattern. Fixed by removing the trailing backslash. Same class as A-121 (XML doc block) and A-131 (byte-backed enum cast): a syntactically invalid literal that the runtime catches but the write verification did not. The lesson: any regex string embedded in a single-quoted PowerShell literal must not end with a lone backslash.
- **A-136** (Phase 2.0, process): The CHANGELOG Phase 2.0 block containing backtick-quoted substrings with embedded single quotes caused a PowerShell parse error that prevented the block from executing. The single quote inside the outer single-quoted string terminated the string early. Fixed by rewriting the A-135 text without raw apostrophes. Lesson: any PowerShell single-quoted string that contains an apostrophe must double it, or the text must avoid the apostrophe entirely. Same family as A-129 (if/else on separate lines) - PowerShell quoting and block syntax traps.

## [0.2.0] - 2026-09-25

### Added (Phase 1.9.5)

- A fluent orchestration layer over the per-concern ApiPilot registration extensions. `AddApiPilot()` returns an `ApiPilotBuilder` whose `Configure*` methods delegate directly to the existing `AddApiPilot*` extensions. The builder is additive: it introduces no aggregate options type, no new validators, and no middleware activation. `AddApiPilot()` alone registers nothing; every concern is activated explicitly through a `Configure*` call.

  - `AspNetCore/DependencyInjection/ApiPilotBuilder.cs` (new): the builder type. Sealed. Six `Configure*` methods (Correlation, Pagination, Json, Exceptions, ContentNegotiation, Validation). Each delegates to the corresponding extension and returns the same builder.
  - `AspNetCore/DependencyInjection/ApiPilotServiceCollectionExtensions.cs` (extended): a new `AddApiPilot()` extension method returns a builder around the given service collection.
  - `tests/AspNetCore.Tests/DependencyInjection/ApiPilotBuilderTests.cs` (new, 10 tests): builder returns the same collection; null services rejected at both `AddApiPilot` and the constructor; each `Configure*` delegates correctly to its extension; `AddApiPilot` alone registers no ApiPilot options and no ApiPilot validators (checked against the six specific options types).

- `AddApiPilotJson` was migrated to the standard options pipeline. This closes the last residual gap of A-122 and is recorded as A-133. Before the migration, `AddApiPilotJson` built a concrete `ApiPilotJsonOptions` via a private `BuildOptions` helper, used it to configure the two JSON serializers, and never registered the configure delegate through `services.AddOptions<ApiPilotJsonOptions>().Configure(configure)`. The result: `IOptions<ApiPilotJsonOptions>.Value` resolved a fresh default instance, and the configure delegate's changes never reached any consumer. After the migration, the two overloads register through the pipeline, both serializers read the resolved instance through `Configure<TOptions, TDep>`, and `ValidateOnStart` validates the same instance every consumer resolves.

- Test count: 469 at Phase 1.9 close. 479 at Phase 1.9.5 close. Delta: +10.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 258 -> 268 (+10).

### Findings from the build (Phase 1.9.5)

- **A-133** (Phase 1.9.5): `AddApiPilotJson` was missed by the Phase 1.8 options-pipeline migration. Its dual-overload shape (one `IMvcBuilder` overload, one `IServiceCollection` overload) made it look different from the four single-overload extensions, so the migration audit did not catch it. The defect was latent until the builder added a test that resolved `IOptions<ApiPilotJsonOptions>` and asserted the configured `EnumMode`. Fixed by registering through `AddOptions<ApiPilotJsonOptions>().Configure(configure)` and wiring the two serializers to read from the pipeline through `Configure<TOptions, TDep>`. The class is the same as A-122. **Pre-write checklist item added:** grep for `new XxxOptions(` in every extension file under `DependencyInjection/`, `Middleware/`, and `Serialization/`. A match means the extension has the A-122/A-133 defect class.
- **A-134** (Phase 1.9.5, process): A test that references an enum member by name must verify the member exists in the enum declaration before writing the test. The test for `ConfigureJson` used `EnumSerializationMode.AsNumber`; the actual member is `EnumSerializationMode.Number`. The compiler reported CS0117 with the missing member name, but the fix cost a build cycle. Same class as A-125 (CA2263 on `Enum.IsDefined`). Read the enum before writing the test.

### Added (Phase 1.8)

- Startup options validation across every AddApiPilot* extension. Every extension now registers through the standard options pipeline (AddOptions<T>().Configure(configure).ValidateOnStart()) instead of building a manual singleton and registering it behind an OptionsWrapper<T> bridge (A-122). The pipeline now validates the same instance the middleware receives. Closes the residual A-073 bridge.

- Five new IValidateOptions<T> implementations under ApiPilot.AspNetCore/Configuration/:
  - `PaginationOptionsValidator.cs`: DefaultPageSize >= 1, MaxPageSize >= 1, DefaultPageSize <= MaxPageSize, PageNumberBase >= 0, SuccessStatusCode within 2xx, ParameterNames non-null with all four entries non-empty.
  - `CorrelationOptionsValidator.cs`: HeaderName non-empty, ValidationPattern non-empty and a valid regex, InvalidIncomingIdPolicy a defined enum value.
  - `ContentNegotiationOptionsValidator.cs`: AcceptableResponseMediaTypes non-empty with valid media-type syntax, AcceptableRequestMediaTypes non-empty with valid media-type syntax, BodyCarryingMethods non-empty with valid HTTP-method tokens, and the AcceptWildcard-false-with-empty-response-set contradiction.
  - `ApiExceptionOptionsValidator.cs`: Mappings non-empty with no null entries and no null ExceptionType, Code, or empty SafeMessage; ErrorCodeToStatusMap keys non-empty and values within 100..599.
  - `ApiPilotJsonOptionsValidator.cs`: MaxDepth >= 1, EnumMode a defined value, DateMode a defined value, ReadCommentHandling a defined value, DefaultIgnoreCondition a defined value.

- Six consumer migrations from the concrete options type to IOptions<T>:
  - `AspNetCore/Middleware/ApiPilotExceptionMiddleware.cs`
  - `AspNetCore/ExceptionHandling/DefaultApiExceptionMapper.cs`
  - `AspNetCore/Middleware/ApiPilotCorrelationMiddleware.cs`
  - `AspNetCore/Middleware/ApiPilotContentNegotiationMiddleware.cs`
  - `AspNetCore/EndpointMetadata/PaginationEndpointFilter.cs`
  - `AspNetCore/EndpointMetadata/PaginationEndpointExtensions.cs` (dereference removed; passes IOptions<T> to the filter)

- The five extension registrations migrated to the options pipeline:
  - `Middleware/ApiPilotExceptionExtensions.cs`
  - `Middleware/ApiPilotCorrelationExtensions.cs`
  - `Middleware/ApiPilotContentNegotiationExtensions.cs`
  - `DependencyInjection/ApiPilotServiceCollectionExtensions.cs` (AddApiPilotPagination only)
  - `Serialization/JsonSerializationExtensions.cs` (both the IMvcBuilder and IServiceCollection overloads)

- Tests added in Phase 1.8 (35 new tests across 6 new test classes):
  - `Configuration/PaginationOptionsValidatorTests.cs` (8): defaults pass, each rule fires on the corresponding invalid configuration.
  - `Configuration/CorrelationOptionsValidatorTests.cs` (5): defaults pass, empty HeaderName fails, invalid regex fails, valid custom regex passes, undefined enum fails.
  - `Configuration/ContentNegotiationOptionsValidatorTests.cs` (7): defaults pass, empty response set fails, empty request set fails, invalid media type fails, empty body-carrying method set fails, invalid HTTP method token fails, wildcard-false-with-empty-response-set fails.
  - `Configuration/ApiExceptionOptionsValidatorTests.cs` (6): defaults pass, empty mappings fail, null code fails, empty safe message fails, out-of-range status map value fails, empty status map key fails.
  - `Configuration/ApiPilotJsonOptionsValidatorTests.cs` (6): defaults pass, MaxDepth zero fails, undefined EnumMode fails, undefined DateMode fails, undefined ReadCommentHandling fails, undefined DefaultIgnoreCondition fails.
  - `Integration/ConfigurationValidationIntegrationTests.cs` (3): a valid configuration starts the host, an invalid pagination configuration applied through the public AddApiPilotPagination delegate throws at host start and names the misconfigured property, an invalid correlation regex applied through AddApiPilotCorrelation throws and names ValidationPattern. The tests exercise the full DI chain through InProcessHost and prove the fail-closed property end to end.

- Test count: 434 at Phase 1.7 close. 469 at Phase 1.8 close. Delta: +35.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 223 -> 258 (+35).

- The fail-closed startup property is proven. An application that configures an invalid value through any public AddApiPilot* delegate fails to start, and the failure message names the specific misconfigured property. The validator runs against the same options instance the middleware receives (A-122 closure).

### Findings from the build (Phase 1.8)

- **A-122** (Phase 1.8): Every AddApiPilot* extension built a manual singleton and registered IOptions<T> via OptionsWrapper<T>. The options pipeline never saw the configured instance. ValidateOnStart would have validated a default instance and passed a misconfigured application at startup. Fixed by migrating every extension to AddOptions<T>().Configure(configure).ValidateOnStart() and every consumer to IOptions<T>. This closes the residual A-073 bridge. Proven by the new configuration validation integration tests.
- **A-123** (Phase 1.8, process): A PowerShell block over ~250 lines with nested control flow can be truncated by the paste channel. Paste 2 (the six consumer migrations) was truncated mid-execution, PowerShell entered continuation mode, and no WriteAllLines call ran. Nothing was corrupted. Recovery required a read-only state-check to confirm the six target files were untouched. Rule: one file per block, under 200 lines, for both reads and writes. This rule had already been recorded at A-105/A-106 for source writes; Phase 1.8 proved it applies to read-backs and to multi-file migration blocks.
- **A-124** (Phase 1.8): The Paste 1 skip count for ApiPilotContentNegotiationExtensions.cs was $i += 5, but the old block was 6 lines. The sixth line ("new OptionsWrapper<ContentNegotiationOptions>(options));") was not skipped and became a stray line after the TryAddEnumerable call. The compiler reported CS1002 and CS1513. Fixed by removing the stray line. Lesson: the $i += N skip count must equal the exact number of physical lines in the block being replaced, not the number of logical lines.
- **A-125** (Phase 1.8): CA2263 on Enum.IsDefined(typeof(T), value). The analyzer prefers the generic overload Enum.IsDefined(value). Fixed at five call sites across CorrelationOptionsValidator and ApiPilotJsonOptionsValidator. Same class as A-058 (prefer AsSpan over Substring): the analyzer prefers a modern BCL overload.
- **A-126** (Phase 1.8): The migrated ApiPilotCorrelationExtensions.cs and ApiPilotContentNegotiationExtensions.cs referenced the new validators in ApiPilot.AspNetCore.Configuration without importing that namespace. Two CS0246 errors. Fixed by adding using ApiPilot.AspNetCore.Configuration; in alphabetical position. Same class as A-024, A-025, A-063, A-089.
- **A-127** (Phase 1.8, process): The pre-write checklist for the Phase 1.8 migration did not enumerate the existing test call sites that would break when constructor signatures changed. Twelve CS1503 errors resulted across six test files. The checklist correction: any migration that changes a constructor signature must, in the same pre-write checklist, enumerate every existing test call site that will break. The correct shape is a grep for new XxxType( across the test project before the source edit is written.
- **A-128** (Phase 1.8, process): A six-file read-back block was truncated by the paste channel in the same way a multi-file write block would be. The output arrived for only one file. Rule: one file per block applies to reads as well as writes.
- **A-129** (Phase 1.8): In Windows PowerShell 5.1, an if (...) { ... } block followed by else { ... } on a new line is a parse error. The else must be on the same line as the closing }, or the code must use two separate top-level if statements (one negated), or an early return at a scope where return behaves correctly. Fixed by restructuring the affected test-migration blocks into two separate top-level ifs. This is a new addition to the PowerShell trap list.
- **A-130** (Phase 1.8): The Options.Create(...) wrap breaks any subsequent configure?.Invoke(options) call, because the delegate expects T, not IOptions<T>. The correct pattern is to invoke the delegate against a concrete instance first, then wrap: var concrete = new T(); configure?.Invoke(concrete); var wrapped = Options.Create(concrete);. The initial test migrations in ApiPilotExceptionMiddlewareTests and DefaultApiExceptionMapperTests used the wrong order; the build caught the type mismatch (CS1503 on configure?.Invoke(options)). Fixed by restructuring the helper.
- **A-131** (Phase 1.8): JsonCommentHandling is a byte-backed enum (public enum JsonCommentHandling : byte). The test that asserts an undefined value using (JsonCommentHandling)999 fails at compile time with CS0221 ("Constant value 999 cannot be converted to a JsonCommentHandling"). Fixed by using (JsonCommentHandling)200, which is within byte range and not a defined member. Lesson: any "undefined enum value" test must use a value within the enum underlying type range. For byte-backed enums that is 0..255; for int-backed enums 999 is fine.
- **A-132** (Phase 1.8): PaginationOptions.ParameterNames initialized to the shared static QueryParameterNames.Default. Any caller that mutated options.ParameterNames.Page = "" on a default-constructed PaginationOptions mutated the process-wide singleton. Tests running later in the same process saw the corrupted default; fifteen downstream tests failed with "PaginationOptions.ParameterNames.Page must not be empty". Fixed by changing the initializer to new() so each PaginationOptions owns its own QueryParameterNames. The QueryParameterNames.Default static remains as a documented convenience but is no longer used as an initializer. The validator caught this defect at exactly the layer it was designed to protect, which is the intended fail-closed behavior.

### Added (Phase 1.7)

- Content negotiation integration in ApiPilot.AspNetCore and one addition in ApiPilot.Core:
  - `Core/Configuration/ContentNegotiationOptions.cs`: mutable options holder carrying the Option D surface for the middleware. AcceptableResponseMediaTypes (default application/json), AcceptWildcard (default true), AcceptableRequestMediaTypes (default application/json), AcceptMissingContentType (default false), BodyCarryingMethods (default POST, PUT, PATCH).
  - `Core/Errors/ApiErrorCode.cs` (extended): added `NotAcceptable` (wire code NOT_ACCEPTABLE) and `UnsupportedMediaType` (wire code UNSUPPORTED_MEDIA_TYPE). The standard error code set grows from twelve to fourteen.
  - `AspNetCore/Results/ErrorResponseResult.cs` (extended): two new arms in BuiltInMapErrorCode mapping NOT_ACCEPTABLE to 406 and UNSUPPORTED_MEDIA_TYPE to 415.
  - `AspNetCore/Middleware/ApiPilotContentNegotiationMiddleware.cs`: rejects requests whose Accept header does not include an acceptable response media type with 406, and requests whose Content-Type header on a body-carrying method is not acceptable with 415. Both use the standard error envelope. The middleware resolves the correlation ID from ICorrelationIdAccessor with TraceIdentifier as fallback (A-118).
  - `AspNetCore/Middleware/ApiPilotContentNegotiationMiddleware.LogMessages.cs`: two LoggerMessage.Define delegates (EventId 3001 for 406, 3002 for 415). Follows the partial-class precedent from Phase 1.3 and Phase 1.6.
  - `AspNetCore/Middleware/ApiPilotContentNegotiationExtensions.cs`: `AddApiPilotContentNegotiation(Action<ContentNegotiationOptions>?)` registers the concrete ContentNegotiationOptions singleton and an OptionsWrapper<ContentNegotiationOptions> bridge (matching the A-073 pattern). `UseApiPilotContentNegotiation()` adds the middleware to the pipeline.

- The Accept header check recognizes the full wildcard */* (when AcceptWildcard is true) and each configured acceptable response media type. Quality parameters (;q=) are ignored: the check is presence, not preference. A missing Accept header is acceptable per RFC 7231. Type-level wildcards (application/*) are out of scope for Phase 1.7 and documented as a deliberate simplification (A-119).

- The Content-Type header check runs only for methods in BodyCarryingMethods (default POST, PUT, PATCH). A missing Content-Type on those methods is rejected by default; AcceptMissingContentType = true accepts it. Header parameters after the first semicolon are ignored.

- SPEC.md updated: two new rows in the error code table (Unacceptable Accept -> 406 -> NOT_ACCEPTABLE, Unsupported content type -> 415 -> UNSUPPORTED_MEDIA_TYPE), plus six new bullets in the Content negotiation section documenting the defaults and the scope of the wildcard handling.

- Tests added in Phase 1.7 (31 new tests across 3 new test classes):
  - `Configuration/ContentNegotiationOptionsTests.cs` (6): defaults for the five transformations, mutability.
  - `Middleware/ApiPilotContentNegotiationMiddlewareTests.cs` (18): no-Accept pass-through, Accept json pass-through, wildcard pass-through, Accept xml 406, wildcard-disabled 406, quality-parameter pass-through, custom media type pass-through, POST no Content-Type 415, POST no Content-Type with AcceptMissingContentType true, POST json pass-through, POST xml 415, POST json with charset pass-through, GET no Content-Type pass-through, 406 envelope uses correlation accessor when supplied, 406 envelope falls back to TraceIdentifier when no accessor, three constructor null rejections.
  - `Integration/ContentNegotiationIntegrationTests.cs` (7): no Accept 200, Accept json 200, Accept xml 406, **Accept xml 406 with correlation ID cross-checked in both response header and JSON body meta**, POST no Content-Type 415, POST json 200, POST xml 415.

- Test count: 403 at Phase 1.6 close. 434 at Phase 1.7 close. Delta: +31.
  - `Core.Tests`: 211 (unchanged; the two new error codes extend an existing test, adding no new test method).
  - `AspNetCore.Tests`: 192 -> 223 (+31).

- The content negotiation surface is proven robust enough for professional use. An application can configure the acceptable response types, wildcard behavior, acceptable request types, missing-Content-Type policy, and body-carrying method set without forking the library.

### Findings from the build (Phase 1.7)

- **A-118** (Phase 1.7): The initial content negotiation middleware plan built the error envelope with `httpContext.TraceIdentifier` directly, bypassing `ICorrelationIdAccessor`. This is a direct regression of A-040, which was resolved in Phase 1.6 for the exception middleware. Caught by external review of the pre-write checklist. Fixed by adding `ICorrelationIdAccessor? correlationAccessor = null` to the middleware constructor and building the envelope from `accessor?.RequestId ?? TraceIdentifier`. Two middleware tests and one integration test verify the fix. The integration test sends `X-Request-Id`, triggers 406, and asserts the ID matches in both the response header and the JSON body meta - this is the test that would have caught A-040 the first time. **The defect class is now proven systemic: any component that writes a response envelope must resolve the correlation ID through the accessor. This is captured as A-120.**
- **A-119** (Phase 1.7, scope decision): The Accept header check recognizes only the full wildcard `*/*`. Type-level wildcards such as `application/*` are out of scope for Phase 1.7. Documented in the middleware XML doc and in the SPEC.md Content negotiation section. The simplification matches the "presence, not preference" approach: the check decides whether a request is acceptable, not which of several acceptable types the client would prefer. A future bug report about `application/*` rejection will be treated as a feature request, not a defect.
- **A-120** (Phase 1.7, discipline): The pre-write checklist for any new component that writes a response envelope must include three cross-cutting concerns: (1) the correlation accessor path (`ICorrelationIdAccessor? correlationAccessor = null` in the constructor, envelope built from `accessor?.RequestId ?? TraceIdentifier`); (2) the `EchoInResponseBody` path (envelope RequestId set to string.Empty when the option is false); (3) the `ErrorResponseResult` serialization path (the envelope is serialized through ErrorResponseResult, not by hand-rolled JsonSerializer.SerializeAsync). If any of the three is missing, the PR is incomplete. This generalizes A-118 and A-109.
- **A-121** (Phase 1.7): A raw blank line inside an XML doc `<remarks>` or `<summary>` block terminates the block, producing CS1570 ("XML comment has badly formed XML - Expected an end tag for element 'remarks'"). Both the middleware and the extensions file initially contained a raw blank line inside the remarks block. Fixed by using `///` alone for internal blank lines. Lesson: XML doc blocks are contiguous sequences of `///` lines. Internal blank lines must be `///` alone.

### Added (Phase 1.6)

- Correlation middleware in ApiPilot.AspNetCore and one addition in ApiPilot.Core:
  - `Core/Metadata/CorrelationInvalidIdPolicy.cs`: three-state enum (Replace, Reject, UseAsIs) describing what the middleware does when an incoming correlation ID is present but invalid.
  - `Core/Metadata/CorrelationOptions.cs` (extended): added `InvalidIncomingIdPolicy`. Default is Replace (the enum zero member; no initializer needed, avoiding CA1805).
  - `AspNetCore/Middleware/ApiPilotCorrelationMiddleware.cs`: reads the incoming ID from the configured header, validates it against the configured regex pattern, generates a new GUIDv7 when absent or invalid, stores the effective ID on `HttpContext.Items` under a well-known key, echoes the ID in the response header when configured, and calls the next middleware. The validator is constructed once in the constructor from the configured pattern and is not rebuilt per request.
  - `AspNetCore/Middleware/ApiPilotCorrelationMiddleware.LogMessages.cs`: two `LoggerMessage.Define` delegates for the invalid-ID warning (EventId 2001) and the reject warning (EventId 2002). Follows the ApiPilotExceptionMiddleware partial-class precedent.
  - `AspNetCore/Middleware/HttpContextCorrelationIdAccessor.cs`: ASP.NET Core implementation of `ICorrelationIdAccessor` that reads the stored ID from `HttpContext.Items` via `IHttpContextAccessor`. Returns null when there is no current HttpContext or no stored value.
  - `AspNetCore/Middleware/ApiPilotCorrelationExtensions.cs`: `AddApiPilotCorrelation(Action<CorrelationOptions>?)` registers the concrete `CorrelationOptions` singleton, an `OptionsWrapper<CorrelationOptions>` bridge (matching the AddApiPilotExceptions pattern from A-073), the default generator, the HttpContext-based accessor, and the `IHttpContextAccessor` if not already registered. `UseApiPilotCorrelation()` adds the middleware to the pipeline.

- Modification in ApiPilot.AspNetCore for A-040 and the EchoInResponseBody wiring:
  - `AspNetCore/Middleware/ApiPilotExceptionMiddleware.cs`: constructor takes two new optional parameters (`ICorrelationIdAccessor?` and `IOptions<CorrelationOptions>?`). The new private method `ResolveRequestId` reads the correlation ID from the accessor when available, falls back to `HttpContext.TraceIdentifier` when not, and returns `string.Empty` when `EchoInResponseBody` is false. Resolves A-040 and wires the EchoInResponseBody flag (A-109).
  - `AspNetCore/Validation/ApiPilotValidationFilter.cs`: constructor takes an optional `IOptions<CorrelationOptions>?` parameter. `OnActionExecuting` reads `EchoInResponseBody` and sets the response meta RequestId accordingly.
  - `AspNetCore/Validation/ApiPilotInvalidModelStateResponseFactory.cs`: reads `IOptions<CorrelationOptions>` from `RequestServices` and sets the response meta RequestId accordingly.
  - `AspNetCore/Validation/ApiPilotValidationEndpointFilter.cs`: reads `IOptions<CorrelationOptions>` from `RequestServices` and sets the response meta RequestId accordingly.

- The three-state CorrelationInvalidIdPolicy closes the invalid-incoming-ID transformation: Replace (default), Reject (return 400 VALIDATION_ERROR), UseAsIs (keep the invalid ID and log a warning).

- Tests added in Phase 1.6 (29 new tests across 5 new test classes):
  - `Middleware/ApiPilotCorrelationMiddlewareTests.cs` (15): no-header generation, valid header passthrough, invalid header replacement, Reject policy 400 response, Reject policy Warning log, UseAsIs policy, EchoInResponseHeader true and false, ValidateIncoming false, context storage, custom header name, custom generator, three constructor null rejections.
  - `Middleware/HttpContextCorrelationIdAccessorTests.cs` (4): no context, no stored value, stored string value, stored non-string value.
  - `Middleware/CorrelationOptionsEchoInResponseBodyTests.cs` (3): EchoInResponseBody true populates the meta requestId, EchoInResponseBody false produces an empty requestId, no correlation registration falls back to TraceIdentifier.
  - `Integration/CorrelationIntegrationTests.cs` (6): header echo when generated, header echo when incoming is valid, default policy replacement of an invalid header, Reject policy 400 through real HTTP, meta.requestId in the response body, custom header name.
  - `Integration/ApiPilotExceptionMiddlewareIntegrationTests.cs` (1): the exception middleware handles exceptions even without AddApiPilotCorrelation being called (proves the works-without-registration contract holds through UseMiddleware).

- Test count: 374 at Phase 1.5 close. 403 at Phase 1.6 close. Delta: +29.
  - `Core.Tests`: 211 (unchanged).
  - `AspNetCore.Tests`: 163 -> 192 (+29).

- Every correlation transformation now has a sensible default and a first-class override: header name, validation pattern, generator, EchoInResponseBody, EchoInResponseHeader, ValidateIncoming, InvalidIncomingIdPolicy. Every override is wired and tested.

### Findings from the build (Phase 1.6)

- **A-109** (Phase 1.6): `CorrelationOptions.EchoInResponseBody` had no consumer between Phase 0.6 (when it was introduced) and Phase 1.6 (when it is wired). The property was documented as an override but no code path read it. Wired in Phase 1.6 across five call sites: the exception middleware, the correlation middleware's own Reject path, the MVC validation filter, the [ApiController] response factory, and the minimal API validation endpoint filter. Same class as A-098 and A-108: an override declared and layered but never consulted by a consumer. Caught by external review of the Phase 1.6 pre-write checklist. **Systemic lesson: the "override declared but not consumed" defect class has now been caught three times in this project (A-098, A-108, A-109), twice by external review. The pre-write checklist for any future phase must include, for every override property in the scope of that phase, a one-line reference to the exact code path that reads it. If no path reads it, the override is either wired now or removed. There is no "later."**
- **A-110** (Phase 1.6, deferred): `ResponseMetadata.RequestId` uses `string.Empty` as the "not echoed" sentinel when `EchoInResponseBody` is false. A cleaner design would make the property nullable (`string?`) with `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` so the requestId key is omitted entirely from the wire. Deferred to a documentation-alignment block. The current `string.Empty` behavior is visible and unambiguous but the JSON shows `"requestId": ""` rather than omitting the key.
- **A-111** (Phase 1.6): Three `LoggerMessage.Define` call sites in `ApiPilotCorrelationMiddleware.cs` omitted the trailing `Exception?` argument, producing CS7036. The compiler message was literal ("no argument given that corresponds to the required parameter 'arg5'/'arg6'"). Fixed by adding `, null` at the three call sites. Lesson: every call site of a `LoggerMessage.Define<T1,T2,T3,T4>` delegate must pass every argument of the `Action<ILogger, ..., Exception?>` shape, including the trailing `Exception?`. The existing `ApiPilotExceptionMiddleware.LogSafely` correctly passes the exception; the new code must match the shape, not the intent.
- **A-112** (Phase 1.6, discipline): The `Test-Path`-only idempotence guard silently prevents correcting an existing file with different content. During the Phase 1.6 test batch, four test files were already on disk from a prior attempt and were skipped by the guard, hiding defects that the guard was supposed to protect against. The correct guard compares content, not just existence: read the existing file, compare its line count and each line to the intended content, skip only if identical.
- **A-113** (Phase 1.6): `CorrelationIntegrationTests.cs` was missing `using Microsoft.AspNetCore.Http;`. The type `HttpContext` was used but its namespace was not imported. CS0246. Regression of the A-024/A-025/A-063/A-089 class. Fixed by adding the using.
- **A-114** (Phase 1.6): The expression-bodied lambda `app.MapGet("/boom", () => throw new InvalidOperationException("x"))` does not bind to `RequestDelegate` in .NET 10. The parameterless lambda that only throws does not infer the delegate shape. CS1593. The block-bodied form `app.MapGet("/boom", () => { throw new InvalidOperationException("x"); })` is required. Two files affected: `CorrelationIntegrationTests.cs` and `ApiPilotExceptionMiddlewareIntegrationTests.cs`.
- **A-115** (Phase 1.6, discipline): Same as A-112, recorded separately because the guard-skip affected two files in the Phase 1.6 test batch.
- **A-116** (Phase 1.6): `Results.Text(...)` in `CorrelationIntegrationTests.cs` collided with the test project's `ApiPilot.AspNetCore.Tests.Results` namespace. Inside `ApiPilot.AspNetCore.Tests.Integration`, the identifier `Results` resolves first to the enclosing namespace subtree, not to `Microsoft.AspNetCore.Http.Results`. CS0234. Fixed by returning a plain string from the endpoint instead of using `Results.Text`. Lesson: when a test project mirrors the production folder structure, test namespaces can collide with BCL types (`Results`, `Configuration`, `Validation`, `Metadata`, `Serialization`). Prefer plain returns over `Results.*` helpers in test endpoint handlers, or fully qualify the type.
- **A-117** (Phase 1.6): The correlation middleware tests' `Context()` helper constructed an empty `ServiceCollection` without registering `AddApiPilotJson()`. The two Reject-policy tests invoke `ErrorResponseResult.ExecuteAsync`, which resolves `IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>` from `httpContext.RequestServices` - a service that only `AddApiPilotJson()` registers. The tests failed at runtime with `InvalidOperationException: No service for type 'IOptions<JsonOptions>' has been registered`. Fixed by registering `AddApiPilotJson()` in the `Context()` helper. Lesson: any unit test that causes a response envelope to be written via `ErrorResponseResult.ExecuteAsync` (or any of the other result classes) must register `AddApiPilotJson()` in its service provider. The existing `ApiPilotExceptionMiddlewareTests.Context()` helper does this correctly; the pattern must be copied into every new test file that exercises response writing.

### Added (Phase 1.5)

- Pagination integration in ApiPilot.AspNetCore and additions in ApiPilot.Core:
  - `Core/Pagination/QueryParameterNames.cs`: configurable names for the four control query parameters (page, pageSize, sort, direction). Default instance plus settable properties.
  - `Core/Pagination/PaginationOptions.cs` (extended): added `ParameterNames`, `PageNumberBase`, `SortDirectionParser`, `SortParser`, `IsFilterParameter`, `IntegerParser`, and `SuccessStatusCode`. Every transformation in the pagination filter is overridable through a property on this type.
  - `Core/Pagination/PagedResultBuilder.cs`: static factory that assembles a `PagedResult<T>` from items and page dimensions. Validates via `PaginationMetadata.Create`.
  - `Core/Responses/PagedApiResponse.cs`: the paginated success envelope. Wire shape matches SPEC.md: `success`, `data` (flat array), `pagination`, `meta`. No `Status` property; paginated responses are always successful.
  - `AspNetCore/Configuration/PaginationOverrides.cs`: per-endpoint mirror of the override surface. Nullable fields; null means the global value applies. Properties use settable accessors so the fluent callback can populate them.
  - `AspNetCore/Configuration/PaginationResolver.cs`: `Resolve` layers overrides on the global options. `FromAttribute` translates a `PaginationMetadataAttribute` into a `PaginationOverrides` (primitives only). `MergeOverrides` composes the fluent and attribute sources, attribute winning for primitives and fluent supplying the delegate and object fields.
  - `AspNetCore/EndpointMetadata/PaginationMetadataAttribute.cs`: attribute-driven per-endpoint overrides for primitives. `DefaultPageSize`, `MaxPageSize`, and `SuccessStatusCode` use -1 as a "not set" sentinel. `PageNumberBase` uses -1. `StrictQueryValidation` uses `TriState` to distinguish unset from explicit false or true.
  - `AspNetCore/EndpointMetadata/TriState.cs`: three-state enum (Unset, False, True) for attribute properties that must distinguish unset from an explicit boolean value.
  - `AspNetCore/EndpointMetadata/PaginationEndpointFilter.cs`: minimal API endpoint filter that reads the effective options (global + fluent + attribute), parses and validates pagination query parameters, and either short-circuits with the standard VALIDATION_ERROR envelope or stores the validated `PageRequest`, `SortRequest`, and `FilterRequest` on the HttpContext. The filter stores the effective `PaginationOptions` under `EffectiveOptionsKey` for downstream consumers.
  - `AspNetCore/EndpointMetadata/PaginationHttpContextExtensions.cs`: HttpContext extension methods `GetPageRequest`, `GetSortRequest`, `GetFilterRequest`, and `GetEffectivePaginationOptions`. Each returns null when the filter did not run for the current request.
  - `AspNetCore/EndpointMetadata/PaginationEndpointExtensions.cs`: fluent extension method `WithApiPilotPagination(Action<PaginationOverrides>?)` that attaches the filter and stores the per-endpoint overrides on the endpoint metadata.
  - `AspNetCore/Results/PagedResponseResult.cs`: `IResult` and `IActionResult` that writes a `PagedApiResponse<T>` to the HTTP response. The status code is read from the effective pagination options resolved for the request (via `GetEffectivePaginationOptions`), falling back to the DI-registered global `SuccessStatusCode` when the filter did not run.
  - `AspNetCore/Results/ApiPilotResultsExtensions.cs` (extended): added `ToPagedResult<T>(this PagedResult<T>, ResponseMetadata)` and `ToResult<T>(this PagedApiResponse<T>)`.
  - `AspNetCore/DependencyInjection/ApiPilotServiceCollectionExtensions.cs` (extended): added `AddApiPilotPagination(Action<PaginationOptions>?)` that registers `PaginationOptions` with the DI container.

- Wire-format corrections:
  - `[JsonIgnore]` applied to `ApiResponse.Status` and `ApiResponse<T>.Status`. The transport-neutral Status property is used in-process for HTTP status mapping; it is no longer serialized on the wire. This aligns the serialized success envelope with SPEC.md, which does not document a `status` key.
  - The fluent and attribute override paths for `SuccessStatusCode` now reach the response via the effective options stored on `HttpContext.Items`.

- Validation-filter and exception-options corrections:
  - `AddApiPilotExceptions` now registers an `OptionsWrapper<ApiExceptionOptions>` in addition to the concrete singleton. The validation filter and the `[ApiController]` response factory, which resolve `IOptions<ApiExceptionOptions>`, now receive the same configured instance as the exception middleware. The `ErrorCodeToStatusMap` override reaches all three call sites.

- Tests added in Phase 1.5 (78 new tests across 8 new test classes):
  - `Core.Tests/Pagination/QueryParameterNamesTests.cs` (7): defaults, mutability.
  - `AspNetCore.Tests/Configuration/PaginationOverridesTests.cs` (9): nullable defaults, settability, delegate fields.
  - `AspNetCore.Tests/Configuration/PaginationResolverTests.cs` (27): per-override default and override tests for every field; `FromAttribute` translation including the `TriState` True and False cases; `MergeOverrides` null, only-fluent, only-attribute, and composed cases.
  - `AspNetCore.Tests/EndpointMetadata/PaginationEndpointFilterTests.cs` (15): parsing, validation, short-circuit, context storage, custom parsers, custom filter predicate, strict-mode rejection, per-endpoint attribute override, fluent override via endpoint metadata, fluent and attribute composition.
  - `AspNetCore.Tests/Integration/PaginationIntegrationTests.cs` (8): real HTTP through `InProcessHost` for the full envelope shape, default page size, custom parameter names, custom success status code, invalid page, page size above max, per-endpoint max page size, and context accessor.
  - `AspNetCore.Tests/Integration/PaginationMetadataAttributeIntegrationTests.cs` (3): attribute override through real HTTP; attribute plus fluent composition; endpoint without filter is unaffected.
  - `AspNetCore.Tests/Integration/ApiExceptionOptionsResolutionIntegrationTests.cs` (2, added during the A-073 correction): a custom `ErrorCodeToStatusMap` reaches both the filter path and the `[ApiController]` path.

- Test count: 296 at Phase 1.4 close. 374 at Phase 1.5 close. Delta: +78.
  - `Core.Tests`: 204 -> 211 (+7).
  - `AspNetCore.Tests`: 101 -> 163 (+62).

- The pagination pipeline is proven end to end through real HTTP for both the minimal API path and the attribute path, and the fluent and attribute override sources compose correctly.

### Findings from the build (Phase 1.5)

- **A-072a** (Phase 1.5): The `ApiResponse<T>.Status` property was serialized on the wire as a `status` key. SPEC.md does not document a `status` field on the success envelope. The defect was latent since Phase 0.2 and confirmed by probe against the running library. Fixed by adding `[JsonIgnore]` to `Status` on `ApiResponse` and `ApiResponse<T>`. A new snapshot test asserts that `status` is absent from the serialized success envelope. Lesson: every value object on the wire needs a serialization test; the existing snapshot tests asserted the presence of expected keys but not the absence of unintended ones.
- **A-072b/c** (Phase 1.5, deferred): `message: null` is always present on the wire (SPEC.md marks `message` as optional), and `meta.timestamp` and `meta.extra` are always present (SPEC.md shows only `meta.requestId`). Deferred to a documentation-alignment block; recorded so it is not lost.
- **A-073** (Phase 1.5): `ApiPilotValidationFilter` and `ApiPilotInvalidModelStateResponseFactory` resolved `IOptions<ApiExceptionOptions>` which, after `AddApiPilotExceptions` registered only a concrete singleton, returned a fresh default rather than the configured instance. The `ErrorCodeToStatusMap` override configured via `AddApiPilotExceptions` never reached either validation path. Confirmed by probe. Fixed by adding an `OptionsWrapper<ApiExceptionOptions>` registration in `AddApiPilotExceptions` that bridges the concrete singleton to the `IOptions<T>` resolution path. Two new integration tests verify the override reaches both paths. Lesson: a value object consulted through two different DI resolutions must be guaranteed to resolve to the same instance.
- **A-074** (Phase 1.5): `PaginationOptions.StrictQueryValidation` had a redundant `= false` initializer, triggering CA1805. This is a regression of A-035. Fixed by removing the initializer. Lesson: consult A-035 before writing any bool property.
- **A-075** (Phase 1.5): `PaginationResolver.cs` referenced `PaginationMetadataAttribute` without importing `ApiPilot.AspNetCore.EndpointMetadata`. CS0246. Regression of A-024/A-025 class. Fixed by adding the using. Lesson: enumerate every type referenced by a file and confirm its namespace is either the file's own or imported.
- **A-076** (Phase 1.5): `PaginationMetadataAttribute.StrictQueryValidation` had a redundant `= false` initializer. Same class as A-074 and A-035. Fixed by removing. Lesson: when a defect class is confirmed in one file, grep the entire tree for that exact class before the next build.
- **A-077** (Phase 1.5): A tree-wide grep for redundant bool initializers matched both `= false` (defect) and `= true` (intentional). `= true` on bool is a meaningful declaration (differs from CLR default). `= false` is the defect. Lesson: the grep must distinguish the actual defect pattern from lookalikes.
- **A-078** (Phase 1.5, discipline): When a defect of a specific class is confirmed in one file, grep the entire tree for that exact class before the next build. This generalizes A-064.
- **A-079** (Phase 1.5): Index-shift arithmetic in a splice block placed `[JsonIgnore]` after the `Status` property in two files instead of before it. Fixed by same-iteration insertion with no index arithmetic.
- **A-080** (Phase 1.5): A here-string anchor in the `ApiPilotInvalidModelStateResponseFactory` edit did not match the file's exact bytes. Fixed by line-by-line anchor matching.
- **A-081** (Phase 1.5): A last-brace insertion placed a new test method inside the `SampleOrder` DTO class instead of the `JsonSerializationSnapshotTests` test class. Fixed by locating the test class's specific close brace before insertion.
- **A-082** (Phase 1.5): Changing `ApiPilotValidationFilter` from `IOptions<ApiExceptionOptions>` to concrete `ApiExceptionOptions` broke the works-without-registration contract that the filter's own XML doc documents. Six tests caught it. Fixed by reverting the resolution to `IOptions<>` and bridging via `OptionsWrapper` in `AddApiPilotExceptions`. Lesson: changing a dependency's type changes its resolvability contract; `IOptions<T>` is always resolvable, a concrete `T` is only resolvable when registered.
- **A-083** (Phase 1.5): `AddApiPilotJson(IServiceCollection)` configures only `Http.Json.JsonOptions`, not MVC JsonOptions. Not a defect now (the result classes write their own bytes and do not use MVC's serializer) but a documentation gap. Recorded for future cleanup.
- **A-084** (Phase 1.5): `PagedApiResponse<T>.From` was a static factory on a generic type, triggering CA1000. Fixed by removing the static method and inlining the construction at the single call site. Lesson: no static members on generic types; factories belong on non-generic helpers or inline.
- **A-085** (Phase 1.5, discipline): Any loop that needs to insert multiple lines or skip lines must be a two-pass approach or a full-file rewrite. Never mutate the loop index.
- **A-086** (Phase 1.5): An orphan-brace anchor pattern in `PaginationResolver` matched an unintended location, inserting the missing if-statement at line 54 instead of its correct position. Fixed by full-file rewrite.
- **A-087** (Phase 1.5): `PagedResponseResult<T>` hardcoded HTTP 200. The success status is a legitimate application choice (200 vs. 206 Partial Content per RFC 7233). Fixed by adding `PaginationOptions.SuccessStatusCode` and `PaginationOverrides.SuccessStatusCode` with the default of 200.
- **A-088** (Phase 1.5): `PaginationMetadataAttribute.StrictQueryValidation` as bool could not distinguish unset from explicit false, so an unset attribute could not override a global true back to false. Fixed by introducing the `TriState` enum (Unset, False, True).
- **A-089** (Phase 1.5): `PaginationEndpointExtensions.cs` was missing `using Microsoft.AspNetCore.Http` for the `AddEndpointFilterFactory` extension method. CS1061. Fixed by adding the using. The pre-write checklist must name extension methods, not just types.
- **A-090** (Phase 1.5): A test file write failed because the parent directory did not exist. `File.WriteAllLines` does not create the directory. Same class as A-051. Fixed by creating the directory first. Lesson: every write block to a possibly-new directory must include the directory-creation guard.
- **A-091** (Phase 1.5): `PaginationEndpointFilterTests.cs` appeared to be missing from the test run. Root cause: the file failed to compile because of a CA1866/CA1310 analyzer error on `field.StartsWith("-")`. The build reported success earlier because the file was not on disk at that build; the test runner used a stale DLL. Fixed by `field.StartsWith('-')` and a fresh build. Lesson: post-write verification must include a fresh build before the test run; the build output must be read line-by-line for failures, not just the exit code.
- **A-092** (Phase 1.5, discipline): The pre-write checklist for a batch of tests must specify which test project each file goes into, and the post-write verification must count tests in both projects.
- **A-093** (Phase 1.5): Same as A-091, recorded separately: a compile error in a newly added file is invisible if the test run uses a stale DLL.
- **A-094** (Phase 1.5, discipline): Always build fresh before running tests. Never trust `dotnet run --no-build` after adding a new file.
- **A-095** (Phase 1.5, discipline): The pre-write checklist's test count must match the write block's `[Test]` attribute count.
- **A-096** (Phase 1.5): `PaginationOptions.SortDirectionParser` was declared and layered by the resolver but never consulted by `PaginationEndpointFilter.InvokeAsync`. The filter used `SortParser` only, and the default `SortParser` inlined the direction parsing rather than delegating to `SortDirectionParser`. The D.1 unit test caught it. Fixed by building the default sort parser from the supplied `SortDirectionParser` when present.
- **A-097** (Phase 1.5): Strict-mode unknown-key rejection only fired when `IsFilterParameter` returned false. When no predicate was supplied, lenient behavior was used regardless of strict mode. The D.1 unit test caught it. Fixed by treating null predicate plus strict mode as "reject all non-control keys."
- **A-098** (Phase 1.5): The fluent extension `WithApiPilotPagination` stored a `PaginationOverrides` object as endpoint metadata, but `PaginationEndpointFilter.InvokeAsync` read only `PaginationMetadataAttribute`. The fluent override path was dead. Discovered during the D.2 pre-write checklist; not caught by D.1 because D.1 called `InvokeAsync` directly with options and bypassed the metadata-storage path. Fixed by `PaginationResolver.MergeOverrides` and by reading both metadata sources in the filter.
- **A-099** (Phase 1.5, discipline): When a component reads two input sources (constructor options and endpoint metadata), the tests must exercise both sources. Testing one source does not exercise the other.
- **A-100** (Phase 1.5, discipline): The write block for tests must include any helper-signature changes the new tests require. A test block that calls a parameter that does not exist on the current helper will not compile.
- **A-101** (Phase 1.5): A PowerShell splice routine used `New-Object System.Collections.Generic.List<string>` (angle brackets), which PowerShell 5.1 does not parse as a generic type. The call failed; the routine continued past the exception and still called `WriteAllLines` with a stale `$out`, corrupting the file. Lesson: use `[System.Collections.Generic.List[string]]::new()` or a plain array.
- **A-102** (Phase 1.5, discipline): Splice routines must not use `New-Object` when building an output list. The correct pattern is a plain array literal, or `[System.Collections.Generic.List[string]]::new()`.
- **A-103** (Phase 1.5, discipline): Every splice routine must verify the result by re-reading the file and confirming the expected structure before declaring success. A "Wrote X" message printed unconditionally is a false success signal.
- **A-104** (Phase 1.5): PowerShell session variables persist across pastes. A stale `$out` from an earlier block was written to a file when the block's own `New-Object` failed. Fix: each write block re-reads the target file, builds a plain array, and writes with no shared variables.
- **A-105** (Phase 1.5, discipline): No more splice routines. Every edit to an existing file is a full-file rewrite with a plain array literal. The files are small enough that this is safe and deterministic.
- **A-106** (Phase 1.5, discipline): The "silent write of a stale variable" class of defect is eliminated by the full-file rewrite rule.
- **A-107** (Phase 1.5): `PaginationOverrides` had `{ get; init; }` properties, but the fluent API `WithApiPilotPagination(Action<PaginationOverrides>?)` tries to set them inside the action's body. Init-only properties cannot be set after construction. The integration test build caught it. Fixed by changing the properties to `{ get; set; }`, matching every other options type in the project. Lesson: the pre-write checklist for a fluent API must confirm the callback target's properties are settable inside a lambda.
- **A-108** (Phase 1.5): `PagedResponseResult<T>.ExecuteAsync` read `SuccessStatusCode` from the DI-registered global `PaginationOptions` only, ignoring the effective options computed by the filter. A per-endpoint `SuccessStatusCode` override was silently ignored on the response path. Detected by `PaginationIntegrationTests.Get_CustomSuccessStatusCode_ReturnsConfiguredStatus`. Fixed by reading the effective options from `HttpContext.Items` via the new `GetEffectivePaginationOptions` accessor, falling back to the DI global. Same class as A-098: an override declared and layered, but not consulted by its consumer.

## [0.1.0-alpha.1] - 2026-09-21

### Added

- Phase 0.1 scaffolding and tooling for the ApiPilot.Core package:
  - `.editorconfig` at the repository root: repository-wide editor and analyzer conventions for all file types.
  - `dotnet/Directory.Build.props`: shared MSBuild properties for every project under `dotnet/`, including target framework, language level, nullable reference types, warnings-as-errors, analyzers, documentation generation, and version sourcing from the single `VERSION` file.
  - `dotnet/src/ApiPilot.Core/`: the framework-independent core package, with folder skeleton for Responses, Errors, Validation, Pagination, Metadata, Policies, Abstractions, and Configuration.
  - `dotnet/tests/ApiPilot.Core.Tests/`: the repository-local executable test harness, with `TestAssert.cs`, `TestRunner.cs`, `Program.cs`, and a `SanityTests` class that verifies harness discovery and execution.
  - `ApiPilot.slnx` updated to reference both projects.
  - `audit.ps1` Section 5 replaced with a real, allowlist-based boundary check: no third-party `PackageReference` in production csproj, compiled ApiPilot.Core.dll may reference only allowed platform assemblies (`System.*`, `mscorlib`, `netstandard`, `Microsoft.CSharp`, `WindowsBase`), and no forbidden boundary symbols in production source.


### Added (Phase 0.2 and Phase 0.3)

#### Phase 0.2 - Response contract

- Public contract types in ApiPilot.Core:
  - `Abstractions/IApiResponse.cs`: marker interface exposing `Success` and `Meta`, implemented by all ApiPilot response envelopes.
  - `Abstractions/IApiResponseBuilder.cs`: injectable contract for building success responses. Four methods: `NoContent`, `Ok<T>`, `Created<T>`, `Accepted<T>`.
  - `Metadata/ResponseMetadata.cs`: immutable record with `RequestId`, `Timestamp` (DateTimeOffset, UTC), and `Extra` (IReadOnlyDictionary).
  - `Metadata/RequestMetadata.cs`: immutable transport-neutral record with `RequestId`, `ReceivedAt`, `Method`, `Path` (all strings).
  - `Responses/ApiResponse.cs`: non-generic success response for envelope-only results such as NoContent.
  - `Responses/ApiResponseOfT.cs`: generic success response carrying `T? Data`.
  - `Responses/ApiResponseStatus.cs`: 12-member transport-neutral enum (Ok, Created, Accepted, NoContent, BadRequest, Unauthorized, Forbidden, NotFound, Conflict, Unprocessable, TooManyRequests, ServerError).
  - `Responses/ApiResponseBuilder.cs`: `DefaultApiResponseBuilder` (sealed implementation) and `ApiResponseBuilder` (static facade delegating to a shared default instance).
- 26 tests across two test classes: `ApiResponseBuilderTests` (13 tests) and `ResponseMetadataTests` (10 tests), plus the existing 3 `SanityTests`.

#### Phase 0.3 - Error contract

- Public contract types in ApiPilot.Core:
  - `Errors/ApiErrorCode.cs`: immutable value object carrying an uppercase wire code. Twelve standard codes as static instances. `From(string)` factory normalizes to uppercase and rejects null, empty, and whitespace.
  - `Errors/ApiErrorField.cs`: immutable pairing of a field name and a snapshot of validation messages. `Create` and `WithMessage` factories. Duplicate message entries rejected. Defensive copy.
  - `Errors/ApiError.cs`: immutable error with `Code`, safe `Message`, and optional `Fields` dictionary. Empty fields collections collapse to null.
  - `Errors/ErrorResponse.cs`: error envelope. `Success` always false. Implements `IApiResponse`.
  - `Errors/IApiErrorMapper.cs`: transport-neutral contract for mapping known exceptions to `ApiError`. Implementation in Phase 1.3.
- 43 new tests in `ApiErrorCodeTests` (10), `ApiErrorFieldTests` (13), `ApiErrorTests` (12), `ErrorResponseTests` (8). Total suite: 69 tests.
- `StandardErrorCodes.cs` from the original plan was skipped. The 12 static instances on `ApiErrorCode` are the single source of truth for standard codes.


### Added (Phase 0.4 and Phase 0.5)

#### Phase 0.4 - Validation contract

- Public contract types in ApiPilot.Core:
  - `Validation/IValidationErrorSource.cs`: framework-neutral contract for supplying validation field errors. Implementations translate results from ASP.NET Core ModelState, FluentValidation, or custom validators without depending on any specific validation framework.
  - `Validation/ValidationErrors.cs`: immutable, deterministic collection of field errors. Sorts field names ordinally. Merges fields with the same name. Deduplicates messages within a merged field while preserving first-occurrence order. `With` and `WithRange` return new instances without mutating the original.
  - `Validation/ValidationResponseFactory.cs`: builds a standard `ErrorResponse` with code `VALIDATION_ERROR`. Default message `"One or more values are invalid."`. Rejects empty field sets. Provides both `FromFields` and `FromSource` overloads.
- 30 new tests in `ValidationErrorsTests` (14) and `ValidationResponseFactoryTests` (16).

#### Phase 0.5 - Pagination contract

- Public contract types in ApiPilot.Core:
  - `Pagination/PaginationMetadata.cs`: immutable record carrying Page, PageSize, and TotalItems. Derived `TotalPages` (with a minimum of 1), `HasNext`, and `HasPrevious` are computed. Private constructor and `Create` factory enforce positive page and page size, and non-negative total items.
  - `Pagination/PagedResult.cs`: generic wrapper pairing a defensive-copy snapshot of `IReadOnlyList<T>` items with `PaginationMetadata`.
  - `Pagination/PaginationOptions.cs`: mutable data holder for `DefaultPageSize` (default 20) and `MaxPageSize` (default 100). Validation happens in the ASP.NET Core options validation step.
  - `Pagination/PageRequest.cs`: validated value object for the 1-based page number and page size. `Create` and `FromQuery` factories enforce that page >= 1 and 1 <= pageSize <= `MaxPageSize`.
  - `Pagination/SortRequest.cs`: validated value object for the sort field and direction. `SortDirection` enum with `Ascending` and `Descending`. Extension methods `ToWireValue` and `TryParseWireValue` map to and from the wire strings `"asc"` and `"desc"`. Transport-neutral: parsing happens at the adapter boundary.
  - `Pagination/FilterRequest.cs`: immutable explicit key/value filter pairs. Rejects null, empty, or whitespace keys. Rejects duplicate keys. Allows empty values. Provides a canonical `Empty` singleton.
  - `Pagination/QueryValidationResult.cs`: discriminated union of a successful query validation or a validation failure. `Success` carries the validated page, sort, and filter requests. `Failure` carries a non-empty `ValidationErrors`.
- 49 new tests across 7 test classes: `PaginationMetadataTests` (15), `PagedResultTests` (8), `PaginationOptionsTests` (6), `PageRequestTests` (12), `SortRequestTests` (14), `FilterRequestTests` (12), `QueryValidationResultTests` (12). Total suite: 178 tests.
- The `Pagination` namespace depends on `Validation`, which depends on `Errors`, which depends on `Metadata`. The dependency graph remains acyclic and transport-neutral.


### Added (Phase 0.6)

- Correlation and metadata contract in ApiPilot.Core:
  - `Metadata/ICorrelationIdGenerator.cs`: thread-safe contract for producing new correlation IDs.
  - `Metadata/DefaultCorrelationIdGenerator.cs`: default implementation producing version 7 GUIDs in the standard hyphenated form. Time-ordered and unpredictable.
  - `Metadata/ICorrelationIdAccessor.cs`: read-only ambient accessor for the current request correlation ID.
  - `Metadata/CorrelationIdValidator.cs`: validates incoming IDs against a configurable regex, compiled once at construction.
  - `Metadata/CorrelationOptions.cs`: mutable data holder. Defaults: header `X-Request-Id`, echo in body and header enabled, incoming validation enabled, pattern `^[A-Za-z0-9_-]{8,128}$`.
- 22 new tests in `CorrelationIdValidatorTests` (8), `CorrelationOptionsTests` (5), `DefaultCorrelationIdGeneratorTests` (6), `InMemoryCorrelationIdAccessorTests` (3). Total suite: 200 tests.
- A test double `Fakes/InMemoryCorrelationIdAccessor.cs` implements the accessor contract for use in future integration tests (Phase 1.6).

### Added (Phase 0.7)

- Hardened `audit.ps1` into the full boundary enforcement script. The script now enforces five distinct boundary levels:
  - Section 5a: no `<PackageReference>` in production csproj files.
  - Section 5b: the compiled `ApiPilot.Core.dll` references only assemblies on the allowlist (`System.*`, `mscorlib`, `netstandard`, `Microsoft.CSharp`, `WindowsBase`).
  - Section 5c: production and test `.cs` files contain no forbidden symbols from a list of 18.
  - Section 5d: `Directory.Packages.props` must not exist.
  - Section 5e: `ApiPilot.Core.csproj` must not declare a `<FrameworkReference>` to `Microsoft.AspNetCore.App`.
- Forbidden-symbol list extended from 8 to 18 entries: Polly, Polly.Extensions, RetryPolicy, CircuitBreaker, IdempotencyKey, JwtIssuer, PaymentIntent, Stripe, PayPal, Microsoft.Extensions.Http, Microsoft.Extensions.Caching, EntityFrameworkCore, Newtonsoft, AutoMapper, FluentValidation, Serilog, NLog, MediatR.
- Scan scope extended from production source only to production and test source.
- The zero-third-party policy is recorded as a maintenance-independence guarantee: no ApiPilot code depends on a third-party library whose changes would force us to change ApiPilot.

### Added (Phase 0.8)

- Version bumped to `0.1.0-alpha.1` in the `VERSION` file. The milestone freezes the Phase 0 contract surface.
- `Directory.Build.props` now declares `<PackageLicenseExpression>MIT</PackageLicenseExpression>` so every produced package carries the MIT license as an SPDX expression.
- `ApiPilot.Core.0.1.0-alpha.1.nupkg` produced and verified. Contents:
  - `lib/net10.0/ApiPilot.Core.dll` (compiled assembly)
  - `lib/net10.0/ApiPilot.Core.xml` (XML documentation)
  - `.nuspec` declares id `ApiPilot.Core`, version `0.1.0-alpha.1`, authors `ApiPilot contributors`, license `MIT` as an SPDX expression.
  - Zero dependencies declared in the package manifest (`<dependencies><group targetFramework="net10.0" /></dependencies>` with no child entries).
- Full audit passes at the new version: 29+ checks green across Sections 1-6.
- The test suite remains at 200 passing tests across 20 classes.

### Added (Phase 1.0)

- `dotnet/src/ApiPilot.AspNetCore/` - the ASP.NET Core integration project:
  - `ApiPilot.AspNetCore.csproj` declares a `FrameworkReference` to `Microsoft.AspNetCore.App` (part of the .NET platform, not a third-party package) and a `ProjectReference` to `ApiPilot.Core`.
  - Folder skeleton for future concerns: `DependencyInjection/`, `Middleware/`, `EndpointMetadata/`, `ExceptionHandling/`, `Serialization/`, `Results/`.
- `dotnet/tests/ApiPilot.AspNetCore.Tests/` - the integration test harness:
  - `Program.cs`, `TestAssert.cs`, `TestRunner.cs` - the same harness pattern as `ApiPilot.Core.Tests`.
  - `InProcessHost.cs` - a helper that starts a real ASP.NET Core application in-process, binds to a random loopback port, and exposes an `HttpClient` configured against that port.
  - `Integration/SmokeTests.cs` - the first integration test. It starts an in-process host, registers a `GET /health` endpoint, issues a real HTTP request, and asserts status 200 with body "ok".
- `dotnet/ApiPilot.slnx` now references all four projects.
- `audit.ps1` extended with two subchecks:
  - **5f** - `ApiPilot.AspNetCore.csproj` must declare the ASP.NET Core `FrameworkReference`.
  - **5g** - the compiled `ApiPilot.AspNetCore.dll` must reference only assemblies on the ASP.NET Core-specific allowlist (`System.*`, `Microsoft.AspNetCore.*`, `Microsoft.Extensions.*`, `mscorlib`, `netstandard`, `Microsoft.CSharp`, `WindowsBase`, `ApiPilot.Core`).
- Full audit passes: 32+ checks across Sections 1-6.
- The full solution now contains four projects and 201 tests: `ApiPilot.Core.Tests` (200) and `ApiPilot.AspNetCore.Tests` (1).

### Added (Phase 1.1)

- JSON serialization policy in ApiPilot.AspNetCore:
  - `Configuration/ApiPilotJsonOptions.cs` - mutable options holder for nine JSON conventions: enum mode, date mode, indentation, property naming policy, dictionary key naming policy, ignore condition, trailing comma allowance, comment handling, and max depth.
  - `Serialization/EnumSerializationMode.cs` and `Serialization/DateSerializationMode.cs` - the two enums that drive the corresponding option values.
  - `Serialization/UnixSecondsDateTimeConverter.cs` and `Serialization/UnixSecondsDateTimeOffsetConverter.cs` - custom System.Text.Json converters for the Unix epoch seconds date mode.
  - `Serialization/JsonSerializerConfigurator.cs` - applies the options to a JsonSerializerOptions instance with idempotent converter registration.
  - `Serialization/JsonSerializationExtensions.cs` - two AddApiPilotJson overloads: one on IMvcBuilder, one on IServiceCollection. Both configure the MVC JSON serializer and the minimal API JSON serializer so either endpoint model receives identical wire format.
- 26 new tests across four test files:
  - `Serialization/ApiPilotJsonOptionsTests.cs` (5 tests): defaults, mutability, null semantics for the KeyTransform-style overrides.
  - `Serialization/JsonSerializerConfiguratorTests.cs` (11 tests): camelCase naming, null naming policies, ignore conditions, enum modes, date modes, idempotent converter registration, argument null checks.
  - `Serialization/JsonSerializationSnapshotTests.cs` (6 tests): parsed-structure assertions on the serialized success envelope shape.
  - `Integration/JsonSerializationIntegrationTests.cs` (4 tests): real HTTP responses from an in-process ASP.NET Core host with the ApiPilot JSON conventions applied.

### Added (Phase 1.2)

- HTTP status mapping in ApiPilot.AspNetCore:
  - `Results/ApiResponseHttpMapper.cs` - the single translation point from the transport-neutral ApiResponseStatus enum to HTTP status codes. Switch expression with a defensive ArgumentOutOfRangeException guard against future enum additions.
  - `Results/ApiResponseResult.cs` - IResult implementation for the non-generic ApiResponse (envelope-only successes such as 204 No Content).
  - `Results/ApiResponseResultOfT.cs` - IResult implementation for the generic ApiResponse<T>. Serializes with the minimal API JSON options configured by AddApiPilotJson.
  - `Results/ApiPilotResultsExtensions.cs` - ToResult extension methods on ApiResponse and ApiResponse<T>, giving the public call shape `ApiResponseBuilder.Ok(data, meta).ToResult()`.
- 17 new tests across two test files:
  - `Results/ApiResponseHttpMapperTests.cs` (13 tests): one per status value plus an unknown-value rejection test.
  - `Integration/HttpStatusMappingIntegrationTests.cs` (4 tests): real HTTP responses for Ok/Created/Accepted/NoContent variants.

### Added (Phase 1.3)

- Exception mapping in ApiPilot.AspNetCore:
  - `Results/ErrorResponseResult.cs` - IResult and IActionResult implementation that writes an ErrorResponse envelope. Implements both interfaces so the same result type works in MVC action filters, the `[ApiController]` response factory, and minimal API endpoints.
  - `ExceptionHandling/IApiExceptionMapper.cs` - ASP.NET Core adapter contract for mapping exceptions to ApiError. Distinct from the transport-neutral IApiErrorMapper in Core; receives HttpContext so implementations can consider request context.
  - `ExceptionHandling/KnownExceptionTypes.cs` and `ExceptionHandling/KnownExceptionType.cs` - default mapping table for five BCL exceptions: ArgumentNullException, ArgumentException, KeyNotFoundException, UnauthorizedAccessException, InvalidOperationException. Ordered so specific types match before their base types.
  - `Configuration/ApiExceptionOptions.cs` - mutable data holder: Mappings list, IncludeExceptionTypeInLogs, IncludeExceptionMessageInLogs, KnownExceptionLogLevel, UnknownExceptionLogLevel, RevealExceptionTypeInResponse, RevealExceptionMessageInResponse. Safe-by-default: no reveal unless explicitly enabled.
  - `ExceptionHandling/DefaultApiExceptionMapper.cs` - walks the mapping list in order, matches the first IsInstanceOfType hit, falls back to INTERNAL_ERROR with a generic safe message. Guarded by ArgumentNullException on both constructor and Map.
  - `Middleware/ApiPilotExceptionMiddleware.cs` - catches downstream exceptions, maps them safely, logs via a LoggerMessage dispatch table, writes the ErrorResponse envelope. Handles client disconnects (rethrown OperationCanceledException when RequestAborted is signaled) and started responses (rethrown exception after logging).
  - `Middleware/ApiPilotExceptionExtensions.cs` - AddApiPilotExceptions and UseApiPilotExceptions.
  - `Middleware/ApiPilotExceptionMiddleware.LogMessages.cs` - six LoggerMessage.Define delegates (one per LogLevel) plus a dispatcher method used by LogSafely.
- `ApiPilot.Core/Errors/ApiErrorCodeJsonConverter.cs` - custom JsonConverter<ApiErrorCode> applied via [JsonConverter] on the type so ApiErrorCode serializes as a plain string matching SPEC.md, not as a nested object.
- Fixes the wire-format defect where ApiError.Fields serialized as nested ApiErrorField objects instead of arrays. ApiError.Fields changed from IReadOnlyDictionary<string, ApiErrorField> to IReadOnlyDictionary<string, IReadOnlyList<string>>. ApiErrorField remains the input type to ApiError.Create.
- 22 new tests:
  - `ExceptionHandling/DefaultApiExceptionMapperTests.cs` (13 tests): built-in mappings, ordering precedence, fallback, reveal controls, custom mappings with IncludeExceptionMessage, null rejection.
  - `Middleware/ApiPilotExceptionMiddlewareTests.cs` (10 tests): mapped responses, log redaction, response-started rethrow, client-disconnect rethrow, idempotent behavior.
  - `Integration/ExceptionMappingIntegrationTests.cs` (6 tests): real HTTP responses for ArgumentException, KeyNotFoundException, unknown exceptions, and no-leak assertions on both message and type name.
  - `Core.Tests/Errors/ErrorResponseSerializationTests.cs` (4 tests): the wire-shape test that would have caught the ApiError.Fields defect. Asserts the full JSON structure of the error envelope matches SPEC.md.

### Added (Phase 1.4)

- Model validation integration in ApiPilot.AspNetCore:
  - `Validation/FieldKeyNormalizer.cs` - default camelCase normalizer for ModelState field keys. Converges the various binder-produced key formats (Items[0].Price, JSONPath with a leading dollar sign, plain names) to a canonical form. Uses AsSpan to avoid unnecessary allocations.
  - `Validation/ModelStateAdapter.cs` - converts ModelStateDictionary entries to IReadOnlyList<ApiErrorField>. Merges colliding transformed keys. Sorts ordinally. Accepts an optional keyTransform parameter.
  - `Validation/ValidationKeyTransforms.cs` - the single resolution rule for the effective key transform: the configured KeyTransform when present, otherwise FieldKeyNormalizer.Normalize. Both MVC paths route through it so they cannot drift.
  - `Configuration/ApiPilotValidationOptions.cs` - the Option D override point. A single Func<string, string>? KeyTransform. Null means use the library default. The identity function (k => k) explicitly disables normalization.
  - `Validation/ApiPilotValidationFilter.cs` - internal MVC action filter that implements the [ApiPilotValidate] behavior. Constructor receives IOptions<ApiPilotValidationOptions> and IOptions<ApiExceptionOptions>. Applies the resolved transform, builds the standard validation envelope, short-circuits the action.
  - `Validation/ApiPilotValidateAttribute.cs` - the declarative attribute. Derives from TypeFilterAttribute so MVC resolves the filter from DI. Source-level usage is unchanged from the initial design.
  - `Validation/ApiPilotInvalidModelStateResponseFactory.cs` - the [ApiController] path. MVC calls this when a [ApiController] controller has invalid ModelState, before any action filter runs. Produces the same envelope as the filter.
  - `Validation/ApiPilotValidationEndpointFilter.cs` - the minimal API endpoint filter with two WithApiPilotValidation overloads: one taking a callback that returns ApiErrorField values, one taking a callback that returns raw key/message pairs and normalizes keys using the resolved KeyTransform via AddEndpointFilterFactory.
  - `DependencyInjection/ApiPilotServiceCollectionExtensions.cs` - AddApiPilotValidation (options only) and AddApiPilotControllers (options plus the [ApiController] response factory override).
  - `ApiPilot.AspNetCore.csproj` gains InternalsVisibleTo for the test project so the internal filter can be exercised directly.
- MVC and minimal API paths produce identical envelopes for the same invalid input. Verified by ValidationEnvelopeParityTests.
- 23 new tests across five test files:
  - `Validation/ApiPilotValidationOptionsTests.cs` (5 tests): defaults, mutability, null semantics of KeyTransform.
  - `Validation/ApiPilotValidationFilterTests.cs` (6 tests): valid state does nothing, invalid state sets an ErrorResponseResult, default transform applies, custom transform applies, identity transform preserves keys, null options rejected.
  - `Integration/ApiPilotValidateAttributeIntegrationTests.cs` (3 tests): filter path over real HTTP with a controller that has [ApiPilotValidate] but not [ApiController].
  - `Integration/ApiPilotControllersIntegrationTests.cs` (4 tests): [ApiController] path over real HTTP, including an idempotence test that verifies AddApiPilotControllers overrides the default ProblemDetails response when called after AddControllers.
  - `Integration/ValidationEnvelopeParityTests.cs` (3 tests): the same invalid input to the filter path and the [ApiController] path produces the same error code, the same field keys, and the same field messages.

### Backfill (After Phase 1.4)

- **B.1** - Resolved A-042 and A-064. The exception middleware no longer suppresses CA1848. A new partial-class file `ApiPilotExceptionMiddleware.LogMessages.cs` defines six LoggerMessage.Define delegates (one per LogLevel) plus a dispatcher. LogSafely builds the detail string once and dispatches. A seventh delegate handles the response-already-started case in InvokeAsync. The `.editorconfig` suppression section is removed.
- **B.2** - Resolved A-050, A-065, A-066. Moved `ApiPilotJsonOptions` from `Serialization/` and `ApiExceptionOptions` from `ExceptionHandling/` to `Configuration/` so all options types live in one place. The moved files gained explicit usings for the types they use from the namespaces they left. Test file `ApiPilotJsonOptionsTests.cs` moved to the mirrored `Configuration/` folder.
- **B.3** - Resolved the Option D audit gap. Added `ApiExceptionOptions.ErrorCodeToStatusMap` as the override point for error-code-to-status mapping. `ErrorResponseResult` accepts an optional `ApiExceptionOptions`, consults the custom map first, then the built-in switch, then the 500 fallback. Three call sites pass the options: `ApiPilotExceptionMiddleware`, `ApiPilotInvalidModelStateResponseFactory`, `ApiPilotValidationFilter`. The `.ToResult()` extension on `ErrorResponse` retains the default behavior for convenience. Five new tests in `Results/ErrorResponseResultStatusMappingTests.cs` verify the default, custom, and fallback paths.
- **B.4** - Resolved A-052. Internal accessors added to `ApiResponseResult` (`Response` property of type `ApiResponse`) and `ApiResponseResult<T>` (`Response` property of type `ApiResponse<T>`), matching the accessor that already existed on `ErrorResponseResult`. Used by tests to inspect the wrapped response without serialization.
- **B.5** - Resolved. Custom code status mapping is folded into B.3.
- **B.6** - Added the idempotence test that proves calling `AddApiPilotControllers` after `AddControllers` overrides MVC's default ProblemDetails factory with the ApiPilot envelope. This proves the ordering rule documented in the AddApiPilotControllers XML doc.
- **Audit fix** - Resolved A-071. Section 5c of `audit.ps1` now uses word-boundary regex matching ('\b' + escaped symbol + '\b') instead of substring matching. The substring form false-positived on the substring `nLog` inside `KnownExceptionLogLevel` and `UnknownExceptionLogLevel`, flagging three files as containing forbidden symbol `NLog`.

### Notes

- `AssemblyVersion` and `FileVersion` are sourced from a numeric prefix extracted from `VERSION`; `InformationalVersion` carries the full SemVer string. This resolves the CS7035/CS7034 build errors that occurred when the prerelease string was used directly for assembly and file versions.
- Analyzer rules CA1707 (underscores in identifiers) and CA1822 (member can be static) are disabled for files under `dotnet/tests/**/*.cs` via a scoped `.editorconfig` section. Both rules are correct for production code and wrong for idiomatic test method names and reflection-based invocation.
- The test harness runs end to end: discovery, execution, sync and async tests, structured output, non-zero exit on failure.
- ApiPilot.Core.dll currently references only `System.Runtime`. The transport-neutral boundary is holding and is now machine-enforced.

### Findings from the build

- **A-005** (Phase 0.1): `AssemblyVersion` and `FileVersion` rejected the SemVer 2.0 prerelease string. Fixed by extracting a numeric version prefix and using it for the numeric-only version properties. Finding recorded.
- **A-006** (Phase 0.1): Analyzer rules CA1707 and CA1822 fired on idiomatic test method names and reflection-based test invocation. Fixed by scoping the analyzer severities to `dotnet/tests/**/*.cs`. Finding recorded.
- **A-007** (Phase 0.1): `<DocumentationFile>` in `Directory.Build.props` referenced `$(AssemblyName)` and `$(OutputPath)`, both undefined at the evaluation point of `Directory.Build.props` (evaluated before the project file and before the SDK sets those properties). The XML documentation file was written to the project root as `.xml` instead of next to the compiled assembly. Fixed by removing the explicit `<DocumentationFile>` property and relying on the .NET SDK default, which places the XML documentation file next to the assembly in the output directory whenever `<GenerateDocumentationFile>true</GenerateDocumentationFile>` is set. Finding recorded.
- **A-008** (Phase 0.1): `audit.ps1` loaded `ApiPilot.Core.dll` via `[System.Reflection.Assembly]::LoadFrom($coreDll)`, which holds a file handle on the DLL for the lifetime of the PowerShell process. A subsequent `dotnet build` failed after ten retries with MSB3027 and MSB3021 because the file was locked. Fixed by switching to `[System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($coreDll))`, which reads the bytes and releases the file handle before loading. Finding recorded.
- **A-009** (Phase 0.1): A verification block ran a `dotnet run --project` against a relative path after changing directory to `C:\Windows\System32` on the build-failure branch. The relative path resolved against the wrong working directory and the test command failed with "The provided file path does not exist". Fixed by using absolute paths for every `dotnet` invocation and avoiding mid-block directory changes. Finding recorded.
- **A-010** (Phase 0.2): `ResponseMetadata` is a record with a dictionary-typed property (`Extra`). Records auto-generate equality using `EqualityComparer<T>.Default`, which for `IReadOnlyDictionary<string, string>` falls back to reference equality. The initial Phase 0.2 tests assumed structural equality and would have failed. Fixed by replacing equality tests with property-level assertions and reference-distinctness assertions. Finding recorded.
- **A-011** (Phase 0.2): Block 0.2j-Fix splice removed one adjacent test (`Extra_AcceptsMultipleValues`) along with the three intended tests, because the start anchor was computed by a fixed offset rather than by scanning backward through a variable-length `///` doc comment block. Fixed by re-inserting the test. Lesson: when splicing a region anchored by a method name, compute the start position by scanning for the start of the method comment, not by a fixed offset. Finding recorded.
- **A-012** (Phase 0.2): `ApiResponse.cs` was overwritten with the content intended for `ApiResponseOfT.cs`. Both files became byte-identical, both declaring `public sealed class ApiResponse<T>`. The non-generic `ApiResponse` class was missing entirely, causing CS0101 and CS0305 build errors. Fixed by rewriting `ApiResponse.cs` with the correct non-generic content. Lesson: PowerShell session variables persist across pastes; re-declare all variables at the top of every block. Finding recorded.
- **A-013** (Phase 0.3): The original plan listed `StandardErrorCodes.cs` as a second type holding the 12 standard codes. On review the second type is redundant with the 12 static instances on `ApiErrorCode`. Decision: skip `StandardErrorCodes.cs`. Consumers use `using static ApiPilot.Core.Errors.ApiErrorCode;` for brevity. One source of truth. Design correction, recorded.
- **A-014** (Phase 0.3): The initial `ErrorResponseTests.cs` block contained a drafting slip: `ApiErrorCode.UnauthorizedOrNot()`, a nonexistent method, instead of `ApiErrorCode.AuthenticationRequired`. The block was sent with a warning in the same message but not held back. Corrective process: when a block contains a defect, the corrected block is sent in place of the original, never as a follow-up. Finding recorded.
- **A-015** (Phase 0.3): A PowerShell script used `$matches` as a counter variable. `$matches` is a PowerShell automatic variable (a `Hashtable`) and cannot be incremented with `++`. Lesson: never use a name from PowerShell automatic variables (`$matches`, `$args`, `$input`, `$error`, `$this`, `$_`, `$PSItem`, `$PSCommandPath`, `$PSScriptRoot`). Use non-reserved names such as `$hits`, `$count`, or `$occurrences`. Finding recorded.
- **A-016** (Phase 0.3): Nine analyzer findings fired on the test project only: CA1861 (prefer static readonly arrays) seven times in `ApiErrorFieldTests`, CA1806 (unused object creation) twice in `ErrorResponseTests`, CA1859 (use concrete type) once in `ErrorResponseTests`. All three rules are correct for production code and wrong for idiomatic test code. Fixed by extending the `[dotnet/tests/**/*.cs]` section in `.editorconfig` with the three suppressions. Production code remains subject to all three rules. Finding recorded.
- **A-017** (Phase 0.3): A verification check in Block 0.3l used `-match` with the pattern `'### Added (Phase 0.2 and Phase 0.3)'`. The unescaped parentheses were treated as a capture group, so the literal pattern never matched and the check reported a false negative. The underlying file was correct. Lesson: use `-eq` for literal string equality, or escape regex metacharacters when using `-match`. Finding recorded.
- **A-018** (Phase 0.4): The original Phase 0.4 plan listed a `ValidationError` type distinct from `ApiErrorField`. On review, `ApiErrorField` already carries exactly the shape validation errors need (field name plus messages). Decision: skip the new type; `ValidationResponseFactory` accepts `IEnumerable<ApiErrorField>` directly. Design correction, same pattern as A-013. Finding recorded.
- **A-019** (Phase 0.4): `ValidationErrorCollection` triggered CA1711 (type names should not end with the reserved BCL suffix `Collection`) because the type does not implement `ICollection<T>`. Renamed to `ValidationErrors` across source, tests, and the response factory. Lesson: avoid reserved BCL suffixes when naming types that wrap but do not implement a collection interface. Finding recorded.
- **A-020** (Phase 0.6): The folder-listing line in Block 0.6h printed `("  " + $_.Name + "  (" + $_.Length + " bytes)")`, and a terminal re-parsed the printed output as input, throwing `Unexpected token 'bytes'` parse errors. The underlying files were correct. Lesson: `Write-Host` with parenthesized expressions can be re-parsed if the terminal echoes output as input. Prefer `-f` formatting or similar when printing lines that contain parentheses and words like `bytes`. Finding recorded.
- **A-021** (Phase 0.7): The extended forbidden-symbol scan flagged `FluentValidation` in a prose comment inside `IValidationErrorSource.cs`. The comment named a specific third-party library as an example. Fixed by rewriting the comment to say 'a third-party validation framework' without naming a library. Lesson: forbidden-symbol scans cannot distinguish code identifiers from prose. When a false positive occurs, rewrite the source rather than weaken the scan. This preserves boundary-enforcement strength. Finding recorded.
- **A-022** (Phase 0.8): The Block 0.8b tail splice used the reversed range `$lines[154..($lines.Count - 1)]` where `$lines.Count - 1 = 153`. PowerShell 5.1 does not throw on a reversed range; it silently returned the elements in the specified order, producing the last element of the original file as a spurious tail that was then appended after the new tail. The result was a duplicated `[0.1.0-alpha.0]` link line at the bottom of the changelog. Fixed by deleting the duplicate. Lesson: when computing a range in PowerShell, always confirm `start <= end`. When the intended slice is past the end of the array, use `@()` or `$null`, never a range. Also: after every splice, read the tail of the file to verify the result. Finding recorded.
- **A-023** (Phase 0.8): The initial `ApiPilot.Core.0.1.0-alpha.1.nupkg` did not include a `<license>` element in its `.nuspec`. Standard NuGet convention expects either `<license type="expression">` (SPDX identifier) or `<license type="file">`. Fixed by adding `<PackageLicenseExpression>MIT</PackageLicenseExpression>` to `dotnet/Directory.Build.props` so every ApiPilot package carries the license automatically. Lesson: `dotnet pack` does not infer license metadata from a `LICENSE` file at the repository root. The license must be declared explicitly. Finding recorded.
- **A-024** (Phase 1.0): The initial `InProcessHost.cs` was missing two `using` directives: `Microsoft.AspNetCore.Hosting` (for the `UseUrls` extension method) and `Microsoft.Extensions.DependencyInjection` (for `GetRequiredService<IServer>()`). Caught by reasoning before the first build, not by the build itself. Fixed by adding the two usings. Lesson: ASP.NET Core and `Microsoft.Extensions.*` namespaces are not in the implicit usings set for a Console project. Every type and every extension method from those namespaces needs its own explicit `using`. Finding recorded.
- **A-025** (Phase 1.0): The initial `SmokeTests.cs` called `app.MapGet(...)` without `using Microsoft.AspNetCore.Builder;`. The compiler reported CS1061 ("does not contain a definition for MapGet and no accessible extension method"). Fixed by adding the using. Lesson: extension methods require their containing namespace to be in scope via `using`, even when the receiver type is available through another using. The compiler message is literal; when it says "are you missing a using directive", the missing using is exactly what it names. Finding recorded.
- **A-026** (Phase 1.0): A Phase 1.0 celebration was posted before the phase was complete; blocks 1.0g, 1.0h, and 1.0i were still outstanding. The celebration was honest about what had been achieved (the integration test infrastructure works, the build is green) but it was premature as a phase-completion signal. Lesson: a green build in the middle of a phase is a progress milestone, not a phase milestone. Phase milestones are only celebrated after every sub-block in the phase has shipped. Finding recorded.
- **A-027** (Phase 1.1): `DateSerializationMode.UnixTimeSeconds` was added to the enum before the corresponding converters existed. Fixed by implementing `UnixSecondsDateTimeConverter` and `UnixSecondsDateTimeOffsetConverter` rather than shipping a mode that would throw. Lesson: do not add enum values whose behavior is not implemented. Finding recorded.
- **A-028** (Phase 1.1): The initial `AddApiPilotJson` extension configured only the MVC JSON serializer (`Microsoft.AspNetCore.Mvc.JsonOptions`). Minimal API endpoints use `Microsoft.AspNetCore.Http.Json.JsonOptions`, a separate options type. Applications using minimal APIs would not receive ApiPilot JSON conventions. Fixed by configuring both. Lesson: MVC and minimal APIs have parallel but separate JSON configuration surfaces. Finding recorded.
- **A-029** (Phase 1.1): `AddApiPilotJson` extended only `IMvcBuilder`. Minimal-API-only applications do not have an `IMvcBuilder`. Added an `IServiceCollection` overload so applications using either endpoint model can configure the library. Finding recorded.
- **A-030** (Phase 1.1): `InProcessHost.StartAsync` took a single callback that received the built `WebApplication`. This prevented tests from calling service configuration extensions like `AddApiPilotJson(IServiceCollection)`, which must run before `builder.Build()`. Fixed by changing the signature to accept two optional callbacks: one for service configuration (pre-build), one for app configuration (post-build). Lesson: a test host helper must provide hooks for both service registration and application configuration phases. Finding recorded.
- **A-032** (Phase 1.1): `JsonSerializerConfiguratorTests.Configure_UnixTimeSecondsDateMode_SerializesDateAsNumber` hardcoded the expected Unix seconds value as a literal that was computed incorrectly from memory. Fixed by computing the expected value from the source `DateTimeOffset` via `ToUnixTimeSeconds()` at test time. Lesson: never hardcode a value derived from a date calculation. Finding recorded.
- **A-033** (Phase 1.1): The `EnumSerializationMode.String` member triggered `CA1720` (identifier contains type name) because `String` collides with `System.String`. Renamed to `AsString`. Lesson: avoid BCL type names in enum member names. Finding recorded.
- **A-034** (Phase 1.1): `ApiPilotJsonOptions.MaxDepth` was typed `int?` but `JsonSerializerOptions.MaxDepth` is `int` (non-nullable). Changed the property to `int` and updated its documentation to remove the "set to null for unlimited" claim (the BCL does not support unlimited depth). Finding recorded.
- **A-035** (Phase 1.1): `ApiPilotJsonOptions.WriteIndented` and `AllowTrailingCommas` had explicit `= false` initializers that trigger `CA1805`. Removed the redundant initializers. Finding recorded.
- **A-036** (Phase 1.1): The `EnumSerializationMode.String` to `AsString` rename was applied only to the enum declaration and one test file. Two production references in `ApiPilotJsonOptions.cs` and `JsonSerializerConfigurator.cs` were missed, and the first build failed with `CS0117`. Fixed via a tree-wide sweep of 79 `.cs` files. Lesson: renaming a public enum member requires a tree-wide search, not a mental inventory. Finding recorded.
- **A-037** (Phase 1.1): A splice that removed a single test method from `ApiPilotJsonOptionsTests.cs` produced a corrupted file - the header comment, using block, and class declaration were removed by mistake, leaving orphaned braces at the top of the file. Fixed by rewriting from a known-good template. Lesson: for any splice that removes a method, verify the resulting structure with a first-10 and last-5 line read before considering the operation complete. Finding recorded.
- **A-038** (Phase 1.1): `JsonSerializerConfiguratorTests` used `long.ToString()` without an `IFormatProvider`, which `CA1305` flagged. Fixed by passing `CultureInfo.InvariantCulture`. Lesson: numeric-to-string conversions in assertions must specify the invariant culture unless culture-sensitive behavior is intentional. Finding recorded.
- **A-039** (Phase 1.2): `ErrorResponse` had no `.ToResult()` extension. Error-path integration tests were deferred to Phase 1.3, where `ErrorResponseResult` was created alongside exception mapping. Resolved in Phase 1.3. Finding recorded.
- **A-040** (Phase 1.3, resolved in Phase 1.6): The exception middleware's `ResponseMetadata.RequestId` used `httpContext.TraceIdentifier` because the correlation middleware did not exist until Phase 1.6. Phase 1.6 updated the middleware to read from `ICorrelationIdAccessor` with `TraceIdentifier` as a fallback. Resolved. Finding recorded.
- **A-041** (Phase 1.3): The exception middleware tests needed `TestAssert.ThrowsAsync<T>` to assert that certain exceptions propagate. The helper did not exist. Extended `TestAssert` with the async variant. Lesson: when a test scenario needs a helper that does not exist, extend the shared harness rather than falling back to a bare try/catch in each test. Finding recorded.
- **A-042** (Phase 1.3, resolved in Backfill B.1): `ApiPilotExceptionMiddleware.LogSafely` selected the log level at runtime from configuration, which `CA1848` cannot express because `LoggerMessage.Define` requires a compile-time level. Initially suppressed for the file in `.editorconfig`. Resolved in Backfill B.1 by replacing the suppression with a level-dispatch table of `LoggerMessage.Define` delegates. Finding recorded.
- **A-043** (Phase 1.3): `DefaultApiExceptionMapperTests.Map_RejectsNullHttpContext` constructed `new Exception("x")`, which `CA2201` flags. Replaced with `new InvalidOperationException("x")`. Lesson: even in test code, use specific exception types when constructing exceptions. Finding recorded.
- **A-044** (Phase 1.3): A PowerShell write block used `\"` escapes inside a double-quoted string. PowerShell 5.1 does not use backslash as an escape character. The block failed to parse and no file was modified. Fixed by using single-quoted strings where the content contains double quotes, or by doubling the quote. Lesson: never use backslash escapes in PowerShell strings. Finding recorded.
- **A-045** (Phase 1.3): `ApiErrorCode` is a record with a `Code` string property. System.Text.Json serialized it as a nested object `{ "code": "VALIDATION_ERROR" }` instead of the SPEC.md wire value `"VALIDATION_ERROR"`. The defect was latent since Phase 0.3 and surfaced in Phase 1.3 integration tests that parse `error.code` as a string. Fixed by adding a custom `JsonConverter<ApiErrorCode>` applied via `[JsonConverter]` on the type. Lesson: every value object that appears on the wire needs a serialization test. Type-level unit tests do not catch wire-shape issues. Finding recorded.
- **A-046** (Phase 1.3): A block inserted usings at the end of the using block instead of in alphabetical order. Fixed by a reordering edit. Lesson: when adding usings programmatically, insert in the correct alphabetical position, not just after the last one. Finding recorded.
- **A-047** (Phase 1.4): Initial field key normalization design assumed a fixed format. ASP.NET Core model binders produce different `ModelStateDictionary` key formats depending on the binder: JSONPath with a `$` prefix for `[FromBody]`, bracket notation for form-urlencoded, plain names for query strings. Any single fixed format is wrong for at least one of the inputs. Resolved by adopting the Option D default plus override pattern: the normalizer converges all three forms, and `ApiPilotValidationOptions.KeyTransform` lets an application supply a different canonical form. Lesson: design for legitimate variety, not for a single assumed shape. Finding recorded.
- **A-048** (Phase 1.4): Option D was formalized as a standing discipline during this phase: every transformation has a sensible default and a first-class override. The discipline is documented in the handoff document Section 9 and applied retroactively and prospectively. Design principle, not a defect. Finding recorded.
- **A-049** (Phase 1.4): The initial `ApiPilotValidateAttribute` hardcoded the field key transform and did not honor a configurable `KeyTransform`. Applications whose frontend expected a different format could not use the attribute without forking the library. This is the defect that forced the formalization of Option D. Fixed by: `ApiPilotValidationOptions.KeyTransform` as the override point; `ValidationKeyTransforms.Resolve` as the single resolution rule; both MVC paths (`[ApiPilotValidate]` action filter and `[ApiController]` response factory) routing through it; the minimal API path using the same options via `AddEndpointFilterFactory`. Finding recorded.
- **A-050** (Phase 1.4, resolved in Backfill B.2): Options types were scattered across feature folders: `ApiPilotJsonOptions` in `Serialization/`, `ApiExceptionOptions` in `ExceptionHandling/`, `ApiPilotValidationOptions` in `Configuration/`. Inconsistent. Resolved by moving all three to `Configuration/`. Finding recorded.
- **A-051** (Phase 1.4): A test write to `ApiPilot.AspNetCore.Tests/Validation/` failed because the target folder did not exist. `File.WriteAllLines` requires the parent directory to exist. Fixed by prepending a folder-creation step. Lesson: every block that writes to a folder which may not exist must include the folder-creation step. Finding recorded.
- **A-052** (Phase 1.4, resolved in Backfill B.4): The filter unit tests needed to inspect the `ErrorResponse` produced by the filter, but `ErrorResponseResult` held it in a private field with no accessor. Added an internal accessor for test inspection. In Backfill B.4, the same accessor pattern was extended to `ApiResponseResult` and `ApiResponseResult<T>` for consistency. Finding recorded.
- **A-053** (Phase 1.4): MVC's `[ApiController]` attribute short-circuits invalid ModelState with a `ProblemDetails` response before any action filter runs. The `[ApiPilotValidate]` filter therefore does not execute for `[ApiController]`-decorated controllers. Applications using `[ApiController]` (the common case) would not see the ApiPilot validation envelope. Fixed by providing `ApiPilotInvalidModelStateResponseFactory` wired into `ApiBehaviorOptions.InvalidModelStateResponseFactory` via `AddApiPilotControllers`. Both MVC paths now produce identical envelopes. Finding recorded.
- **A-054** (Phase 1.4): `ApiError.Fields` was typed as `IReadOnlyDictionary<string, ApiErrorField>`. Serialized JSON produced `{ "email": { "field": "email", "messages": [...] } }`, which does not match SPEC.md's `{ "email": [...] }`. The defect was latent since Phase 0.3. Fixed by changing `ApiError.Fields` to `IReadOnlyDictionary<string, IReadOnlyList<string>>`. `ApiErrorField` remains the input type to `ApiError.Create`. Also added `ErrorResponseSerializationTests` to assert the full wire shape of the error envelope. Finding recorded.
- **A-055** (Phase 1.4): A block used `IEndpointConventionBuilder` but imported `Microsoft.AspNetCore.Routing` instead of `Microsoft.AspNetCore.Builder`, where the interface is defined. Build failed with `CS0246`. Fixed by correcting the using. Lesson: `IEndpointConventionBuilder` lives in `Microsoft.AspNetCore.Builder`; `Microsoft.AspNetCore.Routing` is for `RouteHandlerBuilder` and route matching types. Finding recorded.
- **A-056** (Phase 1.4, process): When a correction supersedes a defective block, if the user runs both, the second block's idempotence guard detects that the fix is already present and refuses. This leaves the file in the partially-fixed state produced by the first block. Lesson: corrected blocks must be self-healing against the partial state left by a superseded block, not refuse. Finding recorded.
- **A-057** (Phase 1.4): `ErrorResponseResult` implemented only `IResult` (the minimal API interface). MVC action filters set `context.Result` to an `IActionResult`. The two interfaces are distinct in ASP.NET Core. The MVC filter path could not use `ErrorResponseResult`. Fixed by making `ErrorResponseResult` implement both `IResult` and `IActionResult`, delegating `ExecuteResultAsync(ActionContext)` to `ExecuteAsync(HttpContext)`. The same result type now works in three pipelines: MVC filter, `[ApiController]` factory, minimal API. Finding recorded.
- **A-058** (Phase 1.4): `FieldKeyNormalizer` used `Substring`, which allocates a new string per call. `CA1846` recommends span-based slicing. Fixed by replacing the two `Substring` calls with `AsSpan` calls. Lesson: `StringBuilder.Append` accepts `ReadOnlySpan<char>` in modern .NET. Prefer span slicing over `Substring` when the target accepts a span. Finding recorded.
- **A-059** (Phase 1.4): The one-argument `ModelStateAdapter.ToErrors(ModelStateDictionary)` overload had an XML doc that referenced a `keyTransform` parameter that does not exist on that overload. The compiler reported `CS1734`. Fixed by rewording the summary to describe the equivalent two-argument call rather than referencing a nonexistent parameter. Lesson: when duplicating XML docs across overloads, remove references to parameters that only exist on the other overload. Finding recorded.
- **A-060** (Phase 1.4): The test file imported `Microsoft.AspNetCore.Mvc.Abstractions` for `ActionDescriptor` but not `Microsoft.AspNetCore.Mvc`, where `ActionContext` lives. The compiler reported `CS0246` for `ActionContext`. Fixed by adding the top-level MVC using. Lesson: the Abstractions subnamespace is not a catch-all for foundational MVC types. Finding recorded.
- **A-061** (Phase 1.4, process): A corrected block claimed to use PascalCase property names in the wire-shape test assertions, but the actual content kept the lowercase form. The test failed at runtime. Lesson: when describing a correction, verify that the block content matches the description. A corrected block that silently keeps the defect is worse than no correction. Finding recorded.
- **A-062** (Phase 1.4, process): A PowerShell block whose content included C# XML doc tags was emitted through the paste stream such that some lines were interpreted by PowerShell as top-level commands, producing parse errors. The array assignment survived intact and the final file was correct. Lesson: blocks with heavy XML doc content may need to be split into smaller sections. Finding recorded.
- **A-063** (Phase 1.4): Three MVC integration test files imported `Microsoft.AspNetCore.Builder` for `UseRouting` and `MapControllers` but not `Microsoft.Extensions.DependencyInjection`, where `AddControllers` is defined. The compiler reported `CS1061` for `AddControllers`. Fixed by adding the DI using. Lesson: `AddControllers` and friends are DI extensions, not builder extensions. Finding recorded.
- **A-064** (Phase 1.4, resolved in Backfill B.1): The initial A-042 fix scope was `LogSafely` only and missed a second `_logger.LogError(...)` call in `InvokeAsync` for the response-already-started branch. `CA1848` fired again after the suppression was removed. Fixed by adding a `LogResponseStartedDelegate` and using it in the branch. Lesson: when removing a suppression, grep the entire file for every call to the suppressed method family. Finding recorded.
- **A-065** (Phase 1.4, resolved in Backfill B.2): When `ApiPilotJsonOptions.cs` and `ApiExceptionOptions.cs` moved to `Configuration/`, they lost same-namespace access to `EnumSerializationMode`, `DateSerializationMode`, and `KnownExceptionType`. Three `CS0246` errors. Fixed by adding the corresponding usings. Lesson: when moving a file across namespaces, check what types the file itself uses from the namespace it is leaving. Finding recorded.
- **A-066** (Phase 1.4, process): A verification block used a PowerShell string containing three consecutive hyphen characters inside single quotes. The terminal misparsed the hyphen sequence as the decrement operator, producing a parser error. The file edits above the verification ran correctly. Lesson: prefer simple separators like `===` in verification output blocks. Finding recorded.
- **A-067** (Phase 1.4, resolved in Backfill B.3): When the `ApiPilotValidationFilter` constructor was extended with a second parameter, the splice order produced a constructor body with the `_keyTransform` assignment first, then the null-check and `_exceptionOptions` assignment. Functionally correct, order differs from the intended sequence. Lesson: when a block applies multiple splices to the same method, verify the resulting code compiles; if the semantics are correct, the order of independent statements does not matter. Finding recorded.
- **A-068** (Phase 1.4, resolved in Backfill B.3): After the `ErrorResponseResult` constructor was extended to accept `ApiExceptionOptions?`, the XML doc block was left with a duplicate `param` tag for `response` (the old doc block was not fully replaced). Additionally, the new `exceptionOptions` parameter on `ApiPilotValidationFilter` had no matching `param` doc. Both were caught by the compiler as `CS1571` and `CS1573`. Fixed by removing the duplicate and adding the missing tag. Lesson: when a constructor signature changes, its XML doc block moves with it as an atomic unit. Finding recorded.
- **A-069** (Phase 1.4, resolved in Backfill B.3): The `ApiPilotValidationFilter` constructor signature change broke two test call sites (the `BuildFilter` helper and the `Constructor_RejectsNullOptions` test). Both were fixed with the `Options.Create(new ApiExceptionOptions())` pattern. Lesson: constructor signature changes require a full-tree grep for constructor invocations, not just a check on the production call sites. Finding recorded.
- **A-070** (Phase 1.4): `ApiExceptionOptions` is registered through `AddApiPilotExceptions`, which also registers the exception middleware. An application that wants to override only the error-code-to-status map (used by the MVC validation filter and the `[ApiController]` factory) must call this extension. The extension works but its name no longer matches all its responsibilities. The library works for the MVC override case; the naming is a documentation concern. Finding recorded for a future cleanup.
- **A-071** (Backfill): The forbidden-symbol scan used substring matching, which false-positived on the substring `nLog` inside `KnownExceptionLogLevel` and `UnknownExceptionLogLevel`. Three files were flagged as containing forbidden symbol `NLog`. Fixed by using word-boundary regex matching in the scan. Lesson: forbidden-symbol scans over source code must use word-boundary matching. Finding recorded.

### Exclusions

- Retry, circuit breaker, idempotency, and payment processing remain explicitly excluded. No third-party NuGet package is permitted in production or test projects. The boundary is enforced by `audit.ps1` on every run.

## [0.1.0-alpha.0] - 2026-09-17

### Added

- Initial repository layout mirroring the Portfolio-style monorepo pattern:
  a language-neutral root (`docs/`, `.github/`, governance files) with a
  `dotnet/` implementation sub-root.
- `VERSION` file pinned to `0.1.0-alpha.0`.
- `.gitignore` covering .NET build outputs, IDE state, NuGet artifacts,
  test results, coverage, benchmark outputs, OS junk, and repository-local
  paths used during local NuGet verification.

[0.4.0]: https://github.com/REPLACE_ORG/ApiPilot/releases/tag/v0.4.0
[Unreleased]: https://github.com/REPLACE_ORG/ApiPilot/compare/v0.1.0-alpha.1...HEAD
[0.1.0-alpha.1]: https://github.com/REPLACE_ORG/ApiPilot/releases/tag/v0.1.0-alpha.1
[0.1.0-alpha.0]: https://github.com/REPLACE_ORG/ApiPilot/releases/tag/v0.1.0-alpha.0

