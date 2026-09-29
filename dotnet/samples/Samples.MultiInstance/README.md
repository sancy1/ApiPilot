<!--
filepath: dotnet/samples/Samples.MultiInstance/README.md
package:  n/a (standalone sample)
since:    v0.5.0
purpose:  How to run the two-instance ApiPilot sample and what it demonstrates.
-->

# Samples.MultiInstance

A minimal standalone sample that demonstrates ApiPilot across two
application instances. It is not part of the published packages and it
is not in the solution file; it is a small integration project.

## What it demonstrates

Two ApiPilot instances configured with the same persisted Data
Protection key ring and the same application name interoperate: a CSRF
token issued by instance A validates on instance B.

This is the operational contract for any application that runs more than
one instance behind a load balancer, or that scales to multiple pods.

## The two requirements

For protected payloads to interoperate across instances, both of these
must be shared:

1. The persisted Data Protection key ring. Both instances must persist
   keys to the same store (here, a shared filesystem directory).
2. The Data Protection application name. Both instances must set the
   same application name. ApiPilot sets it through
   ApiPilotDataProtectionOptions.ApplicationName.

If the key ring is in-memory, or the two instances use different key
rings or different application names, a token issued by one instance
cannot be validated by the other, and legitimate requests fail with the
stable CSRF_TOKEN_INVALID wire code.

## The configuration

    builder.Services.AddApiPilotDataProtection(o =>
    {
        o.ApplicationName = "ApiPilot.Samples.MultiInstance";
        o.MultiInstance = true;
        o.KeyStorage = dp => dp.PersistKeysToFileSystem(
            new DirectoryInfo(keyRingDirectory));
    });

`MultiInstance = true` declares the intent. The startup validation
fails closed when the flag is true and no key storage was configured
through this registration path.

## Running it

    dotnet run --project dotnet/samples/Samples.MultiInstance/Samples.MultiInstance.csproj

The sample starts two instances on loopback ports, issues a CSRF token
from instance A, sends a protected request to instance B with that
token, and prints the result. With the shared key ring and application
name, instance B responds 200.

## Dependencies

The sample has no third-party dependencies. It references the ApiPilot
projects in this repository and the ASP.NET Core shared framework.

## Related documents

- `../../../docs/multi-instance.md` - the multi-instance contract.
- `../../../docs/data-protection.md` - the Data Protection contract.

