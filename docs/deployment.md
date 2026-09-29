<!--
filepath: docs/deployment.md
package:  n/a (repository root docs)
since:    v0.6.0
purpose:  The ApiPilot deployment guide: pipeline order, fail-closed startup, and production configuration.
-->

# Deploying an ApiPilot application

This guide describes how to deploy an ASP.NET Core application that uses
ApiPilot. It covers the pipeline order, the fail-closed startup behavior,
and the production configuration each concern requires. The normative
contracts are the per-concern documents under this directory and SPEC.md.

## The pipeline order

ApiPilot middleware has a required order. Placing a component in the wrong
position causes the wrong request id, an unhandled error shape, or a CSRF
check that reads no endpoint metadata. The order is:

1. Correlation: UseApiPilotCorrelation.
2. Exceptions: UseApiPilotExceptions.
3. Routing: UseRouting.
4. CSRF protection: UseApiPilotCsrfProtection.
5. The platform rate limiter: UseRateLimiter.
6. The endpoints.

Correlation runs first so every later component sees the request id.
Exceptions run second so a fault anywhere downstream becomes the standard
error envelope. The CSRF middleware reads endpoint metadata, so it runs
after routing.

## Fail-closed startup

ApiPilot validates its configuration at startup. An insecure or ambiguous
combination prevents the host from starting. This is the fail-closed rule:
a misconfigured application does not serve a single request.

The startup validators cover the security options: the cookie profile, the
CSRF options, the Origin policy, the Fetch Metadata policy, and the Data
Protection multi-instance requirement. An invalid value throws an options
validation exception before the first request.

Read the failure message. It names the offending option. Correct the
configuration and restart. Do not suppress the validator.

## The multi-instance requirement

An application that runs more than one instance behind a load balancer
must share the Data Protection key ring and the application name across
every instance. Without a shared key ring, a CSRF token issued by one
instance is rejected by another, and legitimate requests fail with the
stable CSRF_TOKEN_INVALID wire code.

Set ApiPilotDataProtectionOptions.MultiInstance to true and configure a
persisted key storage. A non-Development environment with multi-instance
enabled and no key storage fails at startup. See multi-instance.md and
data-protection.md.

## Production configuration per concern

Each concern has a documented contract and a recommended production
configuration:

- Correlation: correlation.md. Read the incoming header, validate it, and
  echo it. The request id appears in the response envelope meta.
- CSRF: csrf.md and unsafe-methods.md. Protect the unsafe methods. Expose
  the bootstrap endpoint. Keep the token in memory on the client.
- Cookies: cookies.md. Use the secure defaults. The auth cookie is
  HttpOnly and Secure. The __Host- prefix rules apply to the effective
  cookie name.
- Origin: origin-policy.md. Allow the exact origins the application
  serves. The Origin check is defense in depth, not authorization.
- Fetch Metadata: fetch-metadata.md. Choose the Compat or Strict profile.
  Compat allows older clients; Strict is recommended for modern clients.
- Rate limiting: rate-limiting.md. The application owns the limiter and
  assigns the ApiPilot rejection handler to OnRejected.
- Observability: observability.md. The library emits a meter, an activity
  source, and sanitized log events. The application connects its own
  exporter.

## TLS and the reverse proxy

ApiPilot requires HTTPS in production. Secure cookies require HTTPS for
transmission. Run the application behind a TLS-terminating reverse proxy
or serve TLS directly. When a reverse proxy forwards the original scheme
and host, configure forwarded headers so the Origin same-origin check sees
the client-facing scheme and host.

The HTTPS reverse-proxy browser fixture is not exercised in the
repository-owned test environment. It is recorded as an environment-
dependent acceptance item. See the supply-chain.md and the test suite
documentation.

## The deployment checklist

Before serving production traffic:

- Confirm the middleware order matches the sequence above.
- Confirm the host starts with the production configuration. A fail-closed
  failure is a signal, not an obstacle.
- Confirm the Data Protection key ring is shared across every instance.
- Confirm TLS is in front of the application.
- Confirm the Origin allow-list names the exact production origins.
- Confirm the CSRF bootstrap endpoint is reachable and protected by the
  CORS policy the application intends.
- Confirm no token, cookie, or key material appears in logs. See
  observability.md.

## Related documents

- `csrf.md`, `cookies.md`, `origin-policy.md`, `fetch-metadata.md`,
  `unsafe-methods.md` - the security concerns.
- `data-protection.md`, `multi-instance.md` - the key-ring contract.
- `rate-limiting.md`, `observability.md` - the platform integration.
- `../SECURITY.md` - the vulnerability disclosure policy.
- `supply-chain.md` - the SBOM and signing contract.
- `troubleshooting-403.md` - diagnosing a rejected request.

