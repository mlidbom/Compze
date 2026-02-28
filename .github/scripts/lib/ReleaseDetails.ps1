function FindProjectChangelogPath([string]$PackageName) {
    $projectDir = Get-ChildItem -Path "src" -Directory -Filter $PackageName | Select-Object -First 1
    if (-not $projectDir) { return $null }
    $path = Join-Path $projectDir.FullName "CHANGELOG.md"
    if (-not (Test-Path $path)) { return $null }
    return $path
}

function ExtractChangelogSectionForVersion([string]$ChangelogPath, [string]$Version) {
    # Returns the content between "## {Version}" and the next "## " heading (or end of file).
    # Leading/trailing blank lines are trimmed. Returns $null if the section is empty or missing.
    $lines = Get-Content $ChangelogPath
    $sectionLines = @()
    $inSection = $false

    foreach ($line in $lines) {
        if ($line -eq "## $Version") {
            $inSection = $true
            continue
        }
        if ($inSection -and $line -match '^## ') {
            break
        }
        if ($inSection) {
            $sectionLines += $line
        }
    }

    while ($sectionLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($sectionLines[0])) {
        $sectionLines = $sectionLines[1..($sectionLines.Count - 1)]
    }
    while ($sectionLines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($sectionLines[-1])) {
        $sectionLines = $sectionLines[0..($sectionLines.Count - 2)]
    }

    $content = ($sectionLines -join "`n").Trim()
    if ([string]::IsNullOrWhiteSpace($content)) { return $null }
    return $content
}

function GetReleaseDetails($Package) {
    # Finds the CHANGELOG.md for a package and extracts the section for its version.
    # Returns an object with ChangelogContent added, or $null if changelog is missing/empty.
    $changelogPath = FindProjectChangelogPath $Package.PackageName
    if (-not $changelogPath) { return $null }

    $changelogContent = ExtractChangelogSectionForVersion $changelogPath $Package.Version
    if (-not $changelogContent) { return $null }

    return [PSCustomObject]@{
        PackageName      = $Package.PackageName
        Version          = $Package.Version
        Tag              = $Package.Tag
        ChangelogContent = $changelogContent
    }
}

function GetAllReleaseDetails($Packages) {
    # Resolves changelogs for all packages. Fails if any are missing.
    $errors = @()
    $result = @()

    foreach ($pkg in $Packages) {
        $resolved = GetReleaseDetails $pkg
        if ($resolved) {
            $result += $resolved
        } else {
            $changelogPath = FindProjectChangelogPath $pkg.PackageName
            if (-not $changelogPath) {
                $errors += "Missing CHANGELOG.md for $($pkg.PackageName)"
            } else {
                $errors += "No changelog entry for version $($pkg.Version) in $changelogPath"
            }
        }
    }

    if ($errors.Count -gt 0) {
        Write-Host "`n::error::Changelog validation failed:" -ForegroundColor Red
        foreach ($err in $errors) { Write-Host "  - $err" -ForegroundColor Red }
        exit 1
    }

    return $result
}
