<!--
filepath: docs/origin-policy.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot Origin policy contract and the RFC 6454 comparison.
-->

# The ApiPilot Origin policy contract

This document describes the defense-in-depth Origin policy. The
policy compares the request Origin against an allow-list and rejects
a request whose Origin does not match. The policy is not a
replacement for CSRF protection, not an authorization mechanism,
and not a CORS implementation. The normative contract is
`../SPEC.md`.

## What this document covers

- The Origin comparison (RFC 6454).
- The three match modes.
- The two independent booleans (AllowSameOrigin, AllowMissingOrigin).
- The Referer fallback.
- The rejection code.
- The pipeline placement.

## The Origin comparison

RFC 6454 defines an origin as scheme "://" host [":" port]. The
comparison normalizes:

- Scheme is lowercased.
- Host is lowercased.
- A missing port is replaced by the scheme default (443 for https,
  80 for http, 443 for wss, 80 for ws).

Two origins match when scheme, host, and normalized port are equal
under the configured mode. A malformed origin is a non-match. The
validator never throws for a malformed input.

## The three match modes

| Mode | Comparison |
| --- | --- |
| Exact (default) | Scheme, host, and normalized port must all match. |
| AnyPortSameHost | Scheme and host must match. The port is ignored. |
| SameSite | Not implemented in this version. Treated as a non-match. |

The SameSite mode requires a public suffix mechanism to compute the
registrable domain (eTLD+1). ApiPilot does not bundle one. The mode
exists in the enum so a future phase can implement it without
changing the surface. The startup validator fails closed when
SameSite is configured.

## The two independent booleans

AllowSameOrigin and AllowMissingOrigin are independent decisions.
Neither implies the other.

- AllowSameOrigin (default true). A present Origin that matches the
  request own origin passes, even when it is not in AllowedOrigins.
- AllowMissingOrigin (default true). An absent Origin passes. This
  matches the practice of modern browsers that omit the header on
  same-origin requests.

An application that wants the Origin policy to reject a missing
Origin on protected methods sets AllowMissingOrigin to false.

## The Referer fallback

The Referer fallback is opt-in. When AllowRefererFallback is true
and the Origin header is absent, the Referer is parsed to extract
its origin and the comparison runs against that. The fallback is
off by default because the Referer header is less reliable than
Origin and is often stripped by proxies and privacy tools.

A Referer that cannot be parsed is treated as a non-match.

## The rejection code

A rejected origin uses the standard error envelope with code
CSRF_ORIGIN_REJECTED (HTTP 403). The code is configurable through
OriginPolicyOptions.RejectionCode. The offending Origin value is
never echoed in the response envelope. The reason is logged
server-side.

## The pipeline placement

The Origin policy middleware runs after UseRouting, after
authentication, and before the CSRF protection middleware. The
Origin policy middleware reads the same CsrfEndpointMetadata that
the CSRF middleware reads; see [unsafe-methods.md](unsafe-methods.md)
for the precedence rule.

## The startup validation

OriginPolicyOptionsValidator runs at startup through
ValidateOnStart. The fail-closed rule: when the policy is enabled
and AllowSameOrigin is false, AllowedOrigins must contain at least
one entry. Otherwise no request could ever pass.

The validator also rejects a malformed origin entry, an undefined
match mode, the SameSite mode, and an empty rejection code.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Enabled | true | OriginPolicyOptions.Enabled |
| Allowed origins | empty | OriginPolicyOptions.AllowedOrigins |
| Same-origin allowance | true | OriginPolicyOptions.AllowSameOrigin |
| Missing Origin allowance | true | OriginPolicyOptions.AllowMissingOrigin |
| Referer fallback | false | OriginPolicyOptions.AllowRefererFallback |
| Match mode | Exact | OriginPolicyOptions.MatchMode |
| Rejection code | CSRF_ORIGIN_REJECTED | OriginPolicyOptions.RejectionCode |

## Compatibility guarantees

- The default match mode is Exact.
- AllowSameOrigin and AllowMissingOrigin default to true.
- The rejection code is CSRF_ORIGIN_REJECTED.
- SameSite is not implemented and the validator fails closed when
  it is configured.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [csrf.md](csrf.md) - the CSRF request token and the bootstrap
  endpoint.
- [unsafe-methods.md](unsafe-methods.md) - the shared endpoint
  policy.
- [fetch-metadata.md](fetch-metadata.md) - the second defense-in-
  depth layer.
- [threat-model.md](threat-model.md) - the cross-origin attacks the
  policy addresses.

