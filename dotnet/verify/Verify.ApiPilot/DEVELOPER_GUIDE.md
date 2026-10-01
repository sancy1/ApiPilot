<!--
filepath: dotnet/verify/Verify.ApiPilot/DEVELOPER_GUIDE.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  The working recipe for the developer who picks up this verifier next.
          How to run it, how to add a scenario, how to read the shipped XML,
          how to reflection-probe the DLL when the XML is silent, the PowerShell
          5.1 traps that cost time in the initial build, and the append-only
          discipline for growing documents.
relates:  Complements README.md (project document) and FINDINGS.md (findings
          register). The three files together are the complete handover.
-->

# Developer Guide

This is the working recipe for the developer who picks this verifier up next.
It records what I learned building the first eleven scenarios, including the
mistakes that cost time and produced no finding. Read it before you write a
single line of scenario code. You should not have to rediscover any of this.

---

## 1. The three files

| File | Purpose |
|---|---|
| `README.md` | What the verifier is, how to run it, the scenario table. Project document. |
| `FINDINGS.md` | The authoritative register of every finding. Evidence. Never a project description. |
| `DEVELOPER_GUIDE.md` | This file. The recipe. |

Do not mix them. The README describes the project. FINDINGS records what the
verifier observed. This file tells you how to extend both without breaking
them.

---

## 2. Running the verifier

From the repository root:

    dotnet restore .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj
    dotnet build   .\dotnet\verify\Verify.ApiPilot\Verify.ApiPilot.csproj -c Release
    dotnet run --project .\dotnet\verify\Verify.ApiPilot -c Release --no-build

Exit codes: 0 all pass, 1 any fail, 2 zero scenarios executed.

The banner prints the resolved version of every ApiPilot package at every run.
Those versions come from `obj/project.assets.json`. They are what the verifier
actually tests. If a package resolves to a different version than the pinned
one, the run should be treated as invalid.

---

## 3. Adding a scenario - the exact recipe

1. Read the shipped XML first. Do not write the scenario from memory of the
   README. The XML is the API authority. Locate it at
   `%USERPROFILE%\.nuget\packages\<package-id>\<version>\lib\net10.0\<Package>.xml`.
2. Enumerate the members you need. Print the exact `<member>` elements for
   every type and method your scenario will call. Note the parameter types.
3. Read the packaged README at
   `%USERPROFILE%\.nuget\packages\<package-id>\<version>\README.md`. Cross-check
   that the behavior you intend to assert is what the README documents.
4. Write the scenario file. One class, one static `Run()` or `RunAsync()`
   method that returns `ScenarioResult`. Follow the pattern in
   `Scenarios/06_ContentNegotiationScenario.cs`. Each sub-check starts its own
   host via `InProcessHost.StartAsync` so no state leaks between sub-checks.
5. Wire it into the catalog. Open `Program.cs`. Find the line for the
   previous scenario. Insert your new line directly after it. Do not rewrite
   the file.
6. Build. Then run. Stop on red.
7. Append to both documents. `README.md` gets a new row in the scenario
   table and the milestone block. `FINDINGS.md` gets the new findings.
   Never rewrite either file.

---

## 3a. When a scenario produces a functional deviation

A functional deviation is a case where the runtime behavior of the shipped
artifact contradicts a specific claim in the shipped documentation. It is
not a documentation gap.

When a scenario produces a functional deviation, append to two files:

1. FINDINGS.md - the full register entry, with all evidence. Place it in
   the "## Functional deviations" section, not in the "## Open findings"
   section. Give it an F- number, a package and version, the documented
   behavior (quoted verbatim from the shipped XML or packaged README), the
   observed behavior, the reproduction, the impact, the recommended
   resolution, the security relevance, the performance relevance, and the
   status.
2. DEVIATIONS.md - the focused action entry. Add a row to the Summary
   table. Add the full entry in the same shape as the FINDINGS.md entry,
   but shorter. Link to the corresponding proposal in PROPOSED_FIXES.md.

