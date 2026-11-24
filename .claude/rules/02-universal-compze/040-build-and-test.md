# Build and test

## Prerequisites

- .NET SDK (version specified in `global.json`, at the repository root)
- PowerShell with the DevScripts module imported: see [DevScripts](050-devscripts.md)

## Building

```powershell
C-Build                  # Build the solution (preferred)
C-Build -Clean           # Deep clean then build
dotnet build Compze.AllProjects.slnx  # Alternative: direct .NET CLI
C-Build-Website          # Build the website with DocFX: fails, listing them, on any warning
```

A change to a published page (`_docs/`) is checked with `C-Build-Website`, and, when the page's example code
(`_docs/*.cs`) changed, with `C-Build`, which compiles it.

## Running tests

```powershell
# DevScripts (preferred) — C-Test BUILDS BY DEFAULT
C-Test                         # Build + run all tests
C-Test -NoBuild                # Skip building, just run tests
C-Test -SingleThreadedTesting  # Sequential execution (for debugging)
C-Test -Iterations 5           # Run suite 5 times, show summary
C-Test -Clean                  # Deep clean + build + test
C-Test -FullGitReset           # Full git clean + build + test

# A subset of the tests, on chosen combinations of pluggable components, or both
C-Test -Filter "FullyQualifiedName~MyTestClass"
C-Test -Combinations 'PgSql:Microsoft:Newtonsoft:AspNetCore', 'MySql:Microsoft:Newtonsoft:AspNetCore'
C-Test -Combinations 'MsSql:Microsoft:Newtonsoft:AspNetCore' -Filter "FullyQualifiedName~CQRS.TaggregateStores"
```

`-Combinations` runs the tests on those combinations, and leaves the configuration file and its copies (below) as they
were, also when the run fails or is interrupted. Use it rather than editing the file, which needs a rebuild, and whose
copies a restored file doesn't overwrite.

## Benchmarks: what code costs in nanoseconds and bytes

`test/Compze.Tests.Benchmarks` holds BenchmarkDotNet benchmarks, for comparing two versions of code too fast for a
stopwatch: `dotnet run -c Release --project test/Compze.Tests.Benchmarks -- --filter *LazyCE*`. Add a benchmark class
there rather than a throwaway project. A size that must not grow is specified as well, with
`ManagedAllocation.BytesAllocatedBy` from Compze.Tests.Infrastructure.

## Test configuration

- Config file: `src/TestUsingPluggableComponentCombinations` (auto-created from `.defaults` on first build). The build
  copies it into each test project's output, which is what the tests read: after editing it, build again. A run with
  `-NoBuild` or `--no-build` silently uses the combinations of the last build.
- Format: `PersistenceLayer:DIContainer:Serializer:Transport` (one combination per line, `#` to comment out)
- Default active combination: `SqliteMemory:Microsoft:Newtonsoft:AspNetCore`

## Testing policy

- **Test thoroughly**: a change to code, or to anything the build or the tests read, isn't done until the full test
  suite has passed. A change to documents alone, or to comments alone, needs none of it.
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **`COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY`**: Set to `true` (the default in CI and `C-Test`) to run
  performance tests as stress tests only, disabling timing assertions. Set to `false` to re-enable timing checks.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test
  timing expectations on slow machines. Only applies when stress-test-only mode is off.
- **Don't write one test per pluggable component** — use `[PCT]`
  (see [compze-test-conventions](../path-scoped/compze-test-conventions.md), which loads when touching `test/**`).
