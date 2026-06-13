# Shared Claude Code configuration

Shared Claude Code configuration — rules, skills, and the scripts to manage them — designed to live at
`.claude-shared/` in any repository, managed as a **git subtree**. Each consuming project symlinks the parts
it adopts from `.claude-shared/` into its own `.claude/`; the catalog itself auto-loads nothing.

## Layout

| Path | Contents |
| --- | --- |
| `rules/universal/` | Always-on rules (no `paths:` frontmatter) — collaboration rules and the code standards, with `.rationale.txt` sidecars |
| `rules/path-scoped/` | Rules with `paths:` frontmatter — injected only when matching files are touched |
| `skills/` | Skills, one folder per skill |
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

**For rules, make shared vs local visible in the names**: put the links behind `…-shared` entries — a
single `01-universal-shared` directory link for the universal rules, and a `01-shared/` folder of per-file
links for the adopted path-scoped rules. Rules discovery is recursive and frontmatter-gated, so such
grouping folders are pure organization. Project-local rules live alongside as regular files and folders, so
the split is obvious at a glance.

**Skills cannot be grouped, and the link folder's name IS the skill's name** (verified empirically):
discovery is one folder level deep, so a link inside `skills/01-shared/` is not found at session start —
and the *folder* name, not the SKILL.md frontmatter `name:`, becomes the name the skill registers and is
invoked under. So link each adopted skill flat, with a **`shared-` prefix on the link name**: the prefix is
the visible marker in the tree (IDEs don't reliably show symlink-ness) and deliberately becomes part of the
skill's name, marking it as shared at invocation time too. The catalog keeps canonical, unprefixed folder
names.

```powershell
New-Item -ItemType SymbolicLink .claude\rules\01-universal-shared -Target ..\..\.claude-shared\rules\universal
New-Item -ItemType SymbolicLink .claude\rules\path-scoped\01-shared\csharp-bdd-specifications.md -Target ..\..\..\..\.claude-shared\rules\path-scoped\csharp-bdd-specifications.md
New-Item -ItemType SymbolicLink .claude\skills\shared-team-review-and-fix-code-standard-issues -Target ..\..\.claude-shared\skills\team-review-and-fix-code-standard-issues
```

In the consuming repo's `.gitattributes`, mark committed **directory** symlinks with `symlink=dir` so Git
for Windows creates them correctly regardless of checkout order.

### Windows prerequisites (one-time per machine)

- Turn on **Developer Mode** (Settings > System > For developers) — symlink creation without elevation.
- `git config --global core.symlinks true` — without it, checkouts silently materialize committed symlinks
  as plain text files containing the target path, which Claude Code would load as garbage.

### Verifying

Two equivalent verifiers check that every git-tracked symlink under `.claude/` is a real, resolving symlink
and fail with fix instructions otherwise — silent on success:

- [git-scripts/verify-claude-symlinks.ps1](git-scripts/verify-claude-symlinks.ps1) — PowerShell, for Windows
  build/setup scripts.
- [git-scripts/verify-claude-symlinks.sh](git-scripts/verify-claude-symlinks.sh) — POSIX sh + git, no
  PowerShell dependency, for a cross-platform Claude Code `SessionStart` hook.

Wire one into the consuming repo so a degraded checkout fails loudly:

- **Build/setup script** — call the `.ps1` (it throws on failure).
- **`SessionStart` hook** — catches a degraded checkout the moment a session starts, before the agent acts on
  garbage rules. Claude Code only merges settings from a project's own `.claude/settings.json` (never from
  `.claude-shared/`), so the activation is per-project — add it to each repo's `.claude/settings.json`. Keep
  it project-level, not user-level: a degraded checkout happens on fresh clones, cloud agents, and teammates,
  which user settings don't reach.

  ```json
  "hooks": {
    "SessionStart": [
      { "hooks": [
          { "type": "command",
            "command": "bash \"${CLAUDE_PROJECT_DIR}/.claude-shared/git-scripts/verify-claude-symlinks.sh\"" }
      ] }
    ]
  }
  ```

  The `.sh` exits 2 with its message on stderr, which Claude Code surfaces at session start. `bash` runs the
  hook via Git Bash on Windows and natively on Linux/macOS; the quoted `${CLAUDE_PROJECT_DIR}` path resolves
  in both (Claude Code expands the variable before spawning the shell).

## Syncing

Run from any directory — the scripts derive the repo root and the mount path themselves:

- Pull upstream changes: `.claude-shared/git-scripts/pull.ps1`
- Push local changes upstream (requires push access): `.claude-shared/git-scripts/push.ps1`
