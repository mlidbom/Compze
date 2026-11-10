# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-PendingReleaseFeedDirectory {
    <#
    .SYNOPSIS
    Gets the folder of this machine's pending-release feed

    .DESCRIPTION
    The pending-release feed is a local NuGet folder feed holding only the Compze package versions published on this
    machine ahead of nuget.org (C-Publish-ToPendingReleaseFeed), so consumer repositories can use a new version without
    waiting for the release workflow and nuget.org's validation. Once nuget.org has a version,
    C-Clear-ReleasedLocalBuilds removes it again.

    One feed per machine, outside any repository: it serves every repository on the machine, and a Compze worktree
    publishing to it must reach the same consumers as the main checkout.
    #>
    [CmdletBinding()]
    param()

    return Join-Path $HOME ".nuget/compze-pending-release-feed"
}
