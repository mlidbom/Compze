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
