// filepath: javascript/ApiPilot.Client/examples/react/src/main.jsx
// package:  n/a (example)
// since:    v0.4.0
// purpose:  React example entry point
// -----------------------------------------------------------------------------

import React, { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { useApiPilot } from './useApiPilot.js';

function App() {
    const client = useApiPilot('/api');
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
        <div style={{ fontFamily: 'system-ui', padding: '2rem' }}>
            <h1>ApiPilot client - React example</h1>
            <button onClick={run}>GET /items</button>
            <pre>{result ? JSON.stringify(result, null, 2) : '(no request yet)'}</pre>
        </div>
    );
}

createRoot(document.getElementById('root')).render(<App />);

