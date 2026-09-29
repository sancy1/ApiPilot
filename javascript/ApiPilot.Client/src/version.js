// filepath: javascript/ApiPilot.Client/src/version.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Exposes the client package version on the internal namespace
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module fragment)
//   Depends on : globalThis.__ApiPilotClient
//   Used by    : the built ESM entry and the built IIFE entry
//   See also   : package.json, scripts/build.js, scripts/manifest.json
// -----------------------------------------------------------------------------

(function (ns) {
    'use strict';
    ns.VERSION = '1.0.0';
})(globalThis.__ApiPilotClient = globalThis.__ApiPilotClient || {});

