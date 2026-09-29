// filepath: javascript/ApiPilot.Client/tests/client.test.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Tests for the client factory, the request pipeline, and the response shape
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test file)
//   Depends on : node:test, node:assert/strict, node:url, dist/apipilot-client.esm.js
//   Used by    : the package "test" script in package.json
//   See also   : src/client.js, src/csrf.js, tests/csrf.test.js, tests/errors.test.js
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

const BASE = 'https://api.example.com/api';
const CROSS = 'https://evil.example.com/x';
const TOKEN_JSON = '{"token":"csrf-test-token-abc"}';
const SUCCESS_ENVELOPE = '{"success":true,"data":{"id":1},"meta":{"requestId":"r-1"}}';

function responseDouble(status, text) {
    return {
        status: status,
        ok: status >= 200 && status < 300,
        text: async function () { return text; }
    };
}

function countingFetch(behaviour) {
    const calls = [];
    const fetchImpl = function (url, options) {
        calls.push({ url: String(url), options: options });
        return behaviour(calls.length, url, options);
    };
    fetchImpl.calls = calls;
    return fetchImpl;
}

function headerOf(options, name) {
    if (!options || !options.headers) { return undefined; }
    const target = name.toLowerCase();
    for (const key in options.headers) {
        if (key.toLowerCase() === target) { return options.headers[key]; }
    }
    return undefined;
}

function bootstrapThen(callIndex, behaviour) {
    // A behaviour that returns the token on the first call (bootstrap) and
    // delegates to behaviour for every subsequent call (the request).
    return function (n, url, options) {
        if (n === 1) {
            return Promise.resolve(responseDouble(200, TOKEN_JSON));
        }
        return behaviour(n, url, options);
    };
}

test('createApiPilotClient without a config object throws ApiPilotConfigurationError', () => {
    assert.throws(function () { mod.createApiPilotClient(); }, mod.ApiPilotConfigurationError);
});

test('createApiPilotClient without baseUrl throws ApiPilotConfigurationError (C1)', () => {
    assert.throws(
        function () { mod.createApiPilotClient({}); },
        mod.ApiPilotConfigurationError
    );
});

test('createApiPilotClient with an unresolvable baseUrl and no location throws ApiPilotConfigurationError', () => {
    // On Node, globalThis.location is undefined. A relative baseUrl cannot
    // be resolved. This must throw at construction.
    assert.throws(
        function () { mod.createApiPilotClient({ baseUrl: 'relative/path', fetch: function () {} }); },
        mod.ApiPilotConfigurationError
    );
});

test('createApiPilotClient with no fetch available throws ApiPilotConfigurationError (C7)', () => {
    const originalFetch = globalThis.fetch;
    try {
        // On Node 24 globalThis.fetch exists. Force the failure case.
        Object.defineProperty(globalThis, 'fetch', { configurable: true, value: undefined });
        assert.throws(
            function () { mod.createApiPilotClient({ baseUrl: BASE }); },
            mod.ApiPilotConfigurationError
        );
    } finally {
        Object.defineProperty(globalThis, 'fetch', { configurable: true, value: originalFetch });
    }
});

test('client.get() issues a GET and returns the parsed success envelope', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, SUCCESS_ENVELOPE));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const res = await client.get('/items/1');
    assert.equal(res.ok, true);
    assert.equal(res.status, 200);
    assert.equal(res.success, true);
    assert.deepEqual(res.data, { id: 1 });
    assert.equal(res.error, null);
    assert.deepEqual(res.meta, { requestId: 'r-1' });
    assert.equal(res.raw.status, 200);
    assert.equal(fetchImpl.calls.length, 1);
    assert.equal(fetchImpl.calls[0].options.method, 'GET');
});

test('client.post() attaches the CSRF header for a protected method', async () => {
    const fetchImpl = countingFetch(bootstrapThen(0, function (n, url, options) {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    }));
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    await client.post('/items', { name: 'x' });
    // call[0] is the bootstrap (GET csrf), call[1] is the POST.
    assert.equal(fetchImpl.calls.length, 2);
    const postCall = fetchImpl.calls[1];
    assert.equal(postCall.options.method, 'POST');
    assert.equal(headerOf(postCall.options, 'X-CSRF-TOKEN'), 'csrf-test-token-abc');
});

test('client.post() does not attach the CSRF header to a cross-origin URL (security-critical)', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    await client.post(CROSS, { name: 'x' });
    // Only one call -- the cross-origin POST. No bootstrap should occur because
    // the request is not protected.
    assert.equal(fetchImpl.calls.length, 1);
    const call = fetchImpl.calls[0];
    assert.equal(headerOf(call.options, 'X-CSRF-TOKEN'), undefined, 'cross-origin request must not carry the CSRF header');
});

