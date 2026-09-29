# filepath: audit.ps1
# package:  n/a (repository root)
# since:    v0.1.0-alpha.0
# purpose:  Boundary and dependency audit for ApiPilot.
# -----------------------------------------------------------------------------
# RELATIONSHIPS
#   Implements : n/a (repository script)
#   Depends on : PowerShell 5.1, dotnet CLI
#   Used by    : local pre-commit, CI test workflow
#   See also   : PLANNING.md, SPEC.md
# -----------------------------------------------------------------------------

param(
    [string]$Root = "C:\Users\HP\Desktop\ApiPilot",
    [switch]$Verbose,
    [switch]$FailOnWarnings
)

$ErrorActionPreference = "Stop"
$failures = 0
$warnings = 0

function Write-Section { param([string]$Title) Write-Host ""; Write-Host "=== $Title ===" }
function Write-Pass    { param([string]$Msg)  Write-Host ("[PASS] " + $Msg) }
function Write-Fail    { param([string]$Msg)  Write-Host ("[FAIL] " + $Msg); $script:failures++ }
function Write-Warn    { param([string]$Msg)  Write-Host ("[WARN] " + $Msg); $script:warnings++ }

Write-Host "ApiPilot audit - root: $Root"

# --- Section 1: root exists ---
Write-Section "Section 1: Repository root"
if (-not (Test-Path -LiteralPath $Root)) {
    Write-Fail "Root does not exist: $Root"
    exit 1
}
Write-Pass "Root exists"

# --- Section 2: required folders ---
Write-Section "Section 2: Required folders"
$requiredFolders = @(
    ".github",
    ".github\workflows",
    "docs",
    "dotnet",
    "dotnet\src",
    "dotnet\tests",
    "dotnet\samples"
)
foreach ($f in $requiredFolders) {
    $p = Join-Path $Root $f
    if (Test-Path -LiteralPath $p) {
        Write-Pass "Folder: $f"
    } else {
        Write-Fail "Missing folder: $f"
    }
}

# --- Section 3: required root files ---
Write-Section "Section 3: Required root files"
$requiredFiles = @(
    ".gitignore",
    "CHANGELOG.md",
    "LICENSE",
    "PLANNING.md",
    "README.md",
    "SPEC.md",
    "VERSION",
    "audit.ps1"
)
foreach ($f in $requiredFiles) {
    $p = Join-Path $Root $f
    if (Test-Path -LiteralPath $p) {
        Write-Pass "File: $f"
    } else {
        Write-Fail "Missing file: $f"
    }
}

# --- Section 4: VERSION format ---
Write-Section "Section 4: VERSION format"
$versionPath = Join-Path $Root "VERSION"
if (Test-Path -LiteralPath $versionPath) {
    $versionText = (Get-Content -LiteralPath $versionPath -Raw).Trim()
    if ($versionText -match '^\d+\.\d+\.\d+(-[A-Za-z0-9\.\-]+)?$') {
        Write-Pass "VERSION: $versionText"
    } else {
        Write-Fail "VERSION format invalid: $versionText"
    }
} else {
    Write-Fail "VERSION file missing"
}

# --- Section 5: Boundary scan ---
Write-Section "Section 5: Boundary scan"

# --- 5a: production csproj must not declare PackageReference or unexpected ProjectReference ---
$srcProjRoot = Join-Path $Root "dotnet\src"
$csprojFiles = @()
if (Test-Path -LiteralPath $srcProjRoot) {
    $csprojFiles = Get-ChildItem -LiteralPath $srcProjRoot -Recurse -Filter *.csproj -File -ErrorAction SilentlyContinue
}
if ($csprojFiles.Count -eq 0) {
    Write-Host "  (no production .csproj files yet - csproj check skipped)"
} else {
    foreach ($proj in $csprojFiles) {
        $text = [System.IO.File]::ReadAllText($proj.FullName)
        $pkgMatches = [regex]::Matches($text, '<PackageReference\s')
        if ($pkgMatches.Count -gt 0) {
            Write-Fail ("PackageReference in production csproj: " + $proj.Name + " (" + $pkgMatches.Count + " occurrence(s))")
        } else {
            Write-Pass ("No PackageReference in: " + $proj.Name)
        }
    }
}

