# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-FlexRef-Sync {
    <#
    .SYNOPSIS
    Syncs FlexRef infrastructure for all solutions

    .DESCRIPTION
    Thin wrapper around 'dotnet flexref sync'. Updates Directory.Build.props,
    all .csproj flex references, and NCrunch .v3.ncrunchsolution files.

    .EXAMPLE
    C-FlexRef-Sync
    Syncs all FlexRef-managed files
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    # The tool is built for an older runtime, and on it MSBuildLocator finds no SDK as new as the one global.json pins
    # ("No instances of MSBuild could be detected"). Rolling it forward to the newest runtime lets it load that SDK.
    $previousRollForward = $env:DOTNET_ROLL_FORWARD
    $env:DOTNET_ROLL_FORWARD = 'LatestMajor'
    try { dotnet flexref sync $script:CompzeRoot }
    finally { $env:DOTNET_ROLL_FORWARD = $previousRollForward }
}
