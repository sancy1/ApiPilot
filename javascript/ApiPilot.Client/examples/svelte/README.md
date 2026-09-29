<!--
filepath: javascript/ApiPilot.Client/examples/svelte/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The Svelte example: a non-reactive client exposed through a readable store.
-->

# Svelte example

A Svelte 5 integration example. The client is framework-agnostic; this
example exposes it through a Svelte store so a component can consume
it.

## What this demonstrates

- A `createApiPilotStore(baseUrl)` function that wraps the client in a
  Svelte `readable` store.
- The distinction between the non-reactive client and the reactive
  request state a consumer keeps (loading, data, error).
- Consuming the store in a Svelte 5 component with runes.

## How to run

    npm install
    npm run dev

The `dev` script starts the Vite dev server with the Svelte plugin.

The repository does not commit `node_modules/`, lockfiles, or build
output. Every dependency is installed locally by the reader. See
`.gitignore` in this directory.

## The pattern

The store is:

    import { readable } from "svelte/store";
    import { createApiPilotClient } from "../../../../dist/apipilot-client.esm.js";

    export function createApiPilotStore(baseUrl) {
        return readable(createApiPilotClient({ baseUrl }));
    }

A component subscribes with the `$` syntax:

    const client = createApiPilotStore("/api");
    const res = await $client.get("/items");

## Non-reactive client, reactive request state

The ApiPilot client is a plain object. It is not reactive. Wrapping it
in a readable store makes the client reference available to a
component through Svelte's subscription syntax. The store does not
make the client itself reactive.

Reactive request state - a loading flag, a data value, an error value
- belongs to the consumer. In the example, `result` is a Svelte 5
`$state` variable that the consumer updates after each request.

If a consumer wants the client reference itself to be reactive (for
example, to swap the client at runtime), a writable store is the right
shape. This example uses a readable store because the client is
created once per `baseUrl`.

## What this example is not

It is a small integration project, not a production sample. It is not
part of the published client package. It is not included in the
client tarball. It is not a full application.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

