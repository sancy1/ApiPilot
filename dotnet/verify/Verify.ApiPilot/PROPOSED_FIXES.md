<!--
filepath: dotnet/verify/Verify.ApiPilot/PROPOSED_FIXES.md
layer:    Verification
package:  Verify.ApiPilot
purpose:  Draft of the library-side documentation fixes that would close
          findings F-30, F-31, and F-36. Each proposal is text to add to the
          shipped XML or to the packaged README. The library author can
          accept, adapt, or reject any proposal.
relates:  Each proposal cites its finding in FINDINGS.md.
-->

# Proposed Documentation Fixes

This file drafts the documentation changes that would close the three open
findings that describe a documentation gap in the shipped ApiPilot artifacts.
The library author is the decision-maker for each proposal. Nothing in this
file has been applied to the library source tree.

The three findings and their proposals:

- F-31 - The shipped XML does not document the default mapping for
  ArgumentNullException. Proposal 1.
- F-30 - The shipped XML does not document the default mapping for
  InvalidOperationException. Proposal 2.
- F-36 - The shipped XML does not name the declared type of
  ApiExceptionOptions.ErrorCodeToStatusMap. Proposal 3.

---

## Proposal 1 - Document the default exception mapping table

**Finding:** F-30 and F-31.

**Underlying gap:** The shipped XML summary of `KnownExceptionTypes.Default`
documents that ArgumentNullException precedes ArgumentException in the table,
and it documents the existence and ordering of the table. It does not
enumerate the CLR exception-to-error-code mappings. A consumer reading the
shipped artifact cannot predict which exception maps to which wire code or
which HTTP status. Scenario 05 observed two specific mappings at runtime:

- ArgumentNullException maps to error.code VALIDATION_ERROR, HTTP 400.
- InvalidOperationException maps to error.code CONFLICT, HTTP 409.

Neither is in the shipped XML or the packaged README. The two observations
are the concrete evidence for the finding.

**Proposed XML change.** In the shipped XML comment for
`KnownExceptionTypes.Default`, expand the summary so it enumerates the table
in order. Suggested text:

    The default mapping table, in match order. The mapper walks the list top
    to bottom and matches the first entry whose ExceptionType is an instance
    of the thrown exception.

        ArgumentNullException        -> VALIDATION_ERROR     (HTTP 400)
        ArgumentException            -> VALIDATION_ERROR     (HTTP 400)
        KeyNotFoundException         -> RESOURCE_NOT_FOUND   (HTTP 404)
        UnauthorizedAccessException  -> FORBIDDEN            (HTTP 403)
        InvalidOperationException    -> CONFLICT             (HTTP 409)
        any other exception          -> INTERNAL_ERROR       (HTTP 500)

    ArgumentNullException is listed before ArgumentException because the
    former derives from the latter. Applications can add, remove, or reorder
    entries through ApiExceptionOptions.Mappings; the order of the list is
    the match order.

The exact entries and their order should be taken from the library source
(the table is defined in `KnownExceptionTypes.Default`). The list above is
the mapping observed at runtime for the two exceptions the verifier
exercised, plus the framework types the shipped XML already names as being
in the table. The remaining entries are proposed from the standard mapping
conventions used by the library. The library author should confirm each row
against the source before adding it.

**Proposed README change.** In the packaged README, add a table immediately
after the existing "Error codes" table. Suggested text:

    ### Default exception mapping

    When an exception escapes an endpoint protected by UseApiPilotExceptions,
    the middleware maps it to an error envelope using the following default
    table, in match order. The first matching entry wins.

    | Exception type | error.code | HTTP status |
    | --- | --- | --- |
    | ArgumentNullException | VALIDATION_ERROR | 400 |
    | ArgumentException | VALIDATION_ERROR | 400 |
    | KeyNotFoundException | RESOURCE_NOT_FOUND | 404 |
    | UnauthorizedAccessException | FORBIDDEN | 403 |
    | InvalidOperationException | CONFLICT | 409 |
    | any other exception | INTERNAL_ERROR | 500 |

    Applications can replace or extend the table through
    ApiExceptionOptions.Mappings. Applications can override the HTTP status
    produced for any wire code through ApiExceptionOptions.ErrorCodeToStatusMap.

