<!--
filepath: dotnet/verify/Verify.ApiPilot/README.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  The public document for the ApiPilot Consumer Verification
          Application. It states what the verifier is, why it consumes the
          published NuGet artifact, how to run it, which scenarios exist,
          what output to expect, and the complete register of findings.
relates:  Consumes ApiPilot.Core, ApiPilot.AspNetCore, ApiPilot.Security.
          Not part of ApiPilot.slnx. Not a published package.
-->

# Verify.ApiPilot

**The ApiPilot Consumer Verification Application.**

A permanent, runnable canary that consumes the published ApiPilot NuGet
packages exactly as an external developer would, exercises every documented
capability through real runtime flows, and fails loudly when the shipped
artifact drifts from its documentation or from its own runtime behavior.

This is not a Sample, Demo, Playground, or Example. It is a canary.

Current baseline: 11 scenarios, all passing against ApiPilot packages 1.0.3.

---

## What this is

Verify.ApiPilot is a console application that lives beside the ApiPilot
library in the same repository. It restores the three published NuGet
packages through PackageReference, calls their public API through the shipped
DLL and the shipped XML documentation, asserts observable behavior, and
reports one visible PASS, FAIL, or UNAVAILABLE result per scenario.

The verifier exists so that packaging defects, metadata defects, dependency
resolution defects, and consumer-surface drift are caught before any real
consumer of ApiPilot sees them.

---

## Why it consumes the published package

The published NuGet artifact is the authority. The verifier restores that
artifact from nuget.org and consumes it through PackageReference. It inspects
the files actually present in each restored package, including the DLL, the
XML documentation, the nuspec, and the README where present. A package README
is a package asset the author configures and embeds; it is not automatic
merely because the repository has a README.

In-repo tests validate the source graph. They cannot catch:

- a file that was not included in the nupkg;
- a nuspec with the wrong dependency group or the wrong target framework;
- a package that resolves to a different version than intended;
- a public member that exists in the source tree but not in the packed
  assembly, or vice versa.

The verifier catches all four classes, because it consumes only the shipped
artifact.

---

## Commands

From the repository root:

    dotnet restore .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj
    dotnet build   .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj -c Release
    dotnet run --project .\dotnet\verify\Verify.ApiPilot -c Release

The verifier must not be added to ApiPilot.slnx. It is built by its own
command. It has its own Directory.Build.props and does not use the
library-wide build rules in dotnet/Directory.Build.props. The isolation is
set with two explicit MSBuild controls in the verifier own configuration:

    <DirectoryBuildPropsPath>$(MSBuildThisFileDirectory)Directory.Build.props</DirectoryBuildPropsPath>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>

The isolation has been confirmed by an MSBuild property dump. The evaluated
Version is 0.0.0, the evaluated Product is Verify.ApiPilot, and the
evaluated GenerateDocumentationFile is false. None of these values can come
from the parent Directory.Build.props.

### Clean verification

To verify that the run is using the restored package assets:

    dotnet nuget locals global-packages --list
    dotnet restore .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj
    dotnet build   .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj -c Release
    dotnet run --project .\dotnet\verify\Verify.ApiPilot -c Release --no-build

The banner must report exactly 1.0.3 for all three ApiPilot packages.

---

## Package references

Pinned mode. The verifier certifies exactly one published version.

    <PackageReference Include="ApiPilot.Core"       Version="[1.0.3]" />
    <PackageReference Include="ApiPilot.AspNetCore" Version="[1.0.3]" />
    <PackageReference Include="ApiPilot.Security"   Version="[1.0.3]" />

Advancing the tested version is a deliberate edit by a human. It is never an
automatic range resolution. A compatibility range such as [1.0.3,2.0.0) is
an explicit opt-in, never the default.

### How to advance the tested version

1. Edit the three Version attributes in Verify.ApiPilot.csproj to the new
   pinned version.
2. Update the expected version in this README if the pinned version changes.
3. Run the verifier. Confirm the banner prints the new resolved version for
   all three packages. Confirm exit code 0.

If any package resolves to a different version than the one pinned, the run
fails.

---

## Resolved versions

The banner prints the resolved version of every ApiPilot package at every
run. The versions are read from obj/project.assets.json, not assumed. The
resolved versions are what the verifier actually tests.