Then draft a proposal in PROPOSED_FIXES.md if the fix is not obvious.

A milestone append is not complete until both FINDINGS.md and
DEVIATIONS.md reflect any new deviation. Do not leave the deviation only
in FINDINGS.md; the focused action list is what the library author reads
first.
## 4. Reading the shipped XML - the pattern that works

    $xmlPath = Join-Path $env:USERPROFILE '.nuget\packages\apipilot.aspnetcore\1.0.3\lib\net10.0\ApiPilot.AspNetCore.xml'
    $raw = [System.IO.File]::ReadAllText($xmlPath)

    function Show-Members {
        param([string]$Pattern)
        $matches = [regex]::Matches($raw, '<member name="' + $Pattern + '">.*?</member>', 'Singleline')
        foreach ($m in $matches) { Write-Host $m.Value; Write-Host "" }
    }

    Show-Members 'T:ApiPilot\.AspNetCore\.SomeType'
    Show-Members 'P:ApiPilot\.AspNetCore\.SomeType\.[^"]*'
    Show-Members 'M:ApiPilot\.AspNetCore\.SomeType\.[^"]*'

Regex patterns use `\.` for literal dots. `[^"]*` matches the remainder of the
member name. The `Singleline` option lets `.` span newlines inside one member
block.

---

## 5. When the XML does not name a type - reflection probe

Some properties are documented by their summary but not by their declared type.
`ApiExceptionOptions.ErrorCodeToStatusMap` is the canonical example. The XML
said it was "the Option D override point" and "applications populate this
dictionary", but the declared type was not in the summary. The XML alone was
not enough to write correct consumer code.

When this happens, use a reflection-only probe of the shipped DLL. Do not
execute the assembly. Do not read the library's source tree.

### The probe recipe

Create a temporary .NET 10 console project outside the verifier folder (in
`%TEMP%`). It must:

1. Target `net10.0` (the same as the packages).
2. Reference `System.Reflection.MetadataLoadContext` (version 9.0.0 or later).
3. Build a search path from `RuntimeEnvironment.GetRuntimeDirectory()` plus
   the `shared/Microsoft.NETCore.App` and `shared/Microsoft.AspNetCore.App`
   subfolders of the dotnet install root.
4. Add the package DLL under inspection and any other ApiPilot package DLL
   it depends on.
5. Pass the core assembly name `System.Private.CoreLib` explicitly to the
   `MetadataLoadContext` constructor.
6. Call `LoadFromAssemblyPath` on the DLL to inspect.
7. Enumerate types and members with `GetTypes()`, `GetConstructors()`,
   `GetProperties()`, and `GetMethods()`.

### Five attempts that failed before the working recipe

For the record, so you do not repeat them:

- `Assembly.Load(byte[])` in Windows PowerShell 5.1 - fails because PowerShell
  runs on .NET Framework 4.x, not .NET 10. The assembly loads but its
  dependency graph cannot be resolved.
- `MetadataLoadContext` in PowerShell - the type is not in the .NET Framework
  GAC. `New-Object` fails with "Cannot find type".
- A temp .NET 10 console project - the first run failed because the core
  assembly name was not passed. The error is "Could not find core assembly".
- The second run failed because `Microsoft.Extensions.Logging.Abstractions.dll`
  was not in the search path. The probe crashed when it hit
  `KnownExceptionLogLevel` (type `Microsoft.Extensions.Logging.LogLevel`).
- The third run succeeded after the search path was expanded to include the
  shared framework directories.

The working probe script is preserved in `FINDINGS.md` section "Notes on the
discovery method". Copy it and adapt the assembly path.

---

## 6. PowerShell 5.1 traps - the list

These cost real time. Each one caused a script to abort or a write to fail.
They are not stylistic. They are hard rules for this environment.

### Trap 1 - if in expression position is illegal

