# Workarounds for upstream bugs

Active workarounds live in [.claude/upstream-bug-workarounds.md](../../upstream-bug-workarounds.md). Read it if C# LSP probes
start returning "No symbols found" or symbols from the wrong `.slnx`, or if the **PowerShell tool returns
`Exit code 1` with no output on every call** (use Bash with `pwsh -NoProfile -NonInteractive -Command "..."`
instead). Currently covers: csharp-ls + Claude Code
[#16360](https://github.com/anthropics/claude-code/issues/16360), and PowerShell tool failure in the VS Code
extension UI mode ([#55671](https://github.com/anthropics/claude-code/issues/55671)).
