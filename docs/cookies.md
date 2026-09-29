<!--
filepath: docs/cookies.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot cookie profile contract: the three profiles, the prefix rules, and the startup validation.
-->

# The ApiPilot cookie contract

This document describes the cookie profiles ApiPilot validates at
startup. The library never sets a cookie. The profiles are a
declaration that the application applies to its own cookie
configuration. The normative contract is `../SPEC.md`.

## What this document covers

- The three default profiles (authentication, session, CSRF).
- The CookieProfile value object.
- The __Host- and __Secure- prefix rules.
- The SameSite=None with Secure rule.
- The CSRF double-submit cookie trade-off.
- The path scope rule.
- The startup validation.

## The three default profiles

From SPEC.md "Cookie baseline", the defaults are:

- Authentication profile. Secure=true, HttpOnly=true,
  SameSite=Lax, Path=/, no Domain (host-only). The base name is
  "auth".
- Session profile. Secure=true, HttpOnly=true, SameSite=Strict,
  Path=/, no Domain. The base name is "session".
- CSRF profile. Secure=true, HttpOnly=false, SameSite=Lax,
  Path=/, no Domain. The base name is "csrf". The CSRF profile is
  only meaningful when the application uses the double-submit
  pattern; see the trade-off section below.

An application that needs different values replaces the profile.

## The CookieProfile value object

A CookieProfile carries the base name (without a prefix), the two
security flags (Secure, HttpOnly), the SameSite value, the Path, and
an optional Domain. The base name does not carry a prefix; the
effective name is CookieProfileOptions.NamePrefix + Name.

The required modifier on Name, Secure, HttpOnly, and SameSite makes
the compiler catch a profile construction that omits a
security-relevant field.

## The prefix rules

RFC 6265bis defines two cookie prefixes:

- __Host- requires Secure=true, no Domain, and Path=/.
- __Secure- requires Secure=true.

The rules apply to the effective name (prefix + base name), not to
the prefix or the name in isolation. The defined prefix values are
the empty string, __Host-, and __Secure-. Any other prefix fails
startup validation.

## The SameSite=None with Secure rule

SameSite=None requires Secure=true. A cookie with SameSite=None and
Secure=false is sent by the browser over plain HTTP and is
vulnerable to interception. ApiPilot rejects the combination at
startup.

## The CSRF double-submit cookie trade-off

The CSRF profile sets HttpOnly=false. This is necessary only because
the double-submit pattern requires browser JavaScript to read the
cookie value and echo it in the request header.

HttpOnly=false is not inherently safer than HttpOnly=true. It
increases exposure to XSS: any script on the origin can read the
cookie value. The double-submit cookie should only be enabled when
the trade-off is deliberate. The recommended pattern is to hold the
CSRF token in memory and skip the cookie entirely.

The CSRF cookie never carries an authentication credential. Its
value is a CSRF request token. The authentication and session
cookies remain HttpOnly.

## The path scope rule

The Path is the cookie scope. ApiPilot does not claim that a
specific path is "narrow". The path must be non-empty and start
with a slash. A value of "/" is the widest possible scope and is
required by RFC 6265bis for cookies whose effective name starts
with __Host-. For all other cookies the application should choose
the narrowest path that serves the cookie purpose.

## The startup validation

CookieProfileOptionsValidator runs at startup through
ValidateOnStart. It rejects:

- An undefined prefix.
- An empty effective name, or an effective name containing a
  character RFC 6265 forbids (comma, semicolon, space, double quote,
  backslash, control characters).
- An empty Path or a Path that does not start with a slash.
- A Domain that is not a valid hostname without scheme, path, port,
  or whitespace.
- An undefined SameSite value.
- SameSite=None with Secure=false.
- A violation of the __Host- or __Secure- prefix rules.

A validation failure prevents the host from starting.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Authentication profile | Secure + HttpOnly + SameSite=Lax + Path=/ | CookieProfileOptions.AuthenticationProfile |
| Session profile | Secure + HttpOnly + SameSite=Strict + Path=/ | CookieProfileOptions.SessionProfile |
| CSRF profile | Secure + HttpOnly=false + SameSite=Lax | CookieProfileOptions.CsrfProfile |
| Cookie name prefix | empty | CookieProfileOptions.NamePrefix |
| Path scope | / | CookieProfile.Path |
| Domain scope | unset (host-only) | CookieProfile.Domain |

## Compatibility guarantees

- The three default profiles are stable.
- The __Host- and __Secure- rules follow RFC 6265bis.
- The SameSite=None + Secure rule is enforced.
- An insecure combination fails startup.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [csrf.md](csrf.md) - the CSRF request token and the bootstrap
  endpoint.
- [threat-model.md](threat-model.md) - the XSS and session-fixation
  threats the cookie rules address.
- `../SPEC.md` - the normative "Cookie baseline" section.

