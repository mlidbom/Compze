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

    .EXAMPLE
    C-SimulateRelease
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param()

    C-Pack -CI
    if ($global:LASTEXITCODE -ne 0) { return }

    & "$script:CompzeRoot/.github/scripts/Publish-NuGetWithReleases.ps1" -WhatIf

    $global:LASTEXITCODE = 0
}
