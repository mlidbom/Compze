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

- **C-Test-Combinations** - Runs the built tests against the pluggable-component combinations you name, from temporary copies, leaving your own combinations file and the build outputs alone

### The across-machines lab

Trying Compze's endpoints across machines: the lab machine is a Hyper-V virtual machine on this computer, in `C:\CompzeVMLab`, and the lab program (`test/Compze.Tests.AcrossMachines.Lab`) hosts an endpoint over TCP in it and converses with that endpoint from the host. The commands need membership of the Hyper-V Administrators group.

- **C-VM-CreateLabMachine** - Creates the lab machine as a clone of another virtual machine's checkpoint, prepares its Windows (computer name, no automatic updates, firewall, the host's .NET runtime and no ASP.NET Core runtime) and takes the `baseline` checkpoint. Run once; takes a few minutes
  - `-SourceMachine`, `-SourceCheckpoint`, `-SourceGuestCredentialPath` - What to clone; by default the baseline of Deskmancer's Windows 11 24H2 machine

- **C-VM-StartLabMachineFromBaseline** - Restores the `baseline` checkpoint, boots the lab machine and waits until its Windows is up. Whatever ran in it before is discarded

- **C-VM-StartLabEndpoint** - Copies the built lab program into the lab machine and starts it there serving the lab endpoint; returns the endpoint's address, which the lab program's `converse` command on the host takes

- **C-VM-RemoveLabMachine** - Removes the lab machine, its checkpoints and its disk

### Packing & Releasing

- **C-Pack** - Packs every package into the local `nupkgs/` feed that Compze's focused solutions restore from
  - Default: timestamped dev versions (`0.6.0-alpha.dev.20261110093000`), each a fresh NuGet cache entry
  - `-CI` - Real versions, as the release workflow packs them, each with its changelog section for its version as its release notes (what nuget.org's package page shows)
  - `-NoBuild` - Pack without building first

- **C-SimulateRelease** - Packs with real versions and dry-runs the release: which packages are new, their changelog entries, the post-release binding check (`.github/scripts/PostReleaseBindingCheck/README.md`), pushes, GitHub releases and tags
  - `-NoBuild`, `-NoPack`, `-NoSummary`
  - Refuses a changelog section that doesn't state the dependency floors its version raises across a minor version, and prints the line to add (`.github/scripts/lib/DependencyFloors.ps1` says why)
  - The real release is the Create Releases workflow (`.github/workflows/publish-nuget.yml`), run by hand only after CI has passed on the same commit: see `.claude/rules/02-universal-compze/100-releasing.md`

- **C-Publish-ToPendingReleaseFeed** - Publishes the versions the next release will publish to this machine's pending-release feed (`~/.nuget/compze-pending-release-feed`), which every repository on the machine restores from: a consumer can use a new version at once, instead of after CI, the release workflow and nuget.org's validation
  - Packs only a clean, committed tree; a published version is frozen to the code that ships
  - `-Replace` - Replace a published version whose package source changed since (also purges the old build from the NuGet cache)
  - `-NoBuild` - Pack without building first
  - Push a consumer that references a published version only once nuget.org has it

- **C-Clear-ReleasedLocalBuilds** - Removes every local build of a version nuget.org has released, from the pending-release feed and the NuGet global packages folder, so restores take nuget.org's build; reports versions still pending. A user-level SessionStart hook runs it in every Claude session

- **C-Get-CompzeUpgradeReport** - Run in a repository that uses Compze, before upgrading its Compze packages. From each project's restore graph: each Compze package its solution uses, at the version the solution names (a direct reference, or a pinned `PackageVersion` line) and the newest (nuget.org's, or a newer one in the pending-release feed); how the solution sets its versions, recommending transitive pinning where central package management lacks it; the packages its projects resolve at different versions, with the chain that holds each older one there and whether Compze's release checked the package holding it beside the newer version; and for each upgrade the other Compze packages it brings along (calling out a patch that brings a minor version, and the lines or references to raise to avoid NU1605, NU1109 or a runtime break) and the changelog sections in between, as released
  - `-Solution` - The solution to report on, when the current directory doesn't hold exactly one

- **C-Clear-NuGetCache** - Removes all cached Compze packages from the NuGet global packages folder, so focused solutions pick up freshly packed versions

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

