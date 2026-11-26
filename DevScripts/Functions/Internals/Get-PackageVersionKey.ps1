# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-PackageVersionKey {
    <#
    .SYNOPSIS
    A package version as the key of the hashtable Restore-NuGetOrgBuilds returns: "<package id>/<version>", lowercase as NuGet compares them
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    return "$PackageId/$Version".ToLowerInvariant()
}
