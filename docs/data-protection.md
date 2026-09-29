<!--
filepath: docs/data-protection.md
package:  n/a (repository root docs)
since:    v0.3.0
purpose:  The ApiPilot Data Protection contract: the key ring, the multi-instance requirement, and the startup diagnostics.
-->

# The ApiPilot Data Protection contract

This document describes how ApiPilot integrates with ASP.NET Core
Data Protection. The library augments the application's existing
Data Protection configuration; it never replaces it. The normative
contract is `../SPEC.md`.

## What this document covers

- The Data Protection boundary.
- The multi-instance requirement.
- The KeyStorage delegate and the declaration contract.
- The startup validation and the diagnostic codes.
- The development-only warning.
- The key lifetime and the relationship to the CSRF token lifetime.

## The Data Protection boundary

Data Protection is the cryptographic authority for every ApiPilot
secret: the CSRF token payload and the binding fingerprint. The
library does not implement its own cryptography, does not manage its
own keys, and does not inspect Data Protection internals.

The library uses IDataProtector. It builds a plaintext payload and
hands it to Protect; it hands the wire value to Unprotect. The
cryptographic protection, the key ring, and the MAC verification
all belong to Data Protection.

## The multi-instance requirement

When an application runs multiple instances behind a load balancer,
the Data Protection key ring must be shared or persistent. If the
key ring is in-memory, a token issued by instance A cannot be
validated by instance B, and legitimate requests fail with
CSRF_TOKEN_INVALID.

The application declares its intent through
ApiPilotDataProtectionOptions.MultiInstance. The default is false.
An application that runs multiple instances sets it to true and
configures a shared key ring.

## The KeyStorage delegate

The application supplies the key storage logic through the
KeyStorage delegate. It receives the IDataProtectionBuilder and
configures the persistence method. Common choices:

    builder.Services.AddApiPilotDataProtection(o =>
    {
        o.MultiInstance = true;
        o.KeyStorage = dp => dp.PersistKeysToFileSystem(
            new DirectoryInfo("/var/lib/keys"));
    });

The library invokes the delegate and records that it ran. It does
not inspect whether the delegate configured a persistent store.

The contract is documented explicitly:

    MultiInstance = true requires the application to explicitly
    configure shared or persistent key storage through the approved
    ApiPilot registration path.

An application that sets MultiInstance to true and supplies a
delegate that does nothing has violated the contract. The library
trusts the declaration.

## The startup validation and the diagnostic codes

The InstanceSafetyValidator runs at startup through the
DataProtectionStartupHostedService, which resolves the
registered validators and throws OptionsValidationException
when validation fails.
It fails closed when MultiInstance is true and the KeyStorage
delegate was not invoked through the ApiPilot registration path.

A missing validator (a broader defect) is reported by the security
diagnostics with the ValidatorMissing code SEC002, which is Fatal.
The in-memory key ring is reported with the InMemoryKeyRing code
SECW001, which is a Warning.

The diagnostic codes are internal log identifiers. They are never
exposed on the wire.

## The development-only warning

When the in-memory key ring is in use (MultiInstance false and no
KeyStorage), the library emits a one-shot warning at host startup:

    ApiPilot: Data Protection is using the in-memory key ring.
    This is acceptable for single-instance development but
    unsuitable for multi-instance deployments.

The warning fires once per process. It never includes key material,
key ring paths, or any other filesystem detail.

## The key lifetime and the CSRF token lifetime

ApiPilotDataProtectionOptions.KeyLifetime is the Data Protection
key lifetime. The default is the framework default (90 days).

This is separate from CsrfTokenOptions.TokenLifetime, which is the
CSRF token lifetime (default two hours). Rotating a Data Protection
key does not invalidate an existing CSRF token unless the Data
Protection configuration itself revokes the old key.

An application that wants to shorten the token lifetime sets
CsrfTokenOptions.TokenLifetime. An application that wants to rotate
keys more often sets ApiPilotDataProtectionOptions.KeyLifetime.
The two are independent.

## The Option D surface

| Concern | Default | Override |
| --- | --- | --- |
| Application name | framework default | ApiPilotDataProtectionOptions.ApplicationName |
| Key lifetime | framework default (90 days) | ApiPilotDataProtectionOptions.KeyLifetime |
| Multi-instance flag | false | ApiPilotDataProtectionOptions.MultiInstance |
| Key storage | none | ApiPilotDataProtectionOptions.KeyStorage |

## Compatibility guarantees

- The library never replaces the application's Data Protection
  configuration.
- The default MultiInstance is false.
- The library never inspects Data Protection internals.
- The diagnostic codes are internal and never on the wire.

Published versions are immutable. See `../SPEC.md` "Contract versioning".

## Related documents

- [csrf.md](csrf.md) - the CSRF token and the signer that uses
  Data Protection.
- [cookies.md](cookies.md) - the cookie profiles and the Secure
  flag.
- [threat-model.md](threat-model.md) - the key-compromise threat.
- `../SPEC.md` - the normative Data Protection references.

