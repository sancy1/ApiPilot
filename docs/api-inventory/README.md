# API Inventory — Parity Checklist

Machine-generated from the .NET XML documentation shipped in each NuGet
package. Every public type and member of the C# implementation is listed
here with its doc summary.

**Purpose:** this is the authoritative checklist for language ports. A port
is complete when every entry below has a counterpart in the target language
(or a documented "not applicable in this ecosystem" decision in an ADR).

## Packages

| Package | Types | Members | Inventory |
|---|---|---|---|
| ApiPilot.Core | 38 | 166 | [ApiPilot.Core.md](ApiPilot.Core.md) |
| ApiPilot.AspNetCore | 64 | 197 | [ApiPilot.AspNetCore.md](ApiPilot.AspNetCore.md) |
| ApiPilot.Security | 74 | 209 | [ApiPilot.Security.md](ApiPilot.Security.md) |
| **Total** | **176** | **572** | |

Plus the JavaScript browser client (`javascript/ApiPilot.Client`), whose
surface is defined by its TypeScript declarations and 60-test suite.

## Regenerating

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/generate-api-inventory.ps1
```

Requires a Release build first (`dotnet build dotnet/ApiPilot.slnx -c Release`)
so the XML doc files exist under `dotnet/src/*/bin/Release/net10.0/`.

## Port status

| Language | Status |
|---|---|
| C# (.NET) | Reference implementation — shipped v1.0.6 |
| TypeScript | Planned |
| Python | Planned |
| Go | Planned |
| Java | Planned |
