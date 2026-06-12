# Shared Claude Code configuration

Shared Claude Code configuration — rules, skills, and the scripts to manage them — designed to live at
`.claude-shared/` in any repository, managed as a **git subtree**. Each consuming project symlinks the parts
it adopts from `.claude-shared/` into its own `.claude/`; the catalog itself auto-loads nothing.

## Layout

| Path | Contents |
| --- | --- |
| `rules/universal/` | Always-on rules (no `paths:` frontmatter) — the code standards, with `.rationale.txt` sidecars |
| `rules/path-scoped/` | Rules with `paths:` frontmatter — injected only when matching files are touched |
| `skills/` | Skills, one folder per skill |
| `reference/` | Docs that are deliberately not rules (ReSharper inspection workflow, …) |
| `git-scripts/` | Subtree sync + symlink verification scripts |

How the rules loader works — include order, the sidecar convention, what loads when — is documented in
[rules/path-scoped/rules-folder.md](rules/path-scoped/rules-folder.md).

## Adding to a repository

From the repository root:

```powershell
git subtree add --prefix .claude-shared https://github.com/mlidbom/copilot-code-standards-and-instructions.git main
git subtree split --prefix .claude-shared --rejoin
```

Do **NOT** use `--squash` — it discards commit objects that `split`/`push` need later. The second command
creates a rejoin marker so future pushes don't walk the entire repo history; run it immediately after the
add, while it's instant.

## Selecting what a project adopts

Symlink the parts you want into the project's `.claude/`. Directory links by default — they carry the
`.rationale.txt` sidecars along; per-file links only where finer grain is genuinely needed. The link name is
project-local, so each project slots shared folders into its own numeric ordering scheme. A project that
adopts the code standards but not, say, the specification style simply doesn't create that link.

```powershell
New-Item -ItemType SymbolicLink .claude\rules\universal\03-code-standards -Target ..\..\..\.claude-shared\rules\universal\03-code-standards
New-Item -ItemType SymbolicLink .claude\rules\path-scoped\csharp-code.md -Target ..\..\..\.claude-shared\rules\path-scoped\csharp-code.md
New-Item -ItemType SymbolicLink .claude\skills\team-review-and-fix-code-standard-issues -Target ..\..\.claude-shared\skills\team-review-and-fix-code-standard-issues
```

In the consuming repo's `.gitattributes`, mark committed **directory** symlinks with `symlink=dir` so Git
for Windows creates them correctly regardless of checkout order.

### Windows prerequisites (one-time per machine)

- Turn on **Developer Mode** (Settings > System > For developers) — symlink creation without elevation.
- `git config --global core.symlinks true` — without it, checkouts silently materialize committed symlinks
  as plain text files containing the target path, which Claude Code would load as garbage.

### Verifying

[git-scripts/verify-claude-symlinks.ps1](git-scripts/verify-claude-symlinks.ps1) checks that every
git-tracked symlink under `.claude/` is a real, resolving symlink, and throws with fix instructions
otherwise. Wire it into the consuming repo's build so a degraded checkout fails loudly.

## Syncing

Run from any directory — the scripts derive the repo root and the mount path themselves:

- Pull upstream changes: `.claude-shared/git-scripts/pull.ps1`
- Push local changes upstream (requires push access): `.claude-shared/git-scripts/push.ps1`
