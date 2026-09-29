// filepath: javascript/ApiPilot.Client/tests/declarations.test.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Declaration/runtime parity check (NOT TypeScript compiler validation)
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (test file)
//   Depends on : node:test, node:assert/strict, node:url, node:fs, node:path,
//                src/types.d.ts, dist/apipilot-client.esm.js
//   Used by    : the package "test" script in package.json
//   See also   : src/types.d.ts, src/index.js, tests/skeleton.test.js
// -----------------------------------------------------------------------------
//
// This file is a NAME-LEVEL parity check. It asserts that every symbol the
// declaration file exposes is present at runtime (and vice versa). It does
// NOT compile the declarations and does NOT prove they are valid TypeScript.
// True compiler validation is deferred to Phase 5.1 (contract tests).
//
// The distinction matters. A name match is not a type match. A declared
// interface name does not prove the interface members are correctly typed.
// This check catches the "declared but not exported" and "exported but not
// declared" defect classes only.

import { test } from 'node:test';
import assert from 'node:assert/strict';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { dirname, join } from 'node:path';
import { readFileSync } from 'node:fs';

const here = dirname(fileURLToPath(import.meta.url));
const clientRoot = join(here, '..');
const typesPath = join(clientRoot, 'src', 'types.d.ts');
const esmPath = join(clientRoot, 'dist', 'apipilot-client.esm.js');
const esmUrl = pathToFileURL(esmPath).href;

const typesText = readFileSync(typesPath, 'utf8');
const mod = await import(esmUrl);

// Extract declared symbol names by category from the declaration file.
// The declaration file uses the "export declare" form for runtime values
// and "export interface" for type-only declarations.
//
// The regex source contains "\\w" (one backslash, then w). In a JavaScript
// single-quoted string, that is written "\\w". The PowerShell array literal
// that writes this file uses "\\w" (two characters) to produce the two
// characters in the file. Do not use four backslashes: that produces a
// literal backslash in the regex and the pattern never matches.
function extractDeclared(pattern) {
    const names = [];
    const re = new RegExp(pattern, 'g');
    let m;
    while ((m = re.exec(typesText)) !== null) {
        names.push(m[1]);
    }
    return names;
}

const declaredConstants  = extractDeclared('export declare const (\\w+)');
const declaredClasses    = extractDeclared('export declare class (\\w+)');
const declaredFunctions  = extractDeclared('export declare function (\\w+)');
const declaredInterfaces = extractDeclared('export interface (\\w+)');

const runtimeExports = Object.keys(mod).sort();

test('every declared runtime constant is a runtime export', () => {
    assert.ok(declaredConstants.length > 0, 'expected at least one declared constant');
    for (const name of declaredConstants) {
        assert.ok(name in mod, `declared constant not exported at runtime: ${name}`);
    }
});

test('every declared class is a runtime export', () => {
    assert.ok(declaredClasses.length > 0, 'expected at least one declared class');
    for (const name of declaredClasses) {
        assert.ok(name in mod, `declared class not exported at runtime: ${name}`);
    }
});

test('every declared function is a runtime export', () => {
    assert.ok(declaredFunctions.length > 0, 'expected at least one declared function');
    for (const name of declaredFunctions) {
        assert.ok(name in mod, `declared function not exported at runtime: ${name}`);
    }
    assert.ok(declaredFunctions.includes('createApiPilotClient'), 'expected createApiPilotClient to be declared');
});

test('every runtime export is declared in types.d.ts', () => {
    const allDeclaredNames = new Set([].concat(
        declaredConstants,
        declaredClasses,
        declaredFunctions,
        declaredInterfaces
    ));
    assert.ok(allDeclaredNames.size > 0, 'expected at least one declared name');
    for (const name of runtimeExports) {
        assert.ok(allDeclaredNames.has(name), `runtime export not declared: ${name}`);
    }
});

test('the four public interfaces are declared exactly once each', () => {
    assert.ok(declaredInterfaces.length > 0, 'expected at least one declared interface');
    const expectedInterfaces = ['ApiPilotWireError', 'ApiPilotClientConfig', 'RequestOptions', 'ApiPilotResponse', 'ApiPilotClient'];
    for (const name of expectedInterfaces) {
        const occurrences = declaredInterfaces.filter(function (n) { return n === name; }).length;
        assert.equal(occurrences, 1, `expected interface ${name} to be declared exactly once, found ${occurrences}`);
    }
});

test('the runtime export set matches the expected eight symbols', () => {
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
    assert.deepEqual(runtimeExports, expected);
});

