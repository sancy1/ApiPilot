// filepath: javascript/ApiPilot.Client/src/csrf.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  The CSRF token store: in-memory only, per-client, with concurrent-safe bootstrap
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module fragment)
//   Depends on : globalThis.__ApiPilotClient (the error classes)
//   Used by    : src/client.js
//   See also   : src/client.js, src/types.d.ts, docs/csrf.md, SPEC.md
// -----------------------------------------------------------------------------

(function (ns) {
    'use strict';

    // Classify a response from the bootstrap fetch. Uses the same rules as
    // the request path in client.js, with one documented exception: the
    // bootstrap endpoint returns a bare { "token": "..." } object, the only
    // successful ApiPilot response that does not use the standard envelope.
    // See SPEC.md "CSRF bootstrap response" and docs/csrf.md.
    //
    // The response is classified by its body, not by response.ok.
    //
    // Inputs: a response-like object with { status, ok, text() }.
    // Returns: { success: true, token }.
    // Throws:  ApiPilotHttpError, ApiPilotProtocolError, ApiPilotEnvelopeError.
    async function classifyBootstrapResponse(response) {
        var text;
        try {
            text = await response.text();
        } catch (readError) {
            throw new ns.ApiPilotHttpError(
                'Failed to read the CSRF bootstrap response body',
                { status: response.status, cause: readError }
            );
        }

        var envelope;
        try {
            envelope = JSON.parse(text);
        } catch (parseError) {
            throw new ns.ApiPilotProtocolError(
                'CSRF bootstrap response body is not JSON',
                { status: response.status, rawBody: text, expected: 'a JSON object with a non-empty string token property', cause: parseError }
            );
        }

        if (!envelope || typeof envelope !== 'object') {
            throw new ns.ApiPilotProtocolError(
                'CSRF bootstrap response is not a JSON object',
                { status: response.status, rawBody: text, expected: 'a JSON object with a non-empty string token property' }
            );
        }

        // Case 1: valid ApiPilot envelope with success: false. This is an
        // envelope error regardless of HTTP status (the mandatory correction
        // from Phase 3.1 -- classification by body, not by response.ok).
        if (typeof envelope.success === 'boolean' && envelope.success === false) {
            var wireError = ns.ApiPilotWireError(envelope.error);
            throw new ns.ApiPilotEnvelopeError(
                wireError.message || 'CSRF bootstrap request failed',
                { status: response.status, wireError: wireError }
            );
        }

        // Case 2: valid ApiPilot envelope with success: true. A server that
        // wraps the token in the standard envelope. The token property must
        // still be a non-empty string.
        if (typeof envelope.success === 'boolean' && envelope.success === true) {
            if (typeof envelope.token === 'string' && envelope.token.length > 0) {
                return { success: true, token: envelope.token };
            }
            throw new ns.ApiPilotProtocolError(
                'CSRF bootstrap response did not carry a non-empty token',
                { status: response.status, rawBody: text, expected: 'a JSON object with a non-empty string token property' }
            );
        }

        // Case 3: the documented bootstrap response shape -- no success field,
        // but a non-empty token string. This is the shape SPEC.md mandates.
        if (typeof envelope.token === 'string' && envelope.token.length > 0) {
            return { success: true, token: envelope.token };
        }

        // Case 4: neither a valid ApiPilot envelope nor a bootstrap response.
        throw new ns.ApiPilotProtocolError(
            'CSRF bootstrap response is neither a valid envelope nor a token object',
            { status: response.status, rawBody: text, expected: 'a JSON object with a non-empty string token property' }
        );
    }

    // The CSRF token store. In-memory only, per-client. The token never
    // leaves the closure.
    //
    // Concurrency: a generation counter is incremented by refresh() and
    // clear(). A bootstrap that was issued before the increment cannot
    // repopulate the store. The inflight promise coalesces concurrent get()
    // calls into a single bootstrap fetch.
    function createCsrfStore(config) {
        var fetchImpl = config.fetchImpl;
        var resolvedCsrfPath = config.resolvedCsrfPath;
        var credentials = config.credentials;

        var token = null;
        var inflight = null;
        var generation = 0;

        async function bootstrap() {
            var response;
            try {
                response = await fetchImpl(resolvedCsrfPath, {
                    method: 'GET',
                    credentials: credentials
                });
            } catch (networkError) {
                throw new ns.ApiPilotHttpError(
                    'CSRF bootstrap request failed',
                    { cause: networkError }
                );
            }
            var classified = await classifyBootstrapResponse(response);
            return classified.token;
        }

        async function get() {
            if (token !== null) {
                return token;
            }
            if (inflight !== null) {
                return inflight;
            }

            var capturedGeneration = generation;
            var promise = bootstrap().then(function (resolvedToken) {
                if (capturedGeneration === generation) {
                    token = resolvedToken;
                }
                if (inflight === promise) {
                    inflight = null;
                }
                return resolvedToken;
            }).catch(function (bootstrapError) {
                if (inflight === promise) {
                    inflight = null;
                }
                throw bootstrapError;
            });
            inflight = promise;
            return promise;
        }

        async function refresh() {
            generation = generation + 1;
            token = null;
            inflight = null;
            return get();
        }

        function clear() {
            generation = generation + 1;
            token = null;
            inflight = null;
        }

        return { get: get, refresh: refresh, clear: clear };
    }

    ns.createCsrfStore = createCsrfStore;
})(globalThis.__ApiPilotClient = globalThis.__ApiPilotClient || {});

