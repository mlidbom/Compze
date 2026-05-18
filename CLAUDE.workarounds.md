# Workarounds

Active workarounds for upstream-tool bugs that affect this repo. Each section: the bug, the workaround, the teardown when fixed, how to recognize regression.

---

## C# language server (csharp-ls)

[anthropics/claude-code#16360](https://github.com/anthropics/claude-code/issues/16360) — Claude Code's LSP client doesn't implement `workspace/configuration`, so csharp-ls can't learn which solution to load and falls back to heuristics. It may index the wrong `.slnx` (this repo ships many — `Compze.Abstractions.slnx`, `Compze.Contracts.slnx`, etc.), causing `documentSymbol`, `findReferences`, or `hover` to miss symbols or report from the wrong subset.

### The ideal config (once #16360 is fixed)

`.claude/settings.json` declares the solution path; Claude Code forwards it via the standard LSP `workspace/configuration` channel. `lspSettings.csharp.solutionPathOverride` is already present as a forward-compatible no-op.

### The workaround that does the work today

Two files, both required:

**1. Project-level `.claude/settings.json`** (git-tracked):
```json
{
  "env": { "CSHARP_LSP_SOLUTION_REL": "src/Compze.AllProjects.slnx" }
}
```
For a focused subset (e.g., `Compze.Contracts.slnx`), change the value.

**2. User-global `~/.claude/plugins/cache/claude-plugins-official/csharp-lsp/<version>/.lsp.json`** (per-machine, not in any repo):
```json
{
  "csharp": {
    "command": "csharp-ls",
    "args": ["--solution", "${CLAUDE_PROJECT_DIR}/${CSHARP_LSP_SOLUTION_REL}", "--loglevel", "info"],
    "extensionToLanguage": { ".cs": "csharp", ".csx": "csharp" }
  }
}
```

**No comments in these JSON files** — Claude Code uses strict JSON. Use `"_comment"` keys if needed.

### When #16360 is fixed

1. Delete the user-global `.lsp.json` from step 2.
2. Delete the `env` block from `.claude/settings.json`.
3. Verify `lspSettings.csharp.solutionPathOverride` takes over.

### Recognizing regression

If C# semantic probes return "No symbols found" or symbols from the wrong subset, the user-global `.lsp.json` is missing or malformed. Claude Code's auto-managed plugin cache may clobber it on csharp-lsp plugin updates — recreate it.

---

## PowerShell tool in VS Code extension UI mode

The PowerShell tool fails on **every** invocation in the VS Code extension's UI mode (sidebar or editor-tab webview). Returns `Exit code 1` with no output, even for `exit 0`, `Write-Output "hi"`, or `2+2`. Failure is immediate (~10 ms) — pwsh never runs the command.

Upstream: [anthropics/claude-code#55671](https://github.com/anthropics/claude-code/issues/55671) (canonical, open). Duplicates: [#57311](https://github.com/anthropics/claude-code/issues/57311). Related variant: [#55727](https://github.com/anthropics/claude-code/issues/55727) (Japanese locale).

### Root cause

The PowerShell permission classifier embeds the full settings context (`additionalDirectories`, allow/deny/ask lists, MCP deferred-tools list) into the `pwsh -Command "..."` invocation. On Windows that can exceed `CreateProcess`'s 32,767-char limit; pwsh exits 1 with `The command line is too long.` before running anything. The classifier runs on every PowerShell call, so every call fails — including in `bypassPermissions` mode. The Bash tool uses a different invocation path and is unaffected.

### Why UI mode and not terminal mode

Same session, same settings, same machine: PowerShell tool works in terminal mode (`claudeCode.useTerminal: true`) and fails in UI mode. The UI harness packs more wrapping/context into each pwsh classifier invocation.

### Workaround

Invoke pwsh via the Bash tool:

```
pwsh -NoProfile -NonInteractive -Command "<your PowerShell here>"
```

Use whenever PowerShell-specific behavior is needed (`C-Build`, `C-Test`, native cmdlets, Windows-paths needing PS quoting). Plain shell commands still go through Bash directly.

### When #55671 is fixed

Suggested upstream fix: pipe the classifier payload via stdin or `pwsh -File <tempfile>`, both bypassing the `CreateProcess` arg-length limit. To verify after a fix: call the PowerShell tool with `Write-Output "hello"` in UI mode; if it returns `hello` with exit 0, drop the Bash wrapping.

### Recognizing regression

Symptom: `PowerShell(<anything>)` returns `Exit code 1` with no output, even for `exit 0`; Bash works; `pwsh -NoProfile -NonInteractive -Command "..."` via Bash works. Diagnostic: write a file via PowerShell tool (`'x' | Out-File "$env:TEMP\probe.txt"`) and check via Bash whether it exists. If not, pwsh never ran — this bug.
