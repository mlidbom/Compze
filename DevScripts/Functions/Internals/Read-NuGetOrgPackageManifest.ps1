# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-NuGetOrgPackageManifest {
    <#
    .SYNOPSIS
    Reads the id, version, build commit and Compze dependency floors of the package nuget.org serves for one version

    .DESCRIPTION
    The implementation is the release scripts' own (.github/scripts/lib/PackageManifests.ps1), which says what a
    dependency floor is.

    .OUTPUTS
    Object with PackageId, Version, RepositoryCommit ($null for an older release that predates the commit stamp) and
    CompzeDependencyFloors (Compze package id to floor)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/PackageManifests.ps1")
    return ReadNuGetOrgPackageManifest $PackageId $Version
}