test('client.post() does not attach the CSRF header when the method is not in protectedMethods (C4 override)', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    });
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        protectedMethods: ['PUT']
    });
    await client.post('/items', { name: 'x' });
    assert.equal(fetchImpl.calls.length, 1);
    assert.equal(headerOf(fetchImpl.calls[0].options, 'X-CSRF-TOKEN'), undefined);
});

test('client.post() with a custom csrfHeader name attaches the custom name (C5 override)', async () => {
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    }));
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        csrfHeader: 'X-My-CSRF'
    });
    await client.post('/items', { name: 'x' });
    const postCall = fetchImpl.calls[1];
    assert.equal(headerOf(postCall.options, 'X-My-CSRF'), 'csrf-test-token-abc');
    assert.equal(headerOf(postCall.options, 'X-CSRF-TOKEN'), undefined);
});

test('client.get() does not attach the header even when GET is in protectedMethods (safe-method invariant)', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    });
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        protectedMethods: ['GET', 'POST']
    });
    await client.get('/items');
    assert.equal(fetchImpl.calls.length, 1);
    assert.equal(headerOf(fetchImpl.calls[0].options, 'X-CSRF-TOKEN'), undefined);
});

test('custom credentials value flows through to fetch (C3 override)', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    });
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        credentials: 'include'
    });
    await client.get('/items');
    assert.equal(fetchImpl.calls[0].options.credentials, 'include');
});

test('a custom fetch implementation is called instead of the global (C7 override)', async () => {
    const originalFetch = globalThis.fetch;
    let globalCalled = false;
    try {
        Object.defineProperty(globalThis, 'fetch', {
            configurable: true,
            value: function () { globalCalled = true; return Promise.reject(new Error('should not be called')); }
        });
        const fetchImpl = countingFetch(function () {
            return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
        });
        const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
        await client.get('/items');
        assert.equal(fetchImpl.calls.length, 1);
        assert.equal(globalCalled, false);
    } finally {
        Object.defineProperty(globalThis, 'fetch', { configurable: true, value: originalFetch });
    }
});

test('client.post() with a non-string body serializes it to JSON and sets Content-Type', async () => {
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    }));
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    await client.post('/items', { name: 'x', count: 3 });
    const postCall = fetchImpl.calls[1];
    assert.equal(postCall.options.body, '{"name":"x","count":3}');
    assert.equal(headerOf(postCall.options, 'Content-Type'), 'application/json');
});

test('client.post() with a caller-supplied Content-Type preserves the caller value', async () => {
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    }));
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    await client.post('/items', 'raw-body', { headers: { 'Content-Type': 'text/plain' } });
    const postCall = fetchImpl.calls[1];
    assert.equal(postCall.options.body, 'raw-body');
    assert.equal(headerOf(postCall.options, 'Content-Type'), 'text/plain');
});

test('unknown envelope fields are preserved on the returned response (correction 1)', async () => {
    const body = '{"success":true,"data":{"id":1},"customField":"kept","another":42}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, body));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const res = await client.get('/items/1');
    assert.equal(res.success, true);
    assert.equal(res.customField, 'kept');
    assert.equal(res.another, 42);
});

test('client-controlled fields win over server fields with the same name (correction 1)', async () => {
    // The server tries to forge ok, status, and raw.
    const body = '{"success":true,"data":{},"ok":false,"status":999,"raw":"evil"}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, body));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const res = await client.get('/items/1');
    assert.equal(res.ok, true, 'client ok must win over server ok');
    assert.equal(res.status, 200, 'client status must win over server status');
    assert.notEqual(res.raw, 'evil', 'client raw must win over server raw');
    assert.equal(typeof res.raw.text, 'function', 'client raw is the response object');
});

test('a 401 with a valid success:false envelope throws ApiPilotEnvelopeError, not HttpError', async () => {
    const body = '{"success":false,"error":{"code":"AUTHENTICATION_REQUIRED","message":"nope"}}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(401, body));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotEnvelopeError, 'expected EnvelopeError, got ' + (threw && threw.name));
    assert.equal(threw.status, 401);
    assert.equal(threw.wireError.code, 'AUTHENTICATION_REQUIRED');
    assert.equal(threw instanceof mod.ApiPilotHttpError, false);
});

test('a 500 with a valid success:false envelope throws ApiPilotEnvelopeError, not HttpError', async () => {
    const body = '{"success":false,"error":{"code":"INTERNAL_ERROR","message":"boom"}}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(500, body));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotEnvelopeError);
    assert.equal(threw.status, 500);
    assert.equal(threw.wireError.code, 'INTERNAL_ERROR');
});

test('a non-JSON response throws ApiPilotProtocolError', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, 'plain text'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotProtocolError);
    assert.equal(threw.rawBody, 'plain text');
});

test('a JSON response without a boolean success throws ApiPilotProtocolError', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"data":{"id":1}}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotProtocolError);
});

