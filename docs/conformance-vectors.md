<!--
filepath: docs/conformance-vectors.md
package:  n/a (repository root docs)
since:    v1.0.6
purpose:  The ApiPilot conformance vector format: language-neutral fixtures that prove wire-contract parity across ports.
-->

# ApiPilot conformance vectors

Conformance vectors are language-neutral JSON fixtures that encode the
**observable behavior** of ApiPilot's wire contract. Every language port
ships a runner that executes the same vectors and must produce identical
results. A port that passes all vectors is wire-conformant with C#
ApiPilot v1.0.6.

Vectors cover only **deterministic, side-effect-free logic** — the parts
of ApiPilot that can be verified without a web framework, a clock, or a
cryptographic key ring. Framework-bound behavior (middleware ordering,
DI registration, HttpContext plumbing) is verified by each port's own
integration tests, not by vectors.

The normative contract remains `SPEC.md`. Vectors are executable
statements of it.

## What this document covers

- The vector file format.
- The vector categories and what each proves.
- The runner contract every port must implement.
- The determinism rules (what may and may not appear in a vector).
- How vectors are versioned.

## The vector file format

Vectors live in `conformance/vectors/<category>.json`. Each file is a
JSON object:

```json
{
  "category": "error-code",
  "spec": "SPEC.md#error-codes",
  "vectors": [
    {
      "id": "error-code.from.uppercases",
      "description": "ApiErrorCode.From normalizes case and whitespace",
      "input": { "raw": "  validation_error  " },
      "expect": { "code": "VALIDATION_ERROR", "valid": true }
    }
  ]
}
```

### File-level fields

| Field | Required | Meaning |
|---|---|---|
| `category` | yes | One of the category names below. Must match the file name. |
| `spec` | yes | Pointer into `SPEC.md` or a per-concern doc justifying the vectors. |
| `vectors` | yes | Non-empty array of vector objects. |

### Vector-level fields

| Field | Required | Meaning |
|---|---|---|
| `id` | yes | Globally unique, dotted, lowercase. `<category>.<slug>`. |
| `description` | yes | One sentence stating the behavior under test. |
| `input` | yes | Object of named inputs. Schema is per-category (below). |
| `expect` | yes | Object of named expected outputs. Schema is per-category. |
| `since` | no | ApiPilot version that fixed this behavior. Default: file's version. |

### Canonical code strings

Where a category's `expect` carries symbolic strings — `violations`
entries in `cookie-profile`, `reason` values in `fetch-metadata`,
`error` tags in `pagination` — those strings are **canonical codes
defined by this vector set**, not literals copied from C# internals.
The C# runner maps its implementation's outcomes onto these codes; a
port maps its own. The codes are part of the vector contract and may
only change with a `SPEC.md`-level review.

### Determinism rules

A vector is valid only if its `expect` is reproducible on any machine,
any time, any language:

- **No wall-clock dependence.** Times are fixed literals. Anything that
  needs "now" takes `now` as an explicit input.
- **No randomness.** Random values are fixed literals. The CSRF token
  entropy is tested at the format level, not the signer level.
- **No cryptography keys.** Data Protection output is platform-specific
  and is NOT vector-tested. The CSRF *payload format* (pre-protection)
  IS vector-tested.
- **No framework types.** Inputs are JSON scalars/arrays/objects only.
- **No floating-point ambiguity.** Numbers that must match exactly are
  integers or strings. Ratios use string decimals.
- **Order-insensitive where the contract is.** Field maps compare as
  objects, not ordered lists, unless the contract guarantees order
  (it does for `error.fields[*]` message arrays — those DO compare
  ordered).

## The categories

### `envelope` — response envelope shapes

Proves the four wire shapes serialize identically: success, collection,
paginated, error (incl. validation `fields`).

Input: a logical response description (kind + payload + meta + optional
pagination). Expect: the exact JSON object, key-for-key.

Runner note: the port builds its envelope type and serializes with its
configured JSON policy (camelCase, pinned envelope keys). Comparison is
deep-equality on the parsed JSON, not byte-equality — key order is not
contractual.

### `error-code` — code normalization and status mapping

Proves `ApiErrorCode.From` semantics and the 14-code status table.

Input: `{ "raw": string }` or `{ "code": string }`. Expect:
`{ "valid": bool, "code": string|null, "status": int }`.

Cases must cover: lowercase input, mixed case, surrounding whitespace,
empty/null rejection, all 14 standard codes, one custom code (→500).

