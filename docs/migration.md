<!--
filepath: docs/migration.md
package:  n/a (repository root docs)
since:    v0.6.0
purpose:  How to adopt ApiPilot and how to move between its versions.
-->

# Migrating to ApiPilot

This guide covers two migrations: adopting ApiPilot in an existing
ASP.NET Core application, and moving between ApiPilot versions. It is
honest about the pre-1.0 state.

## Adopting ApiPilot in an existing application

An application that already returns hand-rolled JSON can adopt ApiPilot
incrementally. ApiPilot does not require a rewrite. The adoption has four
steps, each independent.

### Step 1: Register the JSON policy

Call AddApiPilotJson to apply the ApiPilot JSON conventions: camelCase,
ISO 8601 dates, enums as strings. This step alone standardizes the wire
shape of every response.

    builder.Services.AddApiPilotJson();

### Step 2: Map exceptions to the error envelope

Call AddApiPilotExceptions and add UseApiPilotExceptions early in the
pipeline. Every unhandled exception becomes the standard error envelope
instead of a stack trace or a bare 500.

    builder.Services.AddApiPilotExceptions();
    app.UseApiPilotExceptions();

### Step 3: Adopt the response envelope

Replace a hand-rolled success response with ApiResponseBuilder.Ok and
the ToResult extension. The change is per-endpoint; the application can
migrate endpoints one at a time.

    return ApiResponseBuilder.Ok(payload, meta).ToResult();

### Step 4: Adopt correlation

Call AddApiPilotCorrelation and add UseApiPilotCorrelation first in the
pipeline. Every response envelope then carries the request id, and the
response header echoes it.

    builder.Services.AddApiPilotCorrelation();
    app.UseApiPilotCorrelation();

The remaining concerns - validation, pagination, CSRF, cookies, Origin,
rate limiting - are adopted the same way: register the services, add the
middleware in the documented order, and use the result types. See
deployment.md for the pipeline order.

## Moving between ApiPilot versions

ApiPilot is at 0.5.0. The 1.0.0 release will freeze the public API
surface. Before 1.0.0, the following is the project semver reality:

- A patch bump is a documentation or non-breaking fix.
- A minor bump adds backward-compatible features. Before 1.0.0, a minor
  bump may also break an unstable surface; the CHANGELOG entry names it.
- A major bump is reserved for 1.0.0 and later.

No prior version of ApiPilot is published to a public feed. Every version
in this repository is a local build. Until 1.0.0 is published, there is no
installed-version upgrade path to follow; the repository CHANGELOG is the
authoritative record of what changed between versions. Read it before
moving between versions.

## What 1.0.0 will freeze

The 1.0.0 release freezes:

- The wire contract in SPEC.md: the envelope shape, the error shape, the
  pagination shape, and the error code table.
- The public API of ApiPilot.Core, ApiPilot.AspNetCore, and
  ApiPilot.Security.
- The zero-dependency policy.
- The Option D override surfaces listed in the per-concern documents.

After 1.0.0, a breaking change requires a major version and a migration
note in this document.

## Reading the record

The CHANGELOG.md at the repository root is the version history. Every
change and every finding is recorded there with a sequential id. A reader
moving between versions should read the entries between the two versions.

## Related documents

- `../CHANGELOG.md` - the version history and findings.
- `deployment.md` - the deployment guide.
- `../SPEC.md` - the wire contract.
- `supply-chain.md` - the version and release contract.

