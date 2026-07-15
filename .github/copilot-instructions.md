# Compze Repository - Copilot Instructions

## Rules — Follow These First

- **Don't code until instructed to**. Standard workflow is questions back and forth coming up with what to do. Then I give the go ahead to code. Questions are not instructions to start coding, they are questions to be answered.
- **Test thoroughly**: Always run the full test suite before finalizing.
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **`COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY`**: Set to `true` (the default in CI and `C-Test`) to run performance tests as stress tests only, disabling timing assertions. Set to `false` to re-enable timing checks.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test timing expectations on slow machines. Only applies when stress-test-only mode is off.

### Honesty About Blockers — MANDATORY
- **REFUSE to start work when you lack what you need to succeed.** If the target design is unclear, if you don't know what the end state should look like, if the instructions leave a fundamental gap — SAY SO immediately. Do not guess. Do not substitute a cosmetic change for a structural one. Do not defer the real problem to a later phase.
- **Moving files is not separating concerns.** If you would need to add a cross-reference back to make it compile, the dependency hasn't changed — you are renaming the entanglement, not removing it. Call this out instead of shipping it.
- **Ask the design question upfront.** When structural work requires a design decision you cannot make alone — "What should the plugin mechanism look like?", "Should these be separate endpoints or one?" — stop and ask. The user can answer in 30 seconds what would take you hours of wrong guessing.
- **Name what you don't know.** "I don't know how X should work after this change" is always preferable to silently preserving the old architecture and reporting success.
- **Never hide behind "existing architecture."** Existing entanglement is not a constraint — it is the problem to be solved. "That's how it works today" is never a reason to keep it that way.

### Common Pitfalls
- **Don't write one test per pluggable component** — use `[PCT]` (see Pluggable Component Testing below).
- **DevScripts must be imported** before using `C-*` commands — don't assume they're loaded. Import with: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`
- **New package versions**: When creating a new packable project, set the `<Version>` to an early pre-release version (e.g., `0.1.0-alpha.1`). **NEVER** use `1.0.0` or any stable-looking version for something brand new and in development. Package versions are NOT synchronized across projects — each project has its own version.

## Repository Overview

Compze is a .NET framework for building expressive domains through:
- **Teventive programming**: Type-routed events (also called "Semantic Events" in documentation) that leverage .NET type compatibility for elegant event modeling
- **Typermedia APIs**: Type-based message routing that extends hypermedia principles with .NET types

## Tech Stack

- **Language**: C# (.NET 10, see `src/global.json`)
- **Testing**: xUnit v3 (via `Compze.xUnit`, `Compze.xUnitBDD`, `Compze.xUnitMatrix`)
- **Build System**: MSBuild (.NET SDK), solution file: `Compze.AllProjects.slnx`
- **References**: FlexRef — auto-switches between `ProjectReference` and `PackageReference` depending on which projects are in the current solution (see `flexref.instructions.md`)
- **Dependency Injection**: Pluggable (Microsoft DI, Autofac)
- **Persistence**: Pluggable (SQLite in-memory, SQL Server, PostgreSQL, MySQL)
- **Serialization**: Pluggable (Newtonsoft)
- **Transport**: Pluggable (AspNetCore)
- **Documentation**: DocFX (site in `src/Websites/Website/`)
- **Development Tools**: PowerShell module (`DevScripts/Compze.psm1`)

## Build and Test

### Prerequisites
- .NET SDK (version specified in `src/global.json`)
- PowerShell with DevScripts module imported: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

### Building
```powershell
C-Build                  # Build the solution (preferred)
C-Build -Clean           # Deep clean then build
dotnet build Compze.AllProjects.slnx  # Alternative: direct .NET CLI
```

### Running Tests
```powershell
# DevScripts (preferred) — C-Test BUILDS BY DEFAULT
C-Test                         # Build + run all tests
C-Test -NoBuild                # Skip building, just run tests
C-Test -SingleThreadedTesting  # Sequential execution (for debugging)
C-Test -Iterations 5           # Run suite 5 times, show summary
C-Test -Clean                  # Deep clean + build + test
C-Test -FullGitReset           # Full git clean + build + test

# Running a subset of tests (no DevScripts support, use dotnet directly)
dotnet test Compze.AllProjects.slnx --no-build --filter "FullyQualifiedName~MyTestClass"
```

### Test Configuration
- Config file: `src/TestUsingPluggableComponentCombinations` (auto-created from `.defaults` on first build)
- Format: `PersistenceLayer:DIContainer:Serializer:Transport` (one combination per line, `#` to comment out)
- Default active combination: `SqliteMemory:Microsoft:Newtonsoft:AspNetCore`
- Uncomment lines to test against external databases (SQL Server, PostgreSQL, MySQL) or other DI containers

## Project Structure

Flat layout — each project has its own top-level directory:
- Library projects: `src/<ProjectName>/<ProjectName>.csproj`
- Test projects: `test/<ProjectName>/<ProjectName>.csproj`
- Multiple `.slnx` files exist for different subsets; `Compze.AllProjects.slnx` is the monolith

Discover actual projects by listing `src/` and `test/` directories.

### Test Project Naming
- **`.Specifications`** — BDD-style specification projects (preferred for new projects): `test/Compze.Contracts.Specifications/`
- **`.Tests.`** — older integration/unit test projects: `test/Compze.Tests.Integration/`

## Pluggable Component Testing Pattern

Tests that need to run against all configured pluggable component combinations use this pattern:

1. **Inherit from `UniversalTestBase`** (in `test/Compze.Tests.Infrastructure/`)
2. **Decorate test methods with `[PCT]`** (Pluggable Component Theory)
3. **Access current combination via the static `TestEnv` class** — methods take zero parameters

**Attribute variants:**
- `[PCT]` — runs for all 4 component types (SqlLayer × DIContainer × Serializer × Transport)
- `[PCTSerializer]` — only varies the Serializer component

**DO NOT** write one test per pluggable component. `[PCT]` automatically tests ALL enabled combinations.

## Teventive Programming
- Tevents (type-routed events) use interface inheritance for type-based routing
- Example: `IUserImported : IUserRegistered : IUserTevent : ITaggregateTevent`
- Subscribers receive every tevent compatible with their subscribed type through the type hierarchy

## Documentation Co-Location
- Documentation lives in `_docs/` folders next to the code it documents
- See `src/Documentation-CoLocation.README.md` for details

## DevScripts

Import: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

Discover all commands: `C-Get-Commands` or `C-<Tab>` in PowerShell. Key commands:

| Command | Purpose |
|---------|---------|
| `C-Test` | Build + run full test suite |
| `C-Build` | Build the solution |
| `C-Clean` | Deep clean the solution |
| `C-Create-Project` | Create new projects with proper structure |
| `C-Delete-Project` | Delete a project |
| `C-Rename-Project` | Rename a project |
| `C-Split-Project` | Split a project into multiple |
| `C-Merge-Project` | Merge projects |
| `C-FlexRef-Sync` | Sync FlexRef infrastructure after reference changes |
| `C-Validate-SolutionStructure` | Validate solution structure |

## External Resources
- [Project Website](https://compze.net/)
- [Semantic Events Documentation](https://compze.net/paradigms/semantic-events/definition.html)
