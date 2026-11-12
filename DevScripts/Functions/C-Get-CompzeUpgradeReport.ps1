# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-CompzeUpgradeReport {
    <#
    .SYNOPSIS
    Reports, for a solution that uses Compze, which of its Compze package references have newer versions, what each upgrade changes and brings along, and where its projects run different versions of a package

    .DESCRIPTION
    Run it in a repository that uses Compze, before upgrading its Compze packages. It restores the solution and reads
    each project's restore graph (Read-SolutionRestoreGraphs). Then it reports:
    - Each Compze package the solution uses, at the versions it references directly (or, for a package it gets only
      through other packages, the versions it resolves), and the newest version: nuget.org's, or a newer one in this
      machine's pending-release feed (see C-Publish-ToPendingReleaseFeed).
    - How the solution sets its versions: with central package management, and with transitive pinning or without it.
      Without pinning, a project that gets a package only through other packages gets the lowest version they allow, not
      the one Directory.Packages.props names, so the report recommends turning it on, and names the Compze packages no
      PackageVersion line covers.
    - The packages its projects resolve at different versions: for each older version, the chain that holds it there,
      as dotnet nuget why shows one, how to raise it, and whether Compze's release has checked the package that holds
      it, which was built against the older version, beside the newer one.
    - For each package the solution names a version for (references directly, or pins in Directory.Packages.props) and
      that has a newer version: the other Compze packages the
      upgrade brings along, calling out a patch that brings a minor version, the references to raise with it, and the
      changelog sections in between as released, read at the release tags of this Compze checkout, so it fetches the
      latest tags first.

    What an upgrade breaks in the solution's code is not in the report: upgrade, and the build names it, once every
    project builds against the version it runs with, which transitive pinning makes sure of.

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

    function SortVersions($Versions) {
        # Distinct, oldest first. Like any PowerShell function, it returns one version as a bare string, which indexes
        # by character: call it inside @().
        return @($Versions | Sort-Object -Unique | Sort-Object { [semver]$_ })
    }

    function GetCompzePackagesUsed {
        # For each Compze package any project resolves: its uses (one per project), the versions resolved, and the
        # versions the solution names for it: those it references directly, and under transitive pinning the version its
        # PackageVersion line names, since that line sets the version wherever the solution gets the package.
        $packageIds = @($graphs | ForEach-Object { $_.Packages.Keys } | Where-Object { $_ -like 'Compze.*' } | Sort-Object -Unique)
        foreach ($packageId in $packageIds) {
            $uses = @(foreach ($graph in $graphs) {
                if ($graph.Packages.ContainsKey($packageId)) {
                    [PSCustomObject]@{ Graph = $graph; Version = $graph.Packages[$packageId].Version; IsDirectReference = $graph.DirectPackageReferences.ContainsKey($packageId) }
                }
            })
            $isPinned = $usesTransitivePinningEverywhere -and [bool](GetCentralVersion $packageId)
            $namedVersions = @(if ($isPinned) { GetCentralVersion $packageId } else { SortVersions @($uses | Where-Object IsDirectReference | ForEach-Object { $_.Version }) })
            [PSCustomObject]@{
                PackageId            = $packageId
                Uses                 = $uses
                ResolvedVersions     = @(SortVersions @($uses | ForEach-Object { $_.Version }))
                NamedVersions        = $namedVersions
                IsNamedBySolution    = $namedVersions.Count -gt 0
                IsPinned             = $isPinned
                IsReferencedDirectly = @($uses | Where-Object IsDirectReference).Count -gt 0
            }
        }
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

    function TestIsNewestRelease([string]$PackageId, [string]$Version) {
        $newestRelease = $newestReleases[$PackageId]
        return [bool]$newestRelease -and [semver]$newestRelease.Version -eq [semver]$Version
    }

    function TestIsOnlyInThePendingReleaseFeed([string]$PackageId, [string]$Version) {
        return (@($versionsOnNuGetOrg[$PackageId]) -notcontains $Version.ToLowerInvariant()) -and
               @($pendingReleaseFeedPackages | Where-Object { $_.PackageId -eq $PackageId -and $_.Version -eq $Version }).Count -gt 0
    }

    function FormatVersions([string]$PackageId, $Versions) {
        return (@($Versions | ForEach-Object { if (TestIsOnlyInThePendingReleaseFeed $PackageId $_) { "$_ (pending)" } else { $_ } })) -join ', '
    }

    function GetCentralVersion([string]$PackageId) {
        # The version a Directory.Packages.props names for the package; $null when none does.
        return @($graphs | Where-Object { $_.CentralPackageVersions.ContainsKey($PackageId) } | ForEach-Object { $_.CentralPackageVersions[$PackageId] }) | Select-Object -First 1
    }

    function FormatProjectNames($Graphs) {
        return (@($Graphs | ForEach-Object { $_.ProjectName } | Sort-Object -Unique)) -join ', '
    }

    function WriteOverview {
        $retiredPackages = @(Get-RetiredNuGetPackageIds)
        $idWidth = ($usedPackages | ForEach-Object { $_.PackageId.Length } | Measure-Object -Maximum).Maximum
        $versionTexts = @{}
        foreach ($usedPackage in $usedPackages) {
            $shownVersions = if ($usedPackage.IsNamedBySolution) { $usedPackage.NamedVersions } else { $usedPackage.ResolvedVersions }
            $versionTexts[$usedPackage.PackageId] = FormatVersions $usedPackage.PackageId $shownVersions
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
                      elseif ([semver]$newestRelease.Version -gt [semver]$usedPackage.ResolvedVersions[-1]) { "$($newestRelease.Version)$(if ($newestRelease.IsPending) { ' (pending)' }) is out" }
                      else { "newest" }
            $reference = if ($usedPackage.IsReferencedDirectly) { "referenced directly" }
                         elseif ($usedPackage.IsPinned) { "pinned in Directory.Packages.props" }
                         else { "only through other packages" }
            $differentVersions = if ($usedPackage.ResolvedVersions.Count -gt 1) { "; projects resolve different versions, see below" } else { "" }
            "  {0}  {1}  {2}; {3}{4}" -f $usedPackage.PackageId.PadRight($idWidth), $versionTexts[$usedPackage.PackageId].PadRight($versionWidth), $status, $reference, $differentVersions
        }
        if ($restore.ProjectsWithoutRestoreOutput.Count -gt 0) {
            ""
            "These projects of the solution have no restore output in obj/, so the report leaves them out: $($restore.ProjectsWithoutRestoreOutput -join ', ')."
        }
        $usesOrCanUpgradeToAPendingVersion = ($versionTexts.Values -join ' ') -like '*(pending)*' -or @($newestReleases.Values | Where-Object IsPending).Count -gt 0
        if ($usesOrCanUpgradeToAPendingVersion) {
            ""
            "(pending) marks a version only this machine's pending-release feed has: a commit that references it fails CI everywhere else until nuget.org has it, so push none before."
        }
        if ($upgrades.Count -eq 0) {
            ""
            "No package the solution names a version for has a newer version."
        }
    }

    function WriteHowTheSolutionSetsItsVersions {
        ""
        "== How the solution sets its Compze versions =="
        ""
        $settings = @($graphs | Group-Object { "$($_.UsesCentralPackageManagement)|$($_.UsesTransitivePinning)" })
        if ($settings.Count -gt 1) {
            "Its projects set their versions in different ways:"
            foreach ($setting in $settings) {
                $graph = $setting.Group[0]
                $way = if ($graph.UsesTransitivePinning) { "central package management with transitive pinning" }
                       elseif ($graph.UsesCentralPackageManagement) { "central package management without transitive pinning" }
                       else { "each project names its own versions" }
                "  ${way}: $(FormatProjectNames $setting.Group)"
            }
            ""
        }
        if ($usesTransitivePinningEverywhere) {
            "Central package management with transitive pinning: every project gets the version Directory.Packages.props names for each package listed there, also where it gets the package only through other packages."
        } elseif ($usesCentralPackageManagementEverywhere) {
            "Central package management without transitive pinning: Directory.Packages.props sets the versions of the packages a project references directly, but a project that gets a package only through other packages gets the lowest version they allow, which is often older than the one named. Turn on transitive pinning, in Directory.Packages.props:"
            ""
            "  <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>"
            ""
            "Every project then gets the version Directory.Packages.props names and builds against it, and a restore that would need a newer version fails with NuGet's package-downgrade error (NU1109), where without pinning the mismatch shows only when the code runs."
        } else {
            "Each project names its own package versions, without central package management, so projects can reference different versions of a package, and get different ones through other packages. Central package management with transitive pinning gives every project the one version a Directory.Packages.props names: https://learn.microsoft.com/nuget/consume-packages/central-package-management#transitive-pinning"
        }
        if ($usesCentralPackageManagementEverywhere) {
            $packagesNoLineNames = @($usedPackages | Where-Object { -not (GetCentralVersion $_.PackageId) } | ForEach-Object { $_.PackageId })
            if ($packagesNoLineNames.Count -gt 0) {
                ""
                "No PackageVersion line names these Compze packages, which the solution gets only through other packages, so transitive pinning can't set their versions: $($packagesNoLineNames -join ', '). Add a line for each to pin it."
            }
        }
    }

    function WriteDifferentVersions {
        $packagesAtDifferentVersions = @($usedPackages | Where-Object { $_.ResolvedVersions.Count -gt 1 })
        ""
        "== Packages the projects resolve at different versions =="
        if ($packagesAtDifferentVersions.Count -eq 0) {
            ""
            "None: every project resolves the same version of each Compze package."
            return
        }
        foreach ($usedPackage in $packagesAtDifferentVersions) {
            $newerVersion = $usedPackage.ResolvedVersions[-1]
            ""
            "$($usedPackage.PackageId): $(FormatVersions $usedPackage.PackageId $usedPackage.ResolvedVersions)"
            $holdersBuiltAgainstAnOlderVersion = @()
            $isHeldBackThroughOtherPackages = $false
            $isReferencedDirectlyAtAnOlderVersion = $false
            foreach ($olderVersion in @($usedPackage.ResolvedVersions | Select-Object -SkipLast 1)) {
                $chains = @($usedPackage.Uses | Where-Object Version -eq $olderVersion | ForEach-Object {
                    [PSCustomObject]@{ Graph = $_.Graph; Chain = Find-DependencyChain -Graph $_.Graph -PackageId $usedPackage.PackageId }
                })
                foreach ($holding in @($chains | Group-Object { if ($_.Chain.Holder) { "$($_.Chain.Holder.Name) $($_.Chain.Holder.Version)" } else { "" } })) {
                    $holder = $holding.Group[0].Chain.Holder
                    $cause = if (-not $holder) { $isReferencedDirectlyAtAnOlderVersion = $true; "referenced directly" }
                             elseif ($holder.IsProject) { $isReferencedDirectlyAtAnOlderVersion = $true; "referenced directly by (project) $($holder.Name)" }
                             else { $isHeldBackThroughOtherPackages = $true; $holdersBuiltAgainstAnOlderVersion += $holder; "only through other packages, held there by $($holder.Name) $($holder.Version), which requires it" }
                    "  $olderVersion in $(FormatProjectNames @($holding.Group | ForEach-Object { $_.Graph })): $cause."
                    "    $((@($holding.Group | Sort-Object { $_.Chain.Links.Count }, { $_.Graph.ProjectName }))[0].Chain.Text)"
                }
            }
            if ($isReferencedDirectlyAtAnOlderVersion) {
                "  Raise those direct references to $newerVersion."
            }
            if ($isHeldBackThroughOtherPackages) {
                $centralVersion = GetCentralVersion $usedPackage.PackageId
                if ($usesTransitivePinningEverywhere) {
                    "  No PackageVersion line names it, so transitive pinning can't raise it: add one, at $newerVersion or newer."
                } elseif ($usesCentralPackageManagementEverywhere -and $centralVersion) {
                    "  Transitive pinning would give these projects $centralVersion, the version Directory.Packages.props names."
                } elseif ($usesCentralPackageManagementEverywhere) {
                    "  Add a PackageVersion line for it and turn on transitive pinning, or reference it directly in those projects."
                } else {
                    "  A direct reference to $newerVersion raises it in those projects; central package management with transitive pinning would in all of them."
                }
                foreach ($holder in @($holdersBuiltAgainstAnOlderVersion | Where-Object { $_.Name -like 'Compze.*' } | Sort-Object Name, Version -Unique)) {
                    $notNewest = @()
                    if (-not (TestIsNewestRelease $holder.Name $holder.Version)) { $notNewest += "$($holder.Name) $($holder.Version)" }
                    if (-not (TestIsNewestRelease $usedPackage.PackageId $newerVersion)) { $notNewest += "$($usedPackage.PackageId) $newerVersion" }
                    if ($notNewest.Count -eq 0) {
                        "  Where $($holder.Name) $($holder.Version) runs beside $($usedPackage.PackageId) $newerVersion, it runs against a version it wasn't built against. Compze's release checked that pair: it checks the newest version of every Compze package against the newest of every other, and both are."
                    } else {
                        "  Where $($holder.Name) $($holder.Version) runs beside $($usedPackage.PackageId) $newerVersion, it runs against a version it wasn't built against, and no Compze release has checked that pair: $($notNewest -join ' and ') isn't its package's newest version."
                    }
                }
                "  The projects that resolve an older version build against it: where their own code uses $($usedPackage.PackageId) and runs beside $newerVersion, nothing has checked it."
            }
        }
    }

    function TestIsRaisedByTheUpgrade($Graph, $UsedPackage) {
        # Whether raising the version the solution names for the package raises it in the graph: everywhere it is pinned,
        # and otherwise when the project references it directly, or through a project that does.
        $PackageId = $UsedPackage.PackageId
        if ($UsedPackage.IsPinned) { return $Graph.Packages.ContainsKey($PackageId) }
        if ($Graph.DirectPackageReferences.ContainsKey($PackageId)) { return $true }
        return @($Graph.Projects.Values | Where-Object { $_.Dependencies.ContainsKey($PackageId) }).Count -gt 0
    }

    function WriteUpgrade($UsedPackage, $NewestRelease) {
        $packageId = $UsedPackage.PackageId
        $fromVersion = $UsedPackage.NamedVersions[0]
        $manifest = if ($NewestRelease.Manifest) { $NewestRelease.Manifest } else { Read-NuGetOrgPackageManifest -PackageId $packageId -Version $NewestRelease.Version }
        $raisedGraphs = @($graphs | Where-Object { TestIsRaisedByTheUpgrade $_ $UsedPackage })
        $otherGraphs = @($graphs | Where-Object { $raisedGraphs -notcontains $_ })
        $upgradedAlong = @()
        $addedAlong = @()
        foreach ($dependencyId in $manifest.CompzeDependencyFloors.Keys) {
            $floor = $manifest.CompzeDependencyFloors[$dependencyId]
            $olderVersions = @(SortVersions @($raisedGraphs | Where-Object { $_.Packages.ContainsKey($dependencyId) -and [semver]$_.Packages[$dependencyId].Version -lt [semver]$floor } | ForEach-Object { $_.Packages[$dependencyId].Version }))
            if ($olderVersions.Count -gt 0) {
                $upgradedAlong += [PSCustomObject]@{
                    PackageId                      = $dependencyId
                    FromVersion                    = $olderVersions[0]
                    Floor                          = $floor
                    RaisedGraphsReferencingItOlder = @($raisedGraphs | Where-Object { $_.DirectPackageReferences.ContainsKey($dependencyId) -and [semver]$_.Packages[$dependencyId].Version -lt [semver]$floor })
                    OtherGraphsReferencingItOlder  = @($otherGraphs | Where-Object { $_.DirectPackageReferences.ContainsKey($dependencyId) -and [semver]$_.Packages[$dependencyId].Version -lt [semver]$floor })
                }
            }
            if (@($raisedGraphs | Where-Object { -not $_.Packages.ContainsKey($dependencyId) }).Count -gt 0) {
                $addedAlong += [PSCustomObject]@{ PackageId = $dependencyId; Floor = $floor }
            }
        }
        $upgradedAlongAcrossAMinorVersion = @($upgradedAlong | Where-Object { TestCrossesAMinorVersion $_.FromVersion $_.Floor })

        ""
        "== $packageId $fromVersion -> $($NewestRelease.Version)$(if ($NewestRelease.IsPending) { ' (pending)' }) =="
        if ((-not (TestCrossesAMinorVersion $fromVersion $NewestRelease.Version)) -and $upgradedAlongAcrossAMinorVersion.Count -gt 0) {
            ""
            "A patch upgrade that upgrades other Compze packages across a minor version: $(($upgradedAlongAcrossAMinorVersion | ForEach-Object { $_.PackageId }) -join ', '). Their changes come with it."
        }
        if ($upgradedAlong.Count -gt 0) {
            ""
            "Brings along newer versions of these Compze packages, which it requires, into the projects that reference it directly or through project references:"
            foreach ($dependency in $upgradedAlong) {
                $crossing = if (TestCrossesAMinorVersion $dependency.FromVersion $dependency.Floor) { "across a minor version" } else { "a patch" }
                "  $($dependency.PackageId) $($dependency.FromVersion) -> $($dependency.Floor) or newer: $crossing."
                "    $(DescribeWhatToRaise $dependency $packageId)"
            }
        }
        if ($addedAlong.Count -gt 0) {
            ""
            "Adds these Compze packages to projects that use it and don't use them yet:"
            foreach ($dependency in $addedAlong) { "  $($dependency.PackageId) $($dependency.Floor) or newer" }
        }

        ""
        WriteChanges $packageId $fromVersion $NewestRelease
    }

    function DescribeWhatToRaise($Dependency, [string]$UpgradedPackageId) {
        $centralVersion = GetCentralVersion $Dependency.PackageId
        if ($usesCentralPackageManagementEverywhere -and $centralVersion -and [semver]$centralVersion -lt [semver]$Dependency.Floor) {
            if ($usesTransitivePinningEverywhere) {
                return "Directory.Packages.props pins it at ${centralVersion}: raise that line too, or the restore fails with NuGet's package-downgrade error (NU1109)."
            }
            if ($Dependency.RaisedGraphsReferencingItOlder.Count -gt 0) {
                return "Directory.Packages.props names it at $centralVersion, and projects that use $UpgradedPackageId reference it directly: $(FormatProjectNames $Dependency.RaisedGraphsReferencingItOlder). Raise that line too, or their restore fails with NuGet's package-downgrade error (NU1605)."
            }
            if ($Dependency.OtherGraphsReferencingItOlder.Count -gt 0) {
                return "Directory.Packages.props names it at $centralVersion for projects that don't use ${UpgradedPackageId}: $(FormatProjectNames $Dependency.OtherGraphsReferencingItOlder). Raise that line too: where their code runs beside $UpgradedPackageId, it would run against a newer version than it was built against."
            }
            return "The restore raises it by itself."
        }
        if (-not $usesCentralPackageManagementEverywhere) {
            $texts = @()
            if ($Dependency.RaisedGraphsReferencingItOlder.Count -gt 0) {
                $texts += "Referenced directly at an older version by projects that use ${UpgradedPackageId}: $(FormatProjectNames $Dependency.RaisedGraphsReferencingItOlder). Raise those references too, or their restore fails with NuGet's package-downgrade error (NU1605)."
            }
            if ($Dependency.OtherGraphsReferencingItOlder.Count -gt 0) {
                $texts += "Referenced directly at an older version by projects that don't use ${UpgradedPackageId}: $(FormatProjectNames $Dependency.OtherGraphsReferencingItOlder). Where their code runs beside it, it runs against a newer version than it was built against, so raise those references too."
            }
            if ($texts.Count -gt 0) { return $texts -join ' ' }
        }
        return "The restore raises it by itself."
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
    $restore = Read-SolutionRestoreGraphs -SolutionPath $solutionPath
    $graphs = @($restore.Graphs)
    $usesCentralPackageManagementEverywhere = @($graphs | Where-Object { -not $_.UsesCentralPackageManagement }).Count -eq 0
    $usesTransitivePinningEverywhere = @($graphs | Where-Object { -not $_.UsesTransitivePinning }).Count -eq 0
    $usedPackages = @(GetCompzePackagesUsed)
    if ($usedPackages.Count -eq 0) {
        "$solutionPath uses no Compze packages."
        return
    }

    $fetchResult = git -C $script:CompzeRoot fetch --all --tags --quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not fetch the Compze checkout's latest release tags, so the changelogs of the newest releases may be missing from the report.`n$fetchResult"
    }

    $pendingReleaseFeedPackages = @(Get-PendingReleaseFeedPackages)
    $versionsOnNuGetOrg = Get-NuGetOrgRestorableVersions -PackageId @($usedPackages | ForEach-Object { $_.PackageId })
    $newestReleases = @{}
    foreach ($usedPackage in $usedPackages) {
        $newestRelease = FindNewestRelease $usedPackage.PackageId
        if ($newestRelease) { $newestReleases[$usedPackage.PackageId] = $newestRelease }
    }
    $upgrades = @($usedPackages | Where-Object { $_.IsNamedBySolution -and $newestReleases.ContainsKey($_.PackageId) -and [semver]$newestReleases[$_.PackageId].Version -gt [semver]$_.NamedVersions[0] })
    $upgradedPackageIds = @($upgrades | ForEach-Object { $_.PackageId })

    WriteOverview
    WriteHowTheSolutionSetsItsVersions
    WriteDifferentVersions
    foreach ($upgradedPackage in $upgrades) { WriteUpgrade $upgradedPackage $newestReleases[$upgradedPackage.PackageId] }
}
