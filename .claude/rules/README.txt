# Rules folder

Project rules, read by both humans and the agent.

**Start here:** [`path-scoped/rules-folder.md`](path-scoped/rules-folder.md) — how the folder is loaded, the
include order, and the `name.rationale.txt` rationale-sidecar convention.

Most entries here are symlinks selecting shared rules from the `.claude-shared/` catalog (a git subtree;
see `.claude-shared/README.md`). Project-specific rules live alongside as regular files. If the links look
wrong, run `.claude-shared/git-scripts/verify-claude-symlinks.ps1` — `C-Build` also runs it automatically.

(This README is `.txt` on purpose — a `README.md` with no frontmatter would inject into every session. Like
every `.txt` doc here, it's still written in Markdown.)