This fails:

    Write-Host ("BOM: " + (if ($b -eq 'X') { 'yes' } else { 'no' }))

With: `if : The term 'if' is not recognized...`

The fix:

    $state = 'no'
    if ($b -eq 'X') { $state = 'yes' }
    Write-Host ("BOM: " + $state)

Never use `if` inside `(...)`. Always assign to a variable first.

### Trap 2 - two consecutive hyphens inside an XML comment is illegal

This breaks a `.csproj` immediately:

    <!-- Run: dotnet run --project ... -->

With: `MSB4025: An XML comment cannot contain '--'`.

The fix: write the command in prose, or drop the double-hyphen flag from the
comment. For example, "dotnet run with the project argument" instead of
"dotnet run --project ...".

This rule applies to every XML comment in the project. csproj, props, targets,
and any XML file the verifier owns.

### Trap 3 - apostrophes inside single-quoted here-strings

In PowerShell single-quoted here-strings (`@'...'@`), an apostrophe does not
end the string. But if the string contains an apostrophe and it is then
re-parsed by an outer context, the parse breaks. The safe rule: if your
here-string contains apostrophes, write it as a double-quoted here-string
(`@"..."@`) and escape any `$` and backtick characters, or avoid apostrophes
in prose.

For single-quoted here-strings, the only escape is two single quotes for one.
Backslash is not an escape in PowerShell.

### Trap 4 - a here-string must be closed on its own line

The closing delimiter `'@` must be on its own line with nothing before it. If
it is missing, the shell enters continuation mode and shows `>>`. Press
Ctrl+C to abort. If it is missing because the paste channel truncated the
script, the entire here-string was absorbed and the shell is waiting for
more input.

The rule: every here-string ends with `'@` on its own line, followed by a
blank line, followed by the rest of the script, all inside one fenced block.

### Trap 5 - $matches is reserved

`$matches` is a PowerShell automatic variable. Do not use it as a counter.
Use `$hits`, `$count`, or `$occurrences`.

### Trap 6 - New-Object System.Collections.Generic.List[string] is a parse error

The angle brackets are not parsed. Use:

    [System.Collections.Generic.List[string]]::new()

or use a plain array.

### Trap 7 - fully-qualified ApiPilot.AspNetCore.X references inside a Verify.ApiPilot.* file

The verifier's root namespace is `Verify.ApiPilot`. A fully-qualified
reference to `ApiPilot.AspNetCore.SomeType` inside a `Verify.ApiPilot.Scenarios`
file can resolve incorrectly, because C# walks up the enclosing namespace
chain looking for the first segment `ApiPilot`. It finds `Verify.ApiPilot`
and then looks for `Verify.ApiPilot.ApiPilot.AspNetCore.SomeType`, which does
not exist. The compiler reports: "The type or namespace name 'AspNetCore'
does not exist in the namespace 'Verify.ApiPilot'".

The fix is always the same: add a `using` for the target namespace and use
the short type name. Do not write fully-qualified `ApiPilot.AspNetCore.*`
references in any file under `Verify.ApiPilot`.

When the compiler reports this error, grep the file for every occurrence
of the same fully-qualified pattern, not just the line the compiler named.
Multiple occurrences are common. Fixing them one at a time wastes a build
cycle per occurrence.

Example search:

    $lines | Where-Object { $_ -match 'ApiPilot\.AspNetCore\.' -and $_ -notmatch '^using ' }

Every match is a fix candidate.

### Trap 8 - bundled edits with multiple anchors can fail silently on one field

A single PowerShell script that performs two independent replaces can end up
applying only one of them. The script's anchor check typically runs once,
before the script begins, not per-anchor. If the first replace succeeds and
the second anchor is missing, the write goes through with only the first
change applied. Nothing in the output tells you the second change was
skipped.

The failure mode is: the baseline line stays at the previous milestone's
value while the scenario table and expected-output block advance to the new
milestone. Three numbers that must agree are now out of sync, and the drift
is invisible until something else fails.

