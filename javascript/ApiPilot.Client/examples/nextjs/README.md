<!--
filepath: javascript/ApiPilot.Client/examples/nextjs/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The Next.js example: the client in a Client Component, with the App Router boundary documented.
-->

# Next.js example

A Next.js App Router integration example. The client is framework-
agnostic; this example shows where in the App Router it belongs.

## What this demonstrates

- The client in a Next.js Client Component.
- The `"use client"` directive as the client boundary.
- Why the client cannot live in a Server Component.
- The `app/layout.jsx` / `app/page.jsx` / `app/client.jsx` structure.

## How to run

    npm install
    npm run dev

The `dev` script starts the Next.js dev server.

The repository does not commit `node_modules/`, lockfiles, or build
output. Every dependency is installed locally by the reader. See
`.gitignore` in this directory.

## The App Router boundary

In the App Router, `app/page.jsx` is a Server Component by default.
It cannot import the ApiPilot client, because the client is a
browser-only object. It renders `app/client.jsx`, a Client Component
marked with the `"use client"` directive at the top of the file,
before any imports.

The Client Component creates the client in a `useMemo` on the config
and uses it. The client is not shared across Server Components.

## What this example is not

It is a small integration project, not a production sample. It is not
part of the published client package. It is not included in the
client tarball. It is not a full application.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

