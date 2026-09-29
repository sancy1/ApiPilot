<!--
filepath: docs/unsafe-methods.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  How the ApiPilot CSRF middleware decides which requests to protect.
-->

# The ApiPilot unsafe-methods contract

This document describes how the CSRF protection middleware decides
which requests to protect, how per-endpoint overrides work, and how
the middleware composes with the Origin policy and the Fetch
Metadata policy. The normative contract is `../SPEC.md`.

## What this document covers

- The ProtectedMethods set.
- The [ApiPilotSkipCsrf] and [ApiPilotRequireCsrf] attributes.
- The precedence rule.
- The interaction with the Origin policy and Fetch Metadata.
- The pipeline placement.

## The ProtectedMethods set

CsrfOptions.ProtectedMethods is the set of HTTP methods the
middleware protects. The default is POST, PUT, PATCH, DELETE.
Comparison is case-insensitive.

Safe methods (GET, HEAD, OPTIONS, TRACE) are never in the default
set. The middleware passes them through without reading the CSRF
header.

An application that uses a custom method (for example, a WebDAV
extension) adds the method to the set.

## The two attributes

Two endpoint attributes override the global set:

- [ApiPilotSkipCsrf] marks an endpoint as exempt. The endpoint
  bypasses the CSRF middleware regardless of the request method and
  the global set.
- [ApiPilotRequireCsrf] marks an endpoint as explicitly protected.
  The endpoint is enforced regardless of the request method, the
  global set, and any [ApiPilotSkipCsrf] attribute.

Both attributes are available on controller actions (through the
standard attribute mechanism) and on minimal API endpoints (through
WithMetadata or by decorating the delegate).

## The precedence rule

The middleware resolves the effective policy in this order:

    Require (from [ApiPilotRequireCsrf])
    > Skip (from [ApiPilotSkipCsrf])
    > global ProtectedMethods set

Require wins over the safe-method bypass. An endpoint marked
[ApiPilotRequireCsrf] enforces CSRF even on a GET.

An endpoint marked [ApiPilotSkipCsrf] bypasses the middleware even
when the request method is in the global set. This is the correct
shape for a public endpoint that intentionally accepts
unauthenticated state-changing requests, such as a webhook
receiver.

## The interaction with the Origin policy and Fetch Metadata

The Origin policy middleware and the Fetch Metadata middleware read
the same CsrfEndpointMetadata that the CSRF middleware reads. The
same precedence rule applies. An endpoint marked
[ApiPilotSkipCsrf] also bypasses the Origin policy and the Fetch
Metadata policy, because an endpoint that opts out of CSRF
protection is opting out of the CSRF-adjacent defenses.

An endpoint marked [ApiPilotRequireCsrf] enforces the Origin policy
and the Fetch Metadata policy even on a safe method.

## The pipeline placement

The recommended pipeline order is:

1. UseApiPilotCorrelation() first to establish the correlation ID.
2. UseApiPilotExceptions() second to catch downstream exceptions.
3. UseRouting() to resolve the endpoint.
4. The application authentication middleware.
5. UseApiPilotOriginPolicy() after authentication so the Origin
   check runs on protected requests.
6. UseApiPilotFetchMetadata() after the Origin policy.
7. UseApiPilotCsrfProtection() before the endpoint dispatch.
8. UseApiPilotContentNegotiation() if the application uses it.
9. The application authorization middleware.
10. The endpoint middleware.

The Origin and Fetch Metadata middleware must run after UseRouting()
so that the endpoint metadata is available. The CSRF middleware
must also run after UseRouting() for the same reason.

## The failure envelope

A CSRF rejection uses the standard error envelope. The three public
codes are CSRF_HEADER_MISSING, CSRF_TOKEN_INVALID, and
CSRF_TOKEN_EXPIRED. The internal reason is logged server-side and
never returned. See [csrf.md](csrf.md) for the full list of
internal reasons and their public codes.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Protected methods | POST, PUT, PATCH, DELETE | CsrfOptions.ProtectedMethods |
| Exempt paths | empty plus the bootstrap path | CsrfOptions.ExemptPaths |
| Exempt predicate | none | CsrfOptions.ExemptPredicate |

## Compatibility guarantees

- The default ProtectedMethods set is stable.
- The precedence rule Require > Skip > global is stable.
- The bootstrap path is always exempt.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [csrf.md](csrf.md) - the CSRF token and the service.
- [origin-policy.md](origin-policy.md) - the Origin policy that
  shares the endpoint metadata.
- [fetch-metadata.md](fetch-metadata.md) - the Fetch Metadata
  policy that shares the endpoint metadata.
- `../SPEC.md` - the normative CSRF flow.

