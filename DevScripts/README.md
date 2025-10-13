# Compze PowerShell Development Module

PowerShell commands for Compze development workflows.

## Setup

Add to your PowerShell profile (`notepad $PROFILE`):
```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking
```

Then reload: `. $PROFILE`

## Commands

### Build & Test Workflow

- **C-Clean** - Performs a deep clean of the Compze solution
  - `-FullGitReset` - Performs full git reset, removes all untracked files (requires clean working tree, backs up TestUsingPluggableComponentCombinations)
  - `-Verbose` - Show detailed output
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

- **C-Build** - Builds the Compze solution
  - `-Clean` - Deep clean before building
  - `-FullGitReset` - Full git reset before building (implies `-Clean`)
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

- **C-Test** - Runs Compze tests with proper configuration
  - `-Build` - Build before testing
  - `-Clean` - Clean and build before testing
  - `-FullGitReset` - Full git reset, build, then test (implies `-Clean` and `-Build`)
  - `-SingleThreadedTesting` - Run tests single-threaded for debugging
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

### Code Quality & Structure

- **C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders** - Ensures .csproj files exclude .cs files from projects in subfolders and properly handle _docs folders

- **C-Fix-CsFileEncodings** - Converts .cs files to UTF-8 without BOM encoding
  - `-Path` - Path to scan (defaults to src)
  - `-FilePattern` - File pattern to match (defaults to *.cs)
  - `-WhatIf` - Preview changes without applying

- **C-Remove-RedundantInternalsVisibleTo** - Removes redundant InternalsVisibleTo attributes
  - `-SolutionPath` - Path to solution file
  - `-LogFile` - Path to log file

- **C-Validate-SolutionStructure** - Validates the Compze solution structure

### Pluggable Components

- **C-Get-PluggableComponents** - Displays the currently active pluggable component combinations

- **C-Set-PluggableComponents** - Configures which SQL layers and DI containers to test
  - Individual switches: `-MicrosoftSqlServer`, `-MySql`, `-PostgreSql`, `-Sqlite`, `-SqliteMemory`
  - Container switches: `-Microsoft`, `-SimpleInjector`
  - Convenience switches: `-AllSqlLayers`, `-AllContainers`, `-AllPermutations`
  - `-SetAsDefaults` - Save configuration as default

### Utility

- **C-Get-Commands** - Lists all Compze module commands with their syntax

- **C-Reload-Module** - Reloads the Compze PowerShell module and profile

- **C-Kill-ZombieDevProcesses** - Kills hung test executables that are locking files
  - `-Force` - Kill without confirmation
  - `-WhatIf` - Preview what would be killed

