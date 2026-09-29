// filepath: javascript/ApiPilot.Client/src/client.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  The client factory and the request pipeline
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module fragment)
//   Depends on : globalThis.__ApiPilotClient (errors, csrf store)
//   Used by    : src/index.js (the ESM entry re-exports createApiPilotClient)
//   See also   : src/csrf.js, src/errors.js, src/types.d.ts, README.md, SPEC.md
// -----------------------------------------------------------------------------

(function (ns) {
    'use strict';

    // Ensure a URL string ends with a slash so a relative path is appended
    // as a segment rather than replacing the last segment.
    function ensureTrailingSlash(href) {
        return href.charAt(href.length - 1) === '/' ? href : href + '/';
    }

    // Safe methods cannot be protected even when the caller lists them.
    // RFC 9110 defines these as safe; the CSRF middleware on the server
    // does not protect them by default. This is a defense-in-depth
    // invariant on the client side.
    function isSafeMethod(method) {
        return method === 'GET' || method === 'HEAD'
            || method === 'OPTIONS' || method === 'TRACE';
    }

    // Build the full request URL from the client base and the caller path.
    // The URL constructor handles relative and absolute paths uniformly.
    function resolveRequestUrl(path, apiBase) {
        try {
            return new URL(path, apiBase);
        } catch (urlError) {
            throw new ns.ApiPilotConfigurationError(
                'Request URL is not resolvable against the client base: ' + path,
                { cause: urlError }
            );
        }
    }

    // Build the fetch options from the caller options and the client
    // defaults. The caller headers win over the client defaults for
    // Content-Type. The CSRF header is added only by the caller of this
    // function (which already decided the request is protected).
    function buildFetchOptions(method, headers, body, credentials, signal) {
        var options = {
            method: method,
            headers: headers,
            credentials: credentials
        };
        if (body !== undefined && body !== null && method !== 'GET' && method !== 'HEAD') {
            options.body = typeof body === 'string' ? body : JSON.stringify(body);
        }
        if (signal !== undefined) {
            options.signal = signal;
        }
        return options;
    }

    // Parse the response body into a validated ApiPilot envelope. Uses the
    // same rules as the CSRF bootstrap classifier in csrf.js. The response
    // is classified by its body, not by response.ok.
    async function parseEnvelope(response) {
        var text;
        try {
            text = await response.text();
        } catch (readError) {
            throw new ns.ApiPilotHttpError(
                'Failed to read the response body',
                { status: response.status, cause: readError }
            );
        }

        var envelope;
        try {
            envelope = JSON.parse(text);
        } catch (parseError) {
            throw new ns.ApiPilotProtocolError(
                'Response body is not JSON',
                { status: response.status, rawBody: text, expected: 'a JSON object with a boolean success property', cause: parseError }
            );
        }

        if (!envelope || typeof envelope !== 'object' || typeof envelope.success !== 'boolean') {
            throw new ns.ApiPilotProtocolError(
                'Response body is not a valid ApiPilot envelope',
                { status: response.status, rawBody: text, expected: 'a JSON object with a boolean success property' }
            );
        }

        return envelope;
    }

    // Build the ApiPilotResponse object from the envelope and the raw
    // response. Unknown envelope fields are preserved via spread. The
    // client-controlled fields are placed AFTER the spread so they win on
    // name collision: a server that sends { ok: false, status: 999,
    // raw: "evil" } cannot override the client transport state.
    function buildResponse(envelope, response) {
        var normalizedError = envelope.success === false
            ? ns.ApiPilotWireError(envelope.error)
            : null;
        var result = Object.assign({}, envelope, {
            ok: response.ok,
            status: response.status,
            success: envelope.success,
            data: envelope.data !== undefined ? envelope.data : null,
            message: envelope.message !== undefined ? envelope.message : null,
            error: normalizedError,
            meta: envelope.meta !== undefined ? envelope.meta : null,
            raw: response
        });
        return result;
    }

    function createApiPilotClient(config) {
        if (!config || typeof config !== 'object') {
            throw new ns.ApiPilotConfigurationError(
                'createApiPilotClient requires a configuration object'
            );
        }

        // C1 -- base URL. Required, non-empty. Resolved once at construction.
        if (typeof config.baseUrl !== 'string' || config.baseUrl.length === 0) {
            throw new ns.ApiPilotConfigurationError(
                'createApiPilotClient requires a non-empty baseUrl string'
            );
        }
        var hasLocation = typeof globalThis !== 'undefined'
            && globalThis.location
            && typeof globalThis.location.href === 'string';
        var apiBase;
        try {
            apiBase = new URL(config.baseUrl, hasLocation ? globalThis.location.href : undefined);
        } catch (baseUrlError) {
            throw new ns.ApiPilotConfigurationError(
                'baseUrl is not a valid URL and no globalThis.location is available to resolve it against',
                { cause: baseUrlError }
            );
        }

        // C7 -- fetch implementation. Custom wins over the global.
        var fetchImpl = config.fetch;
        if (typeof fetchImpl !== 'function') {
            fetchImpl = (typeof globalThis !== 'undefined') ? globalThis.fetch : undefined;
        }
        if (typeof fetchImpl !== 'function') {
            throw new ns.ApiPilotConfigurationError(
                'No fetch implementation is available; provide one via the fetch config property'
            );
        }

        // C2 -- CSRF bootstrap path. Resolved once at construction.
        // The default is "csrf" resolved against the base URL with a
        // trailing slash, so /api and /api/ both produce /api/csrf.
        var resolvedCsrfPath;
        if (typeof config.csrfPath === 'string' && config.csrfPath.length > 0) {
            try {
                resolvedCsrfPath = new URL(config.csrfPath, apiBase).href;
            } catch (csrfPathError) {
                throw new ns.ApiPilotConfigurationError(
                    'csrfPath is not a resolvable URL: ' + config.csrfPath,
                    { cause: csrfPathError }
                );
            }
        } else {
            var baseWithSlash = new URL(ensureTrailingSlash(apiBase.href));
            resolvedCsrfPath = new URL('csrf', baseWithSlash).href;
        }

        // C3 -- credentials mode.
        var credentials = (typeof config.credentials === 'string' && config.credentials.length > 0)
            ? config.credentials
            : 'same-origin';

        // C4 -- protected methods. Fresh copy per client (not shared).
        var protectedMethods = Array.isArray(config.protectedMethods)
            ? config.protectedMethods.map(function (m) { return String(m).toUpperCase(); })
            : ['POST', 'PUT', 'PATCH', 'DELETE'];

        // C5 -- CSRF header name.
        var csrfHeader = (typeof config.csrfHeader === 'string' && config.csrfHeader.length > 0)
            ? config.csrfHeader
            : 'X-CSRF-TOKEN';

        // C6 -- expired-token callback. Notification only, not an interceptor.
        var onCsrfExpired = (typeof config.onCsrfExpired === 'function')
            ? config.onCsrfExpired
            : null;

        // The CSRF store is per-client. Two clients have independent stores.
        var csrfStore = ns.createCsrfStore({
            fetchImpl: fetchImpl,
            resolvedCsrfPath: resolvedCsrfPath,
            credentials: credentials
        });

        // A request is protected when: the method is in protectedMethods,
        // the method is not a safe method, and the request origin equals
        // the API origin. Any failure of these conditions means the CSRF
        // header is not attached.
        function isProtected(method, requestOrigin) {
            if (requestOrigin !== apiBase.origin) {
                return false;
            }
            if (isSafeMethod(method)) {
                return false;
            }
            return protectedMethods.indexOf(method) !== -1;
        }

        async function request(path, options) {
            options = options || {};
            var method = (typeof options.method === 'string' && options.method.length > 0)
                ? options.method.toUpperCase()
                : 'GET';

            var effectiveUrl = resolveRequestUrl(path, apiBase);
            var requestOrigin = effectiveUrl.origin;

            // Build the header bag. Caller headers win over client defaults.
            var headers = {};
            if (options.headers && typeof options.headers === 'object') {
                for (var hKey in options.headers) {
                    if (Object.prototype.hasOwnProperty.call(options.headers, hKey)) {
                        headers[hKey] = options.headers[hKey];
                    }
                }
            }

            // If the request is protected, ensure a token is present and
            // attach the CSRF header using the configured name.
            if (isProtected(method, requestOrigin)) {
                var token = await csrfStore.get();
                headers[csrfHeader] = token;
            }

            // Default Content-Type when a body is present and the caller
            // has not set one.
            var hasContentType = false;
            for (var cKey in headers) {
                if (Object.prototype.hasOwnProperty.call(headers, cKey)
                    && cKey.toLowerCase() === 'content-type') {
                    hasContentType = true;
                    break;
                }
            }
            var isBodyMethod = method !== 'GET' && method !== 'HEAD';
            if (isBodyMethod && options.body !== undefined && options.body !== null && !hasContentType) {
                headers['Content-Type'] = 'application/json';
            }

            var fetchOptions = buildFetchOptions(
                method, headers, options.body, credentials, options.signal
            );

            var response;
            try {
                response = await fetchImpl(effectiveUrl.href, fetchOptions);
            } catch (networkError) {
                throw new ns.ApiPilotHttpError(
                    'Network request failed: ' + method + ' ' + effectiveUrl.href,
                    { cause: networkError }
                );
            }

            var envelope = await parseEnvelope(response);
            var result = buildResponse(envelope, response);

            if (envelope.success === true) {
                return result;
            }

            // Failure path. The error is normalized. The callback, when
            // present, is invoked only on CSRF_TOKEN_EXPIRED. Its return
            // value is ignored; its exception is contained on .cause
            // when .cause is free. The original envelope error is always
            // the one thrown.
            var wireError = result.error;
            var envelopeError = new ns.ApiPilotEnvelopeError(
                (wireError && wireError.message) ? wireError.message : 'Request failed',
                { status: response.status, wireError: wireError }
            );

            if (onCsrfExpired !== null
                && wireError
                && wireError.code === 'CSRF_TOKEN_EXPIRED') {
                try {
                    onCsrfExpired(envelopeError, response);
                } catch (callbackError) {
                    if (envelopeError.cause === undefined) {
                        envelopeError.cause = callbackError;
                    }
                }
            }

            throw envelopeError;
        }

        function shorthand(method) {
            return function (path, second, third) {
                var options;
                if (method === 'GET' || method === 'DELETE') {
                    options = second || {};
                } else {
                    options = third || {};
                    options = Object.assign({}, options, { body: second });
                }
                options = Object.assign({}, options, { method: method });
                return request(path, options);
            };
        }

        return {
            get: shorthand('GET'),
            post: shorthand('POST'),
            put: shorthand('PUT'),
            patch: shorthand('PATCH'),
            delete: shorthand('DELETE'),
            request: request,
            csrf: csrfStore
        };
    }

    ns.createApiPilotClient = createApiPilotClient;
})(globalThis.__ApiPilotClient = globalThis.__ApiPilotClient || {});

