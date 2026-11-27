# Exact-version dependencies: a Compze package requires an exact version, [x.y.z], of each Compze package whose internals
# it uses, and a minimum version of its other Compze dependencies. Internals carry no compatibility promise, so a package
# that uses another's internals works only beside the build it was compiled against, and NuGet warns (NU1608) or refuses
# (NU1107) a pairing that no commit built.
#
# A package uses the internals of a package when that package's project grants it InternalsVisibleTo and its project
# references that project directly; the code policy LibraryInternalsVisibleToPolicy keeps every grant between libraries on
# a direct reference. Packing writes the exact versions (Directory.Build.targets). This file holds what the release
# preparation and the release do with them:
# - which packages use whose internals, read from the project files;
# - the changelog line that names the exact-version dependencies a version changed, which a changelog section must have,
#   as it must state the dependency floors it raises (DependencyFloors.ps1);
# - the checks that refuse a release whose exact versions are wrong, or that would leave behind a package that uses the
#   internals of one it releases.
#
# Used by the release scripts and by DevScripts. DevScripts dot-sources this file into an interactive PowerShell session,
# so nothing in it may call exit. Needs PackageManifests.ps1, and for the checks NuGetOrgVersions.ps1, dot-sourced too.

$RepositoryRoot = git -C $PSScriptRoot rev-parse --show-toplevel
$script:ExactVersionDependenciesLineLead = '- **Requires exact versions of the Compze packages whose internals it uses:**'

function ReadPackagesWhoseInternalsEachPackageUses([string[]]$PackageIds) {
    # For each of the packages, the ids of those among them whose internals it uses: each whose project,
    # src/<PackageId>/<PackageId>.csproj, grants it InternalsVisibleTo, and that its project references directly. Packing
    # follows the same rule, and the release's check that each package requires exactly what the grants say keeps the two
    # the same. A hashtable: package id -> the sorted ids.
    $projects = @{}
    foreach ($packageId in $PackageIds) {
        [xml]$project = [System.IO.File]::ReadAllText((Join-Path $RepositoryRoot "src/$packageId/$packageId.csproj"))
        $projects[$packageId] = [PSCustomObject]@{
            Grantees             = @($project.SelectNodes('//InternalsVisibleTo[@Include]') | ForEach-Object { $_.GetAttribute('Include') })
            # Split on either separator: the project files write Windows paths, and the release runs on Linux.
            ProjectsReferenced   = @($project.SelectNodes('//ProjectReference[@Include]') | ForEach-Object { ($_.GetAttribute('Include') -split '[\\/]')[-1] -replace '\.csproj$', '' })
        }
    }
    $packagesWhoseInternalsEachUses = @{}
    foreach ($packageId in $PackageIds) {
        $packagesWhoseInternalsEachUses[$packageId] = @($projects[$packageId].ProjectsReferenced |
            Where-Object { $projects.ContainsKey($_) -and $projects[$_].Grantees -contains $packageId } | Sort-Object -Unique)
    }
    return $packagesWhoseInternalsEachUses
}

function GetPackagesThatUseTheInternalsOf([string[]]$PackageIds, $PackagesWhoseInternalsEachPackageUses) {
    # The packages that use the internals of one of $PackageIds, or of a package this adds, until it adds no more: what a
    # release of $PackageIds must release as well. Sorted; none of $PackageIds among them.
    $releasedTogether = [System.Collections.Generic.HashSet[string]]::new([string[]]@($PackageIds))
    do {
        $added = @($PackagesWhoseInternalsEachPackageUses.Keys | Where-Object {
            -not $releasedTogether.Contains($_) -and @($PackagesWhoseInternalsEachPackageUses[$_] | Where-Object { $releasedTogether.Contains($_) }).Count -gt 0
        })
        foreach ($packageId in $added) { $releasedTogether.Add($packageId) | Out-Null }
    } while ($added.Count -gt 0)
    return @($releasedTogether | Where-Object { $PackageIds -notcontains $_ } | Sort-Object)
}

function GetExactVersionDependenciesChanged($PreviousManifest, $Manifest) {
    # The manifest's exact-version dependencies that the previous version required otherwise: at another exact version, at
    # a minimum version only, or not directly. Each as an object with PackageId, Version and Previously, what the previous
    # version required ($null when it didn't depend on the package directly), ordered by package id.
    foreach ($packageId in $Manifest.CompzeExactVersionDependencies.Keys) {
        $version = $Manifest.CompzeExactVersionDependencies[$packageId]
        $previously = if ($PreviousManifest.CompzeExactVersionDependencies.Contains($packageId)) { $PreviousManifest.CompzeExactVersionDependencies[$packageId] }
                      elseif ($PreviousManifest.CompzeDependencyFloors.Contains($packageId)) { "$($PreviousManifest.CompzeDependencyFloors[$packageId]) or newer" }
                      else { $null }
        if ($previously -eq $version) { continue }
        [PSCustomObject]@{ PackageId = $packageId; Version = $version; Previously = $previously }
    }
}

function FormatExactVersionDependenciesLine($ChangedExactVersionDependencies) {
    # The changelog line naming the exact-version dependencies a version changed; $null when it changed none.
    if (@($ChangedExactVersionDependencies).Count -eq 0) { return $null }
    $dependencies = (@($ChangedExactVersionDependencies) | ForEach-Object {
        $previously = if ($_.Previously) { "was $($_.Previously)" } else { 'not required directly before' }
        "$($_.PackageId) $($_.Version) ($previously)"
    }) -join ', '
    return "$script:ExactVersionDependenciesLineLead $dependencies. Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well."
}