**Impact of closing this finding.** A consumer can predict the response
classification for any escaping exception from the shipped artifact alone.
The two runtime observations become documented behavior instead of an
undocumented surprise.

**Blocks adoption:** no. The runtime behavior is consistent; only the
documentation is incomplete.

---

## Proposal 2 - Name the type of ApiExceptionOptions.ErrorCodeToStatusMap

**Finding:** F-36.

**Underlying gap:** The shipped XML summary of
`ApiExceptionOptions.ErrorCodeToStatusMap` documents that the property is the
Option D override point and that applications populate the dictionary. It
does not name the property's declared type. A consumer cannot tell from the
XML whether the property is assignable as a whole, mutable in place, or
read-only. In the verifier's initial build, this gap required a reflection
probe of the shipped DLL to determine that the declared type is
`IReadOnlyDictionary<string, int>` with a public setter, and that the
correct consumer pattern is a whole-value assignment.

**Proposed XML change.** In the shipped XML comment for
`ApiExceptionOptions.ErrorCodeToStatusMap`, expand the summary so it names
the declared type and shows the correct consumer pattern. Suggested text:

    Optional overrides for the HTTP status code produced for a given error
    code. Keys are wire-format error codes (for example "VALIDATION_ERROR");
    values are the HTTP status codes to use.

    Declared type: IReadOnlyDictionary<string, int> with a public setter.

    Because the property is declared as a read-only dictionary interface,
    the correct consumer pattern is a whole-value assignment. The middleware
    reads the assigned dictionary at request time. Example:

        builder.Services.AddApiPilotExceptions(o =>
        {
            o.ErrorCodeToStatusMap = new Dictionary<string, int>
            {
                ["CONFLICT"] = 503,
            };
        });

    When a code is not present in the assigned dictionary, the built-in
    mapping is used; if the built-in mapping does not recognize the code
    either, the response falls back to 500. Defaults to an empty dictionary,
    which means the built-in mappings are used unchanged.

**Impact of closing this finding.** A consumer can write correct
`ErrorCodeToStatusMap` code from the shipped XML alone. The verification
build no longer requires a reflection probe to determine the declared type.

**Blocks adoption:** no. The runtime behavior is consistent; the
documentation did not describe the shape of the override.

---

---

## Proposal 3 - Name the declared types of the ApiPilotJsonOptions properties

**Finding:** F-51.

**Underlying gap:** The shipped XML summary of `ApiPilotJsonOptions`
documents the default and the behavior of each property. It does not name
the declared type of four properties: `PropertyNamingPolicy`,
`DictionaryKeyPolicy`, `DefaultIgnoreCondition`, and `ReadCommentHandling`.
A consumer cannot tell from the XML what the exact declared types are.

**Proposed XML change.** In the shipped XML comment for each of the four
properties, add a sentence naming the declared type and the practical
implication. Suggested text for each:

For `PropertyNamingPolicy`:

    Declared type: System.Text.Json.JsonNamingPolicy?.
    Defaults to JsonNamingPolicy.CamelCase. Set to null to use the CLR
    property names as written (PascalCase by convention).

For `DictionaryKeyPolicy`:

    Declared type: System.Text.Json.JsonNamingPolicy?.
    Defaults to JsonNamingPolicy.CamelCase. Set to null to use dictionary
    keys as written.

For `DefaultIgnoreCondition`:

    Declared type: System.Text.Json.Serialization.JsonIgnoreCondition.
    Defaults to JsonIgnoreCondition.Never, which writes null values
    explicitly on the wire.

For `ReadCommentHandling`:

    Declared type: System.Text.Json.JsonCommentHandling.
    Defaults to JsonCommentHandling.Disallow for strict input parsing.

