# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Invoke-InCleanWorktreeOfHead {
    <#
    .SYNOPSIS
    Runs a script block with the DevScripts commands working on a temporary clean worktree of HEAD, which is removed afterwards

    .DESCRIPTION
    For a command whose result must be exactly the committed code, whatever the checkout's working tree holds: another
    session's uncommitted edits, a build of other code, files git ignores.

    The DevScripts commands find the repository through $script:CompzeRoot and the paths derived from it. While the script
    block runs, those point at the worktree, so every command it calls, such as C-Pack, works on HEAD's files; afterwards
    they point at the checkout again. The DevScripts code that runs is the module as it was imported, not HEAD's.

    The worktree is a detached checkout of HEAD under the temporary folder. It holds only what HEAD tracks: no build, no
    submodules, and no file git ignores, such as src/TestUsingPluggableComponentCombinations, which C-Pack creates before
    it builds. It is removed when the script block ends, also when it fails.

    .PARAMETER ScriptBlock
    What to run. It runs in the caller's scope chain, so it sees the caller's variables and parameters.

    .OUTPUTS
    Whatever the script block outputs.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$ScriptBlock
    )

    function PointCommandsAt([string]$repositoryRoot) {
        $script:CompzeRoot = $repositoryRoot
        $script:CompzeSrcRoot = Join-Path $repositoryRoot "src"
        $script:CompzeSolutionPath = Join-Path $repositoryRoot "Compze.AllProjects.slnx"
    }

    $checkoutRoot = $script:CompzeRoot
    $headCommit = git -C $checkoutRoot rev-parse HEAD
    $worktree = Join-Path ([IO.Path]::GetTempPath()) "compze-head-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
    git -C $checkoutRoot worktree add --detach --quiet $worktree $headCommit
    if ($LASTEXITCODE -ne 0) { throw "Could not create a worktree of HEAD ($headCommit) at $worktree (git exit code $LASTEXITCODE)." }

    try {
        PointCommandsAt $worktree
        & $ScriptBlock
    } finally {
        PointCommandsAt $checkoutRoot
        git -C $checkoutRoot worktree remove --force $worktree
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not remove the temporary worktree of HEAD at $worktree (git exit code $LASTEXITCODE). Remove it with: git -C '$checkoutRoot' worktree remove --force '$worktree'"
        }
    }
}
