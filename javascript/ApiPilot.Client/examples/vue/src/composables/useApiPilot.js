// filepath: javascript/ApiPilot.Client/examples/vue/src/composables/useApiPilot.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A Vue composable that exposes the ApiPilot client via a browser-safe factory
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : vue, ../../../../dist/apipilot-client.esm.js
//   Used by    : examples/vue/src/main.js
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------

import { ref } from 'vue';
import { createApiPilotClient } from '../../../../dist/apipilot-client.esm.js';

// The composable exposes a browser-safe factory (`ensure`) rather than
// constructing the client only in onMounted. A consumer that needs the
// client during setup() calls ensure() directly. A consumer that needs
// it after mount reads the ref. Under SSR, ensure() returns null and
// the consumer handles the null case.
export function useApiPilot(baseUrl) {
    const client = ref(null);

    const ensure = () => {
        if (typeof window === 'undefined') {
            return null;
        }
        if (client.value === null) {
            client.value = createApiPilotClient({ baseUrl });
        }
        return client.value;
    };

    return { client, ensure };
}