The exact declared types should be confirmed against the library source
before the XML is edited. The types shown above are the standard
System.Text.Json types the properties clearly map to, but the library
author should confirm each one.

**Impact of closing this finding.** A consumer can write correct
`ApiPilotJsonOptions` code from the shipped XML alone. The verification
build no longer requires the compiler to resolve a type the XML did not
name.

**Blocks adoption:** no. The runtime behavior is consistent; the
documentation did not name the types.
---

## Proposal 4 - Correct the documentation for the KeyTransform identity override (applied in 1.0.5)

Status: applied in 1.0.5. The library fixed the deviation at the JSON
serializer level with a property-scoped ApiErrorFieldsConverter on
ApiError.Fields. The verifier scenario 09 sub-check 7 now asserts the
corrected behavior and passes against 1.0.5.
**Finding:** F-59.

**Classification:** Functional deviation. The runtime behavior contradicts a
specific claim in the shipped XML. The finding is not a documentation gap in
the usual sense (where the library works but the docs are incomplete). Here
the docs promise a capability the runtime does not deliver.

**Recommended resolution: documentation correction.** The current behavior of
always normalizing field keys to camelCase is a defensible library default.
Most consumers will use the default and never notice the escape hatch. The
only problem is that the XML summary overstates what the identity transform
achieves.

**Proposed XML change.** Replace the current wording:

    Supply a delegate to replace the normalization entirely; to disable
    normalization, supply the identity function key => key.

with wording that reflects the actual behavior. Suggested replacement:

    The library always normalizes field keys to camelCase before they
    appear in the response. The KeyTransform delegate receives the
    normalized key and may further transform it. To change the wire
    format of a field key, supply a delegate that transforms the
    normalized key. The identity function key => key returns the
    normalized key unchanged and does not preserve the raw binder key.

**Alternate resolution: code fix.** If a consumer genuinely needs raw binder
keys preserved, the library can apply the user transform to the raw key
before the default normalization. That is a larger change; it affects the
default path for every consumer and must be released with a note in the
release history. Treat it as a feature request, not a defect fix. No
performance impact is expected because the normalization already runs.

**Impact of closing this finding.** A consumer who reads the corrected XML
knows that the identity function does not preserve the raw key. The verifier
scenario 09 sub-check 7 will then be updated to assert the documented
behavior (normalized key). Until then, the scenario records the observed
non-behavior as an observation.

**Blocks adoption:** no.

---

## Proposal 5 - Fix the CSRF attributes on minimal-API endpoints (applied in 1.0.5)

Status: applied in 1.0.5. The library introduced CsrfEndpointPolicyResolver,
a shared internal resolver that reads both the canonical
CsrfEndpointMetadata record and the attribute instances, and applies the
documented precedence Require > Skip > UseGlobal. CsrfMiddleware,
OriginMiddleware, and FetchMetadataMiddleware all delegate to it. The
verifier scenario 11 sub-checks 4, 5, and 6 now assert the corrected
behavior and pass against 1.0.5.
**Finding:** F-65.

**Classification:** Functional deviation with security relevance. The
shipped XML states that ApiPilotSkipCsrfAttribute and
ApiPilotRequireCsrfAttribute can be applied to minimal-API endpoints through
WithMetadata. Two different application patterns produce the same
non-behavior. In the Require direction, a consumer's endpoint is unprotected
while the consumer believes it is protected. In the Skip direction, a public
webhook receiver is rejected with HTTP 403.

**Recommended resolution: code fix.** Documentation correction is not
sufficient for this finding. Correcting the docs to say the attributes do
not work on minimal APIs would be honest but would leave a consumer who
already deployed code exposed. The advertised behavior should be
implemented.

