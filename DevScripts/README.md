# Compze PowerShell Development Module

This module provides convenient PowerShell commands for Compze development.

## Available Commands

- **Fix-CsprojExclusions** - Fixes .csproj exclusions for Compze projects
- **Remove-RedundantInternalsVisibleTo** - Removes redundant InternalsVisibleTo attributes
- **Validate-SolutionStructure** - Validates the Compze solution structure
- **Test-Compze** - Runs the full test suite with proper configuration
- **Fix-Encodings** - Converts files to UTF-8 with BOM encoding
- **Reload-Profile** - Reloads your PowerShell profile without restarting

## Setup

To load these commands automatically in your PowerShell profile:

### Option 1: Add to Profile (Recommended)

1. Open your PowerShell profile:
   ```powershell
   notepad $PROFILE
   ```

2. Add this line to the end of your profile:
   ```powershell
   Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking
   ```

3. Reload your profile:
   ```powershell
   . $PROFILE
   ```

### Option 2: Manual Import

Load the module manually when needed:
```powershell
Import-Module C:\Dev\Compze\DevScripts\Compze.psd1 -DisableNameChecking
```

## Usage

### Test-Compze

Run tests (default - no build, assumes already built):
```powershell
Test-Compze
```

Build and run tests:
```powershell
Test-Compze -Build
```

Run tests single-threaded (useful for debugging):
```powershell
Test-Compze -SingleThreadedTesting
```

Build and run tests single-threaded:
```powershell
Test-Compze -Build -SingleThreadedTesting
```

**Note**: When using `-Build`, it uses `-m:1` (single-threaded build) to avoid .NET test runner race conditions.

### Fix-Encodings

Convert all git-tracked .cs files to UTF-8 without BOM (default path is `src`):
```powershell
Fix-Encodings
```

Check what would be converted without making changes:
```powershell
Fix-Encodings -WhatIf
```

Convert files in a specific path:
```powershell
Fix-Encodings -Path "src/Tests"
```

Convert specific file types:
```powershell
Fix-Encodings -FilePattern "*.csproj"
```

This command ensures consistent UTF-8 without BOM encoding across the codebase, which is the modern standard for .NET projects. UTF-8 without BOM provides better cross-platform compatibility and prevents git diff issues caused by BOM changes.

### Other Commands

These commands work from any directory:
```powershell
Fix-CsprojExclusions
Remove-RedundantInternalsVisibleTo
Validate-SolutionStructure
Reload-Profile  # Force-reloads Compze module and your profile
```

**Note**: `Reload-Profile` automatically force-reloads the Compze module to pick up any changes to DevScripts files, then reloads your profile.

## How It Works

- All commands can be run from any directory - the module automatically resolves paths relative to the Compze root
- The `Test-Compze` command:
  - Changes to the `src` directory
  - Builds the solution with `-m:1` (single-threaded, if not using `-NoBuild`)
  - Runs tests with `--no-build` flag (tests run in parallel per assembly attributes)
  - Returns to the original directory when done

## Why Single-Threaded Build?

The `-m:1` flag forces a single-threaded **build**, which is necessary to avoid a race condition in .NET's test runner where test adapters may not be ready when tests start in parallel builds.

**However, tests themselves run in parallel** according to your assembly-level parallelization attributes (e.g., `[assembly: Parallelizable]` for NUnit, `[assembly: CollectionBehavior]` for XUnit).
