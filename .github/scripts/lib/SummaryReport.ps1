# Produces an end-of-run summary table showing every package whose local version
# differs from either its latest git tag or its latest version on nuget.org.
#
# Status values:
#   NEW            — never released; no tag and not on nuget.org.
#   UPDATED        — previously released; local version is ahead of both tag and nuget.
#   TAG MISSING    — local matches nuget but no tag exists; publish loop likely crashed after the nuget push.
#   NUGET MISSING  — local matches tag but nuget is behind; tagged release whose nuget push never landed.
#   DRIFT          — some other mismatch (e.g. tag and nuget disagree with each other).
#
# Packages where local == latest tag == latest nuget are omitted (already released cleanly).

function AsSemVerForSort([string]$Version) {
    if (-not $Version) { return $null }
    try {
        return [System.Management.Automation.SemanticVersion]::Parse($Version)
    } catch {
        return $null
    }
}

function GetLatestTagVersion([string]$PackageName) {
    $tagPrefix = "$PackageName/v"
    $tags = @(git tag --list "$tagPrefix*" 2>$null)
    if ($tags.Count -eq 0) { return $null }

    $versions = $tags | ForEach-Object { $_.Substring($tagPrefix.Length) } | Where-Object { $_ }
    if (-not $versions) { return $null }

    return $versions |
        Sort-Object -Property { AsSemVerForSort $_ } -Descending |
        Select-Object -First 1
}

function GetLatestNuGetVersionMap($PackageNames) {
    # Returns a hashtable: PackageName -> latest published version (or $null if not on nuget.org).
    $versionsByPackageName = GetNuGetOrgRestorableVersionsMap $PackageNames
    $map = @{}
    foreach ($name in $PackageNames) {
        $versions = @($versionsByPackageName[$name])
        # nuget.org lists versions in ascending semver order, so the last is the highest.
        $map[$name] = if ($versions.Count -gt 0) { $versions[-1] } else { $null }
    }
    return $map
}

function GetChangelogSummary([string]$Content) {
    # Returns the full changelog section, trimmed. Format-Table -Wrap renders each line on its own row.
    if (-not $Content) { return "" }
    return $Content.Trim()
}

function GetReleaseStatus([string]$Local, [string]$Tag, [string]$NuGet) {
    $hasTag = [bool]$Tag
    $hasNuGet = [bool]$NuGet
    $tagMatches = $hasTag -and ($Tag -eq $Local)
    $nugetMatches = $hasNuGet -and ($NuGet -eq $Local)

    if ($tagMatches -and $nugetMatches) { return "" }

    $localSv = AsSemVerForSort $Local
    $tagSv = AsSemVerForSort $Tag
    $nugetSv = AsSemVerForSort $NuGet

    $localAheadOfTag = ($null -eq $tagSv) -or ($localSv -gt $tagSv)
    $localAheadOfNuGet = ($null -eq $nugetSv) -or ($localSv -gt $nugetSv)

    if ($localAheadOfTag -and $localAheadOfNuGet) {
        if (-not $hasTag -and -not $hasNuGet) { return "NEW" }
        return "UPDATED"
    }
    if ($nugetMatches -and -not $tagMatches) { return "TAG MISSING" }
    if ($tagMatches -and -not $nugetMatches) { return "NUGET MISSING" }
    return "DRIFT"
}

function WriteSummaryReport($AllPackages, $ChangelogMap) {
    Write-Host ""
    Write-Host "── Summary ──"
    Write-Host "Looking up tags and nuget.org for $($AllPackages.Count) package(s)..."

    if (-not $ChangelogMap) { $ChangelogMap = @{} }

    $names = @($AllPackages | ForEach-Object { $_.PackageName })
    $nugetMap = GetLatestNuGetVersionMap $names

    $rows = @()
    foreach ($pkg in $AllPackages) {
        $tagVersion = GetLatestTagVersion $pkg.PackageName
        $nugetVersion = $nugetMap[$pkg.PackageName]
        $status = GetReleaseStatus $pkg.Version $tagVersion $nugetVersion
        if (-not $status) { continue }

        $rows += [PSCustomObject]@{
            Package   = $pkg.PackageName
            Local     = $pkg.Version
            Tag       = if ($tagVersion) { $tagVersion } else { "—" }
            NuGet     = if ($nugetVersion) { $nugetVersion } else { "—" }
            Status    = $status
            Changelog = GetChangelogSummary $ChangelogMap[$pkg.PackageName]
        }
    }

    if ($rows.Count -eq 0) {
        Write-Host "Everything in sync. No drift detected."
        return
    }

    # Sort: problems first (DRIFT / TAG MISSING / NUGET MISSING), then UPDATED, then NEW. Alphabetical within each.
    $statusPriority = @{ 'DRIFT' = 1; 'TAG MISSING' = 2; 'NUGET MISSING' = 3; 'UPDATED' = 4; 'NEW' = 5 }
    $rows |
        Sort-Object @{Expression={$statusPriority[$_.Status]}}, @{Expression='Package'} |
        Format-Table -AutoSize -Wrap |
        Out-Host

    $new = @($rows | Where-Object { $_.Status -eq 'NEW' }).Count
    $updated = @($rows | Where-Object { $_.Status -eq 'UPDATED' }).Count
    $drift = $rows.Count - $new - $updated
    Write-Host ("{0} new, {1} updated, {2} drift" -f $new, $updated, $drift)
}
