# C# code-intelligence MCP bindings for Compze

The generic guidance (the ReSharper-backed `resharper-joshua` MCP first, addressed by symbol name; the
built-in `jetbrains-rider` MCP for non-semantic work; `sherlock` for referenced-library APIs) lives in a
user-level rule on the dev machine — these MCPs require Rider running locally. The Compze-specific bindings:

- **resharper-joshua: pass `solutionName: 'Compze.AllProjects'`.** Multiple solutions are usually open in
  Rider; omitting it errors or silently answers from the wrong solution.
- **jetbrains-rider MCP: pass `rootFolder` = `C:/Dev/Compze/src`** (the `.slnx`'s parent folder, not the
  repo root) when multiple solutions are open.
- **`test/` projects sit outside the `.slnx`'s folder** (siblings of `src/`), so rider's per-file diagnostic
  tools (`get_file_problems`, `lint_files`) reject them with "outside the project directory". For warnings
  in test files — or whole-solution sweeps — `jb inspectcode` (the `shared-jetbrains-inspect` skill) is the
  only path.
