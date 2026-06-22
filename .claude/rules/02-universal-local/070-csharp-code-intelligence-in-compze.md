# C# code-intelligence MCP bindings for Compze

The generic guidance (the ReSharper-backed `resharper-joshua` MCP first, addressed by symbol name; the
built-in `jetbrains-rider` MCP for non-semantic work; `sherlock` for referenced-library APIs) lives in a
user-level rule on the dev machine — these MCPs require Rider running locally. The Compze-specific bindings:

- **resharper-joshua: pass `solutionName: 'Compze.AllProjects'`.** Multiple solutions are usually open in
  Rider; omitting it errors or silently answers from the wrong solution.
- **jetbrains-rider MCP: pass `rootFolder` = `C:/Dev/Compze`** (the `.slnx`'s parent folder — the repo root,
  now that the solutions live there) when multiple solutions are open.
- **resharper `get_diagnostics` resolves any file in the loaded solution** — `src/` and `test/` alike, down
  to the `hint` tier (verified on `test/Compze.Must.Specifications`) — so per-file diagnostics are always
  live; only **whole-solution sweeps** need `jb inspectcode` (the `shared-jetbrains-inspect` skill). Rider's
  own per-file tools (`get_file_problems`, `lint_files`) used to reject `test/` files as "outside the project
  directory" back when the solution lived in `src/` and `test/` was its sibling; with the solution at the
  repo root, `test/` is inside the `.slnx`'s folder and that rejection should no longer apply — but
  `get_diagnostics` remains the reliable path either way.
