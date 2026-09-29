<!--
filepath: javascript/ApiPilot.Client/examples/vue/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The Vue example: a browser-safe composable exposing the ApiPilot client.
-->

# Vue example

A Vue 3 integration example. The client is framework-agnostic; this
example exposes it through a composable so a Vue component can consume
it.

## What this demonstrates

- A `useApiPilot(baseUrl)` composable that exposes a browser-safe
  factory (`ensure()`), not a client constructed only in `onMounted`.
- Why a browser-safe factory is used: under SSR, `ensure()` returns
  `null` and the consumer handles the null case.
- How to consume the composable in a Vue `setup()`.

## How to run

    npm install
    npm run dev

The `dev` script starts the Vite dev server.

The repository does not commit `node_modules/`, lockfiles, or build
output. Every dependency is installed locally by the reader. See
`.gitignore` in this directory.

## The pattern

The composable is:

    import { ref } from "vue";
    import { createApiPilotClient } from "../../../../dist/apipilot-client.esm.js";

    export function useApiPilot(baseUrl) {
        const client = ref(null);
        const ensure = () => {
            if (typeof window === "undefined") return null;
            if (client.value === null) {
                client.value = createApiPilotClient({ baseUrl });
            }
            return client.value;
        };
        return { client, ensure };
    }

A component calls `ensure()` at the point of use:

    const { ensure } = useApiPilot("/api");
    const client = ensure();
    if (!client) { /* SSR: no client available */ }

The composable does not construct the client in `onMounted`. A
consumer that needs the client during `setup()` gets it from
`ensure()` directly. A consumer that needs it after mount reads the
reactive `client` ref.

## SSR behavior

Vue applications may render on the server. The ApiPilot client is a
browser-only object: it depends on `fetch`, `URL`, and `globalThis`.
The composable returns `null` from `ensure()` when `window` is not
defined. The consumer decides how to handle the null case (defer the
request to after mount, or skip it).

## What this example is not

It is a small integration project, not a production sample. It is not
part of the published client package. It is not included in the
client tarball. It is not a full application.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

