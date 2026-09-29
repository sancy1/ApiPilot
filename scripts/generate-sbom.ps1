<#
.SYNOPSIS
    Generates an SPDX 2.3 JSON software bill of materials for ApiPilot.
.DESCRIPTION
    Reads the repository VERSION file and the production project files, and
    emits an SPDX 2.3 JSON document listing the repository packages. The
    repository has no third-party runtime dependencies, so the document
    lists only the repository packages. Every field is either read from the
    repository or set to NOASSERTION; no external license database is used.
.PARAMETER OutputDirectory
    The directory for the generated SBOM. Defaults to artifacts/sbom.
.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/generate-sbom.ps1
#>

[CmdletBinding()]
param(
    [string]$OutputDirectory = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve the repository root from this script location.
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $scriptDirectory

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $root 'artifacts\sbom'
}

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

# Read the repository version.
$versionPath = Join-Path $root 'VERSION'
if (-not (Test-Path -LiteralPath $versionPath)) {
    throw 'VERSION file not found at ' + $versionPath
}
$version = (Get-Content -LiteralPath $versionPath -Raw).Trim()

# The three production packages and their project paths.
$packages = @(
    @{ Name = 'ApiPilot.Core';        Path = 'dotnet\src\ApiPilot.Core\ApiPilot.Core.csproj';            SpdxId = 'SPDXRef-Package-ApiPilot-Core' },
    @{ Name = 'ApiPilot.AspNetCore';  Path = 'dotnet\src\ApiPilot.AspNetCore\ApiPilot.AspNetCore.csproj';  SpdxId = 'SPDXRef-Package-ApiPilot-AspNetCore' },
    @{ Name = 'ApiPilot.Security';    Path = 'dotnet\src\ApiPilot.Security\ApiPilot.Security.csproj';      SpdxId = 'SPDXRef-Package-ApiPilot-Security' }
)

# Build the packages array.
$packageNodes = @()
$relationships = @()
$downloadLocation = 'NOASSERTION'

foreach ($pkg in $packages) {
    $projPath = Join-Path $root $pkg.Path
    if (-not (Test-Path -LiteralPath $projPath)) {
        throw 'Package project not found: ' + $projPath
    }
    $packageNodes += [ordered]@{
        name             = $pkg.Name
        SPDXID           = $pkg.SpdxId
        versionInfo      = $version
        downloadLocation = $downloadLocation
        filesAnalyzed    = $false
        licenseConcluded = 'NOASSERTION'
        licenseDeclared  = 'MIT'
        copyrightText    = 'NOASSERTION'
    }
    $relationships += [ordered]@{
        spdxElementId      = 'SPDXRef-DOCUMENT'
        relatedSpdxElement = $pkg.SpdxId
        relationshipType   = 'DESCRIBES'
    }
}

# Compose the SPDX document.
$created = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$namespace = 'https://apipilot.example/spdx/' + $version + '/' + [guid]::NewGuid().ToString('N')

$document = [ordered]@{
    spdxVersion       = 'SPDX-2.3'
    dataLicense       = 'CC0-1.0'
    SPDXID            = 'SPDXRef-DOCUMENT'
    name              = 'ApiPilot-' + $version
    documentNamespace = $namespace
    creationInfo      = [ordered]@{
        created  = $created
        creators = @('Organization: ApiPilot contributors')
    }
    packages          = $packageNodes
    relationships     = $relationships
}

# Emit the JSON. ConvertTo-Json with depth 10 covers the nesting.
$json = $document | ConvertTo-Json -Depth 10

$outPath = Join-Path $OutputDirectory ('apipilot-' + $version + '.spdx.json')
[System.IO.File]::WriteAllText($outPath, $json, [System.Text.UTF8Encoding]::new($false))

# Verify: re-read and parse.
$reread = [System.IO.File]::ReadAllText($outPath)
$parsed = $reread | ConvertFrom-Json
if ($parsed.spdxVersion -ne 'SPDX-2.3') {
    throw 'SBOM verification failed: spdxVersion is not SPDX-2.3'
}
if ($parsed.packages.Count -lt 1) {
    throw 'SBOM verification failed: no packages'
}

Write-Host ('Wrote: ' + $outPath)
Write-Host ('Version: ' + $version)
Write-Host ('Packages: ' + $parsed.packages.Count)
Write-Host ('Bytes: ' + (Get-Item $outPath).Length)

# ASCII check on the emitted file.
$bytes = [System.IO.File]::ReadAllBytes($outPath)
$nonAscii = 0
foreach ($b in $bytes) { if ($b -gt 0x7F) { $nonAscii++ } }
Write-Host ('Non-ASCII bytes in SBOM: ' + $nonAscii)

