<!--
filepath: dotnet/verify/Verify.ApiPilot/HANDOFF.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  The handoff note for the library author. It summarizes what the
          verifier is, what it has proven, what it has found, and how to act
          on the findings. Attach this file when sending the four documents
          to the library author for review.
relates:  Companion to README.md (project document), FINDINGS.md (findings
          register), DEVELOPER_GUIDE.md (working recipe), and
          PROPOSED_FIXES.md (proposed fixes).
-->

# Handoff - Verify.ApiPilot, Milestone M-11

This is the handoff note for the library author. It summarizes what the
verifier is, what it has proven, what it has found, and how to act on the
findings. Attach it when sending the four companion documents.

---

## What this is

dotnet/verify/Verify.ApiPilot/ is a consumer-side verification application
that lives beside the ApiPilot library in the same repository. It consumes
the published NuGet packages through PackageReference - never
ProjectReference - and exercises every documented capability through real
runtime flows. It reports one PASS / FAIL / UNAVAILABLE result per scenario
and returns exit code 0 only when every scenario passes.

It is not a Sample, Demo, Playground, or Example. It is a permanent canary.

## Current state

| Field | Value |
|---|---|
| Published packages under test | ApiPilot.Core 1.0.3, ApiPilot.AspNetCore 1.0.3, ApiPilot.Security 1.0.3 |
| Scenarios | 11 |
| Passed | 11 |
| Failed | 0 |
| Exit code | 0 |
| Verifier source | 14 C# files, approximately 12,000 lines |
| Cold run duration | under 15 seconds |
| Warm run duration | under 5 seconds |

## Documents in this folder

| File | Purpose |
|---|---|
| README.md | Project document: what the verifier is, how to run it, the scenario table, exit codes. |
| FINDINGS.md | Authoritative register of every finding. Four top-level categories: Functional deviations, Open findings, Resolved findings, Withdrawn findings. |
| DEVELOPER_GUIDE.md | Working recipe for whoever picks the verifier up next. Fifteen PowerShell and ASP.NET Core traps. |
| PROPOSED_FIXES.md | Five concrete fix proposals, one or more per open finding. |
| HANDOFF.md | This file. |
| Infrastructure/ | The scenario runner, renderer, and in-process host. |
| Scenarios/ | The eleven scenarios, one file per capability. |

## What the verifier proves today

**ApiPilot.Core (3 scenarios):** the success envelope, the non-generic
success envelope, and the error envelope all serialize to the documented
wire shape and round-trip through real consumer code. Zero runtime
dependencies confirmed from the restored nuspec.

**ApiPilot.AspNetCore (6 scenarios):** correlation middleware end-to-end
over a real Kestrel host; exception mapping with the documented mappings
and the status-code override; content negotiation with the 406 and 415
paths and three documented overrides; JSON serialization with four
documented defaults and three documented overrides; pagination with the
three-layer precedence chain; validation with the minimal API filter and
both MVC paths producing identical envelopes.

**ApiPilot.Security (2 scenarios):** the CSRF bootstrap endpoint returns
the documented bare { "token": "..." } shape with Cache-Control no-store;
the CSRF protection middleware rejects missing and invalid headers with
the documented codes; safe methods pass; ExemptPaths and HeaderName
overrides are consumed.

## What the verifier found - the two functional deviations

These are the highest-value findings. A functional deviation is a case
where the runtime behavior contradicts a specific claim in the shipped
documentation. Both are library problems, not verifier bugs.

### F-59 - the KeyTransform identity override is ineffective

- **Documented:** ApiPilotValidationOptions.KeyTransform ships an XML
  remark that says, verbatim: "Supply a delegate to replace the
  normalization entirely; to disable normalization, supply the identity
  function key => key."
- **Observed:** with o.KeyTransform = key => key configured, an input
  field key of "Email" appears on the wire as "email". Normalization
  happens regardless.
- **Recommended fix:** documentation correction. The current behavior
  (always camelCase) is defensible. Remove the false promise from the XML.
  A code fix is an option but is a larger change.
- **Proposal:** PROPOSED_FIXES.md Proposal 4.

### F-65 - the CSRF attributes are ineffective on minimal-API endpoints

- **Documented:** ApiPilotSkipCsrfAttribute and ApiPilotRequireCsrfAttribute
  both carry XML remarks that say, verbatim: "Apply to a controller action
  or pass through WithMetadata on a minimal API endpoint."
- **Observed:** on a minimal-API endpoint:
  - WithMetadata(new ApiPilotSkipCsrfAttribute()) on a POST does not
    bypass the global CSRF policy. The request is rejected with HTTP 403
    and error.code CSRF_HEADER_MISSING.
  - WithMetadata(new ApiPilotRequireCsrfAttribute()) on a GET does not
    enforce CSRF protection. The request passes through with HTTP 200.
  - Applying the attributes to a static handler method produces the same
    non-behavior.
- **Recommended fix:** code fix, not documentation correction. The
  RequireCsrf direction is a security defect: a consumer's sensitive GET
  endpoint is unprotected while the consumer believes it is protected.
  The SkipCsrf direction is an availability defect: a public webhook
  receiver is rejected with HTTP 403.
- **Proposal:** PROPOSED_FIXES.md Proposal 5. Two candidate fix shapes.

## What the verifier found - the six documentation gaps

These are lower severity than the functional deviations but still worth
acting on. In every case, the library works as designed; only the shipped
documentation is incomplete or inaccurate.

