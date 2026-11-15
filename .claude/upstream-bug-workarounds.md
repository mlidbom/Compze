# Workarounds

Active workarounds for upstream-tool bugs that affect this repo. Each section: the bug, the workaround, the teardown when fixed, how to recognize regression.

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
