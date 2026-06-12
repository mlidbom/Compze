# Rules folder

Project rules, read by both humans and the agent.

**Start here:** [`path-scoped/01-shared/rules-folder.md`](path-scoped/01-shared/rules-folder.md) — how the
folder is loaded, the include order, and the `name.rationale.txt` rationale-sidecar convention.

**Shared vs local is visible in the names:** everything pulled from the `.claude-shared/` catalog (a git
subtree; see `.claude-shared/README.md`) sits behind a `…-shared` entry — `01-universal-shared` is a single
directory symlink to the catalog's universal rules; `path-scoped/01-shared/` holds per-file symlinks to the
adopted path-scoped rules. Project-specific rules live alongside as regular files and folders. (Skills are
the exception: grouping or renaming breaks them — see the catalog README — so `.claude/skills/` links shared
skills flat under their canonical names; their being symlinks is the marker.) If the links look wrong, run
`.claude-shared/git-scripts/verify-claude-symlinks.ps1` — `C-Build` also runs it automatically.

(This README is `.txt` on purpose — a `README.md` with no frontmatter would inject into every session. Like
every `.txt` doc here, it's still written in Markdown.)
