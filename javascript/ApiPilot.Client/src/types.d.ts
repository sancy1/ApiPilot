// filepath: javascript/ApiPilot.Client/src/types.d.ts
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Hand-authored TypeScript declarations for the ApiPilot.Client package
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (declaration file)
//   Depends on : n/a
//   Used by    : TypeScript consumers of the package
//   See also   : src/errors.js, src/csrf.js, src/client.js, src/index.js, README.md
// -----------------------------------------------------------------------------

// Internal namespace. The generated IIFE and ESM builds share an internal
// namespace, globalThis.__ApiPilotClient, that the source files populate at
// load time. This namespace is an implementation detail. Applications must
// not read from or write to globalThis.__ApiPilotClient. The public API is
// only the exported symbols from the ESM module and the globalThis.ApiPilot
// global from the IIFE build. The internal namespace is not covered by
// semver.

// Phase 3.1 declares the client factory and the request and response types.
// The createApiPilotClient function and the ApiPilotClient, RequestOptions,
// ApiPilotResponse, and ApiPilotClientConfig interfaces are now part of the
// public type surface. The Phase 3.0 restriction that declared only the
// skeleton is retired because the phase it anticipated has arrived.

// The package version. Matches the version field in package.json.
export declare const VERSION: string;

// The base of every error the client throws. Applications can catch this
// type to handle every ApiPilot.Client failure with one branch, or catch a
// specific subclass to handle one failure category.
export declare class ApiPilotClientError extends Error {
    constructor(message: string, options?: { cause?: unknown });
}

// The client was misconfigured or called incorrectly. The application made
// a mistake; retrying will not help.
export declare class ApiPilotConfigurationError extends ApiPilotClientError {
    constructor(message: string, options?: { cause?: unknown });
}

// The HTTP layer itself failed: a network error, a DNS failure, an abort,
// or a response that could not be read. No envelope was parsed. The
// underlying transport error is carried on .cause.
export declare class ApiPilotHttpError extends ApiPilotClientError {
    constructor(message: string, options?: { cause?: unknown; status?: number });
}

// The HTTP response was received, was a valid ApiPilot envelope, and
// indicated failure (success: false). This includes 4xx and 5xx responses
// that carry a valid envelope. The wire error is on .wireError; the HTTP
// status is on .status.
export declare class ApiPilotEnvelopeError extends ApiPilotClientError {
    readonly status: number;
    readonly wireError: ApiPilotWireError | null;
    constructor(message: string, options?: { cause?: unknown; status?: number; wireError?: ApiPilotWireError });
}

// The HTTP response was received but is not a valid ApiPilot envelope. The
// body is not JSON, or the JSON lacks the success discriminator, or the
// shape is otherwise wrong. The HTTP status is on .status, the raw body on
// .rawBody, and a description of what was expected on .expected.
export declare class ApiPilotProtocolError extends ApiPilotClientError {
    readonly status: number;
    readonly rawBody: string | null;
    readonly expected: string | null;
    constructor(message: string, options?: { cause?: unknown; status?: number; rawBody?: string | null; expected?: string | null });
}

// The wire error is a plain object, not a JavaScript Error. It carries the
// { code, message, fields } shape from SPEC.md, and is set on
// ApiPilotEnvelopeError.wireError. Unknown fields on the source envelope
// are preserved by the runtime factory; the type declares the three known
// fields and allows additional unknown keys via an index signature.
export interface ApiPilotWireError {
    code: string;
    message: string;
    fields: Record<string, string[]> | null;
    [key: string]: unknown;
}

// The client configuration. Every property has a sensible default
// except baseUrl, which is required. The seven properties correspond
// to the Option D surface C1 through C7.
export interface ApiPilotClientConfig {
    // C1 -- the API base URL. Required, non-empty.
    baseUrl: string;
    // C2 -- the CSRF bootstrap path. Defaults to "csrf" resolved
    // against baseUrl with a trailing slash.
    csrfPath?: string;
    // C3 -- the credentials mode. Defaults to "same-origin".
    credentials?: RequestCredentials;
    // C4 -- the methods that receive the CSRF header. Defaults to
    // POST, PUT, PATCH, DELETE.
    protectedMethods?: string[];
    // C5 -- the CSRF header name. Defaults to "X-CSRF-TOKEN".
    csrfHeader?: string;
    // C6 -- the callback invoked on CSRF_TOKEN_EXPIRED. The return
    // value is ignored; a thrown exception is contained and cannot
    // replace the security error the client throws.
    onCsrfExpired?: ((error: ApiPilotEnvelopeError, response: Response) => void) | null;
    // C7 -- the fetch implementation. Defaults to globalThis.fetch.
    // The client uses only status, ok, and text() from a response,
    // so a custom implementation may return a minimal object with
    // those three members.
    fetch?: typeof fetch;
}

