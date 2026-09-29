// filepath: javascript/ApiPilot.Client/tests/errors.test.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Tests for the error hierarchy and the ApiPilotWireError factory
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test file)
//   Depends on : node:test, node:assert/strict, node:url, dist/apipilot-client.esm.js
//   Used by    : the package "test" script in package.json
//   See also   : src/errors.js, src/types.d.ts, tests/skeleton.test.js
// -----------------------------------------------------------------------------

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const clientRoot = join(here, '..');
const esmPath = join(clientRoot, 'dist', 'apipilot-client.esm.js');
const esmUrl = pathToFileURL(esmPath).href;

const mod = await import(esmUrl);

test('the five error classes are exported as constructors', () => {
    assert.equal(typeof mod.ApiPilotClientError, 'function');
    assert.equal(typeof mod.ApiPilotConfigurationError, 'function');
    assert.equal(typeof mod.ApiPilotHttpError, 'function');
    assert.equal(typeof mod.ApiPilotEnvelopeError, 'function');
    assert.equal(typeof mod.ApiPilotProtocolError, 'function');
});

test('ApiPilotClientError is an Error subclass', () => {
    const e = new mod.ApiPilotClientError('x');
    assert.ok(e instanceof Error);
    assert.ok(e instanceof mod.ApiPilotClientError);
    assert.equal(e.name, 'ApiPilotClientError');
});

test('each subclass sets its own name and is a subclass of the base', () => {
    const e1 = new mod.ApiPilotConfigurationError('x');
    assert.equal(e1.name, 'ApiPilotConfigurationError');
    assert.ok(e1 instanceof mod.ApiPilotClientError);

    const e2 = new mod.ApiPilotHttpError('x');
    assert.equal(e2.name, 'ApiPilotHttpError');
    assert.ok(e2 instanceof mod.ApiPilotClientError);

    const e3 = new mod.ApiPilotEnvelopeError('x');
    assert.equal(e3.name, 'ApiPilotEnvelopeError');
    assert.ok(e3 instanceof mod.ApiPilotClientError);

    const e4 = new mod.ApiPilotProtocolError('x');
    assert.equal(e4.name, 'ApiPilotProtocolError');
    assert.ok(e4 instanceof mod.ApiPilotClientError);
});

test('ApiPilotWireError normalizes a non-object source to defaults', () => {
    const w1 = mod.ApiPilotWireError(null);
    assert.equal(w1.code, '');
    assert.equal(w1.message, '');
    assert.equal(w1.fields, null);

    const w2 = mod.ApiPilotWireError(undefined);
    assert.equal(w2.code, '');

    const w3 = mod.ApiPilotWireError('a string');
    assert.equal(w3.code, '');
});

test('ApiPilotWireError normalizes typed-incorrect fields', () => {
    const w = mod.ApiPilotWireError({ code: 123, message: null, fields: 'nope' });
    assert.equal(w.code, '');
    assert.equal(w.message, '');
    assert.equal(w.fields, null);
});

test('ApiPilotWireError preserves unknown fields via spread', () => {
    const source = { code: 'X', message: 'm', fields: { a: ['b'] }, custom: 42, extra: { deep: true } };
    const w = mod.ApiPilotWireError(source);
    assert.equal(w.code, 'X');
    assert.equal(w.message, 'm');
    assert.deepEqual(w.fields, { a: ['b'] });
    assert.equal(w.custom, 42);
    assert.deepEqual(w.extra, { deep: true });
});

test('ApiPilotWireError returns a fresh object each call', () => {
    const w1 = mod.ApiPilotWireError({ code: 'X' });
    const w2 = mod.ApiPilotWireError({ code: 'X' });
    assert.notEqual(w1, w2);
    assert.equal(w1.code, w2.code);
});

test('ApiPilotEnvelopeError and ApiPilotProtocolError carry their extended fields', () => {
    const wire = mod.ApiPilotWireError({ code: 'VALIDATION_ERROR', message: 'bad' });
    const env = new mod.ApiPilotEnvelopeError('bad', { status: 400, wireError: wire });
    assert.equal(env.status, 400);
    assert.equal(env.wireError.code, 'VALIDATION_ERROR');

    const proto = new mod.ApiPilotProtocolError('bad', { status: 500, rawBody: '{}', expected: 'success' });
    assert.equal(proto.status, 500);
    assert.equal(proto.rawBody, '{}');
    assert.equal(proto.expected, 'success');
});

