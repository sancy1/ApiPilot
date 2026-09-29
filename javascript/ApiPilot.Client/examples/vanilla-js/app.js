// filepath: javascript/ApiPilot.Client/examples/vanilla-js/app.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  The vanilla JS example: a plain ESM import of the built client
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : ../../dist/apipilot-client.esm.js
//   Used by    : examples/vanilla-js/index.html
//   See also   : docs/fetch-helper.md, README.md (client package)
// -----------------------------------------------------------------------------

import { createApiPilotClient } from '../../dist/apipilot-client.esm.js';

const client = createApiPilotClient({
    baseUrl: '/api'
});

const out = document.getElementById('output');
const write = (value) => { out.textContent = JSON.stringify(value, null, 2); };

document.getElementById('get-items').addEventListener('click', async () => {
    try {
        const res = await client.get('/items');
        write({ status: res.status, success: res.success, data: res.data });
    } catch (e) {
        write({ error: e.name, message: e.message });
    }
});

document.getElementById('post-item').addEventListener('click', async () => {
    try {
        const res = await client.post('/items', { name: 'example' });
        write({ status: res.status, success: res.success, data: res.data });
    } catch (e) {
        write({ error: e.name, message: e.message });
    }
});

