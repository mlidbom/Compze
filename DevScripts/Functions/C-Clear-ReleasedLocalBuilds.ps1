# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Clear-ReleasedLocalBuilds {
    <#
    .SYNOPSIS
    Removes every local build of a Compze package version nuget.org has released, from the pending-release feed and from the NuGet global packages folder

    .DESCRIPTION
    A local build is a package packed on this machine rather than by the release workflow: a version published to the
    pending-release feed (C-Publish-ToPendingReleaseFeed), or one a Compze restore extracted from nupkgs/ after
    C-Pack -CI or C-SimulateRelease packed the real versions. Once nuget.org has released the version, this machine
    should run nuget.org's build like every other machine does. But the NuGet global packages folder keeps serving the
    first copy of a version any restore extracted, so without this the local build would stay in use for good: the same
    version number with different content, as when a Compze package seemed to be built from the wrong commit
    (2026-07-25).

    For each released version this:
    - removes the local build from the global packages folder (an entry whose .nupkg.metadata names a local folder as
      its source), so the next restore downloads nuget.org's;
    - removes the version from the pending-release feed, so no restore can extract the local build again;
    - warns when the feed's build came from a commit whose src/<PackageId> differs from that of nuget.org's build:
      then what ran on this machine was not what shipped. (Older releases record no commit; then it says it cannot tell.)

    A local build in use (open or memory-mapped by a build server, the IDE or NCrunch) is left entirely as it is, and
    reported, to be cleared on a later run.

    Also reports each version in the pending-release feed that nuget.org has not released yet. Writes nothing when
    there is nothing to clear or report, so a user-level SessionStart hook runs it in every Claude session on this machine.

    .EXAMPLE
    C-Clear-ReleasedLocalBuilds
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    # Stop at the first error, inherited by every function this calls: a failed lookup must never read as
    # "nuget.org has not released it" or "has released it", and carry on deleting or publishing on that misreading.
    $ErrorActionPreference = 'Stop'

    $globalPackagesDirectory = Get-NuGetGlobalPackagesDirectory
    $feedPackages = @(Get-PendingReleaseFeedPackages)
    $feedVersions = @($feedPackages | ForEach-Object { "$($_.PackageId)/$($_.Version)".ToLowerInvariant() })
    $otherLocalBuilds = @(Get-CompzeLocalBuildsInGlobalPackages -GlobalPackagesDirectory $globalPackagesDirectory |
                          Where-Object { $feedVersions -notcontains "$($_.PackageId)/$($_.Version)".ToLowerInvariant() })
    if ($feedPackages.Count -eq 0 -and $otherLocalBuilds.Count -eq 0) { return }

    $restorableOnNuGetOrg = Get-NuGetOrgRestorableVersions -PackageId @(@($feedPackages) + @($otherLocalBuilds) | ForEach-Object { $_.PackageId })

    foreach ($package in $feedPackages) {
        $packageVersion = "$($package.PackageId) $($package.Version)"
        if ($restorableOnNuGetOrg[$package.PackageId] -notcontains $package.Version.ToLowerInvariant()) {
            Write-Host ("$packageVersion is in the pending-release feed (built from $($package.RepositoryCommit.Substring(0, 9))) and not yet on nuget.org: " +
                        "this machine restores the local build, and a commit referencing it fails CI elsewhere until nuget.org has it.")
            continue
        }

        $releasedFrom = (Read-NuGetOrgPackageManifest -PackageId $package.PackageId -Version $package.Version).RepositoryCommit
        if (-not $releasedFrom) {
            Write-Warning "$packageVersion on nuget.org records no build commit, so whether the local build run on this machine matched it cannot be checked."
        } elseif (Test-PackageSourceChangedBetweenCommits -PackageId $package.PackageId -FromCommit $package.RepositoryCommit -ToCommit $releasedFrom) {
            Write-Warning ("$packageVersion as run on this machine was built from $($package.RepositoryCommit.Substring(0, 9)), but nuget.org's from $($releasedFrom.Substring(0, 9)), " +
                           "and src/$($package.PackageId) differs between them: what was tested here against the local build is not what shipped.")
        }
        if (-not (Remove-ReleasedLocalBuildFromGlobalPackages -GlobalPackagesDirectory $globalPackagesDirectory -PackageId $package.PackageId -Version $package.Version)) { continue }
        Remove-Item -LiteralPath $package.NupkgPath
        Write-Host "$packageVersion is on nuget.org: removed its local build from the pending-release feed and the NuGet global packages folder, so the next restore downloads nuget.org's."
    }

    foreach ($localBuild in $otherLocalBuilds) {
        if ($restorableOnNuGetOrg[$localBuild.PackageId] -notcontains $localBuild.Version.ToLowerInvariant()) { continue }
        if (-not (Remove-ReleasedLocalBuildFromGlobalPackages -GlobalPackagesDirectory $globalPackagesDirectory -PackageId $localBuild.PackageId -Version $localBuild.Version)) { continue }
        Write-Host ("$($localBuild.PackageId) $($localBuild.Version) is on nuget.org, but the NuGet global packages folder held a local build of it (extracted from $($localBuild.Source)): " +
                    "removed it, so the next restore downloads nuget.org's.")
    }
}