### `pagination` — pagination math and query semantics

Proves the deterministic parts: `totalPages` ceiling, `hasNext`/
`hasPrevious` boundaries, wire-page → internal-page base conversion,
default filling, and bound validation.

Input: `{ "totalItems": int, "page": int, "pageSize": int,
"pageNumberBase": int, "maxPageSize": int, "defaultPageSize": int }`.
Expect: `{ "totalPages": int, "hasNext": bool, "hasPrevious": bool,
"internalPage": int, "valid": bool, "error": string|null }`.

Cases must cover: totalItems=0, exact multiple, remainder, page beyond
range, pageSize at/over max, base=0 conversion, base=1 conversion.

### `field-key` — validation field-name normalization

Proves `FieldKeyNormalizer.Normalize` / `ValidationKeyTransforms.Resolve`.

Input: `{ "raw": string, "transform": "normalize"|"passthrough"|null }`.
Expect: `{ "normalized": string }`.

Cases must cover: `$.` prefix strip, camelCasing per segment, `[i]`
index preservation, already-camelCase passthrough, deep nesting,
numeric-looking segments, empty/edge inputs.

### `content-negotiation` — Accept and Content-Type evaluation

Proves the 406/415 decision logic as a pure function.

Input: `{ "accept": string|null, "contentType": string|null,
"method": string, "options": {...effective options...} }`.
Expect: `{ "acceptResult": "accept"|"reject", "contentTypeResult":
"accept"|"reject"|"skip", "status": 200|406|415 }`.

Cases must cover: missing Accept, `*/*` with wildcard on/off,
`application/*` literal-only behavior, q-value ignoring, multiple
Accept entries, missing Content-Type on POST/PUT/PATCH/GET, charset
parameters, custom method sets.

### `cookie-profile` — cookie rule validation

Proves the startup-validation rules as a pure function over a profile
description.

Input: `{ "profile": {name, secure, httpOnly, sameSite, path, domain,
namePrefix}, "role": "auth"|"session"|"csrf" }`.
Expect: `{ "valid": bool, "violations": [string] }`.

Cases must cover: each default profile valid, `__Host-` violations
(each rule separately), `__Secure-` without Secure, SameSite=None
without Secure, bad Path, bad Domain, forbidden name chars, undefined
SameSite.

### `origin` — RFC 6454 origin parse and match

Proves origin normalization and the three match modes.

Input: `{ "origin": string, "candidate": string, "mode":
"exact"|"anyPortSameHost"|"sameSite", "allowSameOrigin": bool,
"requestOrigin": string|null }`.
Expect: `{ "parseOk": bool, "normalized": string|null, "match": bool }`.

Cases must cover: default ports (https→443, http→80), explicit vs
default port equivalence, case normalization, malformed origins,
AnyPortSameHost ignoring port, SameSite always non-match.

### `fetch-metadata` — Sec-Fetch-* evaluation

Proves the Off/Compat/Strict profile logic.

Input: `{ "profile": "off"|"compat"|"strict", "site": string|null,
"mode": string|null, "allowedSiteValues": [string],
"allowedModeValues": [string], "allowMissingHeaders": bool }`.
Expect: `{ "decision": "allow"|"reject", "reason":
"none"|"missingSiteHeader"|"malformedSiteHeader"|
"disallowedSiteValue"|"disallowedModeValue" }`.

Cases must cover: all four profiles × present/absent/malformed site ×
disallowed mode × AllowMissingHeaders on/off. Critical case:
AllowMissingHeaders=true must NOT rescue a present disallowed value.

### `csrf-format` — token payload codec

Proves `CsrfTokenFormat.Encode`/`TryDecode` — the pre-protection
payload. This is the deepest vector category because it pins the exact
wire-relevant structure every port must reproduce inside its own
signing envelope.

Input (encode): `{ "issuedAtUnixSeconds": int, "randomBase64Url":
string, "bindingFingerprint": string }`.
Expect: `{ "payload": string }` — exact `a|b|c` string.

Input (decode): `{ "payload": string }`.
Expect: `{ "valid": bool, "issuedAtUnixSeconds": int|null,
"randomBase64Url": string|null, "bindingFingerprint": string|null }`.

Cases must cover: round-trip, pipe-in-part rejection, empty parts,
extra delimiter, non-integer timestamp, out-of-range timestamp
(boundary -62135596800 and 253402300799), empty payload.

### `csrf-binding` — binding fingerprint

