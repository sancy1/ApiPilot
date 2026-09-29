// filepath: javascript/ApiPilot.Client/tests/skeleton.test.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  The skeleton test: load, internal namespace, public surface isolation
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test file)
//   Depends on : node:test, node:assert/strict, node:url, dist/apipilot-client.esm.js
//   Used by    : the package "test" script in package.json
//   See also   : scripts/build.js, src/index.js, package.json, tests/client.test.js
// -----------------------------------------------------------------------------

// The test runs against the built ESM artifact, not the source. The build
// must run before the test. The package "test" script assumes the build has
// already run; run "node ./scripts/build.js" first.
//
// On Windows the dynamic import of a filesystem path fails with
// ERR_UNSUPPORTED_ESM_URL_SCHEME because the drive letter is parsed as a
// URL scheme. The path is converted to a file:// URL via pathToFileURL
// before it is passed to import().
//
// The first two tests are frozen Phase 3.0 assertions: the seven Phase 3.0
// symbols exist on the module, and the internal namespace is populated.
// The third test is a phase-bound assertion of the current runtime surface.
// Phase 3.1 added createApiPilotClient as the eighth symbol. See A-179.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const clientRoot = join(here, '..');
const esmPath = join(clientRoot, 'dist', 'apipilot-client.esm.js');
const esmUrl = pathToFileURL(esmPath).href;

const PHASE_3_0_SYMBOLS = [
    'VERSION',
    'ApiPilotClientError',
    'ApiPilotConfigurationError',
    'ApiPilotHttpError',
    'ApiPilotEnvelopeError',
    'ApiPilotProtocolError',
    'ApiPilotWireError'
];

const PHASE_3_1_SURFACE = PHASE_3_0_SYMBOLS.concat(['createApiPilotClient']);

test('the built ESM entry imports and exports the seven Phase 3.0 symbols', async () => {
    const mod = await import(esmUrl);
    for (const name of PHASE_3_0_SYMBOLS) {
        assert.ok(name in mod, `expected export not found: ${name}`);
    }
    assert.equal(mod.VERSION, '1.0.0');
});

test('the internal namespace is populated after the import', async () => {
    // Ensure the module is loaded so the fragments have run.
    await import(esmUrl);
    const ns = globalThis.__ApiPilotClient;
    assert.ok(ns && typeof ns === 'object', '__ApiPilotClient is not populated');
    assert.equal(ns.VERSION, '1.0.0');
    assert.equal(typeof ns.ApiPilotClientError, 'function');
    assert.equal(typeof ns.ApiPilotConfigurationError, 'function');
    assert.equal(typeof ns.ApiPilotHttpError, 'function');
    assert.equal(typeof ns.ApiPilotEnvelopeError, 'function');
    assert.equal(typeof ns.ApiPilotProtocolError, 'function');
    assert.equal(typeof ns.ApiPilotWireError, 'function');
    assert.equal(typeof ns.createApiPilotClient, 'function');
});

test('the public surface is exactly the eight documented symbols', async () => {
    const mod = await import(esmUrl);
    const actual = Object.keys(mod).sort();
    const expected = PHASE_3_1_SURFACE.slice().sort();
    assert.deepEqual(actual, expected);
});

