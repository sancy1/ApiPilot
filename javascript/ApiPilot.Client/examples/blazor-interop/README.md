<!--
filepath: javascript/ApiPilot.Client/examples/blazor-interop/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The Blazor interop example: a JS module that exposes the client to .NET.
-->

# Blazor interop example

A source-level integration illustration. The JS module that exposes
the ApiPilot client to Blazor JavaScript interop is complete. The
Blazor host project is the reader's responsibility.

## What this demonstrates

- A JS module that Blazor loads through
  `IJSRuntime.InvokeAsync<IJSObjectReference>("import", "...")`.
- The module creates the ApiPilot client once on load.
- The module exports `get` and `post` that Blazor calls from .NET.
- Why JS interop calls from Blazor to JavaScript are asynchronous.
- Why the module must be loaded before any function on it is invoked.

## What this example is not

It is **not** a runnable Blazor project. There is no `.csproj`, no
`.razor` file, and no `Program.cs`. The JS module is the illustration;
the Blazor host is expected to exist in the reader's own Blazor
application.

If you want a runnable Blazor example, scaffold one with the .NET
CLI:

    dotnet new blazorwasm -o MyApp

Then copy `wwwroot/apiPilotInterop.js` into the new project's
`wwwroot/` directory. Update the relative import path from
`../../../../dist/apipilot-client.esm.js` to wherever the ApiPilot
client artifact lives in your project's `wwwroot/` directory. The
client's `dist/` output must be copied into `wwwroot/` at build time
or served from a static path.

## The pattern

The JS module (see `wwwroot/apiPilotInterop.js`):

    import { createApiPilotClient } from "../../../../dist/apipilot-client.esm.js";

    const client = createApiPilotClient({ baseUrl: "/api" });

    export async function get(url) {
        const res = await client.get(url);
        return { status: res.status, success: res.success, data: res.data };
    }

The Blazor side loads the module once and holds the reference:

    @inject IJSRuntime JS

    @code {
        private IJSObjectReference? _module;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./apiPilotInterop.js");
            }
        }

        private async Task LoadItems()
        {
            if (_module is null) return;
            var result = await _module.InvokeAsync<object>("get", "/items");
        }
    }

## Asynchronous interop

JS interop calls from Blazor to JavaScript are **asynchronous**.
`IJSRuntime.InvokeAsync<T>` returns a `ValueTask<T>` (or a `Task<T>` in
older Blazor versions). The .NET caller awaits the result. There is no
synchronous interop path.

The JS module's exported functions return Promises. Blazor awaits the
Promise on the .NET side and receives the resolved value.

## The module must be loaded before invocation

The module must be loaded before any function on it is invoked. The
standard pattern is:

1. Load the module with `IJSRuntime.InvokeAsync<IJSObjectReference>`
   in `OnAfterRenderAsync(true)` or equivalent lifecycle point.
2. Hold the returned `IJSObjectReference` in a field.
3. Invoke functions only after the field is non-null.

If the module is not loaded, invoking a function on a null reference
throws.

## The client lives in the JS module

The ApiPilot client is created on the JavaScript side, in the module's
top-level scope. Blazor holds only the module reference. The client
and its CSRF token store live in the JavaScript runtime.

This is important because the client is a browser-only object: it
depends on `fetch`, `URL`, and `globalThis`. Blazor WebAssembly runs
in the browser, so the client is available. Blazor Server runs on the
server, so the client is not available in the same way. The pattern
is designed for Blazor WebAssembly.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