# --- 5b: compiled ApiPilot.Core.dll must reference only allowed platform assemblies ---
$coreDll = Join-Path $Root "dotnet\src\ApiPilot.Core\bin\Release\net10.0\ApiPilot.Core.dll"
if (-not (Test-Path -LiteralPath $coreDll)) {
    Write-Host "  (ApiPilot.Core.dll not built yet - reflection check skipped)"
    Write-Host "  Build first, then re-run audit for the allowlist check."
} else {
    $asm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($coreDll))
    Write-Host ("  Assembly: " + $asm.FullName)
    $refs = $asm.GetReferencedAssemblies()
    Write-Host "  Referenced assemblies:"
    foreach ($r in $refs) { Write-Host ("    - " + $r.Name) }
    $allowPattern = '^(System(\.|$)|mscorlib$|netstandard$|Microsoft\.CSharp$|WindowsBase$)'
    $violations = @()
    foreach ($r in $refs) {
        if ($r.Name -notmatch $allowPattern) { $violations += $r.Name }
    }
    if ($violations.Count -eq 0) {
        Write-Pass "ApiPilot.Core references only allowed platform assemblies"
    } else {
        Write-Fail ("ApiPilot.Core references forbidden assembly(ies): " + ($violations -join ', '))
    }
}

# --- 5c: source (production and tests) must not contain forbidden boundary symbols ---
$forbiddenSymbols = @(
    'Polly',
    'Polly.Extensions',
    'RetryPolicy',
    'CircuitBreaker',
    'IdempotencyKey',
    'JwtIssuer',
    'PaymentIntent',
    'Stripe',
    'PayPal',
    'Microsoft.Extensions.Http',
    'Microsoft.Extensions.Caching',
    'EntityFrameworkCore',
    'Newtonsoft',
    'AutoMapper',
    'FluentValidation',
    'Serilog',
    'NLog',
    'MediatR',
    'OpenTelemetry',
    'OpenTelemetry.Extensions.Hosting',
    'ApplicationInsights',
    'Datadog',
    'NewRelic',
    'Sentry',
    'AppMetrics',
    'Prometheus',
    'TokenBucketRateLimiter',
    'SlidingWindowRateLimiter',
    'FixedWindowRateLimiter',
    'ConcurrencyLimiter',
    'RateLimiter',
    'PartitionedRateLimiter'
)
$scanRoots = @(
    (Join-Path $Root "dotnet\src"),
    (Join-Path $Root "dotnet\tests")
)
$csFiles = @()
foreach ($scanRoot in $scanRoots) {
    if (Test-Path -LiteralPath $scanRoot) {
        $csFiles += Get-ChildItem -LiteralPath $scanRoot -Recurse -Filter *.cs -File -ErrorAction SilentlyContinue
    }
}
if ($csFiles.Count -eq 0) {
    Write-Host "  (no .cs files found - symbol scan skipped)"
} else {
    $symbolHits = 0
    foreach ($cs in $csFiles) {
        $src = [System.IO.File]::ReadAllText($cs.FullName)
        foreach ($sym in $forbiddenSymbols) {
            $symPattern = '\b' + [regex]::Escape($sym) + '\b'
            if ($src -match $symPattern) {
                Write-Fail ("Forbidden symbol '" + $sym + "' in " + $cs.FullName.Substring($Root.Length))
                $symbolHits++
            }
        }
    }
    if ($symbolHits -eq 0) { Write-Pass "No forbidden boundary symbols in source or tests" }
}

# --- 5d: Directory.Packages.props must not exist ---
$cpp = Join-Path $Root "Directory.Packages.props"
if (Test-Path -LiteralPath $cpp) {
    Write-Fail "Directory.Packages.props must not exist in this repository"
} else {
    Write-Pass "No Directory.Packages.props present"
}

# --- 5e: ApiPilot.Core.csproj must not declare a FrameworkReference to ASP.NET Core ---
$coreProj = Join-Path $Root "dotnet\src\ApiPilot.Core\ApiPilot.Core.csproj"
if (Test-Path -LiteralPath $coreProj) {
    $coreText = [System.IO.File]::ReadAllText($coreProj)
    if ($coreText -match '<FrameworkReference\s+Include="Microsoft\.AspNetCore\.App"') {
        Write-Fail "ApiPilot.Core.csproj declares a FrameworkReference to Microsoft.AspNetCore.App"
    } else {
        Write-Pass "ApiPilot.Core.csproj has no ASP.NET Core FrameworkReference"
    }
} else {
    Write-Host "  (ApiPilot.Core.csproj not found - FrameworkReference check skipped)"
}

