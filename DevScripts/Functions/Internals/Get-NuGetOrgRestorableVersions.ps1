# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-NuGetOrgRestorableVersions {
    <#
    .SYNOPSIS
    Gets, for each package id, the versions a restore can download from nuget.org right now

    .DESCRIPTION
    Answers "has nuget.org released this version yet": a freshly pushed version appears only once nuget.org has validated
    and indexed it. Unlike the registration API (Get-NuGetPackageVersionListings), which answers listed versus unlisted.

    The implementation is the release scripts' own (.github/scripts/lib/NuGetOrgVersions.ps1), shared with the release
    summary so the two never disagree about what nuget.org has.

    .PARAMETER PackageId
    The package ids to query. Queried in parallel.

    .OUTPUTS
    Hashtable from package id to its versions, normalized and lowercase as NuGet compares them; an empty array when
    nuget.org has no such package.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$PackageId
    )

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/NuGetOrgVersions.ps1")
    return GetNuGetOrgRestorableVersionsMap $PackageId
}
