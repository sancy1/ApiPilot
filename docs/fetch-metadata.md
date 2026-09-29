<!--
filepath: docs/fetch-metadata.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot Fetch Metadata policy: the three profiles, the header checks, and the compatibility path.
-->

# The ApiPilot Fetch Metadata policy

This document describes the optional Fetch Metadata policy. The
policy is a second defense-in-depth layer that supplements the
Origin policy and the CSRF check. It is opt-in and disabled by
default. The normative contract is `../SPEC.md`.

## What this document covers

- The three profiles (Off, Compat, Strict).
- The Sec-Fetch-Site header.
- The Sec-Fetch-Mode header.
- The AllowMissingHeaders correction.
- The pipeline placement.
- The compatibility path.

## The three profiles

FetchMetadataProfile defines three values:

- Off (default). The middleware passes every request through. This
  is the compatibility path required by SPEC.md 6.17.
- Compat. The policy enforces the Sec-Fetch-Site check when the
  header is present and well-formed. A missing or malformed header
  is permitted (subject to AllowMissingHeaders).
- Strict. The policy enforces the Sec-Fetch-Site check strictly. A
  disallowed value is always rejected. A missing or malformed
  header is rejected unless AllowMissingHeaders is true.

## The Sec-Fetch-Site header

The Fetch Metadata specification defines four values:

- same-origin. The request originated from the same origin as the
  target. Permitted by default.
- same-site. The request originated from the same site (registrable
  domain) as the target, but a different origin. Permitted by
  default.
- cross-site. The request originated from a different site. Rejected
  by default.
- none. The request was initiated by the user directly (typing a
  URL, following a bookmark). Permitted by default.

The default AllowedSiteValues set contains same-origin, same-site,
and none. cross-site is deliberately absent because the policy
exists to reject it. An application that has a legitimate cross-site
flow adds the value explicitly.

A value outside the four-member spec set is malformed. The
missing-or-malformed policy applies.

## The Sec-Fetch-Mode header

The Fetch Metadata specification defines five values: cors, no-cors,
navigate, same-origin, and websocket. The default AllowedModeValues
set contains cors, same-origin, and navigate. no-cors and websocket
are deliberately absent.

The middleware checks Sec-Fetch-Mode only when the header is
present. A missing Sec-Fetch-Mode header is allowed when the
Sec-Fetch-Site value passes.

## The AllowMissingHeaders correction

AllowMissingHeaders governs the missing-or-malformed case only. It
never overrides an explicitly present disallowed value.

This means that under Strict with AllowMissingHeaders=true, a
cross-site value is still rejected because cross-site is not in
AllowedSiteValues. The AllowMissingHeaders setting is not consulted
when the header is present and well-formed.

The correction was made explicitly so that a config flag intended
for legacy clients cannot accidentally become a general-purpose
bypass.

## The pipeline placement

The Fetch Metadata middleware runs after UseApiPilotOriginPolicy and
before UseApiPilotCsrfProtection. It reads the same
CsrfEndpointMetadata that the CSRF and Origin middleware read. See
[unsafe-methods.md](unsafe-methods.md) for the precedence rule.

## The compatibility path

The default profile is Off. An application that does not configure
the policy sees no change. This is the compatibility path required
by SPEC.md 6.17: an application whose clients do not send the
Sec-Fetch-Site header continues to work. The policy only takes
effect when the application explicitly opts in.

## The rejection

A rejection uses the standard error envelope with code FORBIDDEN
(HTTP 403). There is no dedicated Fetch-Metadata-rejected code in
SPEC.md. The reason (MissingSiteHeader, MalformedSiteHeader,
DisallowedSiteValue, DisallowedModeValue) is logged server-side and
never returned.

## The startup validation

FetchMetadataOptionsValidator runs at startup through
ValidateOnStart. When the profile is Off, the inner checks are
skipped. When the profile is enabled, the allowed-site and
allowed-mode sets must not be empty. Under Strict, the validator
fails closed when cross-site is in AllowedSiteValues, because an
ineffective Strict policy is a configuration defect.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Profile | Off | FetchMetadataOptions.Profile |
| Allowed site values | same-origin, same-site, none | FetchMetadataOptions.AllowedSiteValues |
| Allowed mode values | cors, same-origin, navigate | FetchMetadataOptions.AllowedModeValues |
| Missing-header allowance | true | FetchMetadataOptions.AllowMissingHeaders |

## Compatibility guarantees

- The default profile is Off.
- The default AllowedSiteValues does not include cross-site.
- The default AllowedModeValues does not include no-cors or
  websocket.
- AllowMissingHeaders governs only missing or malformed values.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [origin-policy.md](origin-policy.md) - the first defense-in-depth
  layer.
- [unsafe-methods.md](unsafe-methods.md) - the shared endpoint
  policy.
- [csrf.md](csrf.md) - the CSRF token and the service.
- [threat-model.md](threat-model.md) - the threats the policy
  addresses.