# --- 5f: ApiPilot.AspNetCore.csproj must declare the ASP.NET Core FrameworkReference ---
$aspNetProj = Join-Path $Root "dotnet\src\ApiPilot.AspNetCore\ApiPilot.AspNetCore.csproj"
if (Test-Path -LiteralPath $aspNetProj) {
    $aspNetText = [System.IO.File]::ReadAllText($aspNetProj)
    if ($aspNetText -match '<FrameworkReference\s+Include="Microsoft\.AspNetCore\.App"') {
        Write-Pass "ApiPilot.AspNetCore.csproj declares the ASP.NET Core FrameworkReference"
    } else {
        Write-Fail "ApiPilot.AspNetCore.csproj is missing the ASP.NET Core FrameworkReference"
    }
} else {
    Write-Host "  (ApiPilot.AspNetCore.csproj not found - FrameworkReference check skipped)"
}

# --- 5g: ApiPilot.AspNetCore.dll must reference only allowed assemblies ---
$aspNetDll = Join-Path $Root "dotnet\src\ApiPilot.AspNetCore\bin\Release\net10.0\ApiPilot.AspNetCore.dll"
if (-not (Test-Path -LiteralPath $aspNetDll)) {
    Write-Host "  (ApiPilot.AspNetCore.dll not built yet - reflection check skipped)"
} else {
    $aspNetAsm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($aspNetDll))
    Write-Host ("  Assembly: " + $aspNetAsm.FullName)
    $aspNetRefs = $aspNetAsm.GetReferencedAssemblies()
    Write-Host "  Referenced assemblies:"
    foreach ($r in $aspNetRefs) { Write-Host ("    - " + $r.Name) }
    $aspNetAllowPattern = '^(System(\.|$)|Microsoft\.AspNetCore(\.|$)|Microsoft\.Extensions(\.|$)|mscorlib$|netstandard$|Microsoft\.CSharp$|WindowsBase$|ApiPilot\.Core$)'
    $aspNetViolations = @()
    foreach ($r in $aspNetRefs) {
        if ($r.Name -notmatch $aspNetAllowPattern) { $aspNetViolations += $r.Name }
    }
    if ($aspNetViolations.Count -eq 0) {
        Write-Pass "ApiPilot.AspNetCore references only allowed platform assemblies"
    } else {
        Write-Fail ("ApiPilot.AspNetCore references forbidden assembly(ies): " + ($aspNetViolations -join ', '))
    }
}



# --- 5g-bis: ApiPilot.Security.dll must reference only allowed assemblies ---
$securityDll = Join-Path $Root "dotnet\src\ApiPilot.Security\bin\Release\net10.0\ApiPilot.Security.dll"
if (-not (Test-Path -LiteralPath $securityDll)) {
    Write-Host "  (ApiPilot.Security.dll not built yet - reflection check skipped)"
} else {
    $securityAsm = [System.Reflection.Assembly]::Load([System.IO.File]::ReadAllBytes($securityDll))
    Write-Host ("  Assembly: " + $securityAsm.FullName)
    $securityRefs = $securityAsm.GetReferencedAssemblies()
    Write-Host "  Referenced assemblies:"
    foreach ($r in $securityRefs) { Write-Host ("    - " + $r.Name) }
    $securityAllowPattern = '^(System(\.|$)|Microsoft\.AspNetCore(\.|$)|Microsoft\.Extensions(\.|$)|mscorlib$|netstandard$|Microsoft\.CSharp$|WindowsBase$|ApiPilot\.Core$|ApiPilot\.Security$|ApiPilot\.AspNetCore$)'
    $securityViolations = @()
    foreach ($r in $securityRefs) {
        if ($r.Name -notmatch $securityAllowPattern) { $securityViolations += $r.Name }
    }
    if ($securityViolations.Count -eq 0) {
        Write-Pass "ApiPilot.Security references only allowed platform assemblies"
    } else {
        Write-Fail ("ApiPilot.Security references forbidden assembly(ies): " + ($securityViolations -join ', '))
    }
}

