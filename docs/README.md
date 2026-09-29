<!--
filepath: docs/README.md
package:  n/a (repository root)
since:    v0.1.0-alpha.0
purpose:  Index of ApiPilot per-concern documentation.
-->

# ApiPilot documentation

This directory holds per-concern documentation for ApiPilot. Each document
describes one feature area of the shipped library. Documents are written
after each version builds, so that the documentation describes a real
artifact rather than a plan.

For cross-cutting concepts that are not per-language, see the repository
root files: `SPEC.md` (wire-format contract), `PLANNING.md` (phase plan),
`README.md` (project identity), and `CHANGELOG.md` (version history).

## Status legend

- **Planned** - the phase that will write this document has not yet completed.
- **Shipped** - the document exists and describes a shipped artifact.

## Documentation index

### Contracts and responses

| Document | Status | Phase |
| --- | --- | --- |
| `response-contract.md` | Shipped | 0.2.0 |
| `error-contract.md` | Shipped | 0.2.0 |
| `validation.md` | Shipped | 0.2.0 |
| `pagination.md` | Shipped | 0.2.0 |
| `filtering-sorting.md` | Shipped | 0.2.0 |
| `serialization.md` | Shipped | 0.2.0 |
| `content-negotiation.md` | Shipped | 0.2.0 |

### Request handling

| Document | Status | Phase |
| --- | --- | --- |
| `correlation.md` | Shipped | 0.2.0 |
| `observability.md` | Shipped | 0.5.0 |
| `rate-limiting.md` | Shipped | 0.5.0 |
| `multi-instance.md` | Shipped | 0.5.0 |

### Security

| Document | Status | Phase |
| --- | --- | --- |
| `csrf.md` | Shipped | 0.3.0 |
| `cookies.md` | Shipped | 0.3.0 |
| `origin-policy.md` | Shipped | 0.3.0 |
| `fetch-metadata.md` | Shipped | 0.3.0 |
| `unsafe-methods.md` | Shipped | 0.3.0 |
| `data-protection.md` | Shipped | 0.3.0 |
| `threat-model.md` | Shipped | 0.3.0 |
| `sensitive-action.md` | Planned | 6.3 |

### Client

| Document | Status | Phase |
| --- | --- | --- |
| `fetch-helper.md` | Shipped | 0.4.0 |

### Integration

| Document | Status | Phase |
| --- | --- | --- |
| `openapi.md` | Shipped | 1.0.0 |
| `resilience-integration.md` | Shipped | 1.0.0 |

### Operations and deployment

| Document | Status | Phase |
| --- | --- | --- |
| `deployment.md` | Shipped | 1.0.0 |
| `troubleshooting-403.md` | Shipped | 1.0.0 |
| `supply-chain.md` | Shipped | 1.0.0 |
| `migration.md` | Shipped | 1.0.0 |

## Quick start

A full ApiPilot adoption can be written as one fluent chain:

    services.AddApiPilot()
        .ConfigureCorrelation(o => o.HeaderName = "X-Correlation-Id")
        .ConfigurePagination(o => o.MaxPageSize = 200)
        .ConfigureJson(o => o.EnumMode = EnumSerializationMode.Number)
        .ConfigureExceptions(o => o.RevealExceptionMessageInResponse = false)
        .ConfigureContentNegotiation(o => o.AcceptWildcard = true)
        .ConfigureValidation(o => o.KeyTransform = FieldKeyNormalizer.Normalize);

The builder is additive. It delegates each Configure* call to the
corresponding per-concern extension (AddApiPilotCorrelation,
AddApiPilotPagination, AddApiPilotJson, AddApiPilotExceptions,
AddApiPilotContentNegotiation, AddApiPilotValidation). It
introduces no aggregate options type, no new validators, and no
middleware activation. Calling AddApiPilot() alone registers
nothing; each concern is activated explicitly through a Configure*
call. An application that wants to adopt only a subset of ApiPilot
can call the per-concern extensions directly and ignore the
builder.

Service registration and middleware activation are separate. The
builder only touches the service collection. Middleware is added
to the pipeline through the UseApiPilot* extensions in the order
documented in each per-concern doc.

## Documentation policy

ApiPilot writes per-concern documentation after each version builds. This
means every document in this directory describes a real, shipped artifact
rather than a plan. A document is added in the same pull request that
implements the concern it describes.

When a document lands, its status changes from Planned to Shipped and the
phase column is replaced with the version that shipped it.

Every document starts with an HTML comment block declaring its filepath,
package, since-version, and purpose. Every document cross-links to related
documents and to the repository root files where appropriate.

For the complete version history, see `../CHANGELOG.md`.
For the phase plan, see `../PLANNING.md`.
For the wire-format contract, see `../SPEC.md`.

