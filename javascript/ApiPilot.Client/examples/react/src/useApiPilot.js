// filepath: javascript/ApiPilot.Client/examples/react/src/useApiPilot.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A React hook that memoizes the ApiPilot client on baseUrl
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : react, ../../../dist/apipilot-client.esm.js
//   Used by    : examples/react/src/main.jsx
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------

import { useMemo } from 'react';
import { createApiPilotClient } from '../../../dist/apipilot-client.esm.js';

// The hook memoizes the client on baseUrl, a primitive string. It does
// NOT memoize on the whole config object, because an object literal has
// a new identity on every render and would recreate the client on every
// render, resetting the CSRF token store each time.
export function useApiPilot(baseUrl) {
    return useMemo(
        () => createApiPilotClient({ baseUrl }),
        [baseUrl]
    );
}

