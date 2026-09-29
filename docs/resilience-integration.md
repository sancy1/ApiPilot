<!--
filepath: docs/resilience-integration.md
package:  n/a (repository root docs)
since:    v0.6.0
purpose:  How ApiPilot and the resilience companion fit together at the API boundary.
-->

# Resilience at the ApiPilot boundary

ApiPilot is a boundary library. It standardizes the response envelope, the
error contract, and the security surface of an ASP.NET Core application.
It is not a resilience library. Retry, circuit breaking, and idempotency
are out of scope, and audit.ps1 enforces that separation.

This document describes how an application combines the two: the
resilience companion wraps the external calls, and ApiPilot wraps the
result at the HTTP boundary.

## The two layers

The layering is intentional:

- The resilience companion sits underneath. It wraps calls to external
  systems - a downstream HTTP API, a database, a message broker - with
  retry, circuit breaking, and timeout policies. It is the application
  choice which policy applies to which call.
- ApiPilot sits at the boundary. It converts the outcome of the call,
  including a terminal resilience failure, into the standard response
  envelope.

The application composes the two. ApiPilot does not call the resilience
companion, and the resilience companion does not know about ApiPilot.

## The boundary contract

When a call wrapped by the resilience companion finally fails - the retry
budget is exhausted, the circuit is open, the timeout elapsed - the
application throws an exception that reaches ApiPilot exception handling.
ApiPilot maps it to a stable error envelope.

Two outcomes are typical:

- An unknown exception maps to INTERNAL_ERROR with a generic message. The
  detailed failure is logged server-side, correlated by the request id.
- An application-defined exception that represents an unavailable
  downstream maps to whatever error code the application configures. See
  the ApiExceptionOptions.Mappings override in error-contract.md.

The client sees one envelope. The resilience detail never crosses the
wire.

## What the application owns

The resilience companion is a separate capability. The application:

- Chooses the resilience policies and the calls they wrap.
- Decides which failures are terminal and which the application retries.
- Registers the exception mapping that turns a terminal failure into the
  error code the application wants on the wire.

ApiPilot does not own any of these decisions. It standardizes the result.

## What ApiPilot owns

ApiPilot owns the boundary:

- The envelope shape.
- The HTTP status mapping from the error code.
- The safe message rule: no stack trace, no cryptographic detail, no
  internal note on the wire.
- The correlation id in the meta.

These are fixed by the wire contract. The application cannot override
them; it can only map its exceptions into them.

## The boundary rule

ApiPilot does not implement retry, circuit breaking, or idempotency. The
audit script fails any pull request that introduces one of these in the
ApiPilot source. The resilience companion is where those behaviors live.
An application that uses both gets a resilient call path behind a
standard boundary.

## Related documents

- `error-contract.md` - the error envelope and the mapping override.
- `../SPEC.md` - the wire contract.
- `deployment.md` - the deployment guide, including where the boundary
  sits in the pipeline.
- `supply-chain.md` - the release and version contract.

