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

    .claude-shared is a git worktree of the machine's one clone of the catalog, which the Claude Code
    session-start hook makes. A clone or worktree of Compze that no session has started in has none — the
    verifier itself is then missing, so that case is reported first, with its fix.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $verifier = Join-Path $script:CompzeRoot ".claude-shared\git-scripts\verify-claude-symlinks.ps1"
    if (-not (Test-Path $verifier)) {
        throw "This checkout has no .claude-shared, so the shared Claude rules and skills are missing. Start a Claude Code session in it, or run: sh .claude/verify-shared-claude-config.sessionstart-hook.sh"
    }
    & $verifier
}
