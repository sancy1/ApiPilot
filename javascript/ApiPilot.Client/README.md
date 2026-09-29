<!--
filepath: javascript/ApiPilot.Client/README.md
package:  @apipilot/client
since:    v0.4.0
purpose:  Package README for the ApiPilot browser client.
-->

# @apipilot/client

A framework-agnostic, zero-runtime-dependency browser client for the
ApiPilot API boundary. The client speaks the wire contract defined in
`../../SPEC.md`. It depends only on standard browser APIs: fetch, Headers,
Request, Response, URL, AbortController, document.cookie, and crypto.

npm is one distribution channel, not an architectural dependency. The
client works in plain browser JavaScript, CDN script tags, TypeScript
projects, non-npm bundlers, import maps, Blazor JavaScript interop, and
server-rendered applications. The wire contract is HTTP plus JSON plus
cookies plus headers. React, Vue, Angular, Svelte, and plain JavaScript
all consume the same contract.

## Consumption forms

The client ships in four forms from one source of truth. npm is one of
four. Choosing a package manager is a convenience, not a requirement.

1. **ESM module.** `dist/apipilot-client.esm.js`. Import directly in a
   browser with a script type module tag, an import map, or any bundler
   that understands ESM. No package manager required.
2. **IIFE global.** `dist/apipilot-client.iife.js`. Load with a plain
   script tag. The public global is `window.ApiPilot`. No package
   manager required.
3. **TypeScript declarations.** `dist/apipilot-client.d.ts`. Place beside
   the JavaScript. Editors and the TypeScript compiler find it
   automatically. No package manager required.
4. **npm package.** `@apipilot/client`. One distribution channel among
   four. Installing through npm is convenient for bundler-based
   workflows but is not required for the other three forms.

## Internal namespace

Internal namespace. The generated IIFE and ESM builds share an internal
namespace, globalThis.__ApiPilotClient, that the source files populate at
load time. This namespace is an implementation detail. Applications must
not read from or write to globalThis.__ApiPilotClient. The public API is
only the exported symbols from the ESM module and the globalThis.ApiPilot
global from the IIFE build. The internal namespace is not covered by
semver.

## Bundler notes

Bundler notes. The sideEffects field is intentionally omitted. The source
files populate a shared namespace (globalThis.__ApiPilotClient) at load
time. That is a side effect. Claiming sideEffects: false would tell
bundlers they could safely tree-shake or reorder module initialization,
which could break the client. An application that bundles
ApiPilot.Client should bundle the full module and let its bundler treat
it as having side effects. Tree-shaking of an HTTP boundary helper is a
questionable optimization in the first place.

## How to build

The build script is a plain Node.js program. It uses only the Node
standard library. It reads scripts/manifest.json, concatenates the source
fragments in manifest order, appends the ESM entry to the ESM output and
the IIFE entry to the IIFE output, and copies the declarations file.
There is no minification, no bundling, and no tree-shaking in Phase 3.0
or Phase 3.1.

    node ./scripts/build.js

The command is the same regardless of whether a package manager is in
use. `npm run build` invokes the same command through the package.json
scripts block. The direct invocation is the canonical form.

Phase 3.2 finalized Path A: no minification, no bundling, and no
tree-shaking. The distributed artifacts are the unminified ESM and
IIFE outputs from the same source fragments. A consumer that wants a
minified artifact can produce one with their own build pipeline.

## How to test

The test suite uses the Node.js built-in test runner. There is no Jest,
no Vitest, and no Mocha. The command is:

    node --test --test-reporter=tap "tests/**/*.test.js"

The command produces TAP output, which is ASCII and CI-friendly. The same
command is available as `npm test` through the package.json scripts
block. The direct invocation is the canonical form and works without a
package manager. Run the build first; the tests import the built ESM
artifact, not the source.

## Supported runtimes

The client uses four browser-platform APIs: fetch, URL, globalThis,
and the caller-supplied AbortController. A browser must support all
four. This floor corresponds to browsers released from roughly 2019
onward: Chrome 71+, Firefox 65+, Safari 12.1+, and Edge 79+
(Chromium-based).

If you need a specific floor, verify against the four named APIs.
There is no separate supported-browsers list because there is no
browser feature in the client beyond those four APIs.

The Node floor is 18.0.0. Node 18 is the first release that provides
global fetch and the built-in test runner (node:test) that this package
uses for its own tests. The package.json engines field declares
this floor.

## Content Security Policy

The client is CSP-safe. It does not use any of the following patterns,
which are the ones CSP restricts under unsafe-eval:

- eval(...)
- new Function(...)
- Function(...) in any form
- document.write(...)
- element.innerHTML = ...
- setTimeout("...") with a string argument
- setInterval("...") with a string argument