# --- 5h: ApiPilot.Security.csproj must declare the ASP.NET Core FrameworkReference ---
$securityProj = Join-Path $Root "dotnet\src\ApiPilot.Security\ApiPilot.Security.csproj"
if (Test-Path -LiteralPath $securityProj) {
    $securityText = [System.IO.File]::ReadAllText($securityProj)
    if ($securityText -match '<FrameworkReference\s+Include="Microsoft\.AspNetCore\.App"') {
        Write-Pass "ApiPilot.Security.csproj declares the ASP.NET Core FrameworkReference"
    } else {
        Write-Fail "ApiPilot.Security.csproj is missing the ASP.NET Core FrameworkReference"
    }
} else {
    Write-Host "  (ApiPilot.Security.csproj not found - FrameworkReference check skipped)"
}

# --- Section 6: ASCII check on markdown and scripts ---
Write-Section "Section 6: ASCII check on markdown and scripts"
$asciiTargets = @()
$md = Get-ChildItem -LiteralPath $Root -Filter *.md -File -ErrorAction SilentlyContinue
if ($md) { $asciiTargets += $md }
$ps1 = Get-ChildItem -LiteralPath $Root -Filter *.ps1 -File -ErrorAction SilentlyContinue
if ($ps1) { $asciiTargets += $ps1 }
$versionFile = Join-Path $Root "VERSION"
if (Test-Path -LiteralPath $versionFile) { $asciiTargets += (Get-Item -LiteralPath $versionFile) }
$gitignoreFile = Join-Path $Root ".gitignore"
if (Test-Path -LiteralPath $gitignoreFile) { $asciiTargets += (Get-Item -LiteralPath $gitignoreFile) }
foreach ($t in $asciiTargets) {
    $bytes = [System.IO.File]::ReadAllBytes($t.FullName)
    $nonAscii = 0
    foreach ($b in $bytes) { if ($b -gt 0x7F) { $nonAscii++ } }
    if ($nonAscii -eq 0) {
        Write-Pass "ASCII-only: $($t.Name)"
    } else {
        Write-Fail "Non-ASCII bytes ($nonAscii) in: $($t.Name)"
    }
}


# --- Section 7: Client dependency scan ---
Write-Section "Section 7: Client dependency scan"
$clientRoot = Join-Path $Root "javascript\ApiPilot.Client"
if (-not (Test-Path -LiteralPath $clientRoot)) {
    Write-Host "  (javascript/ApiPilot.Client not present yet - Section 7 skipped)"
} else {
    # 7a: package.json must have empty dependencies and devDependencies
    $pkgPath = Join-Path $clientRoot "package.json"
    if (-not (Test-Path -LiteralPath $pkgPath)) {
        Write-Fail "Client package.json missing: javascript/ApiPilot.Client/package.json"
    } else {
        $pkgText = [System.IO.File]::ReadAllText($pkgPath)
        $pkgJson = $null
        $parseOk = $false
        try { $pkgJson = $pkgText | ConvertFrom-Json; $parseOk = $true } catch { $parseOk = $false }
        if (-not $parseOk) {
            Write-Fail "Client package.json is not valid JSON"
        } else {
            $depCount = 0
            $devCount = 0
            if ($pkgJson.dependencies)    { $depCount = @($pkgJson.dependencies.PSObject.Properties).Count }
            if ($pkgJson.devDependencies) { $devCount = @($pkgJson.devDependencies.PSObject.Properties).Count }
            if ($depCount -eq 0) {
                Write-Pass "Client package.json has no runtime dependencies"
            } else {
                Write-Fail ("Client package.json declares " + $depCount + " runtime dependencies")
            }
            if ($devCount -eq 0) {
                Write-Pass "Client package.json has no development dependencies"
            } else {
                Write-Fail ("Client package.json declares " + $devCount + " development dependencies")
            }
        }
    }

    # 7b: client source must not import any non-node, non-relative specifier
    $clientSrc = Join-Path $clientRoot "src"
    $jsFiles = @()
    if (Test-Path -LiteralPath $clientSrc) {
        $jsFiles = Get-ChildItem -LiteralPath $clientSrc -Recurse -Filter *.js -File -ErrorAction SilentlyContinue
    }
    if ($jsFiles.Count -eq 0) {
        Write-Host "  (no client .js files under src/ - import scan skipped)"
    } else {
        $importViolations = 0
        foreach ($js in $jsFiles) {
            $jsText = [System.IO.File]::ReadAllText($js.FullName)
            # Match import ... from "spec" and require("spec")
            $importMatches = [regex]::Matches($jsText, "from\s+['""]([^'""]+)['""]")
            foreach ($m in $importMatches) {
                $spec = $m.Groups[1].Value
                $isRelative = $spec.StartsWith('./') -or $spec.StartsWith('../')
                $isNodeBuiltin = $spec.StartsWith('node:')
                if (-not $isRelative -and -not $isNodeBuiltin) {
                    Write-Fail ("Client import of non-platform specifier '" + $spec + "' in " + $js.Name)
                    $importViolations++
                }
            }
            $reqMatches = [regex]::Matches($jsText, "require\(\s*['""]([^'""]+)['""]\s*\)")
            foreach ($m in $reqMatches) {
                $spec = $m.Groups[1].Value
                $isRelative = $spec.StartsWith('./') -or $spec.StartsWith('../')
                $isNodeBuiltin = $spec.StartsWith('node:')
                if (-not $isRelative -and -not $isNodeBuiltin) {
                    Write-Fail ("Client require of non-platform specifier '" + $spec + "' in " + $js.Name)
                    $importViolations++
                }
            }
        }
        if ($importViolations -eq 0) {
            Write-Pass "Client source imports only relative paths and node: built-ins"
        }
    }
}