**Root cause (to be confirmed by the library author).** The most likely
mechanism: the attributes implement IEndpointMetadataProvider and write a
CsrfEndpointMetadata record when the framework calls PopulateMetadata. The
framework calls PopulateMetadata only when the attribute is applied to a
method that the framework treats as the endpoint handler. When the attribute
is passed to WithMetadata, the attribute instance is added to the endpoint
metadata collection but PopulateMetadata is not invoked, so the
CsrfEndpointMetadata record is never produced. The middleware reads
CsrfEndpointMetadata, sees none, and falls back to the global policy. That
would explain the observed non-behavior.

**Two candidate fix shapes:**

**Shape A - fluent extensions that attach the metadata record directly.**
Add two methods:

    public static TBuilder WithApiPilotSkipCsrf<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.WithMetadata(new CsrfEndpointMetadata(CsrfPolicy.Skip));

    public static TBuilder WithApiPilotRequireCsrf<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
        => builder.WithMetadata(new CsrfEndpointMetadata(CsrfPolicy.Require));

A consumer writes:

    app.MapPost("/webhook", Handler).WithApiPilotSkipCsrf();
    app.MapGet("/sensitive", Handler).WithApiPilotRequireCsrf();

The metadata record is attached directly; the middleware reads it; the
policy applies. Small, testable, no changes to existing behavior.

**Shape B - make the middleware read the attribute in addition to the
metadata record.** Change the middleware's lookup to:

    var policy = endpoint?.Metadata.GetMetadata<CsrfEndpointMetadata>()?.Policy
        ?? (endpoint?.Metadata.GetMetadata<ApiPilotSkipCsrfAttribute>() is not null ? CsrfPolicy.Skip : CsrfPolicy.UseGlobal)
        ?? (endpoint?.Metadata.GetMetadata<ApiPilotRequireCsrfAttribute>() is not null ? CsrfPolicy.Require : CsrfPolicy.UseGlobal);

This is more invasive. It requires two `GetMetadata` calls per request and
couples the middleware to the attribute types. Shape A is cleaner because
the fluent extension produces the metadata the middleware already reads.

**Shape C - documentation correction only, as a fallback.** If minimal-API
support is intentionally out of scope, correct the XML remarks to state
that the attributes are only supported on controller actions. This is the
fallback if Shape A or Shape B is rejected. It must be accompanied by a
release-notes entry that calls out the scope reduction, because it removes
an advertised capability.

**Recommended fix:** Shape A. Two small fluent extensions, one
CsrfEndpointMetadata record attached via WithMetadata. Roughly ten lines of
production code plus tests. It preserves the existing controller path
(which was not tested by the verifier but presumably works) and adds a
minimal-API path that matches what the XML already advertises.

**Impact of closing this finding.** A consumer who follows the corrected
documentation or the new fluent extension gets the policy they asked for.
A consumer who follows the existing WithMetadata wording continues to get
the wrong behavior until the documentation is corrected in Shape A's
release notes. The release notes should state that WithMetadata is no
longer the recommended mechanism for the two attributes on minimal APIs,
and should point at the fluent extensions.

**Blocks adoption:** conditionally. Yes for minimal-API consumers who need
either escape hatch.
## Note on F-14, F-15, and the packaging findings

F-14, F-15, N-06, N-13, N-20, N-21, N-24, N-32, and N-34 are packaging and
metadata findings. Their fixes are not in the shipped XML or the packaged
README; they are in the way the packages are built and published. They are
recorded in FINDINGS.md with their own impact and status. This file does not
propose fixes for them, because their correct fix depends on decisions the
library author makes about packaging (whether to ship SPEC.md inside the
package, how to phrase the .nuspec description, how to keep the CHANGELOG in
sync with nuget.org). Those are choices, not documentation text.

---

## What this file is not

This file is a proposal. It does not modify the library source tree. It does
not modify any of the three ApiPilot packages. It does not run any code
against the library. The library author reads each proposal, decides whether
to accept it, and applies any accepted change through the library's own
process.

Every proposal is derived from a finding in FINDINGS.md, and each finding is
derived from the published NuGet artifact. No proposal is derived from the
library source tree.