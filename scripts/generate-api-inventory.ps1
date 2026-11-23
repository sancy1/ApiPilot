# Generates API inventory markdown from .NET XML doc files.
param(
    [string]$Root = "C:\Users\HP\Desktop\ApiPilot",
    [string]$OutDir = "C:\Users\HP\Desktop\ApiPilot\docs\api-inventory"
)

$packages = @(
    @{ Name = "ApiPilot.Core";        Xml = "$Root\dotnet\src\ApiPilot.Core\bin\Release\net10.0\ApiPilot.Core.xml" },
    @{ Name = "ApiPilot.AspNetCore";  Xml = "$Root\dotnet\src\ApiPilot.AspNetCore\bin\Release\net10.0\ApiPilot.AspNetCore.xml" },
    @{ Name = "ApiPilot.Security";    Xml = "$Root\dotnet\src\ApiPilot.Security\bin\Release\net10.0\ApiPilot.Security.xml" }
)

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null

foreach ($pkg in $packages) {
    [xml]$doc = Get-Content $pkg.Xml
    $members = $doc.doc.members.member

    $types = @{}
    $orphans = @()

    foreach ($m in $members) {
        $raw = $m.name
        $kind = $raw.Substring(0,1)
        $rest = $raw.Substring(2)
        $summary = ($m.summary -replace "`r?`n"," ").Trim()
        if ($summary.Length -gt 160) { $summary = $summary.Substring(0,160) + "..." }

        if ($kind -eq 'T') {
            if (-not $types.ContainsKey($rest)) {
                $types[$rest] = @{ Kind=''; Summary=$summary; Members=@() }
            }
            $types[$rest].Summary = $summary
            continue
        }

        $declaring = $null; $memberName = $null
        if ($kind -eq 'M') {
            $paren = $rest.IndexOf('(')
            $sig = if ($paren -ge 0) { $rest.Substring(0,$paren) } else { $rest }
            $dot = $sig.LastIndexOf('.')
            if ($dot -gt 0) {
                $declaring = $sig.Substring(0,$dot)
                $memberName = $sig.Substring($dot+1)
                if ($paren -ge 0) { $memberName += $rest.Substring($paren) }
            }
        } else {
            $dot = $rest.LastIndexOf('.')
            if ($dot -gt 0) { $declaring = $rest.Substring(0,$dot); $memberName = $rest.Substring($dot+1) }
        }
        if (-not $declaring) { $orphans += $raw; continue }
        if (-not $types.ContainsKey($declaring)) {
            $types[$declaring] = @{ Kind=''; Summary=''; Members=@() }
        }
        $types[$declaring].Members += [pscustomobject]@{ Kind=$kind; Name=$memberName; Summary=$summary }
    }

    $sorted = $types.GetEnumerator() | Sort-Object { $_.Key }

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# $($pkg.Name) - API Inventory")
    $lines.Add("")
    $lines.Add("Generated from XML documentation. This is the parity checklist for language ports.")
    $lines.Add("")
    $lines.Add("| Type | Members |")
    $lines.Add("|---|---|")
    foreach ($t in $sorted) {
        $count = $t.Value.Members.Count
        $lines.Add("| ``$($t.Key)`` | $count |")
    }
    $lines.Add("")
    $lines.Add("---")
    $lines.Add("")

    $currentNs = ""
    foreach ($t in $sorted) {
        $full = $t.Key
        $lastDot = $full.LastIndexOf('.')
        $ns = if ($lastDot -gt 0) { $full.Substring(0,$lastDot) } else { '' }
        if ($ns -ne $currentNs) {
            $currentNs = $ns
            $lines.Add("## ``$ns``")
            $lines.Add("")
        }
        $typeName = $full.Substring($full.LastIndexOf('.')+1)
        $lines.Add("### ``$typeName``")
        if ($t.Value.Summary) { $lines.Add(""); $lines.Add($t.Value.Summary) }
        if ($t.Value.Members.Count -gt 0) {
            $lines.Add("")
            $lines.Add("| Member | Kind | Summary |")
            $lines.Add("|---|---|---|")
            foreach ($mem in ($t.Value.Members | Sort-Object Name)) {
                $kindLabel = switch ($mem.Kind) { 'M' {'method'} 'P' {'property'} 'F' {'field'} 'E' {'event'} default {$mem.Kind} }
                $sum = $mem.Summary -replace '\|','\|'
                $lines.Add("| ``$($mem.Name)`` | $kindLabel | $sum |")
            }
        }
        $lines.Add("")
    }

    $outFile = Join-Path $OutDir ($pkg.Name + ".md")
    [System.IO.File]::WriteAllLines($outFile, $lines, [System.Text.UTF8Encoding]::new($false))
    $totalMembers = ($types.Values | ForEach-Object { $_.Members.Count } | Measure-Object -Sum).Sum
    Write-Host "$($pkg.Name): $($types.Count) types, $totalMembers members -> $outFile"
}
Write-Host "Done."
