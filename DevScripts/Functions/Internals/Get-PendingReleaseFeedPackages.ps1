# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-PendingReleaseFeedPackages {
    <#
    .SYNOPSIS
    Gets the package versions in this machine's pending-release feed (see Get-PendingReleaseFeedDirectory)

    .OUTPUTS
    Objects with PackageId, Version, RepositoryCommit (the commit the build came from) and NupkgPath; none when the feed
    is empty or this machine never published to it.
    #>
    [CmdletBinding()]
    param()

    $feedDirectory = Get-PendingReleaseFeedDirectory
    if (-not (Test-Path -LiteralPath $feedDirectory)) { return }
    Get-ChildItem -LiteralPath $feedDirectory -Filter "*.nupkg" | ForEach-Object {
        Read-PackageManifest -NupkgPath $_.FullName | Add-Member -NotePropertyName NupkgPath -NotePropertyValue $_.FullName -PassThru
    }
}
