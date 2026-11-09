# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Verify-ClaudeConfigSymlinks {
    <#
    .SYNOPSIS
    Verifies the symlinks that select shared Claude config from .claude-shared/ into .claude/

    .DESCRIPTION
    Runs .claude-shared/git-scripts/verify-claude-symlinks.ps1, which checks that every symlink git
    tracks under .claude/ is a real symlink in the working tree resolving to an existing target.
    Guards against the silent Windows failure mode where a checkout with core.symlinks=false
    materializes committed symlinks as plain text files containing the target path — which Claude Code
    would then load as garbage rules/skills. Throws with fix instructions on failure; silent on success.

    .claude-shared is a git submodule. A clone made without --recurse-submodules, or a new worktree, leaves it
    empty — the verifier itself is then missing, so that case is reported first, with its fix.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $verifier = Join-Path $script:CompzeRoot ".claude-shared\git-scripts\verify-claude-symlinks.ps1"
    if (-not (Test-Path $verifier)) {
        throw "The .claude-shared submodule is not initialized, so the shared Claude rules and skills are missing. Run: git submodule update --init"
    }
    & $verifier
}
