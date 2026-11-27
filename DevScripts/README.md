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

- **C-Build** - Builds the Compze solution, after bringing the project files' generated exclusions of `_docs` example code and nested projects up to date (C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders)
  - `-Clean` - Deep clean before building
  - `-FullGitReset` - Full git reset before building (implies `-Clean`)
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

- **C-Build-Website** - Builds the website (`src/Websites/Website`) with DocFX, as `npm run build` does but without Node.js, and fails, listing them, when DocFX reports any warning: a link, a code include or a table of contents entry that leads nowhere. For checking a docs change in one step. Silent on success; the site is in `src/Websites/Website/_site`. It doesn't compile the pages' example code: `C-Build` does, in the Website project

- **C-Test** - Runs Compze tests with proper configuration (builds by default)
  - `-NoBuild` - Skip building, just run tests
  - `-Clean` - Clean and build before testing
  - `-FullGitReset` - Full git reset, build, then test (implies `-Clean`)
  - `-SingleThreadedTesting` - Run tests single-threaded for debugging
  - `-MaxParallelAssemblies <n>` - Run at most this many test assemblies at once, where by default it runs one for each logical processor
  - `-MaxParallelThreads <n>` - Run at most this many test classes at once within each test assembly. By default 8 when the run includes a MySql combination, since MySQL's DDL throughput stops rising at about 8 at once, and xUnit's own, one for each logical processor, otherwise; 0 leaves it to xUnit
  - `-KeepResultsIn <folder>` - Keep each run's TRX files, with each failure's message and stack trace, under this folder
  - `-Iterations <n>` - Run suite multiple times and show summary
  - `-Combinations <combination>, ...` - Run on these combinations of pluggable components instead of those in `src/TestUsingPluggableComponentCombinations`, such as `'PgSql:Microsoft:Newtonsoft:AspNetCore'`. Writes them into each test project's copy of the file after the build, and restores every copy to exactly what it was once the run is over, also when it fails or is interrupted; the file itself is never changed. Works with `-NoBuild`
  - `-Filter <filter>` - Run only the tests that match, passed to `dotnet test --filter`, such as `"FullyQualifiedName~CQRS.TaggregateStores"`. A filter that matches no test fails the run
  - `-WhatIf` - Preview what would be deleted by git clean (with `-FullGitReset`)

### Packing & Releasing

