<!--
filepath: javascript/ApiPilot.Client/examples/react/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The React example: a hook that memoizes the client on baseUrl.
-->

# React example

A React integration example. The client is framework-agnostic; this
example wraps it in a hook so a React component can consume it.

## What this demonstrates

- A `useApiPilot(baseUrl)` hook that memoizes the client on `baseUrl`.
- Why the memo dependency is `baseUrl` (a primitive) and not a config
  object (whose identity changes on every render).
- Consuming the client in a function component.

## How to run

    npm install
    npm run dev

The example uses Vite. The `dev` script starts the Vite dev server,
which serves `index.html` and bundles `src/main.jsx`.

The repository does not commit `node_modules/`, lockfiles, or build
output. Every dependency is installed locally by the reader. See
`.gitignore` in this directory.

The client is imported from `../../../dist/apipilot-client.esm.js`
(a relative path to the built client artifact). It is not imported
from the published `@apipilot/client` package. This preserves the
client package zero-dependency promise: the client itself does not
depend on React; the React example depends on the client by relative
path only.

## The pattern

    import { useMemo } from "react";
    import { createApiPilotClient } from "../../../dist/apipilot-client.esm.js";

    export function useApiPilot(baseUrl) {
        return useMemo(
            () => createApiPilotClient({ baseUrl }),
            [baseUrl]
        );
    }

A React component calls the hook and receives the client:

    const client = useApiPilot("/api");

The hook recreates the client only when `baseUrl` changes. It does not
recreate on every render, which would reset the in-memory CSRF token
store.

## What this example is not

It is a small integration project, not a production sample. It is not
part of the published client package. It is not included in the
client tarball. It is not a full application.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

