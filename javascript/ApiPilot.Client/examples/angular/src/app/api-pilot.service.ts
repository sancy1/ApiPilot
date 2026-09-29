// filepath: javascript/ApiPilot.Client/examples/angular/src/app/api-pilot.service.ts
// package:  n/a (example)
// since:    v0.4.0
// purpose:  Angular standalone service that wraps the ApiPilot client
// -----------------------------------------------------------------------------
// RELATIONSHIPS
//   Implements : n/a (example)
//   Depends on : @angular/core, ../../../../dist/apipilot-client.esm.js
//   Used by    : Angular components that inject ApiPilotService
//   See also   : docs/fetch-helper.md
// -----------------------------------------------------------------------------
//
// This file is a source-level integration illustration. The service is
// complete. The Angular project scaffolding (angular.json, tsconfig,
// main.ts, components) is the reader's responsibility. See the
// directory README for the runnable setup.

import { Injectable } from '@angular/core';
import { createApiPilotClient } from '../../../../dist/apipilot-client.esm.js';
import type { ApiPilotClient } from '../../../../dist/apipilot-client.d.ts';

@Injectable({ providedIn: 'root' })
export class ApiPilotService {
    private readonly client: ApiPilotClient;

    constructor() {
        this.client = createApiPilotClient({
            baseUrl: '/api'
        });
    }

    get<T>(url: string): Promise<T> {
        return this.client.get<T>(url).then(res => res.data as T);
    }

    post<T>(url: string, body: unknown): Promise<T> {
        return this.client.post<T>(url, body).then(res => res.data as T);
    }

    put<T>(url: string, body: unknown): Promise<T> {
        return this.client.put<T>(url, body).then(res => res.data as T);
    }

    delete<T>(url: string): Promise<T> {
        return this.client.delete<T>(url).then(res => res.data as T);
    }
}

