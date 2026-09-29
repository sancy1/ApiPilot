// filepath: javascript/ApiPilot.Client/examples/blazor-interop/wwwroot/apiPilotInterop.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A JS module that exposes the ApiPilot client to Blazor JavaScript interop
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : ../../../../dist/apipilot-client.esm.js
//   Used by    : Blazor components that call IJSRuntime.InvokeAsync
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------
//
// This file is a source-level integration illustration. The JS module
// is complete. The Blazor host project (the .csproj, the components,
// the IJSRuntime calls) is the reader's responsibility. See the
// directory README for the setup notes.

import { createApiPilotClient } from '../../../dist/apipilot-client.esm.js';

const client = createApiPilotClient({
    baseUrl: '/api'
});

// The module exports functions Blazor calls through
// IJSRuntime.InvokeAsync. Each function returns a Promise. Blazor
// awaits the Promise on the .NET side.

export async function get(url) {
    try {
        const res = await client.get(url);
        return { status: res.status, success: res.success, data: res.data };
    } catch (e) {
        return { error: e.name, message: e.message };
    }
}

export async function post(url, body) {
    try {
        const res = await client.post(url, body);
        return { status: res.status, success: res.success, data: res.data };
    } catch (e) {
        return { error: e.name, message: e.message };
    }
}

