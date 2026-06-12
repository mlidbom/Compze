# Workarounds for upstream bugs

Active workarounds live in [.claude/upstream-bug-workarounds.md](../../upstream-bug-workarounds.md). Read it if the
**PowerShell tool returns `Exit code 1` with no output on every call** (use Bash with
`pwsh -NoProfile -NonInteractive -Command "..."` instead), or if **cloud-session** C# probes via csharp-ls
return "No symbols found" or symbols from the wrong `.slnx`. Currently covers: PowerShell tool failure in
the VS Code extension UI mode ([#55671](https://github.com/anthropics/claude-code/issues/55671)), and the
cloud-only csharp-ls solution pinning ([#16360](https://github.com/anthropics/claude-code/issues/16360)) —
locally csharp-ls is retired in favor of Rider's ReSharper-backed MCPs.