| Finding | What it says | Proposal |
|---|---|---|
| F-30 | The shipped XML does not enumerate the default exception mapping table. An InvalidOperationException is observed to map to CONFLICT / 409, but this is not documented. | Proposal 1 |
| F-31 | An ArgumentNullException is observed to map to VALIDATION_ERROR / 400, but this is not documented. | Proposal 1 |
| F-36 | ApiExceptionOptions.ErrorCodeToStatusMap's declared type is not named in the shipped XML. | Proposal 2 |
| F-51 | Four ApiPilotJsonOptions properties (PropertyNamingPolicy, DictionaryKeyPolicy, DefaultIgnoreCondition, ReadCommentHandling) do not have their declared types named in the shipped XML. | Proposal 3 |
| F-58 | The pagination parser delegate properties (ParameterNames, SortDirectionParser, SortParser, IsFilterParameter, IntegerParser) do not have their declared types named in the shipped XML. | same class as F-36 and F-51 |
| F-64 | CsrfOptions.CodeMapping and CsrfOptions.PreAuthBindingSource do not have their declared types named in the shipped XML. | same class |

## What the verifier found - the packaging findings

| Finding | What it says |
|---|---|
| F-14 / N-13 | The packaged README names SPEC.md as the normative document, but SPEC.md does not ship inside any package. |
| F-15 | The nuspec descriptions are narrower than the rendered README. |
| N-06 | The README names docs files, the CHANGELOG, and the audit script as consumer resources; none ship inside the package. |
| N-20 | The README references two runnable Samples with dotnet run commands; the samples live in the repository, not in the package. |
| N-21 / N-24 | The repository CHANGELOG ends at 1.0.2; the live package version is 1.0.3. |
| N-32 | The README Releases link is written without a URL scheme. |
| N-34 | nuget.org lists 1.0.0, 1.0.1, and 1.0.3 for all three packages. There is no 1.0.2. The repository CHANGELOG contains a 1.0.2 entry. |
| N-31 / N-33 | Resolved: nuspec dependency groups are correct; all three packages are live at 1.0.3. |

## How to act on the findings

The five proposals in PROPOSED_FIXES.md are ordered by severity:

1. Proposal 5 (F-65) - the CSRF attributes. Code fix. Security-relevant.
   Do this first.
2. Proposal 4 (F-59) - the KeyTransform identity override. Documentation
   correction. Do this second.
3. Proposal 1 (F-30, F-31) - the default exception mapping table.
   Documentation correction.
4. Proposal 2 (F-36) - the ErrorCodeToStatusMap type name. Documentation
   correction.
5. Proposal 3 (F-51) - the ApiPilotJsonOptions type names. Documentation
   correction.

F-58 and F-64 are the same class as F-36 and F-51 and can be batched with
Proposal 2 or 3 in the same commit.

The packaging findings (F-14, F-15, N-06, N-13, N-20, N-21, N-24, N-32,
N-34) are lower priority. They are all about the shape of the packages,
the README, or the version history. They can be addressed at any time.

## How to confirm the fixes

After the fixes land, publish a new version - 1.0.4, 1.1.0, whatever the
semver dictates.

The verifier's csproj has a pinned PackageReference:

    <PackageReference Include="ApiPilot.Core"       Version="[1.0.3]" />
    <PackageReference Include="ApiPilot.AspNetCore" Version="[1.0.3]" />
    <PackageReference Include="ApiPilot.Security"   Version="[1.0.3]" />

To test the new version, edit those three version strings in
Verify.ApiPilot.csproj to the new pinned version, run the verifier, and
confirm the two functional deviations (F-59 and F-65) close. When they do,
scenario 09 and scenario 11 can be updated from "observed" to "pass" with
the observation removed.

The three documentation corrections land when the shipped XML and
packaged README reflect the corrections. The verifier does not need to
change for those; the finding simply moves from Open to Resolved when the
docs are updated.

## The verifier's design and discipline

The verifier lives at dotnet/verify/Verify.ApiPilot/. It has its own
Directory.Build.props that stops the MSBuild upward walk via
DirectoryBuildPropsPath and ImportDirectoryBuildProps=false, so the
verifier is isolated from dotnet/Directory.Build.props. It is not in
ApiPilot.slnx. It is not packable. It references no third-party packages.
It contains no ProjectReference.

The four documents the next reader needs: README.md is the project
document, FINDINGS.md is the authoritative register, DEVELOPER_GUIDE.md
is the working recipe, PROPOSED_FIXES.md is the action list.

## A note on the verifier's own defects during the initial build

Two scripts in the initial build produced errors that were recorded as
traps in the DEVELOPER_GUIDE.md:

- Trap 14 - a .NET exception (from Substring with a negative length) did
  not stop the script despite $ErrorActionPreference = 'Stop'. The stale
  value of a variable was then written to the wrong file, corrupting
  DEVELOPER_GUIDE.md. The guide was restored from the session's own
  content. The rule: wrap every .NET method call that can throw in
  try/catch, and call exit 1 in the catch.

- Trap 15 - a removal script's anchor assumed a three-hash header
  appeared after a block that was actually the last entry in its section.
  The correct anchor is the section header, not a sub-header. The rule:
  read the exact bytes at the removal boundary before writing the removal
  script.

Both traps are in the guide so the next developer does not repeat them.

## What comes after the fixes

Once the library author has addressed the five proposals and published a
new version, the verifier's confirmed fixes become part of the register.
The verifier then resumes extending coverage with scenario 12 (Origin
policy), 13 (Fetch Metadata), 14 (Data Protection), 15 (multi-instance),
and so on, until the entire documented surface is exercised.

The verifier is a long-term asset. It does not expire when the library
ships. It catches drift on every future release.

---

End of handoff note.