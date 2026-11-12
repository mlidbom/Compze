# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-PackageManifest {
    <#
    .SYNOPSIS
    Reads a package file's id, version, the commit it was built from and its Compze dependency floors

    .DESCRIPTION
    For the package files this machine packs, which all record their commit: one that does not is refused, because
    whether a published build is still the code that ships rests on knowing which code it is.

    The implementation is the release scripts' own (.github/scripts/lib/PackageManifests.ps1), which says what a
    dependency floor is.

    .OUTPUTS
    Object with PackageId, Version, RepositoryCommit and CompzeDependencyFloors (Compze package id to floor)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$NupkgPath
    )

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/PackageManifests.ps1")
    $packageManifest = ReadNupkgManifest $NupkgPath
    if (-not $packageManifest.RepositoryCommit) {
        throw "$NupkgPath records no repository commit, so nothing can tell which code it was built from."
    }
    return $packageManifest
}
