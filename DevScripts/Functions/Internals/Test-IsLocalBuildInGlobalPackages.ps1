# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Test-IsLocalBuildInGlobalPackages {
    <#
    .SYNOPSIS
    Whether the NuGet global packages folder holds a local build of the version: one extracted from a folder rather than from nuget.org
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
    return [bool]($extractedFrom -and $extractedFrom -notlike "https://api.nuget.org/*")
}
