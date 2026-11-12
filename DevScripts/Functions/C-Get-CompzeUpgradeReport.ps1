# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-CompzeUpgradeReport {
    <#
    .SYNOPSIS
    Reports, for a solution that uses Compze, which of its Compze packages have newer versions, what each upgrade changes, and which other Compze packages each upgrade brings along

    .DESCRIPTION
    Run it in a repository that uses Compze, before upgrading its Compze packages. For each Compze package the
    solution uses, directly or through other packages, it compares the version the solution resolves with the newest
    version: nuget.org's, or a newer one in this machine's pending-release feed (see C-Publish-ToPendingReleaseFeed).
    For each package with a newer version it reports:
    - The Compze packages the newest version requires newer versions of than the solution uses: the upgrade brings
      them along. A direct reference to an older version of one fails the restore with NuGet's package-downgrade error
      (NU1605) until it is raised too. It says which cross a minor version, and so may bring changes the solution
      meets, and calls out a patch upgrade that brings one.
    - The changelog sections from the version the solution uses to the newest, as released: read at the release tags
      of this Compze checkout, so it fetches the latest tags first.

    It restores the solution first, since it reads what a restore resolved.

    What an upgrade breaks in the solution's code is not in the report: upgrade, and the build names it, once every
    project that references a package directly references the same version of it. A project built against an older
    version whose code runs beside a newer one would break only at run time, which is why the report names such
    references.

    .PARAMETER Solution
    The solution to report on. Defaults to the only solution file in the current directory.

    .EXAMPLE
    C-Get-CompzeUpgradeReport
    #>
    [CmdletBinding()]
    param(
        [string]$Solution
    )

    # Stop at the first error, inherited by every function this calls: a failed lookup must never read as "no newer
    # version" or "no changes".
    $ErrorActionPreference = 'Stop'

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/Changelogs.ps1")
    . (Join-Path $script:CompzeRoot ".github/scripts/lib/DependencyFloors.ps1")

    # The helpers come first: PowerShell knows a function only once its definition has run.

    function FindSolution {
        if ($Solution) { return (Resolve-Path -LiteralPath $Solution).Path }
        $solutionFiles = @(Get-ChildItem -LiteralPath (Get-Location).Path -File | Where-Object { $_.Extension -in '.slnx', '.sln' })
        if ($solutionFiles.Count -ne 1) {
            throw "C-Get-CompzeUpgradeReport: $(Get-Location) holds $($solutionFiles.Count) solution files, so name the one to report on with -Solution."
        }
        return $solutionFiles[0].FullName
    }

    function FindNewestRelease([string]$PackageId) {
        # The newest version of the package, as an object with Version, IsPending (only this machine's pending-release
        # feed has it), ChangelogRevision (where the Compze checkout has its changelog as released) and, for a pending
        # one, its Manifest. $null when neither has the package.
        $newestOnNuGetOrg = @($versionsOnNuGetOrg[$PackageId]) | Select-Object -Last 1
        $newestPending = @($pendingReleaseFeedPackages | Where-Object { $_.PackageId -eq $PackageId } | Sort-Object { [semver]$_.Version }) | Select-Object -Last 1
        if ($newestPending -and (-not $newestOnNuGetOrg -or [semver]$newestPending.Version -gt [semver]$newestOnNuGetOrg)) {
            return [PSCustomObject]@{ Version = $newestPending.Version; IsPending = $true; ChangelogRevision = $newestPending.RepositoryCommit; Manifest = $newestPending }
        }
        if (-not $newestOnNuGetOrg) { return $null }
        return [PSCustomObject]@{ Version = $newestOnNuGetOrg; IsPending = $false; ChangelogRevision = "$PackageId/v$newestOnNuGetOrg"; Manifest = $null }
    }

    function TestIsOnlyInThePendingReleaseFeed([string]$PackageId, [string]$Version) {
        return (@($versionsOnNuGetOrg[$PackageId]) -notcontains $Version.ToLowerInvariant()) -and
               @($pendingReleaseFeedPackages | Where-Object { $_.PackageId -eq $PackageId -and $_.Version -eq $Version }).Count -gt 0
    }

    function WriteOverview {
        $retiredPackages = @(Get-RetiredNuGetPackageIds)
        $idWidth = ($usedPackages | ForEach-Object { $_.PackageId.Length } | Measure-Object -Maximum).Maximum
        $versionTexts = @{}
        foreach ($usedPackage in $usedPackages) {
            $versionTexts[$usedPackage.PackageId] = (@($usedPackage.ResolvedVersions | ForEach-Object { if (TestIsOnlyInThePendingReleaseFeed $usedPackage.PackageId $_) { "$_ (pending)" } else { $_ } })) -join ', '
        }
        $versionWidth = ($versionTexts.Values | ForEach-Object { $_.Length } | Measure-Object -Maximum).Maximum

        "The Compze packages $(Split-Path -Leaf $solutionPath) uses, as restored just now, and their newest versions:"
        ""
        foreach ($usedPackage in $usedPackages) {
            $newestRelease = $newestReleases[$usedPackage.PackageId]
            $retired = $retiredPackages | Where-Object { $_.PackageId -eq $usedPackage.PackageId } | Select-Object -First 1
            $status = if ($retired) { "retired: replaced by $($retired.ReplacedBy)" }
                      elseif (-not $newestRelease) { "on neither nuget.org nor this machine's pending-release feed" }
                      elseif ($upgradedPackageIds -contains $usedPackage.PackageId) { "-> $($newestRelease.Version)$(if ($newestRelease.IsPending) { ' (pending)' })" }
                      else { "newest" }
            $reference = if ($usedPackage.DirectlyReferencingProjects.Count -gt 0) { "referenced directly" } else { "only through other packages" }
            "  {0}  {1}  {2}; {3}" -f $usedPackage.PackageId.PadRight($idWidth), $versionTexts[$usedPackage.PackageId].PadRight($versionWidth), $status, $reference
            if ($usedPackage.ResolvedVersions.Count -gt 1) {
                foreach ($olderVersion in @($usedPackage.ResolvedVersions | Select-Object -SkipLast 1)) {
                    $projectNames = @($usedPackage.Uses | Where-Object Version -eq $olderVersion | ForEach-Object { $_.ProjectName } | Sort-Object -Unique)
                    "  {0}  {1} in {2}" -f ''.PadRight($idWidth), $olderVersion, ($projectNames -join ', ')
                }
            }
        }
        $usesOrCanUpgradeToAPendingVersion = ($versionTexts.Values -join ' ') -like '*(pending)*' -or @($upgrades | Where-Object { $newestReleases[$_.PackageId].IsPending }).Count -gt 0
        if ($usesOrCanUpgradeToAPendingVersion) {
            ""
            "(pending) marks a version only this machine's pending-release feed has: a commit that references it fails CI everywhere else until nuget.org has it, so push none before."
        }
        if (@($usedPackages | Where-Object { $_.ResolvedVersions.Count -gt 1 }).Count -gt 0) {
            ""
            "Where the solution's projects resolve more than one version of a package, the lines below it name the projects that resolve the older ones, and the report compares the oldest."
        }
        if ($upgrades.Count -eq 0) {
            ""
            "Nothing to upgrade."
        }
    }

    function WriteUpgrade($UsedPackage, $NewestRelease) {
        # What the upgrade brings along, it brings into the projects that use the package: the other projects keep what
        # they resolve.
        $fromVersion = $UsedPackage.OldestResolvedVersion
        $manifest = if ($NewestRelease.Manifest) { $NewestRelease.Manifest } else { Read-NuGetOrgPackageManifest -PackageId $UsedPackage.PackageId -Version $NewestRelease.Version }
        $projectsUsingIt = @($UsedPackage.Uses | ForEach-Object { $_.ProjectName } | Sort-Object -Unique)
        $upgradedAlong = @()
        $addedAlong = @()
        foreach ($dependencyId in $manifest.CompzeDependencyFloors.Keys) {
            $floor = $manifest.CompzeDependencyFloors[$dependencyId]
            $dependencyUses = @($projectsUsingIt | ForEach-Object { $useByProjectAndPackageId["$_|$dependencyId"] } | Where-Object { $_ })
            $olderDependencyUses = @($dependencyUses | Where-Object { [semver]$_.Version -lt [semver]$floor })
            if ($olderDependencyUses.Count -gt 0) {
                $upgradedAlong += [PSCustomObject]@{
                    PackageId                                    = $dependencyId
                    FromVersion                                  = @($olderDependencyUses | Sort-Object { [semver]$_.Version })[0].Version
                    Floor                                        = $floor
                    ProjectsThatUseItReferencingAnOlderVersion   = @($olderDependencyUses | Where-Object IsDirectReference | ForEach-Object { $_.ProjectName } | Sort-Object -Unique)
                    OtherProjectsReferencingAnOlderVersion       = @($usedPackageById[$dependencyId].Uses |
                                                                     Where-Object { $_.IsDirectReference -and $projectsUsingIt -notcontains $_.ProjectName -and [semver]$_.Version -lt [semver]$floor } |
                                                                     ForEach-Object { $_.ProjectName } | Sort-Object -Unique)
                }
            }
            if ($dependencyUses.Count -lt $projectsUsingIt.Count) {
                $addedAlong += [PSCustomObject]@{ PackageId = $dependencyId; Floor = $floor }
            }
        }
        $upgradedAlongAcrossAMinorVersion = @($upgradedAlong | Where-Object { TestCrossesAMinorVersion $_.FromVersion $_.Floor })

        ""
        "== $($UsedPackage.PackageId) $fromVersion -> $($NewestRelease.Version)$(if ($NewestRelease.IsPending) { ' (pending)' }) =="
        if ((-not (TestCrossesAMinorVersion $fromVersion $NewestRelease.Version)) -and $upgradedAlongAcrossAMinorVersion.Count -gt 0) {
            ""
            "A patch upgrade that upgrades other Compze packages across a minor version: $(($upgradedAlongAcrossAMinorVersion | ForEach-Object { $_.PackageId }) -join ', '). Their changes come with it."
        }
        if ($upgradedAlong.Count -gt 0) {
            ""
            "Brings along newer versions of these Compze packages, which it requires, in the projects that use it:"
            foreach ($dependency in $upgradedAlong) {
                $crossing = if (TestCrossesAMinorVersion $dependency.FromVersion $dependency.Floor) { "across a minor version" } else { "a patch" }
                "  $($dependency.PackageId) $($dependency.FromVersion) -> $($dependency.Floor) or newer: $crossing."
                if ($dependency.ProjectsThatUseItReferencingAnOlderVersion.Count -gt 0) {
                    "    Referenced directly at an older version by projects that use $($UsedPackage.PackageId): $($dependency.ProjectsThatUseItReferencingAnOlderVersion -join ', '). Raise those references too, or their restore fails with NuGet's package-downgrade error (NU1605)."
                }
                if ($dependency.OtherProjectsReferencingAnOlderVersion.Count -gt 0) {
                    "    Referenced directly at an older version by projects that don't use $($UsedPackage.PackageId): $($dependency.OtherProjectsReferencingAnOlderVersion -join ', '). Where their code runs beside it, it runs against the newer version without being built against it, so raise those references too."
                }
                if ($dependency.ProjectsThatUseItReferencingAnOlderVersion.Count -eq 0 -and $dependency.OtherProjectsReferencingAnOlderVersion.Count -eq 0) {
                    "    No project references an older version directly, so the restore raises it by itself."
                }
            }
        }
        if ($addedAlong.Count -gt 0) {
            ""
            "Adds these Compze packages to projects that use it and don't use them yet:"
            foreach ($dependency in $addedAlong) { "  $($dependency.PackageId) $($dependency.Floor) or newer" }
        }

        ""
        WriteChanges $UsedPackage.PackageId $fromVersion $NewestRelease
    }

    function WriteChanges([string]$PackageId, [string]$FromVersion, $NewestRelease) {
        if ((Get-PackageIdsPublishedFromOtherRepositories) -contains $PackageId) {
            "$PackageId is published from its own repository, so its changelog is there, not in Compze's."
            return
        }
        $changelogPath = "src/$PackageId/CHANGELOG.md"
        $changelogLines = Read-CompzeFileAtRevision -Revision $NewestRelease.ChangelogRevision -Path $changelogPath
        if ($null -eq $changelogLines) {
            "The Compze checkout has no $changelogPath at $($NewestRelease.ChangelogRevision), so the report can't show what changed."
            return
        }
        "Its changelog from $FromVersion to $($NewestRelease.Version), as released:"
        foreach ($section in @(GetChangelogSections $changelogLines | Where-Object { ([semver]$_.Version -gt [semver]$FromVersion) -and ([semver]$_.Version -le [semver]$NewestRelease.Version) })) {
            ""
            "## $($section.Version)"
            ""
            if ($section.Content) { $section.Content } else { "(no entry)" }
        }
    }

    $solutionPath = FindSolution
    $usedPackages = @(Get-CompzePackagesASolutionUses -SolutionPath $solutionPath)
    if ($usedPackages.Count -eq 0) {
        "$solutionPath uses no Compze packages."
        return
    }

    $fetchResult = git -C $script:CompzeRoot fetch --all --tags --quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not fetch the Compze checkout's latest release tags, so the changelogs of the newest releases may be missing from the report.`n$fetchResult"
    }

    $usedPackageById = @{}
    $useByProjectAndPackageId = @{}
    foreach ($usedPackage in $usedPackages) {
        $usedPackageById[$usedPackage.PackageId] = $usedPackage
        foreach ($use in $usedPackage.Uses) { $useByProjectAndPackageId["$($use.ProjectName)|$($usedPackage.PackageId)"] = $use }
    }
    $pendingReleaseFeedPackages = @(Get-PendingReleaseFeedPackages)
    $versionsOnNuGetOrg = Get-NuGetOrgRestorableVersions -PackageId @($usedPackages | ForEach-Object { $_.PackageId })
    $newestReleases = @{}
    foreach ($usedPackage in $usedPackages) {
        $newestRelease = FindNewestRelease $usedPackage.PackageId
        if ($newestRelease) { $newestReleases[$usedPackage.PackageId] = $newestRelease }
    }
    $upgrades = @($usedPackages | Where-Object { $newestReleases.ContainsKey($_.PackageId) -and [semver]$newestReleases[$_.PackageId].Version -gt [semver]$_.OldestResolvedVersion })
    $upgradedPackageIds = @($upgrades | ForEach-Object { $_.PackageId })

    WriteOverview
    foreach ($upgradedPackage in $upgrades) { WriteUpgrade $upgradedPackage $newestReleases[$upgradedPackage.PackageId] }
}
