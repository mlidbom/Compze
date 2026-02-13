# copilot-code-standards-and-instructions

Shared code standards and Copilot instructions, designed to live at `.github/instructions/shared-instructions/` in any repository.

## Adding to a new repository

From your **repository root**, run:

```powershell
git clone --depth 1 https://github.com/mlidbom/copilot-code-standards-and-instructions.git .github/instructions/shared-instructions

Remove-Item .github/instructions/shared-instructions/.git -Recurse -Force

git add .github/instructions/shared-instructions
git commit -m "Add shared instructions"
```

That's it. The sync scripts are included automatically.

## Syncing

The `sync-scripts/` directory contains two PowerShell scripts for keeping this folder in sync with the shared repo. Run them from **any directory** — they locate the repo root automatically.

### Pull updates from the shared repo

```powershell
.\.github\instructions\shared-instructions\sync-scripts\pull-shared-instructions.ps1
```

This replaces the contents of `shared-instructions/` with the latest from the remote. Changes appear as normal working-tree modifications — review with `git diff` and commit when ready.

> **Note:** If the sync scripts themselves were updated, re-run the pull to pick up the new versions.

### Push local changes to the shared repo

```powershell
.\.github\instructions\shared-instructions\sync-scripts\push-shared-instructions.ps1
```

This clones the shared repo into a temp directory, replaces its contents with your local `shared-instructions/` files, commits, and pushes. Requires push access to the remote.