The rule:

- One field per script. If a milestone requires three small edits, use three
  separate scripts, not one script that does three things.
- If a script must do more than one edit, verify each anchor separately
  before the write. Abort if any anchor is missing.
- After every milestone, verify these three numbers together: the baseline
  line, the scenario table row count, and the expected-output PASS line
  count. They must match. A one-line verification script that prints all
  three is the safety net.

### Trap 9 - a scenario payload helper must respect the validated request

The library produces pagination metadata. The application produces the paged
slice of items. Both must agree. If a scenario hands the library 100 items
and a metadata object describing a page of 20, the response correctly
serializes all 100 items, and the scenario fails on its own inconsistency,
not on an artifact defect.

Scenario 08 hit this. The helper returned the full 100-item array while the
metadata described a page of 20. The failing assertion was
"data array had 100 items, expected 20". The artifact was right. The helper
was wrong.

The rule: in any scenario that exercises pagination, slice the item array
according to the validated PageRequest before handing it to the response
builder. The library does not slice the array; the application does.

### Trap 10 - the [PaginationMetadata] attribute alone does not attach the pagination filter

The XML documents that the fluent WithApiPilotPagination extension attaches
the pagination endpoint filter. The [PaginationMetadata] attribute describes
per-endpoint overrides. Both must be present for the override to be consumed.
A scenario that applies only the attribute sees the global default, not the
attribute value.

Scenario 08 hit this. Sub-check 5 applied only the attribute, without the
fluent extension, and observed the global DefaultPageSize (20) instead of the
attribute value (5). The fix was to chain both.

The rule: whenever a scenario relies on a [PaginationMetadata] override,
attach the fluent extension in the same chain. The attribute describes; the
filter consumes.

### Trap 11 - CA1861 fires on any inline collection literal

The CA1861 analyzer rule fires on any collection literal that would be
allocated per call and passed to a method that may be called more than once.
The analyzer message mentions array arguments, but the rule covers every form:

- Method arguments: AssertSomething(new[] { "x" })
- Dictionary initializer values: ["key"] = new[] { "x" }
- List initializers: new List<T> { ... }
- Any collection literal constructed at the call site

Scenario 09 hit this nine times. The first sweep caught method arguments; the
second sweep caught dictionary initializer values.

The rule: before writing a scenario, list every collection literal in the
plan. Give each distinct value a static readonly field at the top of the
class. Reference the field by name at every call site.

### Trap 12 - after three targeted edits on a file, stop and rewrite it cleanly

Scenario 09 went through several rounds of targeted edits to the same file:
add missing usings, sweep fully-qualified references, add a helper method,
change a call site, add observations, remove a duplicate method. By the
third round, the file's structure was no longer certain. A removal script
deleted the wrong method.

The rule: after three targeted edits on the same file have landed and the
build still fails, stop and rewrite the file cleanly. Do not patch a file
whose structure is no longer known.

### Trap 13 - IEndpointMetadataProvider attributes are not invoked by WithMetadata

An attribute that implements IEndpointMetadataProvider produces endpoint
metadata through its PopulateMetadata method. The ASP.NET Core framework
invokes PopulateMetadata only when the attribute is applied to a handler
method that the framework treats as the endpoint target. Passing the
attribute instance to WithMetadata(...) attaches the instance to the
endpoint metadata collection but does not invoke PopulateMetadata. The
metadata the attribute would have produced is never written.

Scenario 11 hit this with the ApiPilot CSRF attributes. The shipped XML
says: "Apply to a controller action or pass through WithMetadata on a
minimal API endpoint." The runtime does not honor the second clause. Two
application patterns (WithMetadata and the static-handler-with-attribute
form) produced the same non-behavior. Recorded as finding F-65.

For a scenario developer: when a documented usage pattern for an
IEndpointMetadataProvider attribute does not work and the shipped XML
claims it should, check whether the attribute's own documentation claims
WithMetadata support. If it does, and the runtime does not honor it, the
finding is real. Do not assume the scenario is wrong.


