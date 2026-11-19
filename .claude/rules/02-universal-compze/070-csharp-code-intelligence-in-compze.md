# C# code-intelligence MCP bindings for Compze

The generic guidance (the ReSharper-backed `dotnet-analysis-and-refactoring` MCP first, addressed by symbol name; the
built-in `jetbrains-rider` MCP for non-semantic work; `sherlock` for referenced-library APIs) lives in a
user-level rule on the dev machine — these MCPs require Rider running locally. The Compze-specific bindings:

- **dotnet-analysis-and-refactoring: pass `solutionName: 'Compze.AllProjects'`.** Multiple solutions are usually open in
  Rider; omitting it errors or silently answers from the wrong solution.
- **jetbrains-rider MCP: pass `rootFolder` = the `.slnx`'s parent folder,** which is the repository root
  (`C:/Dev/Compze` in the main checkout), when multiple solutions are open.
