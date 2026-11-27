# What the release says about each package it publishes: the package's changelog section for its version (see
# Changelogs.ps1), which must state the dependency floors the version raises across a minor version (see
# DependencyFloors.ps1) and the exact-version dependencies it changes (see ExactVersionDependencies.ps1). Needs
# Changelogs.ps1, PackageManifests.ps1, DependencyFloors.ps1, ExactVersionDependencies.ps1 and NuGetOrgVersions.ps1
# dot-sourced too.

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

function FindDependencyLinesErrors($Package, $ReleaseDetails, $VersionsOnNuGetOrg) {
    # Why the changelog section's lines about the version's Compze dependencies, against the previous version on nuget.org,
    # are missing or wrong; nothing when they are right. One line states the dependency floors the version raises across a
    # minor version (DependencyFloors.ps1), the other the exact-version dependencies it changes (ExactVersionDependencies.ps1).
    # The first release of a package changes neither, and then the section must have neither line.
    $previousVersion = @($VersionsOnNuGetOrg | Where-Object { [semver]$_ -lt [semver]$Package.Version }) | Select-Object -Last 1
    $previousManifest = if ($previousVersion) { ReadNuGetOrgPackageManifest $Package.PackageName $previousVersion } else { $null }
    $manifest = ReadNupkgManifest $Package.NupkgPath
    $section = "the $($Package.Version) section of src/$($Package.PackageName)/CHANGELOG.md"
    $previously = if ($previousVersion) { $previousVersion } else { 'the version before it' }

    FindDependencyLineError $section "which Compze packages the version requires newer minor versions of than $previously did" `
        (FindRaisedDependencyFloorsLine $ReleaseDetails.ChangelogContent) `
        $(if ($previousManifest) { FormatRaisedDependencyFloorsLine @(GetDependencyFloorsRaisedAcrossAMinorVersion $previousManifest $manifest) } else { $null })
    FindDependencyLineError $section "which exact versions of the Compze packages whose internals it uses the version requires where $previously required others" `
        (FindExactVersionDependenciesLine $ReleaseDetails.ChangelogContent) `
        $(if ($previousManifest) { FormatExactVersionDependenciesLine @(GetExactVersionDependenciesChanged $previousManifest $manifest) } else { $null })
}

function FindDependencyLineError([string]$Section, [string]$WhatTheLineStates, [string]$ActualLine, [string]$ExpectedLine) {
    # Why the section's line is missing or wrong, saying what to change; $null when it is right.
    if ($ActualLine -eq $ExpectedLine) { return $null }
    if (-not $ActualLine) { return "$Section doesn't state $WhatTheLineStates. Add this line to it:`n      $ExpectedLine" }
    if (-not $ExpectedLine) { return "$Section has a line stating $WhatTheLineStates, and there are none. Remove this line from it:`n      $ActualLine" }
    return "$Section misstates $WhatTheLineStates. Replace this line:`n      $ActualLine`n    with this one:`n      $ExpectedLine"
}

function GetAllReleaseDetails($Packages) {
    # Resolves changelogs for all packages. Fails if any are missing, or misstate the dependency floors their version
    # raises across a minor version or the exact-version dependencies it changes.
    $errors = @()
    $result = @()
    $versionsOnNuGetOrgByPackageName = GetNuGetOrgRestorableVersionsMap @($Packages | ForEach-Object { $_.PackageName })

    foreach ($pkg in $Packages) {
        $resolved = GetReleaseDetails $pkg
        if ($resolved) {
            $errors += @(FindDependencyLinesErrors $pkg $resolved $versionsOnNuGetOrgByPackageName[$pkg.PackageName] | Where-Object { $_ })
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