function FindExactVersionDependenciesLine([string]$ChangelogSection) {
    # The section's line naming the exact-version dependencies the version changed; $null when it has none.
    return @($ChangelogSection -split "`n" | Where-Object { $_.StartsWith($script:ExactVersionDependenciesLineLead) }) | Select-Object -First 1
}

function GetExactVersionDependencyFailures($PublishablePackages, [string[]]$NamesOfPackagesToRelease) {
    # Why the packed packages' exact-version dependencies refuse the release, one message per failure; none when they hold.
    # $PublishablePackages are the packed packages that are published to nuget.org, each with PackageName, Version and
    # NupkgPath; $NamesOfPackagesToRelease are those the release publishes.
    $manifests = @{}
    foreach ($package in $PublishablePackages) { $manifests[$package.PackageName] = ReadNupkgManifest $package.NupkgPath }
    $packageIds = @($PublishablePackages | ForEach-Object { $_.PackageName } | Sort-Object)
    $packagesWhoseInternalsEachUses = ReadPackagesWhoseInternalsEachPackageUses $packageIds
    $versionsOfTheRelease = @{}
    foreach ($package in @($PublishablePackages | Where-Object { $NamesOfPackagesToRelease -contains $_.PackageName })) { $versionsOfTheRelease[$package.PackageName] = $package.Version }

    # Each package requires an exact version of exactly the packages whose internals it uses. This catches the packing
    # target doing nothing, as it would if NuGet renamed what it relies on.
    foreach ($packageId in $packageIds) {
        $required = @($manifests[$packageId].CompzeExactVersionDependencies.Keys)
        $usesTheInternalsOf = @($packagesWhoseInternalsEachUses[$packageId])
        $notRequired = @($usesTheInternalsOf | Where-Object { $required -notcontains $_ })
        $requiredNeedlessly = @($required | Where-Object { $usesTheInternalsOf -notcontains $_ })
        if ($notRequired.Count -gt 0) {
            "$packageId uses the internals of $($notRequired -join ', '), but its package doesn't require an exact version of them. Packing writes those (Directory.Build.targets): has a NuGet change broken it?"
        }
        if ($requiredNeedlessly.Count -gt 0) {
            "$packageId's package requires an exact version of $($requiredNeedlessly -join ', '), whose internals it doesn't use: none grants it InternalsVisibleTo, or it doesn't reference them directly."
        }
    }

    # Each package the release publishes requires exact versions that the release publishes too, or that are nuget.org's
    # newest: a consumer that takes the newest of each package would otherwise get NU1608.
    $releasedPackageIds = @($packageIds | Where-Object { $versionsOfTheRelease.ContainsKey($_) })
    $requiredFromNuGetOrg = @($releasedPackageIds | ForEach-Object { $manifests[$_].CompzeExactVersionDependencies.Keys } | Where-Object { -not $versionsOfTheRelease.ContainsKey($_) } | Sort-Object -Unique)
    $nuGetOrgVersions = if ($requiredFromNuGetOrg.Count -gt 0) { GetNuGetOrgRestorableVersionsMap $requiredFromNuGetOrg } else { @{} }
    foreach ($packageId in $releasedPackageIds) {
        $exactVersionDependencies = $manifests[$packageId].CompzeExactVersionDependencies
        foreach ($dependencyId in $exactVersionDependencies.Keys) {
            $version = $exactVersionDependencies[$dependencyId]
            if ($versionsOfTheRelease.ContainsKey($dependencyId)) {
                if ($versionsOfTheRelease[$dependencyId] -ne $version) {
                    "$packageId $($versionsOfTheRelease[$packageId]) requires exactly $dependencyId $version, but the release publishes $dependencyId $($versionsOfTheRelease[$dependencyId])."
                }
                continue
            }
            $newestOnNuGetOrg = @($nuGetOrgVersions[$dependencyId]) | Select-Object -Last 1
            if ($newestOnNuGetOrg -ne $version) {
                $onNuGetOrg = if ($newestOnNuGetOrg) { "nuget.org's newest version of it is $newestOnNuGetOrg" } else { 'nuget.org has no version of it' }
                "$packageId $($versionsOfTheRelease[$packageId]) requires exactly $dependencyId $version, which the release doesn't publish, and $onNuGetOrg. A version released moments ago may not be indexed yet: nuget.org takes 5 to 20 minutes, at times up to an hour."
            }
        }
    }

    # No package the release leaves at its released version uses the internals of one it publishes. Read from the fresh
    # packs, so it holds for released versions that predate exact versions too.
    foreach ($packageId in @($packageIds | Where-Object { -not $versionsOfTheRelease.ContainsKey($_) })) {
        $usesTheInternalsOfReleased = @($manifests[$packageId].CompzeExactVersionDependencies.Keys | Where-Object { $versionsOfTheRelease.ContainsKey($_) })
        if ($usesTheInternalsOfReleased.Count -gt 0) {
            "$packageId uses the internals of $($usesTheInternalsOfReleased -join ', '), which the release publishes, but the release leaves $packageId at $($manifests[$packageId].Version), built against their released versions. Release it too: C-Prepare-Release prepares it."
        }
    }
}
