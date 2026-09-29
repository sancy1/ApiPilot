// filepath: javascript/ApiPilot.Client/tests/csrf.test.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Tests for the CSRF token store: caching, coalescing, concurrency, classification
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test file)
//   Depends on : node:test, node:assert/strict, node:url, dist/apipilot-client.esm.js
//   Used by    : the package "test" script in package.json
//   See also   : src/csrf.js, src/client.js, tests/client.test.js
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
const TOKEN_JSON = '{"token":"csrf-test-token-abc"}';

function responseDouble(status, text) {
    return {
        status: status,
        ok: status >= 200 && status < 300,
        text: async function () { return text; }
    };
}

function deferred() {
    let resolveFn, rejectFn;
    const promise = new Promise(function (resolve, reject) {
        resolveFn = resolve;
        rejectFn = reject;
    });
    return { promise, resolve: resolveFn, reject: rejectFn };
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

test('client.csrf.get() fetches the token and caches it', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, TOKEN_JSON));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const token1 = await client.csrf.get();
    assert.equal(token1, 'csrf-test-token-abc');
    assert.equal(fetchImpl.calls.length, 1);
});

test('a second client.csrf.get() returns the cached token without a second fetch', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, TOKEN_JSON));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const t1 = await client.csrf.get();
    const t2 = await client.csrf.get();
    assert.equal(t1, t2);
    assert.equal(fetchImpl.calls.length, 1);
});

test('concurrent client.csrf.get() calls coalesce into one bootstrap fetch', async () => {
    const d = deferred();
    const fetchImpl = countingFetch(function () { return d.promise; });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const p1 = client.csrf.get();
    const p2 = client.csrf.get();
    const p3 = client.csrf.get();
    d.resolve(responseDouble(200, TOKEN_JSON));
    const [t1, t2, t3] = await Promise.all([p1, p2, p3]);
    assert.equal(t1, t2);
    assert.equal(t2, t3);
    assert.equal(fetchImpl.calls.length, 1);
});

test('client.csrf.refresh() clears and re-fetches', async () => {
    let counter = 0;
    const fetchImpl = countingFetch(function () {
        counter += 1;
        return Promise.resolve(responseDouble(200, '{"token":"tok-' + counter + '"}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const t1 = await client.csrf.get();
    assert.equal(t1, 'tok-1');
    const t2 = await client.csrf.refresh();
    assert.equal(t2, 'tok-2');
    assert.equal(fetchImpl.calls.length, 2);
});

test('client.csrf.clear() empties the store; the next get() re-fetches', async () => {
    let counter = 0;
    const fetchImpl = countingFetch(function () {
        counter += 1;
        return Promise.resolve(responseDouble(200, '{"token":"tok-' + counter + '"}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const t1 = await client.csrf.get();
    client.csrf.clear();
    const t2 = await client.csrf.get();
    assert.equal(t1, 'tok-1');
    assert.equal(t2, 'tok-2');
    assert.equal(fetchImpl.calls.length, 2);
});

test('refresh() during an in-flight get() discards the in-flight result (generation guard)', async () => {
    let counter = 0;
    const deferreds = [];
    const fetchImpl = countingFetch(function () {
        counter += 1;
        const d = deferred();
        deferreds.push(d);
        return d.promise;
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const p1 = client.csrf.get();
    const pRefresh = client.csrf.refresh();
    deferreds[0].resolve(responseDouble(200, '{"token":"stale"}'));
    deferreds[1].resolve(responseDouble(200, '{"token":"fresh"}'));
    const t1 = await p1;
    const t2 = await pRefresh;
    assert.equal(t1, 'stale');
    assert.equal(t2, 'fresh');
    const t3 = await client.csrf.get();
    assert.equal(t3, 'fresh');
});

test('clear() during an in-flight get() discards the in-flight result (generation guard)', async () => {
    let counter = 0;
    const deferreds = [];
    const fetchImpl = countingFetch(function () {
        counter += 1;
        const d = deferred();
        deferreds.push(d);
        return d.promise;
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const p1 = client.csrf.get();
    client.csrf.clear();
    deferreds[0].resolve(responseDouble(200, '{"token":"late"}'));
    await p1;
    const p2 = client.csrf.get();
    deferreds[1].resolve(responseDouble(200, '{"token":"after-clear"}'));
    const t = await p2;
    assert.equal(t, 'after-clear');
});

test('a non-2xx response with a valid success:false envelope throws ApiPilotEnvelopeError', async () => {
    const body = '{"success":false,"error":{"code":"CSRF_TOKEN_INVALID","message":"bad"}}';
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(403, body));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.csrf.get(); } catch (e) { threw = e; }
    assert.ok(threw, 'expected a throw');
    assert.ok(threw instanceof mod.ApiPilotEnvelopeError, 'expected EnvelopeError, got ' + threw.name);
    assert.equal(threw.wireError.code, 'CSRF_TOKEN_INVALID');
    assert.equal(threw.status, 403);
});

test('a 200 response with success:true but no token throws ApiPilotProtocolError', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"success":true}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.csrf.get(); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotProtocolError);
});

test('a non-JSON response throws ApiPilotProtocolError', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, 'not json'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.csrf.get(); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotProtocolError);
});

test('the documented bootstrap shape is accepted (no success field)', async () => {
    // Per SPEC.md "CSRF bootstrap response" and docs/csrf.md, the bootstrap
    // endpoint returns a bare { "token": "..." } object, the only successful
    // ApiPilot response that does not use the standard envelope.
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"token":"documented-shape-token"}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    const token = await client.csrf.get();
    assert.equal(token, 'documented-shape-token');
});

test('a JSON body with neither a boolean success nor a token string throws ApiPilotProtocolError', async () => {
    const fetchImpl = countingFetch(function () {
        return Promise.resolve(responseDouble(200, '{"foo":"bar"}'));
    });
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.csrf.get(); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotProtocolError);
});

test('a network failure (fetch throws) throws ApiPilotHttpError with cause', async () => {
    const underlying = new Error('dns');
    const fetchImpl = function () { return Promise.reject(underlying); };
    const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
    let threw = null;
    try { await client.csrf.get(); } catch (e) { threw = e; }
    assert.ok(threw instanceof mod.ApiPilotHttpError);
    assert.equal(threw.cause, underlying);
});

test('the token is never written to localStorage or sessionStorage', async () => {
    const originalLocal = globalThis.localStorage;
    const originalSession = globalThis.sessionStorage;
    let localTouched = false;
    let sessionTouched = false;
    try {
        Object.defineProperty(globalThis, 'localStorage', {
            configurable: true,
            get: function () { localTouched = true; return {}; }
        });
        Object.defineProperty(globalThis, 'sessionStorage', {
            configurable: true,
            get: function () { sessionTouched = true; return {}; }
        });
    } catch (e) {
        // Some environments cannot define these. Skip the local assertion.
    }
    try {
        const fetchImpl = countingFetch(function () {
            return Promise.resolve(responseDouble(200, TOKEN_JSON));
        });
        const client = mod.createApiPilotClient({ baseUrl: BASE, fetch: fetchImpl });
        await client.csrf.get();
        assert.equal(localTouched, false, 'localStorage was accessed');
        assert.equal(sessionTouched, false, 'sessionStorage was accessed');
    } finally {
        try {
            if (originalLocal === undefined) { delete globalThis.localStorage; } else { Object.defineProperty(globalThis, 'localStorage', { configurable: true, value: originalLocal }); }
            if (originalSession === undefined) { delete globalThis.sessionStorage; } else { Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, value: originalSession }); }
        } catch (e) { /* ignore */ }
    }
});

