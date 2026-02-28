# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-SimulateRelease {
    <#
    .SYNOPSIS
    Simulates a release without publishing anything

    .DESCRIPTION
    Packs all projects with real versions (C-Pack -CI), then runs the publish script
    in WhatIf mode. This shows exactly what would happen during a real release:
    which packages are new, their changelog entries, NuGet pushes, GitHub releases,
    and git tags — without actually doing any of it.

    Use -NoBuild to skip building when only iterating on changelogs or versions.
    Use -NoPack to skip both building and packing, using existing nupkg files.
    Use -Verbose to show all details (NuGet push, assets, tags).

    .EXAMPLE
    C-SimulateRelease

    .EXAMPLE
    C-SimulateRelease -NoBuild
    Skips building, just repacks and checks changelogs.

    .EXAMPLE
    C-SimulateRelease -NoPack
    Skips building and packing, just checks existing packages against changelogs.

    .EXAMPLE
    C-SimulateRelease -Verbose
    Shows full details including NuGet push targets, assets, and git tags.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild,
        [switch]$NoPack
    )

    if (-not $NoPack) {
        C-Pack -CI -NoBuild:$NoBuild
        if ($global:LASTEXITCODE -ne 0) { return }
    }

    $verboseFlag = @()
    if ($VerbosePreference -eq 'Continue') { $verboseFlag = @('-Verbose') }
    & "$script:CompzeRoot/.github/scripts/Publish-NuGetWithReleases.ps1" -DryRun @verboseFlag

    $global:LASTEXITCODE = 0
}
