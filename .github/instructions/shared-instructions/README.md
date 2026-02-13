# copilot-code-standards-and-instructions

Shared code standards and Copilot instructions, designed to live at `.github/instructions/shared-instructions/` in any repository. Managed as a **git subtree**.

## Adding to a new repository

From your **repository root**, run:

```powershell
git subtree add --prefix .github/instructions/shared-instructions https://github.com/mlidbom/copilot-code-standards-and-instructions.git main
git subtree split --prefix .github/instructions/shared-instructions --rejoin
```

Do **NOT** use `--squash` — it discards commit objects that `split`/`push` need later.

The second command creates a rejoin marker so that future pushes don't have to walk the entire repo history. Do this immediately after the add — it's instant at this point since only one commit touches the prefix.

## Syncing

The `git-scripts/` directory contains two PowerShell scripts for keeping this folder in sync with the shared repo. Run them from **any directory** — they locate the repo root automatically.

### Pull updates from the shared repo

```powershell
.\.github\instructions\shared-instructions\git-scripts\pull-shared-instructions.ps1
```

This performs a `git subtree pull`, merging upstream changes into your repo. Conflicts are resolved through normal git merge.

### Push local changes to the shared repo

```powershell
.\.github\instructions\shared-instructions\git-scripts\push-shared-instructions.ps1
```

This uses `git subtree split --rejoin` followed by `git push` to push changes back to the shared repo. The `--rejoin` flag ensures future pushes remain fast by only processing new commits. Requires push access to the remote.