// Options for a single request. The caller headers win over the
// client defaults for Content-Type. The credentials value on the
// client config is used unless this object overrides it.
export interface RequestOptions {
    method?: string;
    headers?: HeadersInit;
    body?: unknown;
    signal?: AbortSignal;
    credentials?: RequestCredentials;
}

// The parsed ApiPilot response. The strongly-typed fields are the
// minimum contract from SPEC.md. Unknown envelope fields are
// preserved via spread and accessible via the index signature.
// The client-controlled fields (ok, status, success, data,
// message, error, meta, raw) always win over a server field with
// the same name.
export interface ApiPilotResponse<T> {
    ok: boolean;
    status: number;
    success: boolean;
    data: T | null;
    message: string | null;
    error: ApiPilotWireError | null;
    meta: { requestId: string | null; [key: string]: unknown } | null;
    raw: Response;
    [key: string]: unknown;
}

// The client. Every method resolves an ApiPilotResponse<T>. On a
// failure response (success: false) the method rejects with an
// ApiPilotEnvelopeError. On a protocol violation the method
// rejects with an ApiPilotProtocolError. On a transport failure
// the method rejects with an ApiPilotHttpError.
export interface ApiPilotClient {
    get<T>(url: string, options?: RequestOptions): Promise<ApiPilotResponse<T>>;
    post<T>(url: string, body: unknown, options?: RequestOptions): Promise<ApiPilotResponse<T>>;
    put<T>(url: string, body: unknown, options?: RequestOptions): Promise<ApiPilotResponse<T>>;
    patch<T>(url: string, body: unknown, options?: RequestOptions): Promise<ApiPilotResponse<T>>;
    delete<T>(url: string, options?: RequestOptions): Promise<ApiPilotResponse<T>>;
    request<T>(url: string, options: RequestOptions): Promise<ApiPilotResponse<T>>;
    csrf: {
        get(): Promise<string>;
        refresh(): Promise<string>;
        clear(): void;
    };
}

// The client factory. Throws ApiPilotConfigurationError when the
// configuration is invalid (missing baseUrl, unresolvable URL,
// no fetch available).
export declare function createApiPilotClient(config: ApiPilotClientConfig): ApiPilotClient;

// The public shape of the internal namespace. Applications must not read
// from or write to globalThis.__ApiPilotClient; this declaration exists so
// the ESM build can re-export the namespace members with correct types.
// It is not part of the public API surface.
declare global {
    // eslint-disable-next-line no-var
    var __ApiPilotClient: {
        VERSION: string;
        ApiPilotClientError: typeof ApiPilotClientError;
        ApiPilotConfigurationError: typeof ApiPilotConfigurationError;
        ApiPilotHttpError: typeof ApiPilotHttpError;
        ApiPilotEnvelopeError: typeof ApiPilotEnvelopeError;
        ApiPilotProtocolError: typeof ApiPilotProtocolError;
        ApiPilotWireError: (source: unknown) => ApiPilotWireError;
        createApiPilotClient: typeof createApiPilotClient;
    };
}

// The public IIFE global. Populated only by the IIFE build. Applications
// that load the IIFE build access the client through this global.
declare global {
    // eslint-disable-next-line no-var
    var ApiPilot: {
        VERSION: string;
        ApiPilotClientError: typeof ApiPilotClientError;
        ApiPilotConfigurationError: typeof ApiPilotConfigurationError;
        ApiPilotHttpError: typeof ApiPilotHttpError;
        ApiPilotEnvelopeError: typeof ApiPilotEnvelopeError;
        ApiPilotProtocolError: typeof ApiPilotProtocolError;
        ApiPilotWireError: (source: unknown) => ApiPilotWireError;
        createApiPilotClient: typeof createApiPilotClient;
    };
}

export {};

