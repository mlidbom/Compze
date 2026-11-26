# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Switch-ReleasedLocalBuildToNuGetOrgs {
    <#
    .SYNOPSIS
    Puts nuget.org's build of a released version in the place of its local build in the NuGet global packages folder; returns whether no local build remains there

    .DESCRIPTION
    Leaves an entry that came from nuget.org alone, and an absent one is already clear.

    The local build is never just deleted. A build between its restore and its compile has resolved the entry's files and
    not yet opened them, so nothing refuses the deletion, and the compile then finds them gone: CS0006, in a publish that
    a session's start cleared Compze.Internals.SystemCE 0.11.0-internal under (2026-11-26). Instead nuget.org's build,
    which Restore-NuGetOrgBuilds restored beside the global packages folder, is swapped in, two renames apart: a build in
    that moment reads nuget.org's build of the same version at the same paths.

    When the local build is in use, open or memory-mapped by a build server, the IDE or NCrunch, Windows refuses the
    rename and nothing changes. Then, and when nuget.org's build wasn't restored, this warns and returns $false, so the
    caller leaves the rest of the version's cleanup for a later run.

    .PARAMETER NuGetOrgBuilds
    What Restore-NuGetOrgBuilds returned for the versions that include this one.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesDirectory,
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [hashtable]$NuGetOrgBuilds
    )

    if (-not (Test-IsLocalBuildInGlobalPackages -GlobalPackagesDirectory $GlobalPackagesDirectory -PackageId $PackageId -Version $Version)) { return $true }

    $nuGetOrgBuild = $NuGetOrgBuilds[(Get-PackageVersionKey -PackageId $PackageId -Version $Version)]
    if (-not $nuGetOrgBuild) {
        Write-Warning ("$PackageId $Version is on nuget.org now, but restoring nuget.org's build of it failed, so its local build in the NuGet global packages folder is left in place for now: " +
                       "C-Clear-ReleasedLocalBuilds tries again.")
        return $false
    }
    if (Switch-NuGetGlobalPackagesEntry -GlobalPackagesDirectory $GlobalPackagesDirectory -PackageId $PackageId -Version $Version -By $nuGetOrgBuild) { return $true }

    Write-Warning ("$PackageId $Version is on nuget.org now, but its local build in the NuGet global packages folder is in use, so it is left in place for now. " +
                   "Run 'dotnet build-server shutdown' (and restart the IDE or NCrunch if they hold it), then C-Clear-ReleasedLocalBuilds.")
    return $false
}
