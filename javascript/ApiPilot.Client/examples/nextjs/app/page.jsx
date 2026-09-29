// filepath: javascript/ApiPilot.Client/examples/nextjs/app/page.jsx
// package:  n/a (example)
// since:    v0.4.0
// purpose:  A Server Component that renders the Client Component
// -----------------------------------------------------------------------------
// The page is a Server Component (Next.js App Router default). It cannot
// import the ApiPilot client, because the client is browser-only. It
// renders a Client Component, which is where the client is created.

import Client from './client.jsx';

export default function Page() {
    return (
        <main>
            <h1>ApiPilot client - Next.js example</h1>
            <p>
                The page is a Server Component. The client is created in
                the Client Component below, which is marked with the
                "use client" directive.
            </p>
            <Client />
        </main>
    );
}

