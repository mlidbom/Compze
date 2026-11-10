# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-NuGetOrgRestorableVersions {
    <#
    .SYNOPSIS
    Gets, for each package id, the versions a restore can download from nuget.org right now

    .DESCRIPTION
    Reads nuget.org's package content index (the v3 flat container), which is where restore itself downloads from.
    A freshly pushed version appears there only once nuget.org has validated and indexed it: typically 5 to 20 minutes
    after the push, and documented as up to an hour. So this answers "has nuget.org released it yet", unlike the
    registration API (Get-NuGetPackageVersionListings), which answers listed versus unlisted.

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

    $results = $PackageId | Sort-Object -Unique | ForEach-Object -Parallel {
        $ProgressPreference = 'SilentlyContinue'
        $packageId = $_
        $response = Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/index.json" -SkipHttpErrorCheck -TimeoutSec 30
        if ($response.StatusCode -eq 404) { return [PSCustomObject]@{ PackageId = $packageId; Versions = @() } }
        if ($response.StatusCode -ne 200) { throw "nuget.org returned $($response.StatusCode) for $packageId" }
        [PSCustomObject]@{ PackageId = $packageId; Versions = @(($response.Content | ConvertFrom-Json).versions) }
    } -ThrottleLimit 20

    $versionsByPackageId = @{}
    foreach ($result in $results) { $versionsByPackageId[$result.PackageId] = $result.Versions }
    return $versionsByPackageId
}
