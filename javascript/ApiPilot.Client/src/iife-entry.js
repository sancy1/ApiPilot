// filepath: javascript/ApiPilot.Client/src/iife-entry.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Publishes the public IIFE global from a whitelisted subset of the internal namespace
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module fragment)
//   Depends on : globalThis.__ApiPilotClient
//   Used by    : scripts/build.js (the IIFE output appends this file)
//   See also   : src/types.d.ts, src/index.js, README.md
// -----------------------------------------------------------------------------

(function () {
    'use strict';
    // The public IIFE global. The whitelist below is the exact public surface.
    // It MUST match the export list in src/index.js. The internal namespace
    // __ApiPilotClient carries implementation details (createCsrfStore) that
    // are not part of the public API. Assigning the namespace wholesale would
    // leak those internal symbols. See A-189.
    if (typeof globalThis !== 'undefined') {
        var ns = globalThis.__ApiPilotClient;
        globalThis.ApiPilot = {
            VERSION: ns.VERSION,
            ApiPilotClientError: ns.ApiPilotClientError,
            ApiPilotConfigurationError: ns.ApiPilotConfigurationError,
            ApiPilotHttpError: ns.ApiPilotHttpError,
            ApiPilotEnvelopeError: ns.ApiPilotEnvelopeError,
            ApiPilotProtocolError: ns.ApiPilotProtocolError,
            ApiPilotWireError: ns.ApiPilotWireError,
            createApiPilotClient: ns.createApiPilotClient
        };
    }
})();

