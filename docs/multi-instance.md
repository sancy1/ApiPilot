<!--
filepath: docs/multi-instance.md
package:  n/a (repository root docs)
since:    v0.5.0
purpose:  The ApiPilot multi-instance contract: the shared key ring, the application name, and the failure mode.
-->

# The ApiPilot multi-instance contract

This document describes how ApiPilot behaves when an application runs
more than one instance: behind a load balancer, or scaled to multiple
pods or processes. The normative contract is `../SPEC.md`. Where this
document and SPEC.md disagree, SPEC.md wins.

## What this document covers

- The two requirements for cross-instance interoperability.
- The failure mode when the requirements are not met.
- The startup validation.
- The diagnostic signals.
- The Option D surface.

## The two requirements

For protected payloads to interoperate across instances, both of these
must be shared:

1. The persisted Data Protection key ring. Every instance must persist
   keys to the same store. The application supplies the storage logic
   through the KeyStorage delegate:

       o.KeyStorage = dp => dp.PersistKeysToFileSystem(
           new DirectoryInfo(sharedKeyRingDirectory));

2. The Data Protection application name. Every instance must set the
   same application name through ApiPilotDataProtectionOptions.ApplicationName.
   Two instances that share a key ring but use different application
   names do not interoperate.

ApiPilot does not invent a key-management system. It uses ASP.NET Core
Data Protection as-is; the platform owns the key ring, the key lifecycle,
and the cryptography. ApiPilot owns only the configuration surface and
the startup validation.

## The failure mode

When the two requirements are not met, a token issued by one instance
cannot be validated by another. The CSRF middleware rejects the request
with the standard error envelope and the stable wire code
`CSRF_TOKEN_INVALID` (HTTP 403). The internal reason (an invalid
signature from a foreign key ring) is logged server-side and never
returned to the client.

## The startup validation

The ApiPilotDataProtectionOptions.MultiInstance flag declares the
application intent. When it is true, the application must have supplied
a KeyStorage delegate through the approved ApiPilot registration path.

The InstanceSafetyValidator enforces this contract at startup through
the DataProtectionStartupHostedService, which resolves the registered
validators and throws OptionsValidationException when validation fails.
The host refuses to start when MultiInstance is true and no key storage
was configured. The failure is fail-closed: a misconfigured multi-instance
deployment does not start in an unsafe state.

The validator records only that a KeyStorage delegate ran. It cannot
verify that the delegate configured a persistent store. An application
that supplies a delegate that does nothing has violated the contract;
the library trusts the declaration.

## The diagnostic signals

Two separate signals report the in-memory key ring condition, which is
acceptable for single-instance development but unsuitable for
multi-instance deployments:

- SECW001 (InMemoryKeyRing). A non-fatal warning returned by the
  ApiPilotSecurityDiagnostics service when MultiInstance is false and no
  KeyStorage was configured. This signal is deterministic: it is returned
  on every GetDiagnostics call while the condition holds.
- The startup log event, id 9001 (InMemoryKeyRingInUse). A one-shot
  Warning emitted once per process through the framework logger.

SECW001 and the 9001 log event are two views of the same condition.
The diagnostic codes are internal log identifiers; they are never
exposed on the wire.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Application name | framework default | ApiPilotDataProtectionOptions.ApplicationName |
| Key lifetime | framework default (90 days) | ApiPilotDataProtectionOptions.KeyLifetime |
| Multi-instance flag | false | ApiPilotDataProtectionOptions.MultiInstance |
| Key storage | none | ApiPilotDataProtectionOptions.KeyStorage |

## Compatibility guarantees

- The library never replaces the application existing Data Protection
  configuration.
- The library never inspects Data Protection internals.
- The default MultiInstance is false.
- The diagnostic codes are internal and never on the wire.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [data-protection.md](data-protection.md) - the Data Protection
  contract and the key ring.
- [csrf.md](csrf.md) - the CSRF token and the signer that uses
  Data Protection.
- [correlation.md](correlation.md) - the request id.
- `../SPEC.md` - the normative Data Protection references.

