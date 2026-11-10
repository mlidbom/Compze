# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-ReleasedLocalBuildFromGlobalPackages {
    <#
    .SYNOPSIS
    Removes the local build of a version nuget.org has released from the NuGet global packages folder; returns whether no local build remains there

    .DESCRIPTION
    Leaves an entry that came from nuget.org alone, and an absent one is already clear. When the local build is in use
    (see Remove-NuGetGlobalPackagesEntry), warns and returns $false, so the caller leaves the rest of the version's
    cleanup for a later run.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesDirectory,
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $extractedFrom = Get-NuGetGlobalPackagesEntrySource -GlobalPackagesDirectory $GlobalPackagesDirectory -PackageId $PackageId -Version $Version
    $isLocalBuild = $extractedFrom -and $extractedFrom -notlike "https://api.nuget.org/*"
    if (-not $isLocalBuild) { return $true }
    if (Remove-NuGetGlobalPackagesEntry -GlobalPackagesDirectory $GlobalPackagesDirectory -PackageId $PackageId -Version $Version) { return $true }

    Write-Warning ("$PackageId $Version is on nuget.org now, but its local build in the NuGet global packages folder is in use, so it is left in place for now. " +
                   "Run 'dotnet build-server shutdown' (and restart the IDE or NCrunch if they hold it), then C-Clear-ReleasedLocalBuilds.")
    return $false
}