Expected for the current commit:

    ApiPilot.AspNetCore -> 1.0.3
    ApiPilot.Core       -> 1.0.3
    ApiPilot.Security   -> 1.0.3

---

## Scenarios

The scenario catalog runs in explicit order. Each scenario uses only the
public API described in the shipped XML and the packaged README. Each
scenario returns exactly one ScenarioResult: Passed, Failed, or Unavailable.

| #  | Name                | Package              | What it asserts | Current |
|----|---------------------|----------------------|-----------------|---------|
| 01 | CoreEnvelopeSuccess | ApiPilot.Core        | The success envelope built via ApiResponseBuilder.Ok<T> serializes to the documented wire shape: top-level success, data, message, meta; meta carries requestId, timestamp, extra; status is absent. | PASS |
| 02 | NoContent           | ApiPilot.Core        | The non-generic success envelope built via ApiResponseBuilder.NoContent serializes without a data key and without a status key. | PASS |
| 03 | ErrorEnvelope       | ApiPilot.Core        | The error envelope built via ApiError.Create and ErrorResponse serializes to the documented error wire shape: success false, error.code, error.message, error.fields as a map of arrays, meta.requestId. The serialized payload contains no stack-trace frame, no Exception type name, no StackTrace, and no InnerException. | PASS |
| 04 | Correlation         | ApiPilot.AspNetCore  | The correlation middleware, placed in a real pipeline on a real Kestrel host, generates the default header when absent, echoes a valid incoming header, replaces an invalid incoming header, and consumes the CorrelationOptions.HeaderName override. The response header value equals meta.requestId in every sub-check. | PASS |
| 05 | ExceptionMapping    | ApiPilot.AspNetCore  | The exception middleware maps a mapped exception, an unmapped exception, and a custom-mapped scenario-local exception to the standard error envelope; the ErrorCodeToStatusMap whole-value setter is consumed; the success path is unaffected; no internal exception detail leaks. Observed mappings: ArgumentNullException produces VALIDATION_ERROR at HTTP 400; InvalidOperationException produces CONFLICT at HTTP 409. | PASS |
| 06 | ContentNegotiation  | ApiPilot.AspNetCore  | The content negotiation middleware rejects an unacceptable Accept with HTTP 406 and error.code NOT_ACCEPTABLE, rejects an unacceptable Content-Type on a body-carrying method with HTTP 415 and error.code UNSUPPORTED_MEDIA_TYPE, honours the documented defaults, and consumes the AcceptWildcard, AcceptMissingContentType, and AcceptableResponseMediaTypes overrides. | PASS |
| 07 | JsonSerialization   | ApiPilot.AspNetCore  | The JSON serialization conventions: the documented defaults (camelCase, string enums, ISO 8601 dates, explicit nulls) are on the wire, and the documented overrides (EnumMode, DateMode, PropertyNamingPolicy) are consumed. JsonSerializerConfigurator.Configure is idempotent on converter registration. | PASS |
| 08 | Pagination          | ApiPilot.AspNetCore  | The pagination defaults (DefaultPageSize 20, MaxPageSize 100, SuccessStatusCode 200) are on the wire; the three-layer override chain (global, fluent, attribute) is consumed and the attribute wins over the fluent; GetPageRequest and GetEffectivePaginationOptions return the effective values in the handler; the paginated envelope shape matches the packaged README. | PASS |
| 09 | Validation          | ApiPilot.AspNetCore  | The minimal API validation filter and both MVC paths (attribute and [ApiController]) produce the standard VALIDATION_ERROR envelope; the two MVC paths agree on error.code and error.fields; the default camelCase KeyTransform is on the wire; the KeyTransform identity override is observed but not honored (F-59). | PASS |
| 10 | CsrfBootstrap       | ApiPilot.Security    | The CSRF bootstrap endpoint returns the documented bare { "token": "..." } shape (one property, non-empty string, no envelope wrappers) with Cache-Control no-store. The CsrfBootstrapOptions.Enabled=false override suppresses the endpoint. The CsrfOptions.BootstrapPath override is consumed. | PASS |
| 11 | CsrfProtection      | ApiPilot.Security    | The CSRF middleware rejects a missing header with CSRF_HEADER_MISSING and an invalid header with CSRF_TOKEN_INVALID, both at HTTP 403. Safe methods pass the global policy. CsrfOptions.ExemptPaths and CsrfOptions.HeaderName overrides are consumed. The [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf] attributes on minimal-API endpoints are observed but not honored (F-65). | PASS |

