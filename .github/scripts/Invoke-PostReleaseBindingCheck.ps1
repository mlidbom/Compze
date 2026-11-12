# The post-release binding check (see PostReleaseBindingCheck/README.md): fails when, once this release is out, the Compze
# packages nuget.org serves would fail at runtime against each other. It checks the release's own build of each package
# it publishes together with nuget.org's newest version of every other package.
#
# Used by Publish-NuGetWithReleases.ps1 (so by the Create Releases workflow and C-SimulateRelease), and by
# C-Publish-ToPendingReleaseFeed, before either publishes anything.
#
# Prerequisites: run from the repository root, after C-Pack -CI.
# Exits 1 when the packages would fail against each other, printing why. Silent, with exit code 0, when they bind or when
# the release publishes nothing. A script rather than a library, for the reason Get-PackagesToRelease.ps1 gives.
#
# Usage:
#   & .github/scripts/Invoke-PostReleaseBindingCheck.ps1 -NupkgsPath nupkgs

param(
    [Parameter(Mandatory)][string]$NupkgsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
. "$scriptDir/lib/GitTagging.ps1"
. "$scriptDir/lib/NupkgParsing.ps1"
. "$scriptDir/lib/ReleaseDetails.ps1"
. "$scriptDir/lib/ReleaseSelection.ps1"
. "$scriptDir/lib/NuGetOrgVersions.ps1"

function GetNuGetOrgNewestVersions($PackagesTheReleaseLeavesAsTheyAre) {
    # For each package, the newest version nuget.org serves: what a consumer gets. A package with a release tag that
    # nuget.org has no version of is a failed publish, which the release summary reports as NUGET MISSING; it can't be
    # checked, so it's left out with a warning.
    $versionsByPackageName = GetNuGetOrgRestorableVersionsMap @($PackagesTheReleaseLeavesAsTheyAre | ForEach-Object { $_.PackageName })
    foreach ($package in $PackagesTheReleaseLeavesAsTheyAre) {
        $versions = @($versionsByPackageName[$package.PackageName])
        if ($versions.Count -eq 0) {
            Write-Warning "Post-release binding check: $($package.PackageName) has a release tag, but nuget.org has no version of it, so the check leaves it out."
            continue
        }
        [PSCustomObject]@{ PackageName = $package.PackageName; Version = $versions[-1] }
    }
}

function SaveNuGetOrgNupkgs($Packages, [string]$Directory) {
    # Downloads each package's nupkg from nuget.org's package content index, where restore downloads it from. Returns the paths.
    $downloads = @($Packages | ForEach-Object {
        $id = $_.PackageName.ToLowerInvariant()
        $version = $_.Version.ToLowerInvariant()
        [PSCustomObject]@{
            Uri  = "https://api.nuget.org/v3-flatcontainer/$id/$version/$id.$version.nupkg"
            Path = Join-Path $Directory "$id.$version.nupkg"
        }
    })
    $downloads | ForEach-Object -Parallel {
        $ErrorActionPreference = 'Stop'
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $_.Uri -OutFile $_.Path -TimeoutSec 60
    } -ThrottleLimit 20
    return @($downloads | ForEach-Object { $_.Path })
}

$publishablePackages = @(GetPublishablePackages @(GetPackagesFromNupkgFiles $NupkgsPath))
$packagesToRelease = @(GetPackagesToRelease $publishablePackages)
if ($packagesToRelease.Count -eq 0) { exit 0 }

$namesOfPackagesToRelease = @($packagesToRelease | ForEach-Object { $_.PackageName })
$packagesTheReleaseLeavesAsTheyAre = @($publishablePackages | Where-Object { $namesOfPackagesToRelease -notcontains $_.PackageName })

$downloadDirectory = Join-Path ([IO.Path]::GetTempPath()) "compze-post-release-binding-check-$([guid]::NewGuid())"
New-Item -ItemType Directory -Path $downloadDirectory | Out-Null
try {
    $nuGetOrgNupkgs = @(SaveNuGetOrgNupkgs @(GetNuGetOrgNewestVersions $packagesTheReleaseLeavesAsTheyAre) $downloadDirectory)
    $thisReleaseNupkgs = @($packagesToRelease | ForEach-Object { Join-Path $NupkgsPath "$($_.PackageName).$($_.Version).nupkg" })

    & dotnet run --project (Join-Path $scriptDir "PostReleaseBindingCheck") --configuration Release --verbosity quiet -- --this-release @thisReleaseNupkgs --on-nuget-org @nuGetOrgNupkgs
    $checkExitCode = $LASTEXITCODE
} finally {
    Remove-Item -LiteralPath $downloadDirectory -Recurse -Force
}
exit $checkExitCode