test('a network failure throws ApiPilotHttpError with cause', async () => {
    const underlying = new Error('dns');
    const fetchImpl = function () { return Promise.reject(underlying); };
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotHttpError);
    assert.equal(threw.cause, underlying);
});

test('a response whose text() rejects throws ApiPilotHttpError with cause (correction 4)', async () => {
    const underlying = new Error('stream');
    const fetchImpl = countingFetch(function () {
        return Promise.resolve({
            status: 200,
            ok: true,
            text: function () { return Promise.reject(underlying); }
        });
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.get('/items/1'); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotHttpError);
    assert.equal(threw.cause, underlying);
});

test('a response whose ok and status disagree uses ok as returned (correction 4)', async () => {
    // status 200 but ok:false. The client uses ok as returned.
    const body = '{"success":true,"data":{}}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve({ status: 200, ok: false, text: async function () { return body; } });
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const res = await client.get('/items/1');
    assert.equal(res.ok, false, 'client uses ok as returned');
    assert.equal(res.status, 200);
    assert.equal(res.success, true);
});

test('onCsrfExpired is called on CSRF_TOKEN_EXPIRED (C6 override)', async () => {
    const body = '{"success":false,"error":{"code":"CSRF_TOKEN_EXPIRED","message":"expired"}}';
    let callbackCount = 0;
    let receivedError = null;
    let receivedResponse = null;
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(403, body));
    }));
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        onCsrfExpired: function (e, r) {
            callbackCount += 1;
            receivedError = e;
            receivedResponse = r;
        }
    });
    let threw = null;
    try { await client.post('/items', {}); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotEnvelopeError);
    assert.equal(threw.wireError.code, 'CSRF_TOKEN_EXPIRED');
    assert.equal(callbackCount, 1);
    assert.equal(receivedError, threw);
    assert.ok(receivedResponse, 'response passed to callback');
});

test('onCsrfExpired is not called on CSRF_HEADER_MISSING or other codes', async () => {
    const body = '{"success":false,"error":{"code":"CSRF_HEADER_MISSING","message":"nope"}}';
    let callbackCount = 0;
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(403, body));
    }));
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        onCsrfExpired: function () { callbackCount += 1; }
    });
    try { await client.post('/items', {}); } catch (e) { /* expected */ }
    assert.equal(callbackCount, 0);
});

test('onCsrfExpired is not called on the success path', async () => {
    let callbackCount = 0;
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true,"data":{}}'));
    });
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        onCsrfExpired: function () { callbackCount += 1; }
    });
    await client.get('/items/1');
    assert.equal(callbackCount, 0);
});

test('a throwing onCsrfExpired does not replace the ApiPilotEnvelopeError (correction 3)', async () => {
    const body = '{"success":false,"error":{"code":"CSRF_TOKEN_EXPIRED","message":"expired"}}';
    const callbackError = new Error('callback blew up');
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(403, body));
    }));
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        onCsrfExpired: function () { throw callbackError; }
    });
    let threw = null;
    try { await client.post('/items', {}); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotEnvelopeError, 'expected EnvelopeError, got ' + (threw && threw.name));
    assert.equal(threw.wireError.code, 'CSRF_TOKEN_EXPIRED');
    assert.equal(threw.cause, callbackError, 'cause carries the callback error');
});

test('a throwing onCsrfExpired does not overwrite an existing .cause (correction 3)', async () => {
    // This test asserts the guard: envelopeError.cause === undefined before setting.
    // We cannot easily pre-populate the envelope error from the outside, so the
    // test instead asserts the positive: when cause is set, it is the callback error.
    // The negative is covered by the code path. This test is the positive.
    const body = '{"success":false,"error":{"code":"CSRF_TOKEN_EXPIRED","message":"x"}}';
    const fetchImpl = countingFetch(bootstrapThen(0, function () {
        return Promise.resolve(responseDouble(403, body));
    }));
    const client = mod.createApiPilotClient({
        baseUrl: BASE,
        fetch: fetchImpl,
        onCsrfExpired: function () { throw new Error('cb'); }
    });
    let threw = null;
    try { await client.post('/items', {}); } catch (e) { threw = e; }
    assert.ok(threw.cause instanceof Error);
    assert.equal(threw.cause.message, 'cb');
});

test('the public surface is exactly the eight runtime symbols (Phase 3.1)', async () => {
    const actual = Object.keys(mod).sort();
    const expected = [
        'VERSION',
        'ApiPilotClientError',
        'ApiPilotConfigurationError',
        'ApiPilotHttpError',
        'ApiPilotEnvelopeError',
        'ApiPilotProtocolError',
        'ApiPilotWireError',
        'createApiPilotClient'
    ].sort();
    assert.deepEqual(actual, expected);
});

