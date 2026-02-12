# Compze Development Tools Setup

## Quick Start

Add this line to your PowerShell profile:

```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psm1 -DisableNameChecking
```

To edit your profile, run: `notepad $PROFILE`

## Available Commands

### C-Test
Runs the full test suite. Builds by default before running tests.

```powershell
# Build + run tests (default)
C-Test

# Skip building, just run tests
C-Test -NoBuild

# Single-threaded testing (for debugging)
C-Test -SingleThreadedTesting

# Skip build and test single-threaded
C-Test -NoBuild -SingleThreadedTesting
```

### C-Reload-Module
Reloads your PowerShell profile without restarting the shell.

```powershell
C-Reload-Module
```

**Important**: This force-reloads the Compze module first to pick up any changes to DevScripts, then reloads your profile. Use this after editing any module files.

### C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
Ensures .csproj files exclude .cs files from projects in subfolders and properly handle _docs folders.

```powershell
C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
```

### C-Remove-RedundantInternalsVisibleTo
Removes redundant InternalsVisibleTo attributes.

```powershell
C-Remove-RedundantInternalsVisibleTo
```

### C-Validate-SolutionStructure
Validates the Compze solution structure.

```powershell
C-Validate-SolutionStructure
```

### C-Get-Commands
Lists all available commands.

```powershell
C-Get-Commands
```

## Key Benefits

✅ **Work from any directory** - All commands automatically resolve paths relative to Compze root  
✅ **Fast by default** - C-Test runs tests without building (use `-Build` when needed)  
✅ **Parallel builds** - Full parallel compilation for maximum speed  
✅ **Parallel test execution** - Tests run in parallel according to assembly-level attributes  
✅ **Easy profile reload** - `C-Reload-Module` picks up DevScripts changes without restarting  
✅ **Clean command names** - Simple, discoverable C-* function names (type `C-<Tab>` to see all)

## Technical Details

### Why Build Then Test Separately?

There's a known race condition in `dotnet test` when it builds and runs tests in one command - test adapters (NUnit, XUnit) may not be fully copied to the output directory before test discovery starts, causing intermittent failures.

**Workaround**: Always build first (`dotnet build`), then run tests with `--no-build` flag. This is why `C-Test -Build` uses two separate commands instead of letting `dotnet test` handle the build.

### Test Execution

Tests run in parallel by default, respecting your assembly-level parallelization attributes:
- NUnit: `[assembly: Parallelizable(ParallelScope.All)]`
- XUnit: `[assembly: CollectionBehavior(DisableTestParallelization = false)]`

Use `-SingleThreadedTesting` for debugging when you need sequential execution.

## Files

- **Compze.psm1** - Main module with function definitions
- **README.md** - Detailed documentation
- **ProfileExample.ps1** - Example profile configuration
