# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-SimulateRelease {
    <#
    .SYNOPSIS
    Simulates a release without publishing anything

    .DESCRIPTION
    Packs all projects with real versions (C-Pack -CI), then runs the publish script
    in dry-run mode. This shows exactly what would happen during a real release:
    which packages are new, their changelog entries, the post-release binding check,
    NuGet pushes, GitHub releases, and git tags — without actually doing any of it.

    The post-release binding check (.github/scripts/PostReleaseBindingCheck/README.md)
    stops the run, as it would stop the release, when the new packages and nuget.org's
    newest version of the other packages would fail at runtime against each other.

    It refuses, before packing, as the release refuses, when a package that the release
    doesn't release, its version having a release tag, has shipped files that changed since
    that tag: its project folder's files but its CHANGELOG.md and its _docs and dev_docs
    folders. The release would leave such a package at its released version, and a package
    released beside it that needs what changed would fail against it. The refusal names each
    package and its changed files. Either give the package a changelog entry and run
    C-Prepare-Release, so that it is released, or, when its changes ship nothing a consumer
    sees, acknowledge them with C-Acknowledge-UnreleasedChanges.

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

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/UnreleasedChanges.ps1")

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

    $unacknowledgedUnreleasedChanges = @(GetUnacknowledgedUnreleasedChanges)
    if ($unacknowledgedUnreleasedChanges.Count -gt 0) { throw "C-SimulateRelease refuses: $(FormatUnreleasedChangesRefusal $unacknowledgedUnreleasedChanges)" }

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
