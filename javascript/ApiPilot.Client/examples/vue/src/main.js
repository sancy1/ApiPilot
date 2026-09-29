// filepath: javascript/ApiPilot.Client/examples/vue/src/main.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  Vue example entry point
// -----------------------------------------------------------------------------

import { createApp, h, ref } from 'vue';
import { useApiPilot } from './composables/useApiPilot.js';

const App = {
    setup() {
        const { ensure } = useApiPilot('/api');
        const result = ref(null);

        const run = async () => {
            const client = ensure();
            if (!client) {
                result.value = { error: 'client unavailable in this environment' };
                return;
            }
            try {
                const res = await client.get('/items');
                result.value = { status: res.status, success: res.success, data: res.data };
            } catch (e) {
                result.value = { error: e.name, message: e.message };
            }
        };

        return () => h('div', { style: 'font-family: system-ui; padding: 2rem' }, [
            h('h1', 'ApiPilot client - Vue example'),
            h('button', { onClick: run }, 'GET /items'),
            h('pre', result.value ? JSON.stringify(result.value, null, 2) : '(no request yet)')
        ]);
    }
};

createApp(App).mount('#app');

