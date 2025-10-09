# Compze Development Tools Setup

## Quick Start

Add this line to your PowerShell profile:

```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking
```

To edit your profile, run: `notepad $PROFILE`

## Available Commands

### Test-Compze
Runs the full test suite. Default is to run tests without building (assumes already built).

```powershell
# Run tests (default - no build)
Test-Compze

# Build then test
Test-Compze -Build

# Single-threaded testing (for debugging)
Test-Compze -SingleThreadedTesting

# Build and test single-threaded
Test-Compze -Build -SingleThreadedTesting
```

### Reload-Profile
Reloads your PowerShell profile without restarting the shell.

```powershell
Reload-Profile
```

**Important**: This force-reloads the Compze module first to pick up any changes to DevScripts, then reloads your profile. Use this after editing any module files.

### Fix-CsprojExclusions
Fixes .csproj exclusions for Compze projects.

```powershell
Fix-CsprojExclusions
```

### Remove-RedundantInternalsVisibleTo
Removes redundant InternalsVisibleTo attributes.

```powershell
Remove-RedundantInternalsVisibleTo
```

### Validate-SolutionStructure
Validates the Compze solution structure.

```powershell
Validate-SolutionStructure
```

## Key Benefits

✅ **Work from any directory** - All commands automatically resolve paths relative to Compze root  
✅ **Fast by default** - Test-Compze runs tests without building (use `-Build` when needed)
✅ **Single-threaded builds when needed** - Using `-Build` flag uses `-m:1` to avoid .NET test runner race conditions  
✅ **Parallel test execution** - Tests run in parallel according to assembly-level attributes  
✅ **Easy profile reload** - `Reload-Profile` picks up DevScripts changes without restarting  
✅ **Clean command names** - Simple, memorable function names

## Technical Details

### Why Single-Threaded Build but Parallel Tests?

**Build**: The .NET test runner has a race condition in parallel builds where test adapters (NUnit, XUnit) may not be fully copied to the output directory before tests start. Using `-m:1` for the build ensures all test adapters are ready.

**Tests**: Once built, tests can run in parallel safely. The test runner respects your assembly-level parallelization attributes:
- NUnit: `[assembly: Parallelizable(ParallelScope.All)]`
- XUnit: `[assembly: CollectionBehavior(DisableTestParallelization = false)]`

This gives you:
- ✅ Reliable test discovery (single-threaded build)
- ✅ Fast test execution (parallel test running)

## Files Created

- **Compze.psm1** - Main module with function definitions
- **Compze.psd1** - Module manifest
- **README.md** - Detailed documentation
- **ProfileExample.ps1** - Example profile configuration
