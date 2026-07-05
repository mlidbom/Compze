# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-SimulateRelease {
    <#
    .SYNOPSIS
    Simulates a release without publishing anything

    .DESCRIPTION
    Packs all projects with real versions (C-Pack -CI), then runs the publish script
    in dry-run mode. This shows exactly what would happen during a real release:
    which packages are new, their changelog entries, NuGet pushes, GitHub releases,
    and git tags — without actually doing any of it.

    Release detection and the summary table are driven entirely by local git tags, so the
    run first fetches every remote's tags (git fetch --all --tags). Without this, a clone
    whose tags lag behind the remote reports already-released packages as pending. If the
    fetch fails (e.g. offline) the run continues with a warning that tags may be stale.

    Use -NoBuild to skip building when only iterating on changelogs or versions.
    Use -NoPack to skip both building and packing, using existing nupkg files.
    Use -NoSummary to skip the end-of-run summary table (which queries nuget.org).
    Use -Verbose to show all details (NuGet push, assets, tags).

    The run ends with a summary table listing every package whose local version
    differs from its latest git tag or its latest version on nuget.org — useful
    for spotting failed publishes (TAG MISSING / NUGET MISSING) as well as the
    normal PENDING set.

    .EXAMPLE
    C-SimulateRelease

    .EXAMPLE
    C-SimulateRelease -NoBuild
    Skips building, just repacks and checks changelogs.

    .EXAMPLE
    C-SimulateRelease -NoPack
    Skips building and packing, just checks existing packages against changelogs.

    .EXAMPLE
    C-SimulateRelease -NoSummary
    Skips the nuget.org lookup at the end (faster, works offline).

    .EXAMPLE
    C-SimulateRelease -Verbose
    Shows full details including NuGet push targets, assets, and git tags.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild,
        [switch]$NoPack,
        [switch]$NoSummary
    )

    # Release detection (PackagesWithNoMatchingReleaseTag) and the summary table
    # (GetLatestTagVersion) read only local tags, so a clone that lags the remote reports
    # already-released packages as pending. Refresh every remote's tags first so the run
    # reflects the true published state. No --prune-tags: it would delete a local-only tag
    # left by a real release that crashed before pushing — exactly the drift the summary
    # report is meant to surface. A failed fetch (e.g. offline) warns but does not abort.
    $fetchResult = git fetch --all --tags --quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not fetch the latest tags — the release simulation may be based on stale tags.`n$fetchResult"
    }

    if (-not $NoPack) {
        C-Pack -CI -NoBuild:$NoBuild
        if ($global:LASTEXITCODE -ne 0) { return }
    }

    $extraParams = @{}
    if ($VerbosePreference -eq 'Continue') { $extraParams.Verbose = $true }
    if ($NoSummary) { $extraParams.NoSummary = $true }
    & "$script:CompzeRoot/.github/scripts/Publish-NuGetWithReleases.ps1" -DryRun @extraParams

    $global:LASTEXITCODE = 0
}
