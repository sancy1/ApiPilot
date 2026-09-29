<!--
filepath: javascript/ApiPilot.Client/examples/README.md
package:  n/a (examples)
since:    v0.4.0
purpose:  Index of the ApiPilot client framework integration examples.
-->

# ApiPilot client examples

Seven framework integration examples. Each demonstrates one pattern
for wrapping the ApiPilot client in a specific framework.

## What these are

Small integration projects. Each is a demonstration of a pattern, not
a production sample. Each is a couple of files: an entry point, a
framework-specific wrapper (a hook, a composable, a store, a service,
or a JS module), and a README.

The examples do not belong to the published client package. They are
not included in the client package's `files` array. They are not
shipped in the tarball. They live in the repository beside the client
source and are read by anyone learning the client.

The examples do **not** depend on `@apipilot/client` as an npm package.
Each example imports the client from a relative path
(`../../dist/apipilot-client.esm.js` or a deeper equivalent). This is
the boundary that keeps the client framework-agnostic: the client
does not depend on any framework, and the examples depend on the
client only by relative path.

## The examples

| # | Directory | Framework | Pattern | Runnable? |
| --- | --- | --- | --- | --- |
| 1 | `vanilla-js/` | none | Plain `<script type="module">` | Yes (open in a browser) |
| 2 | `react/` | React 19 | `useApiPilot(baseUrl)` hook | Yes (`npm install && npm run dev`) |
| 3 | `nextjs/` | Next.js 16 | App Router Client Component | Yes (`npm install && npm run dev`) |
| 4 | `vue/` | Vue 3 | `useApiPilot(baseUrl)` composable | Yes (`npm install && npm run dev`) |
| 5 | `svelte/` | Svelte 5 | `createApiPilotStore(baseUrl)` readable store | Yes (`npm install && npm run dev`) |
| 6 | `angular/` | Angular 22 | `@Injectable({ providedIn: 'root' })` standalone service | Source-level illustration |
| 7 | `blazor-interop/` | Blazor WASM | JS module loaded via `IJSRuntime` | Source-level illustration |

## Runnable vs source-level illustration

- **Runnable**: the example has a `package.json` (where appropriate)
  and can be started with the commands in its README. Vanilla JS needs
  no install; it is opened in a browser. React, Next.js, Vue, and
  Svelte are runnable with their framework's dev server.
- **Source-level illustration**: the example ships the framework-
  specific integration file and a README, but not the framework's
  project scaffolding. Angular and Blazor interop are illustrations:
  the reader scaffolds the framework project and copies the
  integration file in.

## What every example shares

- The client is imported from the built ESM artifact
  (`dist/apipilot-client.esm.js`) by relative path.
- The client is constructed once. It is not reconstructed on every
  render or every request.
- The client is a plain object. It is not reactive. Any reactivity in
  the example belongs to the framework's state mechanism, not to the
  client.
- The framework-specific file wraps the client in the framework's
  idiom: a hook, a composable, a store, a service, or a JS module.

## Installing dependencies

The repository does not commit `node_modules/`, lockfiles, or build
output. Each example's directory has its own `.gitignore`. The reader
runs `npm install` (or the equivalent for their package manager)
locally.

## Related documents

- [docs/fetch-helper.md](../../../docs/fetch-helper.md) - the client
  contract.
- [Client package README](../README.md) - the package overview.

