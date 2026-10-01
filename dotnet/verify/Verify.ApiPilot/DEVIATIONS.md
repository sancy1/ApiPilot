<!--
filepath: dotnet/verify/Verify.ApiPilot/DEVIATIONS.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  The focused action list of every functional deviation the verifier
          has found against the published ApiPilot artifacts. A functional
          deviation is a case where the runtime behavior contradicts a
          specific claim in the shipped documentation, or where the code has
          a logic or business-logic defect. Documentation gaps and packaging
          findings are NOT in this file; they are in FINDINGS.md.
relates:  A subset view of FINDINGS.md. The full register, with all four
          categories, is authoritative in FINDINGS.md. This file is the
          action list for the library author.
-->

# Functional Deviations

This is the focused action list of every functional deviation the verifier
has found against the published ApiPilot artifacts.

A **functional deviation** is a case where the runtime behavior of the
shipped artifact contradicts a specific claim in the shipped documentation,
or where the code has a logic or business-logic defect. These are the most
pressing findings. They are not documentation gaps. A documentation gap
means the library works and the docs are incomplete. A functional deviation
means the library does not work as its documentation says.

This file lists only the functional deviations. The full register, with all
four categories (functional deviations, open documentation gaps, resolved
positive assertions, and withdrawn findings), is in FINDINGS.md.

Every entry in this file is also in FINDINGS.md with the full evidence.
Every entry names the corresponding proposal in PROPOSED_FIXES.md.

**Status legend:**

- **open** - observed, recorded, not yet resolved.
- **in progress** - the library author has acknowledged the finding and is
  working on a fix.
- **resolved** - the fix is published in a new version and the verifier has
  confirmed the fix on the next run.
- **withdrawn** - the finding was recorded in error and has been corrected.

---

## Summary

| ID | Package | Severity | Category | Status |
|----|---------|----------|----------|--------|
| F-59 | ApiPilot.AspNetCore | Medium | Logic defect | resolved in 1.0.5 |
| F-65 | ApiPilot.Security | High | Security defect | resolved in 1.0.5 |

Two deviations. Both were library problems. Neither was a verifier bug.
Both are resolved in 1.0.5. The re-run of the verifier against 1.0.5
confirms the fixes.

---

## Resolved deviations

The two deviations below were found against 1.0.3 and fixed in 1.0.5. The
re-run of the verifier against 1.0.5 confirms both fixes. The entries are
kept here as a record of what was found and what was fixed.

### F-59 - The KeyTransform identity override is ineffective (resolved in 1.0.5)

**Package:** ApiPilot.AspNetCore 1.0.3.

**Severity:** Medium.

**Category:** Logic defect. The documented escape hatch does not work.

**Documented behavior (verbatim from the shipped XML for
ApiPilotValidationOptions.KeyTransform):**

> "Supply a delegate to replace the normalization entirely; to disable
> normalization, supply the identity function key => key."

**Documented behavior (verbatim from the shipped XML for the convenience
overload of WithApiPilotValidation):**

> "The keys are normalized using the KeyTransform configured in
> ApiPilotValidationOptions, falling back to FieldKeyNormalizer.Normalize
> when no transform is configured."

**Observed behavior:** With `o.KeyTransform = key => key` configured through
AddApiPilotValidation, an input field key of `"Email"` appears on the wire as
`"email"`. The identity override is not honored. Normalization happens
regardless. The convenience callback normalizes anyway.

**Reproduction:** Scenario 09, sub-check 7. Configure
`o.KeyTransform = key => key`. Attach the convenience overload of
WithApiPilotValidation. Send a dictionary with the key `"Email"`. Observe
the wire: `error.fields.email`.

**Impact:** Medium. A consumer who needs to preserve raw binder keys (for
example, to match a client-side parser that expects PascalCase) cannot.
The documented escape hatch does not work.

**Recommended resolution:** Documentation correction. The current behavior
(always normalizing to camelCase) is a defensible library default. The XML
overstates what the identity transform achieves. Correct the XML remark to
describe the actual behavior. A functional code fix - apply the user
transform to the raw key before default normalization - is a larger change
and should be treated as a feature request, not a defect fix.

**Security relevance:** None. The key form does not change which data is
exposed or to whom.

**Performance relevance:** None. The normalization operation runs either
way.

**Proposal:** PROPOSED_FIXES.md Proposal 4.

**Full evidence:** FINDINGS.md section "Functional deviations", entry F-59.

**Status:** resolved in 1.0.5.

**Resolution:** A property-scoped ApiErrorFieldsConverter on ApiError.Fields
writes and reads dictionary keys verbatim, overriding the global
DictionaryKeyPolicy = CamelCase for that property only. The default fallback
is preserved: with no KeyTransform configured, FieldKeyNormalizer.Normalize
still produces email.

**Blocks adoption:** no.

---

### F-65 - The CSRF attributes are ineffective on minimal-API endpoints (resolved in 1.0.5)

**Package:** ApiPilot.Security 1.0.3.

**Severity:** High.