A strict Content Security Policy that forbids unsafe-eval and
unsafe-inline does not need to be relaxed to use this client.
The static scan that confirms this is documented in the repository
CHANGELOG under the Phase 3.2 entry.

## The client API

The client is created by a single factory function. The factory accepts a
configuration object. Every property except baseUrl has a sensible
default and a first-class override:

    import { createApiPilotClient } from "@apipilot/client";

    const client = createApiPilotClient({
        baseUrl: "/api",
        csrfPath: "/api/csrf",          // C2
        credentials: "same-origin",     // C3
        protectedMethods: ["POST", "PUT", "PATCH", "DELETE"],  // C4
        csrfHeader: "X-CSRF-TOKEN",     // C5
        onCsrfExpired: null,            // C6
        fetch: globalThis.fetch,        // C7
    });

The seven configuration properties correspond to the Option D surface
C1 through C7. Each has a test that uses the override:

| # | Property | Default | Purpose |
| --- | --- | --- | --- |
| C1 | baseUrl | required | The API base URL |
| C2 | csrfPath | baseUrl + /csrf | The bootstrap endpoint path |
| C3 | credentials | same-origin | The fetch credentials mode |
| C4 | protectedMethods | POST, PUT, PATCH, DELETE | Methods that receive the CSRF header |
| C5 | csrfHeader | X-CSRF-TOKEN | The CSRF header name |
| C6 | onCsrfExpired | null | Callback fired on CSRF_TOKEN_EXPIRED |
| C7 | fetch | globalThis.fetch | The fetch implementation |

The client has seven methods:

    const res = await client.get("/items/1");
    const created = await client.post("/items", { name: "x" });
    const updated = await client.put("/items/1", { name: "y" });
    const patched = await client.patch("/items/1", { name: "z" });
    const gone = await client.delete("/items/1");
    const custom = await client.request("/items/1", { method: "HEAD" });
    const token = await client.csrf.get();

The response is an ApiPilotResponse object. The eight client-controlled
fields are ok, status, success, data, message, error, meta, and raw. The
raw field is the underlying Response object. Unknown envelope fields are
preserved on the response object via spread, but the eight client-
controlled fields always win on a name collision. A server cannot forge
the client transport state.

The four failure categories map to the four error classes:

- ApiPilotHttpError -- the HTTP request itself failed (network, DNS,
  abort, unreadable body). The cause is on .cause.
- ApiPilotEnvelopeError -- the server returned a valid ApiPilot envelope
  with success: false. This includes 4xx and 5xx responses with a valid
  envelope. The wire error is on .wireError; the HTTP status on .status.
- ApiPilotProtocolError -- the response is not a valid ApiPilot envelope
  (not JSON, or JSON without the success discriminator). The raw body is
  on .rawBody; what was expected is on .expected.
- ApiPilotConfigurationError -- the client was misconfigured or called
  incorrectly (missing baseUrl, unresolvable URL, no fetch available).

The CSRF bootstrap contract. The bootstrap endpoint returns a bare
`{ "token": "<token>" }` object, the only successful ApiPilot response
that does not use the standard envelope. The token is stored in memory
only. It is never written to localStorage, sessionStorage, or a cookie.
The CSRF header is attached only to protected requests whose origin
matches the API origin. A cross-origin request never receives the
header. On CSRF_TOKEN_EXPIRED the client does not retry and does not
replay. It throws an ApiPilotEnvelopeError and, if configured, calls
onCsrfExpired. The callback is a notification, not an interceptor: its
return value is ignored, and a thrown exception is contained on .cause
and cannot replace the security error the client throws.

The minimal response contract. The client uses exactly three members
from a response: status (number), ok (boolean), and text() (a function
returning a Promise of the body string). It does not use .json(),
.headers, .clone(), .body, or any other member. A custom fetch
implementation only needs to return an object with those three members.
This is deliberately a lower bar than a full Response, so unusual
environments can supply their own.

## Phase 3.0 and 3.1 scope

Phase 3.0 shipped the package skeleton and the build pipeline. Phase 3.1
adds the client factory (createApiPilotClient), the request pipeline, the
CSRF integration, and the typed errors. The ESM and IIFE entries export
exactly eight runtime symbols: VERSION, the six error symbols
(ApiPilotClientError, ApiPilotConfigurationError, ApiPilotHttpError,
ApiPilotEnvelopeError, ApiPilotProtocolError, and ApiPilotWireError),
and createApiPilotClient.

The request and response types (ApiPilotClient, RequestOptions,
ApiPilotResponse, ApiPilotClientConfig) are TypeScript interfaces
declared in src/types.d.ts. They have no runtime footprint and are
not present in Object.keys(module).

## License

MIT. See LICENSE.

