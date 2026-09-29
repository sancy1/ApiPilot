<!--
filepath: dotnet/samples/Samples.Api/README.md
package:  n/a (standalone sample)
since:    v0.6.0
purpose:  How to run the standalone reference API sample and what it demonstrates.
-->

# Samples.Api

A standalone reference API that exercises the ApiPilot response envelope,
CSRF protection, correlation, pagination, and a real platform rate-limit
rejection. It is not part of the published packages and it is not in the
solution file. It is the target for the Phase 5 contract tests and the
Phase 5 browser tests.

## What it demonstrates

- A success envelope (`GET /api/ok`).
- A mapped error envelope (`GET /api/fail`).
- A validation failure (`POST /api/validate`).
- A paginated response (`GET /api/items`).
- The CSRF bootstrap endpoint (`GET /api/csrf`).
- A CSRF-protected unsafe endpoint (`POST /api/submit`).
- Correlation echoed in the response header and the envelope meta.
- A real rate-limit rejection (`GET /api/limited`).

## The default URL and how to override it

The sample does not hard-code a single port. It uses the ASP.NET Core
standard URL configuration. The default below applies only when no URL
is supplied.

    http://127.0.0.1:5090

Override it with the `ASPNETCORE_URLS` environment variable or the
`--urls` command-line argument:

    ASPNETCORE_URLS=http://127.0.0.1:5095 dotnet run --project dotnet/samples/Samples.Api/Samples.Api.csproj
    dotnet run --project dotnet/samples/Samples.Api/Samples.Api.csproj -- --urls http://127.0.0.1:5095

## Running it

    dotnet run --project dotnet/samples/Samples.Api/Samples.Api.csproj

## Exercising the CSRF success and rejection paths

1. Fetch a token from the bootstrap endpoint:

       curl http://127.0.0.1:5090/api/csrf

2. Send a protected request WITH the token (expected 200):

       curl -X POST http://127.0.0.1:5090/api/submit -H "X-CSRF-TOKEN: <token>"

3. Send the same request WITHOUT the token (expected 403, CSRF_HEADER_MISSING):

       curl -X POST http://127.0.0.1:5090/api/submit

## Exercising the rate-limit rejection

The sample registers a fixed-window limiter named `sample` with a
permit limit of 3 requests per 30 seconds. The endpoint is
`GET /api/limited`. The fourth request in the window receives the
standard `RATE_LIMITED` envelope with HTTP 429.

    curl http://127.0.0.1:5090/api/limited   # request 1
    curl http://127.0.0.1:5090/api/limited   # request 2
    curl http://127.0.0.1:5090/api/limited   # request 3
    curl http://127.0.0.1:5090/api/limited   # request 4 -> 429 RATE_LIMITED

## Dependencies

The sample has no third-party dependencies. It references the ApiPilot
projects in this repository and the ASP.NET Core shared framework.

## Related documents

- `../../../docs/rate-limiting.md` - the rate-limit contract.
- `../../../docs/csrf.md` - the CSRF contract.
- `../../../docs/correlation.md` - the correlation contract.
- `../../../docs/response-contract.md` - the envelope contract.

