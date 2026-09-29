<!--
filepath: SECURITY.md
package:  n/a (repository root)
since:    v0.6.0
purpose:  The ApiPilot vulnerability disclosure policy and security contact.
-->

# Security policy

ApiPilot is a security-relevant library. It issues and validates CSRF
tokens, manages cookie profiles, and enforces Origin and Fetch Metadata
policies. A defect in this library can weaken the security of every
application that depends on it. We take vulnerability reports seriously
and we respond to them promptly.

## Supported versions

Security fixes are issued for the latest released minor version. Before
the 1.0.0 release, only the latest preview is supported.

| Version | Supported |
| --- | --- |
| Latest release | Yes |
| Older releases | No |

## Reporting a vulnerability

Do not open a public GitHub issue for a security vulnerability.

Report privately through GitHub Security Advisories:

1. Open the repository Security tab.
2. Click "Report a vulnerability".
3. Provide the details described below.

If GitHub Security Advisories are unavailable, email the security
contact listed in the repository profile.

## What to include

A useful report includes:

- The affected version or commit.
- A description of the vulnerability and its impact.
- A minimal reproduction: the code, the request, or the environment.
- Any known mitigations or workarounds.
- Whether the issue is already public and where.

Do not include live credentials, tokens, or personal data. Redact them.

## Our response

We aim to respond within the following windows. These are goals, not a
contractual guarantee.

| Stage | Target |
| --- | --- |
| Acknowledge the report | 3 business days |
| Initial assessment | 7 business days |
| Fix or mitigation plan | 30 business days |
| Coordinated disclosure | agreed with the reporter |

We will keep the reporter informed at each stage.

## Disclosure policy

We follow coordinated disclosure. We ask that the reporter give us the
response window above before public disclosure. We will credit the
reporter in the advisory unless the reporter prefers to remain anonymous.

## Scope

In scope:

- The ApiPilot packages: ApiPilot.Core, ApiPilot.AspNetCore, ApiPilot.Security.
- The zero-dependency browser client under javascript/ApiPilot.Client.
- The CSRF token format, cookie profiles, Origin policy, and Fetch Metadata
  enforcement.

Out of scope:

- Vulnerabilities in a consuming application that uses ApiPilot incorrectly.
- Vulnerabilities in ASP.NET Core itself, Data Protection, or the .NET runtime.
- Denial of service that requires an already-authenticated privileged caller.
- Reports that require an unsupported configuration that the library
  rejects at startup.

## Hardening the deployment

ApiPilot fails closed on insecure configuration. Read the per-concern
documents under docs/ for the security-relevant defaults and the
deployment guidance. See docs/deployment.md and docs/supply-chain.md.

