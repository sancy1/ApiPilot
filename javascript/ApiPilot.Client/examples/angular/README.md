<!--
filepath: javascript/ApiPilot.Client/examples/angular/README.md
package:  n/a (example)
since:    v0.4.0
purpose:  The Angular example: a standalone service that wraps the ApiPilot client.
-->

# Angular example

A source-level integration illustration. The Angular service that wraps
the ApiPilot client is complete. The Angular project scaffolding is the
reader's responsibility.

## What this demonstrates

- An `@Injectable({ providedIn: 'root' })` standalone service that
  wraps the ApiPilot client.
- The standalone service pattern, which is the modern Angular model.
- A thin typed API (`get<T>`, `post<T>`, `put<T>`, `delete<T>`) over the
  client, returning `T` (the `data` field) rather than the full
  `ApiPilotResponse<T>` object.

## What this example is not

It is **not** a runnable Angular project. There is no `package.json`,
no `angular.json`, no `tsconfig.json`, and no bootstrap file in this
directory. The service file is the illustration; the project around it
is expected to exist in the reader's own Angular application.

If you want a runnable example, scaffold one with the Angular CLI:

    npx @angular/cli new my-app

Then copy `src/app/api-pilot.service.ts` into the new project's
`src/app/` directory. Update the relative import path from
`../../../../dist/apipilot-client.esm.js` to wherever the ApiPilot
client artifact lives in your project (or import from the published
`@apipilot/client` package if you have installed it).

## The pattern

The service:

    import { Injectable } from "@angular/core";
    import { createApiPilotClient } from "../../../../dist/apipilot-client.esm.js";
    import type { ApiPilotClient } from "../../../../dist/apipilot-client.d.ts";

    @Injectable({ providedIn: "root" })
    export class ApiPilotService {
        private readonly client: ApiPilotClient;

        constructor() {
            this.client = createApiPilotClient({ baseUrl: "/api" });
        }

        get<T>(url: string): Promise<T> {
            return this.client.get<T>(url).then(res => res.data as T);
        }
    }

A component injects the service through Angular's dependency injection:

    @Component({ ... })
    export class MyComponent {
        private readonly api = inject(ApiPilotService);

        async load() {
            const items = await this.api.get<Item[]>("/items");
        }
    }

## Standalone services

The service uses `@Injectable({ providedIn: "root" })`, which is the
Angular standalone service pattern. It is provided at the root
injector. No `NgModule` is required. This is the current Angular model
and the one this example follows.

## Related documents

- [docs/fetch-helper.md](../../../../docs/fetch-helper.md) - the
  client contract.
- [Client package README](../../README.md) - the package overview.
- [examples/README.md](../README.md) - the index of all examples.

