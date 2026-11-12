# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-CompzePackagesASolutionUses {
    <#
    .SYNOPSIS
    Gets the Compze packages a solution uses, directly or through other packages, and the versions its restore resolves

    .DESCRIPTION
    Restores the solution first, since what a restore resolved is what it reads (dotnet list package). Works whether the
    solution's projects name their package versions themselves or through central package management.

    .PARAMETER SolutionPath
    The solution file.

    .OUTPUTS
    For each Compze package, ordered by id, an object with:
    - PackageId
    - Uses: for each project that uses it, an object with ProjectName, Version (the version its restore resolved) and
      IsDirectReference.
    - ResolvedVersions: the versions the solution's projects resolve, oldest first. Usually one; more when projects
      reference different versions, or get it through packages that require different versions.
    - OldestResolvedVersion
    - DirectlyReferencingProjects: the names of the projects that reference it directly; none when the solution gets it
      only through other packages.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )

    $restoreOutput = dotnet restore $SolutionPath --verbosity quiet 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Restoring $SolutionPath failed:`n$($restoreOutput -join "`n")" }

    $listingJson = dotnet list $SolutionPath package --include-transitive --format json
    if ($LASTEXITCODE -ne 0) { throw "Listing the packages of $SolutionPath failed:`n$($listingJson -join "`n")" }
    $listing = ($listingJson -join "`n") | ConvertFrom-Json
    if ($listing.PSObject.Properties['problems'] -and @($listing.problems).Count -gt 0) {
        throw "Listing the packages of $SolutionPath reported problems, so it would leave packages out:`n$((@($listing.problems) | ForEach-Object { "$($_.project): $($_.text)" }) -join "`n")"
    }

    $usesByPackageId = @{}
    foreach ($project in $listing.projects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project.path)
        foreach ($framework in $project.frameworks) {
            $directReferences = @($framework.topLevelPackages | Where-Object { $_ })
            $packagesFromOtherPackages = @($framework.transitivePackages | Where-Object { $_ })
            foreach ($package in @($directReferences + $packagesFromOtherPackages | Where-Object { $_.id -like 'Compze.*' })) {
                if (-not $usesByPackageId.ContainsKey($package.id)) { $usesByPackageId[$package.id] = @() }
                $usesByPackageId[$package.id] += [PSCustomObject]@{
                    ProjectName       = $projectName
                    Version           = $package.resolvedVersion
                    IsDirectReference = $directReferences -contains $package
                }
            }
        }
    }

    foreach ($packageId in @($usesByPackageId.Keys | Sort-Object)) {
        $uses = @($usesByPackageId[$packageId])
        $resolvedVersions = @($uses | ForEach-Object { $_.Version } | Sort-Object -Unique | Sort-Object { [semver]$_ })
        [PSCustomObject]@{
            PackageId                   = $packageId
            Uses                        = $uses
            ResolvedVersions            = $resolvedVersions
            OldestResolvedVersion       = $resolvedVersions[0]
            DirectlyReferencingProjects = @($uses | Where-Object IsDirectReference | ForEach-Object { $_.ProjectName } | Sort-Object -Unique)
        }
    }
}
