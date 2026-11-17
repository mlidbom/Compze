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

    dotnet flexref sync $script:CompzeRoot
}
