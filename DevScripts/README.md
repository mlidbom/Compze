# Compze PowerShell Development Module

PowerShell commands for Compze development workflows.

## Important: Logging Convention

**Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.**

This keeps the console clean and makes it easy to spot actual problems. Success should be silent.

## Setup

Add to your PowerShell profile (`notepad $PROFILE`):
```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psm1 -DisableNameChecking
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

- **C-Test** - Runs Compze tests with proper configuration (builds by default)
  - `-NoBuild` - Skip building, just run tests
  - `-Clean` - Clean and build before testing
  - `-FullGitReset` - Full git reset, build, then test (implies `-Clean`)
  - `-SingleThreadedTesting` - Run tests single-threaded for debugging
  - `-Iterations <n>` - Run suite multiple times and show summary
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

### Code Quality & Structure

- **C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders** - Ensures .csproj files exclude .cs files from projects in subfolders and properly handle _docs folders

- **C-Fix-CsFileEncodings** - Converts .cs files to UTF-8 without BOM encoding
  - `-Path` - Path to scan (defaults to src)
  - `-FilePattern` - File pattern to match (defaults to *.cs)
  - `-WhatIf` - Preview changes without applying

- **C-Validate-SolutionStructure** - Validates the Compze solution structure

### Project Management

- **C-Create-Project** - Creates a new project with proper directory structure and adds it to the solution
  - `-ProjectName` - Full name of the project to create (e.g., "Compze.Wiring.Testing")
  - Creates project directory based on namespace (Compze.Wiring.Testing -> src/Compze/Wiring/Testing/)
  - Creates basic .csproj file
  - Adds to solution file in correct folder structure

- **C-Rename-Project** - Renames a project and updates all references
  - `-Old` - Current project name (e.g., "Compze.Tessaging.Hosting.Configuration")
  - `-New` - New project name (e.g., "Compze.Common.Configuration")
  - `-SolutionPath` - Path to solution file (defaults to Compze.AllProjects.slnx)
  - Updates project file name, ProjectReferences, and all solution files (.slnx and .sln)

- **C-Relocate-Project** - Moves a project to match solution structure conventions
  - `-ProjectName` - Name of the project to relocate (e.g., "Compze.Common.Configuration")
  - `-SolutionPath` - Path to solution file (defaults to Compze.AllProjects.slnx)
  - Moves project directory to match name (Compze.A.B.C -> Compze/A/B/C)
  - Updates all ProjectReferences, solution paths, and solution folder structure

- **C-Place-ProjectInSolution** - Places a project in the correct solution folder structure
  - `-ProjectName` - Name of the project (e.g., "Compze.Common.Configuration")
  - `-SolutionPath` - Path to solution file (defaults to Compze.AllProjects.slnx)
  - Adds project to solution if it doesn't exist
  - Moves project to correct folder based on its path
  - Only updates the solution file folder structure, doesn't move any files

### Pluggable Components

- **C-Get-PluggableComponents** - Displays the currently active pluggable component combinations

- **C-Set-PluggableComponents** - Configures which SQL layers and DI containers to test
  - Individual switches: `-MicrosoftSqlServer`, `-MySql`, `-PostgreSql`, `-Sqlite`, `-SqliteMemory`
  - Container switches: `-Microsoft`, `-Autofac`
  - Convenience switches: `-AllSqlLayers`, `-AllContainers`, `-AllPermutations`
  - `-SetAsDefaults` - Save configuration as default

### Utility

- **C-Get-Commands** - Lists all Compze module commands with their syntax

- **C-Reload-Module** - Reloads the Compze PowerShell module and profile

- **C-Kill-ZombieDevProcesses** - Kills hung test executables that are locking files
  - `-Force` - Kill without confirmation
  - `-WhatIf` - Preview what would be killed

