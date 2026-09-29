// filepath: javascript/ApiPilot.Client/src/index.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  The ESM entry. Declares the public named exports of the ESM output.
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module entry)
//   Depends on : globalThis.__ApiPilotClient (populated by the fragments)
//   Used by    : scripts/build.js (the ESM output appends this file)
//   See also   : scripts/manifest.json, src/types.d.ts, src/client.js, README.md
// -----------------------------------------------------------------------------

// This file is appended to the end of the concatenated fragments in the
// built ESM output. The fragments have already run at this point and
// populated globalThis.__ApiPilotClient. Each named export below reads a
// value from that namespace.
//
// Phase 3.0 exported the version constant and the six error symbols.
// Phase 3.1 adds the client factory (createApiPilotClient). The ESM
// module re-exports only runtime values; the request and response types
// (ApiPilotClient, RequestOptions, ApiPilotResponse, ApiPilotClientConfig)
// are TypeScript interfaces declared in src/types.d.ts and re-exported via
// export type. They have no runtime footprint and do not appear in
// Object.keys(module).

const __ns = globalThis.__ApiPilotClient;

export const VERSION = __ns.VERSION;
export const ApiPilotClientError = __ns.ApiPilotClientError;
export const ApiPilotConfigurationError = __ns.ApiPilotConfigurationError;
export const ApiPilotHttpError = __ns.ApiPilotHttpError;
export const ApiPilotEnvelopeError = __ns.ApiPilotEnvelopeError;
export const ApiPilotProtocolError = __ns.ApiPilotProtocolError;
export const ApiPilotWireError = __ns.ApiPilotWireError;
export const createApiPilotClient = __ns.createApiPilotClient;