Note: the general mechanism above is permanent. The specific CSRF
attribute issue that first exposed it - finding F-65 against 1.0.3 - was
fixed in 1.0.5. The library introduced CsrfEndpointPolicyResolver, a
shared internal resolver that reads both the canonical CsrfEndpointMetadata
record and the attribute instances, and applies the documented precedence
Require > Skip > UseGlobal. The lesson in this trap remains: a scenario
that encounters an inert IEndpointMetadataProvider attribute should first
check whether the framework is invoking PopulateMetadata before assuming
the scenario code is wrong. See PROPOSED_FIXES.md Proposal 5 and
DEVIATIONS.md entry F-65.

### Trap 14 - a .NET exception does not stop the script; the stale variable survives

`$ErrorActionPreference = 'Stop'` stops the script on a PowerShell cmdlet
error. It does NOT stop the script on a .NET method call that throws. For
example, `$str.Substring(0, -1)` throws an ArgumentOutOfRangeException from
the .NET runtime. The exception is reported to the console, but PowerShell
continues to the next line.

The consequence: if a variable was assigned earlier in the session and a
later reassignment throws, the variable still holds the earlier value.
If the script then writes that variable to a file, the file receives the
stale content.

That is what destroyed DEVELOPER_GUIDE.md in the initial build. A failed
`Substring` left `$after` holding the README's content, and the script
wrote it to the developer guide's path.

The rule: wrap every .NET method call that can throw in a try/catch, and
call `exit 1` in the catch. Do not rely on `$ErrorActionPreference` alone.

    try {
        $after = $before.Substring(0, $idx) + $append + $before.Substring($idx)
    } catch {
        Write-Host "Substring failed: " + $_.Exception.Message
        exit 1
    }

Also: before writing any file, verify the variable that is about to be
written holds the expected content. A length check and a first-line check
are enough.

### Trap 15 - before a removal script, read the exact bytes at the removal boundary

