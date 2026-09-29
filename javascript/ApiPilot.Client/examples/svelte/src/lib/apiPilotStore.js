// filepath: javascript/ApiPilot.Client/examples/svelte/src/lib/apiPilotStore.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A Svelte store that exposes the ApiPilot client
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : svelte/store, ../../../../dist/apipilot-client.esm.js
//   Used by    : examples/svelte/src/App.svelte
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------

import { readable } from 'svelte/store';
import { createApiPilotClient } from '../../../../dist/apipilot-client.esm.js';

// The store exposes the ApiPilot client as a readable value. The client
// itself is NOT reactive. It is a plain object. Reactive request state
// (loading, data, error) is the consumer's responsibility, kept in the
// consumer's own component state.
export function createApiPilotStore(baseUrl) {
    return readable(createApiPilotClient({ baseUrl }));
}

