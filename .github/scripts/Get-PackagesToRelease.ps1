# Lists the packages a release would publish from the packed nupkg files: the same selection
# Publish-NuGetWithReleases.ps1 makes (see lib/ReleaseSelection.ps1). Used by C-Publish-ToPendingReleaseFeed.
#
# Prerequisites: run from the repository root, after C-Pack -CI.
# Exits 1 when a package to release has no changelog entry for its version, 0 otherwise. A script rather than a library:
# the lib functions exit on failure, which ends only this script when it is invoked, but would end an interactive
# PowerShell session if a DevScripts command dot-sourced them.
#
# Usage:
#   & .github/scripts/Get-PackagesToRelease.ps1 -NupkgsPath nupkgs

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

GetPackagesToRelease @(GetPublishablePackages @(GetPackagesFromNupkgFiles $NupkgsPath))
# Explicit, because the last native command run above is a `git rev-parse` probing for a tag, which fails for every
# unreleased version and would otherwise leave this script's exit code at 1.
exit 0
