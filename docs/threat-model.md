<!--
filepath: docs/threat-model.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot threat model: what the library protects against and what it does not.
-->

# The ApiPilot threat model

This document states, in practical terms, what ApiPilot protects
against and what it explicitly does not protect against. The
library is a boundary library. It hardens the API edge. It does not
replace the application security stack.

## What this document covers

- The attacker model.
- The threat classes ApiPilot addresses.
- The threat classes ApiPilot explicitly does not address.
- The assumptions the library makes about the application.

## The attacker model

ApiPilot assumes the following attackers are in scope:

- A network attacker who can intercept or modify unencrypted
  traffic. Mitigation: HTTPS is assumed, and the cookie Secure
  flag is enforced.
- A cross-origin attacker: a website controlled by the adversary
  that causes a victim browser to send a state-changing request to
  the target application. Mitigation: CSRF tokens, the Origin
  policy, and the Fetch Metadata policy.
- An XSS attacker: a script running on the target origin itself,
  either injected by the application or by a third-party asset.
  Partial mitigation: the authentication credential is HttpOnly;
  the CSRF request token is HttpOnly or in memory.
- An attacker who has obtained a Data Protection key. Mitigation:
  none within ApiPilot. The application must protect the key ring.

ApiPilot does not assume the following attackers are in scope:

- An attacker with code execution on the server.
- An attacker who has already authenticated as the victim.
- A malicious administrator with access to the Data Protection
  keys.

## Threat classes ApiPilot addresses

### Cross-site request forgery

A cross-origin attacker causes the victim browser to send a
state-changing request with the victim's authentication cookie. The
browser sends the cookie automatically; the attacker cannot read the
response but can trigger the side effect.

Mitigation: the CSRF request token. The token is issued by ApiPilot,
bound to the authenticated session, held in memory by the client,
and sent in a custom header. A cross-origin attacker cannot read
the token and cannot add the header.

See [csrf.md](csrf.md) and [unsafe-methods.md](unsafe-methods.md).

### Cross-origin requests from a disallowed origin

A browser adds an Origin header on state-changing requests. An
attacker who can trigger a request from a page under their control
produces an Origin header that does not match the application.

Mitigation: the Origin policy. The policy compares the Origin
against an allow-list and rejects a non-matching origin. See
[origin-policy.md](origin-policy.md).

### Cross-site requests in browsers that send Fetch Metadata

Modern browsers send the Sec-Fetch-Site, Sec-Fetch-Mode, and
Sec-Fetch-Dest headers. The Sec-Fetch-Site header carries cross-site
or same-site, giving the server an explicit signal about the request
context.

Mitigation: the Fetch Metadata policy. When the profile is Compat or
Strict, a cross-site value is rejected on protected methods. See
[fetch-metadata.md](fetch-metadata.md).

### Session fixation via cookie scope

An attacker who can set a cookie for a broader domain than the
application can cause the victim browser to send a session cookie on
requests the attacker controls.

Mitigation: the cookie profile. Secure, HttpOnly, SameSite, and the
__Host- prefix rules. See [cookies.md](cookies.md).

### Token replay across sessions

A CSRF token issued for one session is presented in another. The
attacker obtains a token and tries to use it after a session change.

Mitigation: the binding fingerprint. The token payload carries a
binding derived from the authenticated subject or the server
session. A token issued for one binding does not validate for
another. See [csrf.md](csrf.md).

### Token replay after logout

A CSRF token issued before logout remains valid until it expires.
The attacker holds the token and tries to use it in a new session.

Mitigation: the authentication transition hooks. The application
calls OnLogoutAsync; when a rotation store is registered, the
marker is cleared and the token becomes unusable. Without a store,
the token remains valid until natural expiry; the library reports
this honestly. See [csrf.md](csrf.md).

## Threat classes ApiPilot does not address

### Authentication

ApiPilot does not authenticate users, does not issue JWTs, does not
manage sessions beyond the binding fingerprint, and does not
integrate with ASP.NET Core Identity directly. The application owns
authentication.

### Authorization

ApiPilot does not authorize requests. A CSRF-valid request may still
be unauthorized. The application authorization layer is separate.

### Injection attacks

ApiPilot does not sanitize SQL, command-line arguments, or output.
Injection defenses belong to the application data layer.

### Denial of service

ApiPilot does not implement rate limiting or request throttling.
Rate limiting belongs to the application or the reverse proxy.

### Trust of the application's own code

ApiPilot trusts the application's controllers, endpoints, and
handlers. A vulnerability in application code that bypasses the
CSRF pipeline is not a vulnerability in ApiPilot.

## The assumptions the library makes

ApiPilot assumes:

- HTTPS is used in production. Without HTTPS, the Secure flag is
  ineffective and the network attacker model is in scope.
- Data Protection keys are protected. The key ring must be stored
  in a location the attacker cannot read. In production, the key
  ring must be shared or persistent across instances.
- The application uses HttpOnly and Secure cookies for its
  authentication credential.
- The application does not expose the CSRF request token to
  JavaScript when the double-submit cookie pattern is unnecessary.

Violating an assumption weakens the corresponding mitigation. The
application is responsible for the assumptions.

## Related documents

- [csrf.md](csrf.md) - the CSRF contract.
- [cookies.md](cookies.md) - the cookie profiles.
- [unsafe-methods.md](unsafe-methods.md) - the method protection
  contract.
- [origin-policy.md](origin-policy.md) - the Origin policy.
- [fetch-metadata.md](fetch-metadata.md) - the Fetch Metadata
  policy.
- [data-protection.md](data-protection.md) - the key ring and the
  multi-instance configuration.
- `../SPEC.md` - the normative security contract.

