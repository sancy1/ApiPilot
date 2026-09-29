<!--
filepath: docs/troubleshooting-403.md
package:  n/a (repository root docs)
since:    v0.6.0
purpose:  How to diagnose an ApiPilot 403 response, from the wire code to the server-side reason.
-->

# Troubleshooting an ApiPilot 403

ApiPilot returns HTTP 403 for a request it rejects on security grounds.
The client sees the standard error envelope with a stable wire code. The
reason for the rejection is recorded in the server-side log, correlated by
the request id. This guide maps each case to its meaning and its fix.

## Read the envelope first

Every 403 carries the standard error envelope. Read the error code:

- `CSRF_HEADER_MISSING` - the request had no CSRF header.
- `CSRF_TOKEN_INVALID` - the request had a CSRF header but the token did
  not validate.
- `CSRF_TOKEN_EXPIRED` - the token parsed and verified but its lifetime
  has passed.
- `CSRF_ORIGIN_REJECTED` - the request Origin did not match the allow-list.

The code is the wire contract. The reason behind the code is in the log.

## The log carries the specific reason

The CSRF validation records one of the following reasons server-side. The
client never sees these; the operator reads them in the log, filtered by
the request id from the response meta.

| Reason | Meaning | Common cause | Fix |
| --- | --- | --- | --- |
| Ok | The token was valid. | Not a rejection. | Not applicable. |
| Malformed | The token could not be decoded. | A token from a different producer, or a payload-format change without a purpose-version bump. | Confirm the token came from this application and the same version. |
| WrongVersion | The token was produced for another format version. | Reserved for a future version; not produced by the v1 signer. | Upgrade the token producer and the consumer together. |
| InvalidSignature | The signature did not verify. | Tampering, a token from a different Data Protection key ring, or an expired key. | Share the key ring across instances. See data-protection.md. |
| Expired | The issue time plus the lifetime is in the past. | The token lifetime elapsed before the request. | Increase TokenLifetime, or refresh the token. |
| WrongSession | The binding did not match the current request. | A cross-session replay, or a binding change after login. | Confirm the binding source is stable. See csrf.md. |
| Rotated | A rotation marker for the binding is newer than the token. | The token was rotated after issue. | Re-fetch the token after rotation. See csrf.md. |
| BindingMissing | The request could not produce a binding. | A pre-auth issuance path with no binding source. | Configure PreAuthBindingSource. See csrf.md. |

## The Origin rejection

`CSRF_ORIGIN_REJECTED` means the request Origin did not match the
allow-list. The response never echoes the offending Origin; that is
deliberate. Check the server-side log for the rejection reason.

Common causes:

- A reverse proxy forwards the wrong scheme or host, so the same-origin
  check sees an internal host. Configure forwarded headers.
- The allow-list does not name the production origin exactly. The match is
  scheme, host, and port. A trailing slash or a missing port is a mismatch.
- The OriginMatchMode is Exact and the request uses a different port than
  the allow-listed origin.

See origin-policy.md for the full contract.

## The CSRF header is absent

`CSRF_HEADER_MISSING` means the request had no CSRF header. Common causes:

- The client never fetched the token. Confirm the client calls the
  bootstrap endpoint before the first protected request.
- The client sent the header under a different name. Confirm the configured
  header name matches what the client sends. See csrf.md.
- The client attached the header to the wrong request. The CSRF header
  applies to the unsafe methods, not to the safe ones.

## The token never validates across instances

When two instances reject each other tokens with `CSRF_TOKEN_INVALID` and
the internal reason is `InvalidSignature`, the instances do not share the
Data Protection key ring or the application name. See multi-instance.md
and data-protection.md.

## What the client must never see

The 403 envelope never contains the internal reason, the expected token,
the MAC, the binding, or any cryptographic detail. If a client response
carries any of these, that is a defect. Report it per SECURITY.md.

## Related documents

- `csrf.md`, `unsafe-methods.md` - the CSRF contract.
- `origin-policy.md` - the Origin contract.
- `data-protection.md`, `multi-instance.md` - the key-ring contract.
- `deployment.md` - the deployment guide.
- `../SECURITY.md` - the vulnerability disclosure policy.

