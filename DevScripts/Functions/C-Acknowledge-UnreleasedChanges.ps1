# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Acknowledge-UnreleasedChanges {
    <#
    .SYNOPSIS
    Records that packages' changes since their release ship nothing a consumer sees, so a release may leave them at their released version

    .DESCRIPTION
    C-Prepare-Release, C-SimulateRelease and the Create Releases workflow refuse while a package that the release doesn't
    release, its version having a release tag, has shipped files that changed since that tag: its project folder's files
    but its CHANGELOG.md and its _docs and dev_docs folders. A change that ships nothing a consumer sees, such as comments
    only, or build settings that produce the same result, needs no release. Check it against
    git diff <release tag> HEAD -- src/<PackageId>, then acknowledge it with this command.

    It records each package in .github/scripts/AcknowledgedUnreleasedChanges.txt with HEAD's commit and the reason, replacing
    any line the package had. Commit the file with the release. The acknowledgement lasts until the package is next
    released, since only the changes after that commit count: C-Prepare-Release removes the line when it prepares the
    package's release.

    It only records: it prepares and checks nothing. Run C-Prepare-Release or C-SimulateRelease after it.

    .PARAMETER PackageId
    The packages whose changes since their release, up to HEAD, ship nothing a consumer sees.

    .PARAMETER Reason
    Why their changes ship nothing a consumer sees: one line.

    .EXAMPLE
    C-Acknowledge-UnreleasedChanges Compze.Sql.Sqlite -Reason 'An InternalsVisibleTo for a test project.'

    .EXAMPLE
    C-Acknowledge-UnreleasedChanges Compze.Must, Compze.DbPool -Reason 'Comments only.'
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory, Position = 0)]
        [string[]]$PackageId,
        [Parameter(Mandatory)]
        [string]$Reason
    )

    $ErrorActionPreference = 'Stop'

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/UnreleasedChanges.ps1")
    AcknowledgeUnreleasedChanges $PackageId $Reason
}
