# Compze Development Tools Setup

## Quick Start

Add this line to your PowerShell profile:

```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking
```

To edit your profile, run: `notepad $PROFILE`

## Available Commands

### C-Test
Runs the full test suite. Default is to run tests without building (assumes already built).

```powershell
# Run tests (default - no build)
C-Test

# Build then test
C-Test -Build

# Single-threaded testing (for debugging)
C-Test -SingleThreadedTesting

# Build and test single-threaded
C-Test -Build -SingleThreadedTesting
```

### C-Reload-Module
Reloads your PowerShell profile without restarting the shell.

```powershell
C-Reload-Module
```

**Important**: This force-reloads the Compze module first to pick up any changes to DevScripts, then reloads your profile. Use this after editing any module files.

### C-Ensure-CsprojExcludesCsFiles
Ensures .csproj files exclude .cs files from projects in subfolders.

```powershell
C-Ensure-CsprojExcludesCsFiles
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

## Files Created

- **Compze.psm1** - Main module with function definitions
- **Compze.psd1** - Module manifest
- **README.md** - Detailed documentation
- **ProfileExample.ps1** - Example profile configuration