A removal script that uses a heuristic anchor ("the next ### header within
N characters") will fail when the boundary it assumes does not match the
file. The failure mode is silent when the script aborts on a sanity check
and noisy when it does not.

The F-59 removal in this session assumed a three-hash header (`### `) would
appear after the F-59 block. In fact, F-59 was the last entry in its section
and the next boundary was a two-hash section header (`## Withdrawn
findings`), more than 600 characters away. The correct anchor is the section
header, not a sub-header.

The rule: before writing a removal script, read the exact bytes at the
removal boundary. Print the last few lines of the block, the blank line if
any, and the first line of the next content. Then build the anchor from what
you see, not from what you remember writing.

### Trap 16 - before writing a multi-line anchor, check the file line endings

A PowerShell script that builds a multi-line anchor with CRLF (`r`n) will
never match a file that uses LF-only line endings. The FINDINGS.md,
DEVIATIONS.md, PROPOSED_FIXES.md, and HANDOFF.md files in this repository
use LF-only. README.md has mixed line endings.

The check, before any multi-line write:

    $text = [System.IO.File]::ReadAllText($path)
    $crlf = ([regex]::Matches($text, "`r`n")).Count
    $lf   = ([regex]::Matches($text, "`n")).Count

If CRLF count is 0, the file is LF-only: use `n in anchors. If CRLF count
equals LF count, the file is CRLF: use `r`n. If mixed, prefer single-line
anchors or regex patterns that match `r?`n.

The DEVIATIONS.md, FINDINGS.md, and HANDOFF.md updates in this session
failed once or more because of this exact issue. The failure is always
"anchor not found" with no other diagnostic. The fix is the check above,
run before the anchor is built.
---

## 7. Writing files - the discipline

Every write to a file that already exists must be one of:

- Targeted replace - read the file, replace exactly one anchor with the new
  content, write back, verify the byte delta.
- Append - read the file, append the new content to a bounded section, write
  back, verify the byte delta.

Never rewrite a growing document from scratch. A rewrite loses what was there.
The four documents that grow are `README.md`, `FINDINGS.md`,
`DEVELOPER_GUIDE.md`, and `PROPOSED_FIXES.md`. Append only.

For a brand new file, a single `WriteAllText` is correct.

When a growing file has been corrupted and its content must be restored, a
single full write is the correct tool. Move the corrupt file aside first with
a `.corrupt` extension, so nothing is lost and the mistake is inspectable.

### BOM-less UTF-8 always

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8NoBom)

After every write, verify the first three bytes are not `EF BB BF`.

### Read before write

Before writing any file, read it. If it already exists and the content is not
what you expect, abort. Do not silently overwrite.

---

## 8. The assertion discipline

Assert only what the shipped artifact documents. Record the rest as an
observation.

The exception-mapping scenario taught this. The README's error table lists
CONFLICT / 409 and INTERNAL_ERROR / 500 as wire conditions. It does not
state which CLR exception maps to which code. I wrote a sub-check asserting
InvalidOperationException produces INTERNAL_ERROR / 500, and the run
returned CONFLICT / 409. My assertion was wrong; the artifact was right.

The corrected rule:

- Assert shape: the envelope is valid JSON, success is present, error.code
  is non-empty, error.message is non-empty, no internal detail leaks.
- Record the observed values as an observation, not as an assertion.
- If the observed code is not documented in the shipped artifact, record a
  finding. Do not fail the scenario for the documentation gap alone.

That rule prevents the verifier from failing on its own assumptions.

### The categories in FINDINGS.md

FINDINGS.md distinguishes four categories:

- **Functional deviations** - runtime behavior contradicts a specific claim
  in the shipped documentation. Library problem. Highest priority.
- **Open findings (documentation gaps)** - the library works but the shipped
  documentation is incomplete or inaccurate. Medium priority.
- **Resolved findings (positive assertions)** - the library behaves as
  documented. Recorded for completeness.
- **Withdrawn findings** - a finding that was recorded in error and corrected.

The verifier's most valuable output is the functional deviations.

---

## 9. The environment traps for running MSBuild probes

When you need to read the evaluated MSBuild properties (for example, to prove
the verifier's isolation from `dotnet/Directory.Build.props`):

1. Write a small targets file inside `obj/`. It is a build-output folder, not
   a source folder.
2. Have the target write its output to a file with `WriteLinesToFile`. Do not
   rely on `Message` tasks printing to the console. Verbosity filters and
   piping can eat the messages.
3. Read the output file after the build.
4. Delete the targets file.

The properties you care about are `Version`, `Product`,
`GenerateDocumentationFile`, `DirectoryBuildPropsPath`, and
`ImportDirectoryBuildProps`.

---

## 10. Stop on red

Build after every write. Run after every build. If the build fails, read the
errors, fix one thing, rebuild. If the run fails, read the failure message,
diagnose against the shipped XML, fix, re-run. Stop the moment something goes
red. Do not accumulate unverified changes.

---

## 11. What not to do

Do not read `dotnet/src`, `dotnet/tests`, or `dotnet/samples` as API or
behavioral authority. The verifier's whole value is that it consumes the
published artifact only.

Do not add a `ProjectReference`. The whole point of the verifier is
`PackageReference` to the published packages.

Do not add the verifier to `ApiPilot.slnx`. It must not be buildable as part
of the library's solution.

Do not add a third-party package. The verifier's csproj has zero third-party
references. Keep it that way.

---

## 12. The one-line summary

Read the shipped XML. Write one scenario at a time. Build. Run. Append to
both documents. Never rewrite a growing file. Never use `if` in expression
position. Never write two consecutive hyphens inside an XML comment. Assert
shape, record observations. Wrap .NET method calls in try/catch. Move a
corrupt file aside before restoring it.