<!--
filepath: docs/fetch-helper.md
package:  n/a (repository root docs)
since:    v0.4.0
purpose:  The ApiPilot browser client contract: the request pipeline, the CSRF integration, and the Option D surface.
-->

# The ApiPilot browser client contract

This document describes the ApiPilot browser client: how it makes
requests, how it attaches the CSRF token, how it classifies responses,
and how it surfaces failures. The normative wire contract is
`../SPEC.md`. Where this document and SPEC.md disagree, SPEC.md wins.

The client is framework-agnostic and has zero runtime dependencies. It
is consumed through four forms (ESM module, IIFE global, TypeScript
declarations, npm package), all generated from one source of truth.
npm is one distribution channel, not an architectural dependency.

## What this document covers

- The client lifecycle: the factory and the config.
- The request pipeline: URL resolution, protected-method detection,
  the CSRF header, and the fetch call.
- The response classification: how the response body determines what
  the client does.
- The four failure categories and the error classes they map to.
- The response object and the client-precedence rule.
- The CSRF bootstrap contract.
- The `onCsrfExpired` callback.
- The minimal response contract.
- The CSP-safe guarantee.
- Supported runtimes.
- The Option D surface.

## The client lifecycle

The client is created by `createApiPilotClient(config)`. The factory
returns an object with seven methods: `get`, `post`, `put`, `patch`,
`delete`, `request`, and the `csrf` store accessor.

The client is a plain object. It is not reactive. It does not hold
application state. The only state it holds is the CSRF token, and
that state is in a closure variable inside the CSRF store.

The client is safe to create once and reuse. It is not safe to create
on every render in a React component, because that would reset the
CSRF token store on every render. See `examples/react/` for the
memoization pattern.

## The request pipeline

A request passes through these steps:

1. **URL resolution.** The request path is resolved against `baseUrl`
   with `new URL(path, apiBase)`. A relative path appends to the base.
   An absolute path replaces the base path. A malformed path throws
   `ApiPilotConfigurationError`.

2. **Origin comparison.** The request origin is compared against the
   API origin. The comparison uses `.origin`, which normalizes
   scheme, host, and port. A cross-origin request never receives the
   CSRF header.

3. **Protected-method detection.** A request is protected when the
   method is in `protectedMethods` (default POST, PUT, PATCH, DELETE),
   the method is not a safe method (GET, HEAD, OPTIONS, TRACE), and
   the request origin equals the API origin. All three conditions must
   hold. A safe method is never protected, even when listed in
   `protectedMethods`.

4. **CSRF header attachment.** A protected request reads the CSRF
   token from the store (fetching it lazily from the bootstrap
   endpoint if not present) and attaches it to the configured header
   (default `X-CSRF-TOKEN`).

5. **Header merge.** The caller-supplied headers are copied first.
   The CSRF header is added. If the request carries a body and the
   caller has not set a `Content-Type`, the client sets
   `application/json`. A caller-supplied `Content-Type` always wins.

6. **Fetch invocation.** The `fetch` implementation is called. By
   default the client uses `globalThis.fetch`. The implementation is
   overridable through the `fetch` config property.

7. **Body read.** The response body is read as text via
   `response.text()`. A failure here throws `ApiPilotHttpError` with
   the underlying error on `.cause`.

8. **Envelope parse.** The text is parsed as JSON. A parse failure
   throws `ApiPilotProtocolError`. A parsed value that is not an
   object, or that does not carry a boolean `success` field, also
   throws `ApiPilotProtocolError`. The `rawBody` and `expected` fields
   describe the mismatch.

9. **Failure path.** If `envelope.success` is `false`, the client
   builds an `ApiPilotEnvelopeError`. The wire error is on
   `.wireError`; the HTTP status is on `.status`. On
   `CSRF_TOKEN_EXPIRED`, the `onCsrfExpired` callback is invoked
   (see below). The error is thrown.

10. **Success path.** If `envelope.success` is `true`, the client
    returns the response object. No error is thrown.

## The response classification

The response is classified by its **body**, not by `response.ok`. A
4xx or 5xx response with a valid ApiPilot error envelope is an
envelope error, not a transport error. The classification tree:

- Transport failure (fetch throws) -> `ApiPilotHttpError`.
- Body read failure (`text()` rejects) -> `ApiPilotHttpError`.
- Body is not JSON -> `ApiPilotProtocolError`.
- Body is JSON without a boolean `success` -> `ApiPilotProtocolError`.
- Body is JSON with `success: false` -> `ApiPilotEnvelopeError`.
- Body is JSON with `success: true` -> the response is returned.

This distinction is important: an application can catch
`ApiPilotEnvelopeError` to handle "the server understood and
rejected" separately from "the network failed."

## The four failure categories

- `ApiPilotHttpError` - the HTTP request itself failed. Network, DNS,
  abort, or an unreadable body. The underlying error is on `.cause`.
- `ApiPilotEnvelopeError` - the server returned a valid ApiPilot
  envelope with `success: false`. This includes 4xx and 5xx responses
  that carry a valid envelope. The wire error is on `.wireError`; the
  HTTP status on `.status`.
- `ApiPilotProtocolError` - the response is not a valid ApiPilot
  envelope. The raw body is on `.rawBody`; what was expected is on
  `.expected`; the HTTP status on `.status`.
- `ApiPilotConfigurationError` - the client was misconfigured or
  called incorrectly. Missing `baseUrl`, unresolvable URL, no `fetch`
  available. Thrown at construction time or at the call site.

## The response object

