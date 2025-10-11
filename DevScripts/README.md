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

- **Clean-Compze** - Performs a deep clean of the Compze solution
  - `-FullGitReset` - Performs full git reset, removes all untracked files (requires clean working tree, backs up TestUsingPluggableComponentCombinations)
  - `-Verbose` - Show detailed output

- **Build-Compze** - Builds the Compze solution
  - `-Clean` - Deep clean before building
  - `-FullGitReset` - Full git reset before building (implies `-Clean`)

- **Test-Compze** - Runs Compze tests with proper configuration
  - `-Build` - Build before testing
  - `-Clean` - Clean and build before testing
  - `-FullGitReset` - Full git reset, build, then test (implies `-Clean` and `-Build`)
  - `-SingleThreadedTesting` - Run tests single-threaded for debugging

### Code Quality & Structure

- **Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders** - Ensures .csproj files exclude .cs files from projects in subfolders

- **Fix-CompzeEncodings** - Converts files to UTF-8 without BOM encoding
  - `-Path` - Path to scan (defaults to src)
  - `-FilePattern` - File pattern to match (defaults to *.cs)
  - `-WhatIf` - Preview changes without applying

- **Remove-CompzeRedundantInternalsVisibleTo** - Removes redundant InternalsVisibleTo attributes
  - `-SolutionPath` - Path to solution file
  - `-LogFile` - Path to log file

- **Validate-CompzeSolutionStructure** - Validates the Compze solution structure

### Utility

- **Get-CompzeCommands** - Lists all Compze module commands with their syntax

- **Reload-CompzeModule** - Reloads the Compze PowerShell module and profile

