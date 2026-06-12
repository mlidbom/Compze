# Build and test

## Prerequisites

- .NET SDK (version specified in `src/global.json`)
- PowerShell with DevScripts module imported: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

## Building

```powershell
C-Build                  # Build the solution (preferred)
C-Build -Clean           # Deep clean then build
dotnet build src/Compze.AllProjects.slnx  # Alternative: direct .NET CLI
```

## Running tests

```powershell
# DevScripts (preferred) — C-Test BUILDS BY DEFAULT
C-Test                         # Build + run all tests
C-Test -NoBuild                # Skip building, just run tests
C-Test -SingleThreadedTesting  # Sequential execution (for debugging)
C-Test -Iterations 5           # Run suite 5 times, show summary
C-Test -Clean                  # Deep clean + build + test
C-Test -FullGitReset           # Full git clean + build + test

# Running a subset of tests (no DevScripts support, use dotnet directly)
dotnet test src/Compze.AllProjects.slnx --no-build --filter "FullyQualifiedName~MyTestClass"
```

## Test configuration

- Config file: `src/TestUsingPluggableComponentCombinations` (auto-created from `.defaults` on first build)
- Format: `PersistenceLayer:DIContainer:Serializer:Transport` (one combination per line, `#` to comment out)
- Default active combination: `SqliteMemory:Microsoft:Newtonsoft:AspNetCore`

## Testing policy

- **Test thoroughly**: Always run the full test suite before finalizing.
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **`COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY`**: Set to `true` (the default in CI and `C-Test`) to run
  performance tests as stress tests only, disabling timing assertions. Set to `false` to re-enable timing checks.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test
  timing expectations on slow machines. Only applies when stress-test-only mode is off.
- **Don't write one test per pluggable component** — use `[PCT]`
  (see [compze-test-conventions](../path-scoped/compze-test-conventions.md), which loads when touching `test/**`).