# --- Section 8: Client source scan ---
Write-Section "Section 8: Client source scan"
if (-not (Test-Path -LiteralPath $clientRoot)) {
    Write-Host "  (javascript/ApiPilot.Client not present yet - Section 8 skipped)"
} else {
    # 8a: forbidden symbols in client .js files (same list as Section 5c)
    $clientSrc = Join-Path $clientRoot "src"
    $jsFiles = @()
    if (Test-Path -LiteralPath $clientSrc) {
        $jsFiles = Get-ChildItem -LiteralPath $clientSrc -Recurse -Filter *.js -File -ErrorAction SilentlyContinue
    }
    if ($jsFiles.Count -eq 0) {
        Write-Host "  (no client .js files under src/ - symbol scan skipped)"
    } else {
        $clientSymbolHits = 0
        foreach ($js in $jsFiles) {
            $jsText = [System.IO.File]::ReadAllText($js.FullName)
            foreach ($sym in $forbiddenSymbols) {
                $symPattern = '\b' + [regex]::Escape($sym) + '\b'
                if ($jsText -match $symPattern) {
                    Write-Fail ("Forbidden symbol '" + $sym + "' in " + $js.Name)
                    $clientSymbolHits++
                }
            }
        }
        if ($clientSymbolHits -eq 0) {
            Write-Pass "No forbidden boundary symbols in client source"
        }
    }

    # 8b: ASCII-only for client .js files and package.json
    $asciiClientTargets = @()
    if (Test-Path -LiteralPath $clientSrc) {
        $asciiClientTargets += Get-ChildItem -LiteralPath $clientSrc -Recurse -Filter *.js -File -ErrorAction SilentlyContinue
    }
    $clientPkg = Join-Path $clientRoot "package.json"
    if (Test-Path -LiteralPath $clientPkg) { $asciiClientTargets += (Get-Item -LiteralPath $clientPkg) }
    foreach ($t in $asciiClientTargets) {
        $bytes = [System.IO.File]::ReadAllBytes($t.FullName)
        $nonAscii = 0
        foreach ($b in $bytes) { if ($b -gt 0x7F) { $nonAscii++ } }
        if ($nonAscii -eq 0) {
            Write-Pass ("ASCII-only: " + $t.Name)
        } else {
            Write-Fail ("Non-ASCII bytes (" + $nonAscii + ") in client file: " + $t.Name)
        }
    }
}
# --- Summary ---