A successful request resolves to an `ApiPilotResponse<T>` object. The
eight client-controlled fields are:

| Field | Type | Meaning |
| --- | --- | --- |
| `ok` | boolean | The HTTP `response.ok` value |
| `status` | number | The HTTP status code |
| `success` | boolean | The envelope success discriminator |
| `data` | T \| null | The envelope data field |
| `message` | string \| null | The envelope message field |
| `error` | ApiPilotWireError \| null | The normalized wire error |
| `meta` | object \| null | The envelope meta field |
| `raw` | Response | The underlying response object |

Unknown envelope fields are preserved on the response object via
spread. The eight client-controlled fields are placed **after** the
spread, so they always win on a name collision. A server that sends
`{ "success": true, "ok": false, "status": 999, "raw": "evil" }`
cannot forge the client transport state.

## The CSRF bootstrap contract

The CSRF bootstrap endpoint returns a bare `{ "token": "<token>" }`
object. This is documented in `../SPEC.md` and in `csrf.md` as the
only successful ApiPilot response that does not use the standard
success envelope.

The bootstrap response is classified by the same rules as any other
response, with one additional case:

- Body is JSON with a non-empty string `token` and no `success` field
  -> success. The token is extracted.
- Body is JSON with `success: false` -> `ApiPilotEnvelopeError`.
- Body is JSON with `success: true` and a non-empty string `token`
  -> success.
- Any other body -> `ApiPilotProtocolError`.

The token is stored in a closure variable. It is never written to
`localStorage`, `sessionStorage`, `document.cookie`, or any other
persistent store. It is never logged. It is never exposed on the
client object.

Concurrent `client.csrf.get()` calls coalesce into a single
bootstrap fetch. A `refresh()` or `clear()` during an in-flight
bootstrap causes the in-flight result to be discarded (a generation
counter guards the write). The next `get()` starts a fresh bootstrap.

## The onCsrfExpired callback

When the server responds with `CSRF_TOKEN_EXPIRED`, the client throws
an `ApiPilotEnvelopeError` and, if the `onCsrfExpired` callback is
configured, invokes it with the error and the response.

The callback is a notification, not an interceptor:

- Its return value is ignored.
- Its exception is contained on `.cause` if `.cause` is free.
- It cannot replace the security error the client throws.

The client does not retry the original request. It does not re-fetch
the CSRF token. It does not replay. The application decides what to do
next.

## The minimal response contract

The client uses exactly three members of a response:

- `status` - a number.
- `ok` - a boolean.
- `text()` - a function returning a Promise of the body string.

It does not use `.json()`, `.headers`, `.clone()`, `.body`, or any
other member. A custom `fetch` implementation only needs to return an
object with those three members. This is deliberately a lower bar than
a full `Response`, so unusual environments can supply their own.

## The CSP-safe guarantee

The client is CSP-safe. It does not use any of the following patterns,
which are the ones CSP restricts under unsafe-eval:

- `eval(...)`
- `new Function(...)`
- `Function(...)` in any form
- `document.write(...)`
- `element.innerHTML = ...`
- `setTimeout("...")` with a string argument
- `setInterval("...")` with a string argument

A strict Content Security Policy that forbids unsafe-eval and
unsafe-inline does not need to be relaxed to use this client.

## Supported runtimes

The client uses four browser-platform APIs: `fetch`, `URL`,
`globalThis`, and the caller-supplied `AbortController`. A browser must
support all four. This floor corresponds to browsers released from
roughly 2019 onward: Chrome 71+, Firefox 65+, Safari 12.1+, and
Edge 79+ (Chromium-based).

The Node floor is 18.0.0. Node 18 is the first release that provides
global `fetch` and the built-in test runner (`node:test`) that the
client package uses for its own tests. The `engines` field in
`package.json` declares this floor.

## The Option D surface

Every transformation on the client path has a sensible default and a
first-class override. The seven overrides are the client config:

| # | Property | Default | Purpose |
| --- | --- | --- | --- |
| C1 | `baseUrl` | required | The API base URL |
| C2 | `csrfPath` | `baseUrl + "/csrf"` | The bootstrap endpoint path |
| C3 | `credentials` | `"same-origin"` | The fetch credentials mode |
| C4 | `protectedMethods` | `["POST", "PUT", "PATCH", "DELETE"]` | Methods that receive the CSRF header |
| C5 | `csrfHeader` | `"X-CSRF-TOKEN"` | The CSRF header name |
| C6 | `onCsrfExpired` | `null` | Callback fired on `CSRF_TOKEN_EXPIRED` |
| C7 | `fetch` | `globalThis.fetch` | The fetch implementation |

Each override has a test that uses it, not just a test that asserts
it exists. The overrides are consumed at the point where the
transformed value is used.

## Compatibility guarantees

- The eight runtime symbols are stable: `VERSION`, the six error
  symbols, and `createApiPilotClient`.
- The four error classes are stable.
- The documented bootstrap response shape is stable.
- The four failure categories are stable.
- The wire contract is fixed by `../SPEC.md`. The client cannot
  override it.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [csrf.md](csrf.md) - the server-side CSRF contract.
- [cookies.md](cookies.md) - the cookie profiles and the CSRF cookie
  trade-off.
- [threat-model.md](threat-model.md) - what ApiPilot protects against
  and what it does not.
- `../SPEC.md` - the normative wire contract.
- `../javascript/ApiPilot.Client/README.md` - the client package
  README.
- `../javascript/ApiPilot.Client/examples/README.md` - the examples
  index.