---

## Expected output

    ================================================================================
      ApiPilot Consumer Verification Application
      Verify.ApiPilot
    ================================================================================
      Resolved package versions:
        ApiPilot.AspNetCore -> 1.0.3
        ApiPilot.Core -> 1.0.3
        ApiPilot.Security -> 1.0.3
    ================================================================================

    [PASS] 01 - 01_CoreEnvelopeSuccess
    [PASS] 02 - 02_NoContent
    [PASS] 03 - 03_ErrorEnvelope
    [PASS] 04 - 04_Correlation
    [PASS] 05 - 05_ExceptionMapping
    [PASS] 06 - 06_ContentNegotiation
    [PASS] 07 - 07_JsonSerialization
    [PASS] 08 - 08_Pagination
    [PASS] 09 - 09_Validation
    [PASS] 10 - 10_CsrfBootstrap
    [PASS] 11 - 11_CsrfProtection

    Summary:
      Passed:      11
      Failed:      0
      Unavailable: 0
      Total:       11
    Exit: 0

---
## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Every required scenario passed. |
| 1 | At least one required scenario failed. |
| 2 | No failures, but zero scenarios executed. The silent-success guard. |

An Unavailable result is visible and is never folded into the pass count. If
a scenario cannot run because a prerequisite is unavailable, the run reports
it and the exit code is nonzero unless the prerequisite is explicitly
accepted.

---

## Findings from the published artifact

The complete register of findings the verifier has produced against the
published ApiPilot artifacts is in [FINDINGS.md](FINDINGS.md). The file is the
authoritative record of every observation, its evidence, its impact, and its
status. This README does not duplicate that register.

The draft of the library-side documentation fixes that would close the open
documentation findings is in [PROPOSED_FIXES.md](PROPOSED_FIXES.md). Those
proposals are drafts for the library author; nothing in this folder modifies
the library source tree.

The focused action list of only the functional deviations - the cases where
runtime behavior contradicts the shipped documentation - is in
[DEVIATIONS.md](DEVIATIONS.md). That file is the priority list for the
library author.
---
## How to add a scenario

1. Create Scenarios/NN_NameScenario.cs, where NN is the next two-digit number
   and Name describes the capability. Use the first scenario file under
   Scenarios/ as the template.
2. Implement public static ScenarioResult Run() or RunAsync(). Use only the
   public API described in the shipped XML and the packaged README. Assert
   observable behavior. Do not test internals. Do not use dotnet/src,
   dotnet/tests, or dotnet/samples as API or behavioral authority.
3. Add one line to Program.cs in the results collection, in the intended
   order.

Run the verifier. The new scenario reports its own PASS, FAIL, or UNAVAILABLE
result and counts in the summary.

---

For a complete working recipe - including how to read the shipped XML, how to
reflection-probe the DLL when the XML is silent, and the PowerShell 5.1 traps
that cost time in the initial build - see [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md).
## Scope

This project:

- consumes ApiPilot.Core, ApiPilot.AspNetCore, and ApiPilot.Security from
  nuget.org through PackageReference;
- is not added to ApiPilot.slnx;
- is not a published package (IsPackable=false);
- does not use dotnet/Directory.Build.props; it has its own
  Directory.Build.props and sets DirectoryBuildPropsPath and
  ImportDirectoryBuildProps explicitly;
- does not use dotnet/src, dotnet/tests, dotnet/samples, or artifacts/probe-*
  as API or behavioral authority;
- does not use SPEC.md, the docs files, or the repository CHANGELOG as API
  authority.

---

## Links

- ApiPilot.Core on nuget.org: https://www.nuget.org/packages/ApiPilot.Core
- ApiPilot.AspNetCore on nuget.org: https://www.nuget.org/packages/ApiPilot.AspNetCore
- ApiPilot.Security on nuget.org: https://www.nuget.org/packages/ApiPilot.Security
- Repository: https://github.com/sancy1/ApiPilot
- Issues: https://github.com/sancy1/ApiPilot/issues

---

## License and provenance

This project is part of the ApiPilot repository. It inherits the repository
MIT license. It is not a published package. It is not part of ApiPilot.slnx.

The ApiPilot library itself is authored by Alexander Sanchez Cyril (@sancy1).
The verifier is a consumer-side application, not a library artifact.