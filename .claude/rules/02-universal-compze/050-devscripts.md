# DevScripts

The repo's PowerShell command module. **It must be imported before any `C-*` command works:**

```powershell
Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking
```

Discover all commands: `C-Get-Commands` or `C-<Tab>` in PowerShell.

## Key commands

| Command | Purpose |
|---------|---------|
| `C-Test` | Build + run full test suite |
| `C-Build` | Build the solution |
| `C-Clean` | Deep clean the solution |
| `C-Create-Project` | Create new projects with proper structure |
| `C-Delete-Project` | Delete a project |
| `C-Rename-Project` | Rename a project |
| `C-Split-Project` | Split a project into multiple |
| `C-Merge-Project` | Merge projects |
| `C-FlexRef-Sync` | Sync FlexRef infrastructure after reference changes |
| `C-Validate-SolutionStructure` | Validate solution structure |