- **C-Pack** - Packs every package into the local `nupkgs/` feed that Compze's focused solutions restore from
  - Default: timestamped dev versions (`0.6.0-alpha.dev.20261110093000`), each a fresh NuGet cache entry
  - `-CI` - Real versions, as the release workflow packs them, each with its changelog section for its version as its release notes (what nuget.org's package page shows)
  - `-NoBuild` - Pack without building first

- **C-Compare-PublicApi** - Lists the public API changes between two commits, project by project: every public and protected type and member added, removed or changed, from the declarations DocFX's metadata step gives them. For a review of a branch's public API, and for a release's changelog
  - Default: the working tree against its merge-base with `main`, the projects under `src` with changed files
  - `-Base`, `-Head` (a commit, checked out in a temporary worktree), `-Projects` (names, wildcards allowed), `-OutFile` (markdown)
  - A type that moved to another namespace or project, or was renamed, is listed once, with its member changes, when evidence pairs it with what it was: its file (git's rename detection, or the file of a type already paired), or its namespace's move (a project's rename, or a type already paired); the report says what each pairing rests on
  - DocFX skips the members of C# 14 extension blocks: the report names the changed files that hold them, to compare with git diff

- **C-Get-PackagesChangedSinceTheirRelease** - Lists the packages whose shipped files changed since their last release tag, and those never released: for each, whether its version is bumped and its changelog changed since the tag, and the files that changed. For checking, before a release, that no package changed without changelog entries and a bump. Changes acknowledged in `.github/scripts/AcknowledgedUnreleasedChanges.txt` (see C-Acknowledge-UnreleasedChanges) aren't listed
  - `-PackageId` to limit the list

- **C-Acknowledge-UnreleasedChanges** `<PackageId>, ... -Reason '<why>'` - Records that packages' changes since their release ship nothing a consumer sees (comments only, build settings that produce the same result), so that a release may leave them at their released version, which C-Prepare-Release, C-SimulateRelease and the Create Releases workflow otherwise refuse
  - Writes each package's line in `.github/scripts/AcknowledgedUnreleasedChanges.txt`: the package, HEAD's commit, and the reason. It lasts until the package is next released, since only the changes after that commit count. Commit the file with the release
  - It only records: it prepares and checks nothing

- **C-Prepare-Release** - Prepares the next release of every package whose changelog gained entries since its release: bumps its version (minor, or patch with `-Patch`), splits its changelog's top section into the new version's section and the released one, then packs and adds the line stating the dependency floors the version raises across a minor version
  - `-PackageId` to prepare only these packages
  - Refuses, changing nothing, when a package it doesn't prepare has shipped files that changed since its release tag: the release would leave it at its released version. The refusal names each package and its changed files. Either give the package a changelog entry, so that it is prepared, or, when its changes ship nothing a consumer sees (comments only, build settings that produce the same result), acknowledge them with C-Acknowledge-UnreleasedChanges. Preparing a package's release removes its acknowledgement
  - With nothing to release, it says so and refuses nothing

- **C-SimulateRelease** - Packs with real versions and dry-runs the release: which packages are new, their changelog entries, the post-release binding check (`.github/scripts/PostReleaseBindingCheck/README.md`), pushes, GitHub releases and tags
  - `-NoBuild`, `-NoPack`, `-NoSummary`
  - Refuses a changelog section that doesn't state the dependency floors its version raises across a minor version, and prints the line to add (`.github/scripts/lib/DependencyFloors.ps1` says why)
  - Refuses, before packing, a package the release doesn't release whose shipped files changed since its release tag, unless the changes are acknowledged, as C-Prepare-Release does; the Create Releases workflow refuses it too
  - The real release is the Create Releases workflow (`.github/workflows/publish-nuget.yml`), run by hand only after a CI run that covers the commit has passed: see `.claude/rules/02-universal-compze/100-releasing.md`

- **C-Publish-ToPendingReleaseFeed** - Publishes the versions the next release will publish to this machine's pending-release feed (`~/.nuget/compze-pending-release-feed`), which every repository on the machine restores from: a consumer can use a new version at once, instead of after CI, the release workflow and nuget.org's validation
  - Packs what HEAD holds, in a temporary clean worktree of HEAD, so a published version is frozen to the code that ships; uncommitted changes in the checkout don't stop it, and are not published
  - `-Replace` - Replace a published version whose package source changed since (also purges the old build from the NuGet cache)
  - Push a consumer that references a published version only once nuget.org has it

- **C-Clear-ReleasedLocalBuilds** - Removes every local build of a version nuget.org has released, from the pending-release feed and the NuGet global packages folder, so restores take nuget.org's build; reports versions still pending. A user-level SessionStart hook runs it in every Claude session

- **C-Get-CompzeUpgradeReport** - Run in a repository that uses Compze, before upgrading its Compze packages. From each project's restore graph: each Compze package its solution uses, at the version the solution names (a direct reference, or a pinned `PackageVersion` line) and the newest (nuget.org's, or a newer one in the pending-release feed); how the solution sets its versions, recommending transitive pinning where central package management lacks it; the packages its projects resolve at different versions, with the chain that holds each older one there and whether Compze's release checked the package holding it beside the newer version; and for each upgrade the other Compze packages it brings along (calling out a patch that brings a minor version, and the lines or references to raise to avoid NU1605, NU1109 or a runtime break), the Compze packages it leaves behind at versions built against older ones (whether each may use their internals, whether a Compze release checked it beside the new versions, marking one on internals that none checked UNCHECKED, ON INTERNALS, and the version of it to upgrade to as well), and the changelog sections in between, as released
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

- **C-Set-TestDatabaseServers** - Chooses the machine whose SQL Server, PostgreSQL and MySQL the tests use, through the DbPool's three `COMPOSABLE_*_DATABASE_POOL_MASTER_CONNECTIONSTRING` user environment variables. Without a switch, shows the current choice. Restart Rider (NCrunch) and terminals afterwards
  - `-Computer` - Another machine on the network, such as `threadripper`: changes nothing unless all three servers answer there
  - `-ThisMachine` - This machine's servers

### Utility

- **C-Get-Commands** - Lists all Compze module commands with their syntax

- **C-Reload-Module** - Reloads the Compze PowerShell module and profile

- **C-Kill-ZombieDevProcesses** - Kills hung test executables that are locking files
  - `-Force` - Kill without confirmation
  - `-WhatIf` - Preview what would be killed

