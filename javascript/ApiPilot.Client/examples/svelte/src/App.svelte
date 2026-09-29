<!--
filepath: javascript/ApiPilot.Client/examples/svelte/src/App.svelte
package:  n/a (example)
since:    v0.4.0
purpose:  The Svelte component that consumes the ApiPilot client store
-->

<script>
    import { createApiPilotStore } from './lib/apiPilotStore.js';

    const client = createApiPilotStore('/api');
    let result = $state(null);

    async function run() {
        try {
            const res = await $client.get('/items');
            result = { status: res.status, success: res.success, data: res.data };
        } catch (e) {
            result = { error: e.name, message: e.message };
        }
    }
</script>

<main style="font-family: system-ui; padding: 2rem;">
    <h1>ApiPilot client - Svelte example</h1>
    <button onclick={run}>GET /items</button>
    <pre>{result ? JSON.stringify(result, null, 2) : '(no request yet)'}</pre>
</main>