Proves the deterministic SHA-256 fingerprint every port must reproduce
bit-for-bit (it is the cross-language interoperability anchor for CSRF).

Input: `{ "source": string, "kind": "subject"|"session"|"preauth" }`.
Expect: `{ "fingerprint": string }` — exact base64url SHA-256 of
`"ApiPilot.Csrf.Binding.v1|" + prefixedSource` where prefixedSource is
`source` for subject, `"session:"+source` for session,
`"preauth:"+source` for preauth.

Cases must cover: all three kinds, known-answer vectors (fixed
source → fixed fingerprint computed once from C#), unicode source.

### `sort-direction` — direction token parsing

Proves `SortDirectionExtensions.TryParseWireValue`.

Input: `{ "raw": string|null }`. Expect: `{ "valid": bool,
"direction": "asc"|"desc"|null }`.

Cases: `asc`, `desc`, case variants, surrounding whitespace, null,
empty, garbage.

### `correlation-id` — incoming ID validation

Proves the validation-pattern contract shape (the regex itself is
configurable, so vectors test the *policy outcomes*, not the regex).

Input: `{ "headerValue": string|null, "patternMatches": bool,
"policy": "replace"|"reject"|"useAsIs" }`.
Expect: `{ "effectiveId": "incoming"|"generated"|"rejected",
"warn": bool }`.

Cases: valid incoming (all policies → incoming), invalid+replace,
invalid+reject, invalid+useAsIs, absent header.

## The runner contract

Every port ships `conformance/runner.<ext>` (or equivalent) that:

1. Loads every `conformance/vectors/*.json` file.
2. For each vector, maps `category` to the port's implementation of
   that pure function.
3. Compares `expect` to actual output using the category's comparison
   rule (deep-equality for objects; ordered-equality for message
   arrays).
4. Reports per-vector pass/fail with the vector `id` on failure.
5. Exits non-zero if any vector fails.

The runner is part of the port's test suite, not its shipped library.
It may use the port's test framework.

### Mapping table (port implementer fills this in)

| Category | C# implementation under test |
|---|---|
| `envelope` | `ApiResponseBuilder`, `ErrorResponseBuilder`, `PagedResultBuilder`, `ValidationResponseFactory` + `System.Text.Json` with `ApiPilotJsonOptions` |
| `error-code` | `ApiErrorCode.From`, `ApiErrorCode.HttpStatus` |
| `pagination` | `PaginationResolver`, `PageRequest`, `PagedResultBuilder` pagination math |
| `field-key` | `FieldKeyNormalizer.Normalize`, `ValidationKeyTransforms.Resolve` |
| `content-negotiation` | `AcceptHeaderEvaluator`, `ContentTypeEvaluator` (pure cores of the middleware) |
| `cookie-profile` | `CookieProfileOptionsValidator` rule logic |
| `origin` | `OriginValidator.TryParse`/`Matches` |
| `fetch-metadata` | `FetchMetadataEvaluator` |
| `csrf-format` | `CsrfTokenFormat.Encode`/`TryDecode` |
| `csrf-binding` | `CsrfBindingProvider.Fingerprint` |
| `sort-direction` | `SortDirectionExtensions.TryParseWireValue` |
| `correlation-id` | `CorrelationMiddleware` decision logic |

## Versioning

- Vectors are versioned with the repository, not independently.
- A vector may only be added when the behavior it pins is shipped and
  tested in C#. A port must not be asked to conform to unshipped
  behavior.
- Removing or weakening a vector requires the same review bar as
  changing `SPEC.md` — it is a contract change.
- `since` on a vector records the ApiPilot version that established the
  behavior; ports targeting earlier versions may skip it.

## What vectors deliberately exclude

- **Data Protection output.** The protector's wire format is
  platform-specific. Ports use their own AEAD/MAC construction; only
  the pre-protection payload format is pinned.
- **Middleware ordering.** Verified by integration tests per port.
- **DI registration graphs.** Verified by each port's own smoke tests.
- **Log event IDs and metric names.** Pinned in `observability.md`;
  vector-testing them adds no behavioral signal.
- **OpenAPI document bytes.** The emitter is framework-bound; the
  schema *shapes* are pinned by `openapi.md` and the parity test.
- **Timing, concurrency, cancellation.** Not deterministic enough for
  fixtures.

## Related documents

- `SPEC.md` — the normative wire contract.
- `port-behavioral-spec.md` — the full behavioral checklist.
- `api-inventory/` — the API surface parity checklist.
- Per-concern docs — the detailed behavior each category draws from.
