// filepath: javascript/ApiPilot.Client/examples/svelte/src/main.js
// package:  n/a (example)
// since:    v0.4.0
// purpose:  Svelte example entry point
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : svelte, ./App.svelte
//   Used by    : examples/svelte/index.html
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------

import { mount } from 'svelte';
import App from './App.svelte';

const app = mount(App, {
    target: document.getElementById('app')
});

export default app;

