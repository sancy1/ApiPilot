// filepath: javascript/ApiPilot.Client/examples/nextjs/app/layout.jsx
// package:  n/a (example)
// since:    v0.4.0
// purpose:  The root layout required by the Next.js App Router
// -----------------------------------------------------------------------------

export const metadata = {
    title: 'ApiPilot client - Next.js example'
};

export default function RootLayout({ children }) {
    return (
        <html lang="en">
            <body style={{ fontFamily: 'system-ui', padding: '2rem' }}>
                {children}
            </body>
        </html>
    );
}

