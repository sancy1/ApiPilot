// filepath: javascript/ApiPilot.Client/scripts/build.js
// package:  @apipilot/client
// since:    v0.4.0
// purpose:  Concatenates src fragments into the dist ESM and IIFE outputs and copies the declarations
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (build script)
//   Depends on : node:fs, node:path, node:url, scripts/manifest.json
//   Used by    : the package "build" script in package.json
//   See also   : scripts/manifest.json, src/index.js, src/iife-entry.js
// -----------------------------------------------------------------------------

import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const clientRoot = resolve(here, '..');
const manifestPath = join(here, 'manifest.json');

function readUtf8(relPath) {
    const abs = join(clientRoot, relPath);
    return readFileSync(abs, 'utf8');
}

function writeUtf8(relPath, content) {
    const abs = join(clientRoot, relPath);
    const dir = dirname(abs);
    if (!existsSync(dir)) {
        mkdirSync(dir, { recursive: true });
    }
    writeFileSync(abs, content, 'utf8');
    return { abs, bytes: Buffer.byteLength(content, 'utf8') };
}

function concatSources(manifest) {
    const parts = [];
    for (const rel of manifest.sources) {
        parts.push(readUtf8(rel));
    }
    return parts.join('\n');
}

function buildEsm(manifest) {
    const body = concatSources(manifest);
    const entry = readUtf8(manifest.esmEntry);
    return body + '\n' + entry;
}

function buildIife(manifest) {
    const body = concatSources(manifest);
    const entry = readUtf8(manifest.iifeEntry);
    // Outer IIFE wrapper. The inner fragments already use their own IIFEs;
    // the outer wrapper keeps the whole build's top-level scope isolated.
    // Template literals hold the embedded single quotes safely.
    const header = `(function () {
'use strict';
`;
    const footer = `
})();
`;
    return header + body + '\n' + entry + footer;
}

function main() {
    const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));

    const esm = buildEsm(manifest);
    const iife = buildIife(manifest);
    const types = readUtf8('src/types.d.ts');

    const esmInfo = writeUtf8(manifest.outputs.esm, esm);
    const iifeInfo = writeUtf8(manifest.outputs.iife, iife);
    const typesInfo = writeUtf8(manifest.outputs.types, types);

    process.stdout.write('Build OK\n');
    process.stdout.write('  ' + manifest.outputs.esm + ' (' + esmInfo.bytes + ' bytes)\n');
    process.stdout.write('  ' + manifest.outputs.iife + ' (' + iifeInfo.bytes + ' bytes)\n');
    process.stdout.write('  ' + manifest.outputs.types + ' (' + typesInfo.bytes + ' bytes)\n');
}

try {
    main();
} catch (err) {
    process.stderr.write('Build FAILED: ' + (err && err.message ? err.message : String(err)) + '\n');
    process.exit(1);
}