# --- Section 9: Examples boundary scan ---
Write-Section "Section 9: Examples boundary scan"
$examplesRoot = Join-Path $clientRoot "examples"
if (-not (Test-Path -LiteralPath $examplesRoot)) {
    Write-Host "  (javascript/ApiPilot.Client/examples not present yet - Section 9 skipped)"
} else {
    $exampleDirs = Get-ChildItem -LiteralPath $examplesRoot -Directory -ErrorAction SilentlyContinue
    Write-Host ("  Scanned " + $exampleDirs.Count + " example directory(ies)")

    $section9Failures = 0

    foreach ($edir in $exampleDirs) {
        $pkgPath = Join-Path $edir.FullName "package.json"
        $giPath  = Join-Path $edir.FullName ".gitignore"

        $hasPkg = Test-Path -LiteralPath $pkgPath
        if ($hasPkg) {
            $parseOk = $false
            try {
                $pkg = Get-Content -LiteralPath $pkgPath -Raw | ConvertFrom-Json
                $parseOk = $true
            } catch {
                $parseOk = $false
            }
            if (-not $parseOk) {
                Write-Fail ("Example package.json is not valid JSON: " + $edir.Name)
                $section9Failures++
            }
            if ($parseOk) {
                $depHit = $false
                if ($pkg.dependencies)    { if ($pkg.dependencies.PSObject.Properties.Name    -contains '@apipilot/client') { $depHit = $true } }
                if ($pkg.devDependencies) { if ($pkg.devDependencies.PSObject.Properties.Name -contains '@apipilot/client') { $depHit = $true } }
                if ($depHit) {
                    Write-Fail ("Example declares @apipilot/client as a dependency: " + $edir.Name)
                    $section9Failures++
                }
                if ($pkg.files) {
                    foreach ($f in $pkg.files) {
                        if ($f -eq 'dist/' -or $f -match '\.tgz$') {
                            Write-Fail ("Example package.json files array ships " + $f + ": " + $edir.Name)
                            $section9Failures++
                        }
                    }
                }
            }
            if (-not (Test-Path -LiteralPath $giPath)) {
                Write-Fail ("Example has package.json but no .gitignore: " + $edir.Name)
                $section9Failures++
            } else {
                $giText = [System.IO.File]::ReadAllText($giPath)
                if ($giText -notmatch 'node_modules/') {
                    Write-Fail ("Example .gitignore does not ignore node_modules/: " + $edir.Name)
                    $section9Failures++
                }
            }
        }

        $tgzFiles = Get-ChildItem -LiteralPath $edir.FullName -Recurse -Filter "*.tgz" -File -ErrorAction SilentlyContinue
        if ($tgzFiles.Count -gt 0) {
            Write-Fail ("Example directory contains a .tgz: " + $edir.Name)
            $section9Failures++
        }
    }

    $clientDist = Join-Path $clientRoot "dist\apipilot-client.esm.js"
    $exampleSources = Get-ChildItem -LiteralPath $examplesRoot -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in @('.js','.jsx','.ts','.tsx','.svelte','.vue','.html') }
    Write-Host ("  Scanned " + $exampleSources.Count + " example source file(s) for import boundary")
    $importCount = 0
    foreach ($src in $exampleSources) {
        $srcText = [System.IO.File]::ReadAllText($src.FullName)
        $matchesRel = [regex]::Matches($srcText, 'from\s+[\x22\x27](\.\.[^\x22\x27]*apipilot-client\.esm\.js)[\x22\x27]')
        foreach ($m in $matchesRel) {
            $importCount++
            $relSpec = $m.Groups[1].Value
            $absResolved = [System.IO.Path]::GetFullPath((Join-Path $src.DirectoryName $relSpec))
            if ($absResolved -ne $clientDist) {
                Write-Fail ("Example relative import does not resolve to client dist: " + $src.Name + " -> " + $relSpec)
                $section9Failures++
            }
        }
        $matchesAbs = [regex]::Matches($srcText, 'from\s+[\x22\x27]@apipilot/client[\x22\x27]')
        if ($matchesAbs.Count -gt 0) {
            Write-Fail ("Example imports @apipilot/client as a specifier: " + $src.Name)
            $section9Failures++
        }
    }
    if ($importCount -gt 0) {
        Write-Host ("  Resolved " + $importCount + " relative client import(s)")
    }

    if ($section9Failures -eq 0) {
        Write-Pass "Examples boundary holds (no @apipilot/client dependency, relative imports resolve to dist)"
    }
}
Write-Section "Summary"
Write-Host "Failures: $failures"
Write-Host "Warnings: $warnings"
if ($failures -gt 0) {
    Write-Host "AUDIT RESULT: FAIL"
    exit 1
}
if ($warnings -gt 0 -and $FailOnWarnings) {
    Write-Host "AUDIT RESULT: FAIL (warnings as errors)"
    exit 1
}
Write-Host "AUDIT RESULT: PASS"
exit 0

