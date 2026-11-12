# Publishes NuGet packages and creates GitHub releases for any newly published versions.
#
# Prerequisites: Run C-Pack -CI first to create nupkg files in nupkgs/
#
# Sequence:
#   1. Detect which packages are new by checking for existing git tags ({PackageName}/v{Version})
#   2. Validate that every new package has a CHANGELOG.md entry for its version
#   3. Run the post-release binding check (Invoke-PostReleaseBindingCheck.ps1): the new packages and nuget.org's newest
#      version of every other package must not fail at runtime against each other
#   4. For each new package: push to NuGet, create GitHub release, create and push git tag
#
# Environment variables (set by the GitHub Actions workflow):
#   NUGET_API_KEY  — short-lived nuget.org API key from the workflow's Trusted Publishing login step
#   GH_TOKEN       — GitHub token for gh CLI (release creation)
#
# Usage:
#   ./Publish-NuGetWithReleases.ps1          # Publish for real
#   ./Publish-NuGetWithReleases.ps1 -DryRun  # Dry run — shows what would happen

param(
    [switch]$DryRun,
    [switch]$Verbose,
    [switch]$NoSummary
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = $PSScriptRoot
. "$scriptDir/lib/GitTagging.ps1"
. "$scriptDir/lib/NupkgParsing.ps1"
. "$scriptDir/lib/ReleaseDetails.ps1"
. "$scriptDir/lib/ReleaseSelection.ps1"
. "$scriptDir/lib/NuGetPublishing.ps1"
. "$scriptDir/lib/GitHubReleases.ps1"
. "$scriptDir/lib/NuGetOrgVersions.ps1"
. "$scriptDir/lib/SummaryReport.ps1"

# ── Main ──

if (-not $DryRun) {
    if (-not $env:NUGET_API_KEY) {
        Write-Error "NUGET_API_KEY environment variable is not set"
        exit 1
    }
    if (-not $env:GH_TOKEN) {
        Write-Error "GH_TOKEN environment variable is not set"
        exit 1
    }
}

$nupkgsPath = "nupkgs"

# @(...) keeps each result a collection: PowerShell unwraps an empty array to $null on assignment,
# and under Set-StrictMode the later `.Count` access throws on $null. Forcing array context keeps
# `.Count` valid whether 0, 1, or many packages match.
$allPackages = @(GetPublishablePackages @(GetPackagesFromNupkgFiles $nupkgsPath))
$newPackages = @(GetPackagesToRelease $allPackages)

if ($newPackages.Count -eq 0) {
    Write-Host "`nNo new packages to release."
} else {
    Write-Host "`nNew packages to release:"
    foreach ($pkg in $newPackages) {
        Write-Host "  $($pkg.PackageName) v$($pkg.Version)"
    }

    & "$scriptDir/Invoke-PostReleaseBindingCheck.ps1" -NupkgsPath $nupkgsPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "The post-release binding check failed (see above), so nothing was published."
        exit 1
    }

    Write-Host ""
    foreach ($pkg in $newPackages) {
        Write-Host "── $($pkg.PackageName) v$($pkg.Version) ──"
        PushToNuGet $pkg $nupkgsPath -DryRun:$DryRun -Verbose:$Verbose
        CreateGitHubRelease $pkg $nupkgsPath -DryRun:$DryRun -Verbose:$Verbose
        Write-Host ""
    }

    Write-Host "Done! Published $($newPackages.Count) package(s)."
}

if (-not $NoSummary) {
    $changelogMap = @{}
    foreach ($pkg in $newPackages) {
        $changelogMap[$pkg.PackageName] = $pkg.ChangelogContent
    }
    WriteSummaryReport $allPackages $changelogMap
}
