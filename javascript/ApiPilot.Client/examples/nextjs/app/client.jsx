'use client';

// filepath: javascript/ApiPilot.Client/examples/nextjs/app/client.jsx
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A Client Component that creates and uses the ApiPilot client
// -----------------------------------------------------------------------------
// The "use client" directive is the first line, before any imports.
// It marks this file as a Client Component. The ApiPilot client is
// browser-only; it cannot be imported from a Server Component.

import { useMemo, useState } from 'react';
import { createApiPilotClient } from '../../../dist/apipilot-client.esm.js';

export default function Client() {
    const client = useMemo(
        () => createApiPilotClient({ baseUrl: '/api' }),
        []
    );
    const [result, setResult] = useState(null);

    const run = async () => {
        try {
            const res = await client.get('/items');
            setResult({ status: res.status, success: res.success, data: res.data });
        } catch (e) {
            setResult({ error: e.name, message: e.message });
        }
    };

    return (
        <div>
            <button onClick={run}>GET /items</button>
            <pre>{result ? JSON.stringify(result, null, 2) : '(no request yet)'}</pre>
        </div>
    );
}