**Category:** Security defect. The RequireCsrf direction leaves a sensitive
endpoint unprotected while the consumer believes it is protected. The
SkipCsrf direction breaks a public webhook integration.

**Documented behavior (verbatim from the shipped XML for
ApiPilotSkipCsrfAttribute):**

> "Apply to a controller action or pass through WithMetadata on a minimal
> API endpoint. Use this attribute for endpoints that intentionally accept
> unauthenticated state-changing requests (for example, a public webhook
> receiver)."

**Documented behavior (verbatim from the shipped XML for
ApiPilotRequireCsrfAttribute):**

> "Apply to a controller action or pass through WithMetadata on a minimal
> API endpoint. Use this attribute for endpoints whose method is safe by
> HTTP semantics but whose behavior is state-changing or sensitive."

**Observed behavior:** On a minimal-API endpoint:

- `.WithMetadata(new ApiPilotSkipCsrfAttribute())` on a POST endpoint does
  **not** bypass the global CSRF policy. The request is rejected with HTTP
  403 and `error.code = CSRF_HEADER_MISSING`.
- `.WithMetadata(new ApiPilotRequireCsrfAttribute())` on a GET endpoint does
  **not** enforce CSRF protection. The request passes through with HTTP 200.
- Applying the attributes to a static handler method (the standard
  ASP.NET Core pattern for IEndpointMetadataProvider attributes) produces
  the same non-behavior.

**Reproduction:** Scenario 11, sub-checks 4, 5, and 6. Two independent host
lifetimes. Two different application patterns. Both are inert.

**Impact:** High for minimal-API consumers.

- **Skip direction:** A public webhook receiver is rejected with HTTP 403.
  The consumer's integration with their upstream provider breaks.
- **Require direction:** A sensitive GET endpoint that the consumer marked
  with `[ApiPilotRequireCsrf]` is unprotected while the consumer believes it
  is protected. An attacker can trigger it with a cross-site request and
  the middleware will not stop them. **The consumer's security posture is
  worse than they think.**

**Recommended resolution:** Code fix, not documentation correction.
Documentation that says "this does not work" is honest but does not help a
consumer who has already deployed code that trusted the library.

**Two candidate fix shapes** (full details in PROPOSED_FIXES.md
Proposal 5):

- **Shape A** - add fluent extensions `WithApiPilotSkipCsrf()` and
  `WithApiPilotRequireCsrf()` that attach the `CsrfEndpointMetadata` record
  directly. Roughly ten lines of production code plus tests. Small,
  testable, no changes to existing behavior.
- **Shape B** - make the middleware read the attribute in addition to the
  metadata record. More invasive; couples the middleware to the attribute
  types.

**Recommended fix:** Shape A.

**Security relevance:** High. See the Require direction above.

**Performance relevance:** None. Adding the metadata check is a single
endpoint-metadata lookup per request.

**Proposal:** PROPOSED_FIXES.md Proposal 5.

**Full evidence:** FINDINGS.md section "Functional deviations", entry F-65.

**Scope note:** The controller path for the two attributes was not tested by
scenario 11. The finding stands as written for minimal APIs. Testing the
controller path is a follow-up.

**Status:** resolved in 1.0.5.

**Resolution:** A shared internal resolver, CsrfEndpointPolicyResolver, reads
both the canonical CsrfEndpointMetadata record and the attribute instances,
aggregates them, and applies the documented precedence Require > Skip >
UseGlobal. CsrfMiddleware, OriginMiddleware, and FetchMetadataMiddleware all
delegate to it. The canonical policy model is unchanged; no public API
changed; direct-record behavior is preserved.

**Blocks adoption:** conditionally. Yes for minimal-API consumers who need
either escape hatch.

---

## What is NOT in this file

The following categories of findings are in FINDINGS.md, not here. They are
lower severity than functional deviations.

- **Documentation gaps** - the library works as designed; the shipped
  documentation is incomplete or inaccurate. Six are currently open:
  F-14, F-15, F-30, F-31, F-36, F-51. (F-58 and F-64 are the same class as
  F-36 and F-51.)
- **Packaging findings** - issues with the shape of the packages, the
  README, or the version history. Nine are currently open: F-14, F-15,
  N-06, N-13, N-20, N-21, N-24, N-32, N-34.
- **Resolved positive assertions** - the library behaves as documented.
  These are recorded for completeness.

For the full register, see FINDINGS.md.

## How to use this file

1. Read the Summary table. It names every open deviation, its severity, its
   category, and its status.
2. Read the entry for each open deviation. It names the exact documented
   claim that is contradicted, the exact observed behavior, and the
   reproduction.
3. Read the corresponding proposal in PROPOSED_FIXES.md for the recommended
   fix.
4. When a fix lands in a new published version, update the Status field to
   "resolved" and note the version. The verifier's re-run confirms the fix.

## Rule for the next developer

Every scenario that produces a functional deviation appends to two files:

- FINDINGS.md - the full register entry, with all evidence.
- DEVIATIONS.md - the focused action entry.

A milestone append is not complete until both files reflect any new
deviation.

**End of DEVIATIONS.md**