// filepath: javascript/ApiPilot.Client/src/errors.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  The client error hierarchy and the wire-error plain-object factory
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (module fragment)
//   Depends on : globalThis.__ApiPilotClient
//   Used by    : src/csrf.js, src/client.js, src/index.js, src/iife-entry.js, tests/*
//   See also   : src/types.d.ts, README.md, SPEC.md
// -----------------------------------------------------------------------------

(function (ns) {
    'use strict';

    // The base of every error the client throws. Applications can catch
    // this type to handle every ApiPilot.Client failure with one branch,
    // or catch a specific subclass to handle one failure category.
    class ApiPilotClientError extends Error {
        constructor(message, options) {
            super(message);
            this.name = 'ApiPilotClientError';
            if (options && 'cause' in options) {
                this.cause = options.cause;
            }
            if (typeof Error.captureStackTrace === 'function') {
                Error.captureStackTrace(this, this.constructor);
            }
        }
    }

    // The client was misconfigured or called incorrectly. The application
    // made a mistake; retrying will not help.
    class ApiPilotConfigurationError extends ApiPilotClientError {
        constructor(message, options) {
            super(message, options);
            this.name = 'ApiPilotConfigurationError';
            if (typeof Error.captureStackTrace === 'function') {
                Error.captureStackTrace(this, this.constructor);
            }
        }
    }

    // The HTTP layer itself failed: a network error, a DNS failure, an
    // abort, or a response that could not be read. No envelope was parsed.
    // The underlying transport error is carried on .cause when one is
    // available.
    class ApiPilotHttpError extends ApiPilotClientError {
        constructor(message, options) {
            super(message, options);
            this.name = 'ApiPilotHttpError';
            if (typeof Error.captureStackTrace === 'function') {
                Error.captureStackTrace(this, this.constructor);
            }
        }
    }

    // The HTTP response was received, was a valid ApiPilot envelope, and
    // indicated failure (success: false). This includes 4xx and 5xx
    // responses that carry a valid envelope. The wire error is carried on
    // .wireError; the HTTP status is carried on .status.
    //
    // A .cause is accepted so the classification helper can attach the
    // underlying transport or protocol failure when one is relevant.
    class ApiPilotEnvelopeError extends ApiPilotClientError {
        constructor(message, options) {
            super(message, options);
            this.name = 'ApiPilotEnvelopeError';
            this.status = (options && options.status) || 0;
            this.wireError = (options && options.wireError) || null;
            if (typeof Error.captureStackTrace === 'function') {
                Error.captureStackTrace(this, this.constructor);
            }
        }
    }

    // The HTTP response was received but is not a valid ApiPilot envelope.
    // The body is not JSON, or the JSON lacks the success discriminator, or
    // the shape is otherwise wrong. The HTTP status is on .status, the raw
    // body on .rawBody, and a description of what was expected on .expected.
    //
    // A .cause is accepted so a JSON.parse failure can be attached when the
    // protocol error came from a parse step.
    class ApiPilotProtocolError extends ApiPilotClientError {
        constructor(message, options) {
            super(message, options);
            this.name = 'ApiPilotProtocolError';
            this.status = (options && options.status) || 0;
            this.rawBody = (options && options.rawBody) || null;
            this.expected = (options && options.expected) || null;
            if (typeof Error.captureStackTrace === 'function') {
                Error.captureStackTrace(this, this.constructor);
            }
        }
    }

    // The wire error is a plain object, not a JavaScript Error. It carries
    // the { code, message, fields } shape from SPEC.md, and is set on
    // ApiPilotEnvelopeError.wireError.
    //
    // The factory preserves any unknown fields on the source envelope so the
    // application can inspect the full wire error the server sent. The known
    // fields (code, message, fields) are normalized; unknown fields are copied
    // through unchanged. When the source is not an object, a fully defaulted
    // wire error is returned.
    //
    // The normalized known fields take precedence over any source field with
    // the same name. This means a server that returns { code: 123 } (a
    // number) gets normalized to { code: '' } (an empty string). The
    // server cannot forge a non-string code, message, or a non-object fields
    // value.
    function ApiPilotWireError(source) {
        if (!source || typeof source !== 'object') {
            return { code: '', message: '', fields: null };
        }
        var copy = {};
        for (var key in source) {
            if (Object.prototype.hasOwnProperty.call(source, key)) {
                copy[key] = source[key];
            }
        }
        copy.code = typeof source.code === 'string' ? source.code : '';
        copy.message = typeof source.message === 'string' ? source.message : '';
        copy.fields = (source.fields && typeof source.fields === 'object') ? source.fields : null;
        return copy;
    }

    ns.ApiPilotClientError = ApiPilotClientError;
    ns.ApiPilotConfigurationError = ApiPilotConfigurationError;
    ns.ApiPilotHttpError = ApiPilotHttpError;
    ns.ApiPilotEnvelopeError = ApiPilotEnvelopeError;
    ns.ApiPilotProtocolError = ApiPilotProtocolError;
    ns.ApiPilotWireError = ApiPilotWireError;
})(globalThis.__ApiPilotClient = globalThis.__ApiPilotClient || {});

