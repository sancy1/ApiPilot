<!--
filepath: javascript/ApiPilot.Client/examples/vanilla-js/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The vanilla JS example: a plain ESM import of the built client.
-->

# Vanilla JS example

A plain browser example that imports the ApiPilot client from the
built ESM artifact using a relative path. No package manager is
involved. No build step. No framework.

## What this demonstrates

- Importing the built client from a relative path.
- Constructing a client with a single required config property
  (`baseUrl`).
- A GET request and a POST request.
- The response shape: `status`, `success`, `data`.
- Error handling with the typed error classes.

## How to run

The example is two files:

- `index.html` - the markup.
- `app.js` - the JavaScript.

The browser loads `app.js` as an ES module via the
`<script type="module" src="./app.js">` tag. The client is imported
from `../../dist/apipilot-client.esm.js`.

Serve the directory with any static file server. For example:

    python -m http.server 8000

Then open <http://localhost:8000/examples/vanilla-js/>.

The client calls `/api/items`, which must be a running ApiPilot
server. If no server is running, the requests fail with an
`ApiPilotHttpError` and the example displays the error.

## What the reader learns

Nothing framework-specific. This is the baseline against which the
other six examples are understood. React, Vue, Angular, Svelte,
Next.js, and Blazor each wrap the same client in a way specific to
their framework. The client itself is the same in every case.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

