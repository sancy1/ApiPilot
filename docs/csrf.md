<!--
filepath: docs/csrf.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot CSRF contract: the two-credential model, the token format, and the service.
-->

# The ApiPilot CSRF contract

This document describes how ApiPilot issues, validates, and rotates
CSRF tokens. The normative contract is `../SPEC.md`. Where this
document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The two-credential model.
- The token format and the signing primitive.
- The bootstrap endpoint.
- The service: issuance, validation, rotation.
- The authentication transition hooks.
- The failure codes and the internal reasons.
- The Option D surface.

## The two-credential model

From SPEC.md "CSRF flow - two-credential model", ApiPilot separates
the authentication credential from the CSRF request credential:

- Authentication or session credential: an HttpOnly and Secure
  cookie, browser-managed, unreadable by JavaScript.
- CSRF request credential: a separate token issued by ApiPilot, held
  in memory by the client, sent on protected requests in a custom
  header.

The two are separate so that an XSS attack that reads every
JavaScript-readable value on the page cannot extract the
authentication credential, and an attacker who can force a
cross-site request cannot guess the CSRF request token.

The default header is X-CSRF-TOKEN. The default bootstrap path is
/api/csrf. Both are configurable through CsrfOptions.

## The token format

The token is produced by Data Protection. The library does not
implement its own cryptography. DataProtectionCsrfTokenSigner is the
default signer. It builds a plaintext payload of the form

    issuedAtUnixSeconds | randomBase64Url | bindingFingerprint

and hands it to IDataProtector.Protect. The wire token is the
protector output. The library treats the wire format as opaque.

The binding is a SHA-256 fingerprint of a session identifier, a
subject claim, or a configured pre-authentication source. The
fingerprint is derived through CsrfBindingProvider. The binding is
never used directly as a client-visible value and is never written
to a log line.

## The bootstrap endpoint

From SPEC.md "CSRF bootstrap response", the endpoint is a GET that
responds with a bare object:

    { "token": "<csrf-request-token>" }

The response contains only the CSRF request token. No authentication
credential is serialized. The response sets Cache-Control: no-store.

This is the only place in ApiPilot where the successful response is
not the standard success envelope. The wire shape is mandated by
SPEC.md.

The path is configured by CsrfOptions.BootstrapPath (default
/api/csrf). When CsrfBootstrapOptions.Enabled is false, the endpoint
is not mapped and the path falls through.

## The service

ICsrfService exposes three async methods:

- IssueAsync(HttpContext, CancellationToken) returns a fresh token
  bound to the current request, or null when no binding can be
  resolved.
- ValidateAsync(HttpContext, string?, CancellationToken) returns a
  CsrfValidationResult carrying the internal reason and the public
  wire code.
- RotateAsync(HttpContext, CancellationToken) issues a fresh token
  and, when a rotation store is registered, records a rotation
  marker so previous tokens for the binding are rejected.

The service never generates an anonymous binding. A missing binding
is a normal, expected outcome.

## Rotation

The rotation store is an optional ICsrfRotationStore. When one is
registered, RotateAsync records a marker and ValidateAsync rejects
any token issued before the marker.

When no store is registered, RotateAsync issues a fresh token and
emits a structured warning on every call: the previous token
remains valid until its natural expiry. The warning is not
throttled.

## The authentication transition hooks

The library never hooks ASP.NET Core Identity, SignInManager, or
any authentication framework directly. The application calls
ICsrfTransitionListener after its authentication flow completes and
before its session state ends:

- OnLoginAsync(HttpContext) issues a fresh token bound to the new
  authenticated subject and returns it so the login response can
  deliver it to the client.
- OnLogoutAsync(HttpContext) resolves the binding from the current
  context and clears the rotation marker when a store is registered.
  It works when called before the application clears session state.
- OnLogoutAsync(string previousBinding) accepts the binding the
  application captured before clearing session state. It works in
  both orderings.

## The failure codes

The wire-visible codes are stable and frozen by SPEC.md:

- CSRF_HEADER_MISSING: the request did not carry the CSRF header.
- CSRF_TOKEN_INVALID: the token failed validation for any reason
  other than expiry.
- CSRF_TOKEN_EXPIRED: the token was valid but past its lifetime.

The internal reason distinguishes eight cases (Malformed,
WrongVersion, InvalidSignature, WrongSession, Rotated, Expired,
BindingMissing, Ok). The reason is logged server-side and never
returned to the client. CsrfOptions.CodeMapping maps the internal
reason to the public code.

## The Option D surface

Every transformation on the CSRF path has a sensible default and a
first-class override.

| Concern | Default | Override |
| --- | --- | --- |
| Token signer | DataProtectionCsrfTokenSigner | ICsrfTokenSigner (replaceable DI service) |
| Data Protection purpose | ApiPilot.Csrf.v1 | CsrfTokenOptions.ProtectionPurpose |
| Token entropy | 32 bytes | CsrfTokenOptions.TokenEntropyBytes |
| Token lifetime | 2 hours | CsrfTokenOptions.TokenLifetime |
| Clock | TimeProvider.System | CsrfTokenOptions.TimeProvider |
| Header name | X-CSRF-TOKEN | CsrfOptions.HeaderName |
| Rotation policy | OnBootstrap | CsrfOptions.RotationPolicy |
| Bootstrap path | /api/csrf | CsrfOptions.BootstrapPath |
| Missing-header code | CSRF_HEADER_MISSING | CsrfOptions.MissingHeaderCode |
| Reason-to-code mapping | SPEC.md defaults | CsrfOptions.CodeMapping |
| Pre-auth binding source | none (fail closed) | CsrfOptions.PreAuthBindingSource |
| Binding source | CsrfBindingProvider | ICsrfBindingProvider (replaceable DI service) |
| Rotation store | none (expiry-only) | ICsrfRotationStore (replaceable DI service) |
| Transition listener | DefaultCsrfTransitionListener | ICsrfTransitionListener (replaceable DI service) |

## Compatibility guarantees

- The header name defaults to X-CSRF-TOKEN.
- The bootstrap endpoint returns a bare token object.
- The three public failure codes are stable.
- The default token lifetime is two hours.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [unsafe-methods.md](unsafe-methods.md) - how the CSRF middleware
  decides which requests to protect.
- [cookies.md](cookies.md) - the optional double-submit cookie
  profile and the XSS trade-off.
- [data-protection.md](data-protection.md) - the key ring and the
  multi-instance configuration.
- [threat-model.md](threat-model.md) - what ApiPilot protects
  against and what it does not.
- `../SPEC.md` - the normative CSRF sections.

