# What the release says about each package it publishes: the package's changelog section for its version (see
# Changelogs.ps1), which must state the dependency floors the version raises across a minor version (see
# DependencyFloors.ps1). Needs Changelogs.ps1, PackageManifests.ps1, DependencyFloors.ps1 and NuGetOrgVersions.ps1
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

function FindRaisedDependencyFloorsLineError($Package, $ReleaseDetails, $VersionsOnNuGetOrg) {
    # Why the changelog section's line stating the dependency floors the version raises across a minor version, over the
    # previous version on nuget.org, is missing or wrong; $null when it is right. The first release of a package raises
    # no floor, and neither does a version whose floors cross no minor version: then the section must have no such line.
    $previousVersion = @($VersionsOnNuGetOrg | Where-Object { [semver]$_ -lt [semver]$Package.Version }) | Select-Object -Last 1
    $expectedLine = if ($previousVersion) {
        FormatRaisedDependencyFloorsLine @(GetDependencyFloorsRaisedAcrossAMinorVersion (ReadNuGetOrgPackageManifest $Package.PackageName $previousVersion) (ReadNupkgManifest $Package.NupkgPath))
    } else { $null }
    $actualLine = FindRaisedDependencyFloorsLine $ReleaseDetails.ChangelogContent
    if ($actualLine -eq $expectedLine) { return $null }

    $section = "the $($Package.Version) section of src/$($Package.PackageName)/CHANGELOG.md"
    if (-not $actualLine) {
        return "$section doesn't say that the version requires newer minor versions of Compze packages than $previousVersion did. Add this line to it:`n      $expectedLine"
    }
    if (-not $expectedLine) {
        return "$section says that the version requires newer minor versions of Compze packages than $(if ($previousVersion) { $previousVersion } else { 'the version before it' }) did, and it doesn't. Remove this line from it:`n      $actualLine"
    }
    return "$section misstates which Compze packages the version requires newer minor versions of than $previousVersion did. Replace this line:`n      $actualLine`n    with this one:`n      $expectedLine"
}

function GetAllReleaseDetails($Packages) {
    # Resolves changelogs for all packages. Fails if any are missing, or misstate the dependency floors their version
    # raises across a minor version.
    $errors = @()
    $result = @()
    $versionsOnNuGetOrgByPackageName = GetNuGetOrgRestorableVersionsMap @($Packages | ForEach-Object { $_.PackageName })

    foreach ($pkg in $Packages) {
        $resolved = GetReleaseDetails $pkg
        if ($resolved) {
            $raisedFloorsLineError = FindRaisedDependencyFloorsLineError $pkg $resolved $versionsOnNuGetOrgByPackageName[$pkg.PackageName]
            if ($raisedFloorsLineError) { $errors += $raisedFloorsLineError }
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
