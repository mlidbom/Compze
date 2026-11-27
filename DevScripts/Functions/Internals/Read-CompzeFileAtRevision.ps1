# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-CompzeFileAtRevision {
    <#
    .SYNOPSIS
    Reads a file of this Compze checkout as it was at a revision: a release tag or a commit

    .DESCRIPTION
    One file of what Read-CompzeFilesAtRevision reads, which says how.

    .PARAMETER Revision
    A release tag, such as Compze.Tessaging/v0.6.1-alpha, or a commit.

    .PARAMETER Path
    The file's path from the repository root, such as src/Compze.Tessaging/CHANGELOG.md.

    .OUTPUTS
    The file's lines; $null when the revision doesn't exist in this checkout or has no such file.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Revision,
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    return (Read-CompzeFilesAtRevision -Revision $Revision -Path @($Path))[$Path]
}
