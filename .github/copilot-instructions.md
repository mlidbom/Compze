# Compze Repository - Copilot Instructions

## Rules — Follow These First

- **Don't code until instructed to**. Standard workflow is questions back and forth coming up with what to do. Then I give the go ahead to code. Questions are not instructions to start coding, they are questions to be answered.
- **Test thoroughly**: Always run the full test suite before finalizing.
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test timing expectations on slow machines.

### Common Pitfalls
- **Don't write one test per pluggable component** — use `[PCT]` (see Pluggable Component Testing below).
- **DevScripts must be imported** before using `C-*` commands — don't assume they're loaded. Import with: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`
- **New package versions**: When creating a new packable project, set the `<Version>` to an early pre-release version (e.g., `0.1.0-alpha.1`). **NEVER** use `1.0.0` or any stable-looking version for something brand new and in development. Package versions are NOT synchronized across projects — each project has its own version.

## Repository Overview

Compze is a .NET framework for building expressive domains through:
- **Teventive programming**: Type-routed events (also called "Semantic Events" in documentation) that leverage .NET type compatibility for elegant event modeling
- **Typermedia APIs**: Type-based message routing that extends hypermedia principles with .NET types

## Tech Stack

- **Language**: C# (.NET 9+, see `src/global.json`)
- **Testing**: xUnit
- **Build System**: MSBuild (.NET SDK), solution file: `src/Compze.slnx`
- **Dependency Injection**: Pluggable (Microsoft DI, SimpleInjector)
- **Persistence**: Pluggable (SQLite in-memory, SQL Server, PostgreSQL, MySQL)
- **Serialization**: Pluggable (Newtonsoft)
- **Transport**: Pluggable (Memory, AspNetCore)
- **Documentation**: DocFX (site in `Websites/Website/`)
- **Development Tools**: PowerShell module (`DevScripts/Compze.psm1`)

## Build and Test

### Prerequisites
- .NET SDK (version specified in `src/global.json`)
- PowerShell with DevScripts module imported: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

### Building
```powershell
C-Build                  # Build the solution (preferred)
C-Build -Clean           # Deep clean then build
dotnet build src/Compze.slnx  # Alternative: direct .NET CLI
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
dotnet test src/Compze.slnx --no-build --filter "FullyQualifiedName~MyTestClass"
```

### Test Configuration
- Config file: `src/TestUsingPluggableComponentCombinations` (auto-created from `.defaults` on first build)
- Format: `PersistenceLayer:DIContainer:Serializer:Transport` (one combination per line, `#` to comment out)
- Default active combination: `SqliteMemory:Microsoft:Newtonsoft:Memory`
- Uncomment lines to test against external databases (SQL Server, PostgreSQL, MySQL) or other DI containers

## Project Structure

The solution uses a **flat layout** — each project has its own top-level directory:
- Library projects: `src/<ProjectName>/<ProjectName>.csproj`
- Test projects: `test/<ProjectName>/<ProjectName>.csproj`

```
src/
  Compze.slnx                         # Solution file
  Directory.Build.props                # Shared MSBuild properties (do not modify)
  TestUsingPluggableComponentCombinations  # Active test config (do not modify)
  Compze.Core/                         # Core abstractions
  Compze.Serialization.Newtonsoft/     # Newtonsoft serialization
  Compze.Sql.Common/                   # SQL persistence (common)
  Compze.Sql.MicrosoftSql/             # SQL Server persistence
  Compze.Sql.MySql/                    # MySQL persistence
  Compze.Sql.PostgreSql/               # PostgreSQL persistence
  Compze.Sql.Sqlite/                   # SQLite persistence
  Compze.Tessaging/                    # Type-based messaging + Teventive
  Compze.Tessaging.Hosting.AspNetCore/ # ASP.NET Core hosting
  Compze.Tessaging.Hosting.Testing/    # Testing hosting
  Compze.Tessaging.Teventive.TeventStore/ # TeventStore
  Compze.Utilities/                    # Diverse utilities
  Compze.Utilities.DependencyInjection.Microsoft/   # Microsoft DI
  Compze.Utilities.DependencyInjection.SimpleInjector/ # SimpleInjector DI
  Compze.Utilities.Logging.Serilog/    # Serilog logging
  Compze.Utilities.Testing.DbPool/     # Database pool for testing
  Compze.Utilities.Testing.Must/       # Must assertion library
  Compze.Utilities.Testing.XUnit/      # xUnit testing infrastructure
  Websites/Website/                    # DocFX documentation site
test/
  Compze.Tests.CodePolicies/           # Code policy enforcement tests
  Compze.Tests.Common/                 # Shared test base classes
  Compze.Tests.Infrastructure/         # Test infrastructure (XUnit attributes, UniversalTestBase)
  Compze.Tests.Integration/            # Integration tests
  Compze.Tests.Performance.Internals/  # Performance tests
  Compze.Tests.ScratchPad/             # Scratch/experimental tests
  Compze.Tests.Unit/                   # Unit tests
  Compze.Tests.Unit.Internals/         # Unit tests (internals)
  Compze.Utilities.Testing.XUnit.Tests/ # Testing framework tests
  Compze.Utilities.Tests/              # Utility tests
Samples/                             # Sample applications (AccountManagement)
DevScripts/                            # PowerShell development automation module
```

### Naming Conventions
- **Variables/Methods**: Use descriptive names; long names are acceptable if they improve clarity

## Pluggable Component Testing Pattern

Tests that need to run against all configured pluggable component combinations use this pattern:

1. **Inherit from `UniversalTestBase`** (in `Tests/Infrastructure/`)
2. **Decorate test methods with `[PCT]`** (Pluggable Component Theory, in `Tests/Infrastructure/XUnit/`)
3. **Access current combination via the static `TestEnv` class** — methods take zero parameters


**Attribute variants:**
- `[PCT]` — runs for all 4 component types (SqlLayer × DIContainer × Serializer × Transport)
- `[PCTSerializer]` — only varies the Serializer component
- `[PCTDIContainer]` — only varies the DIContainer component

**DO NOT** write one test per pluggable component. The `[PCT]` mechanism automatically tests ALL enabled combinations.

**Good examples to reference:**
- Simple: `test/Compze.Tests.Integration/Infrastructure/PluggableComponentsTheoryTests.cs`
- With service locator: `test/Compze.Tests.Common/Sql/DocumentDb/DocumentDbTestsBase.cs`

## Teventive Programming
- Events use interface inheritance for type-based routing
- Example: `IUserImported : IUserRegistered : IUserEvent : IAggregateEvent`
- Subscribers receive events they're compatible with through type hierarchy

## Documentation Co-Location
- Documentation lives in `_docs/` folders next to the code it documents
- Files are excluded from compilation via `Directory.Build.props` (`DefaultItemExcludes`)
- Included as `None` items for visibility in Solution Explorer and refactoring participation
- See `src/Documentation-CoLocation.README.md` for full details

## DevScripts Commands

Import: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking` (add to `$PROFILE`)

| Command | Purpose |
|---------|---------|
| `C-Test` | Build + run full test suite |
| `C-Build` | Build the solution |
| `C-Clean` | Deep clean the solution |
| `C-Create-Project` | Create new projects with proper structure |
| `C-Validate-SolutionStructure` | Validate solution structure |
| `C-Remove-RedundantInternalsVisibleTo` | Clean up unnecessary InternalsVisibleTo attributes |
| `C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders` | Fix .csproj exclusions |
| `C-Get-Commands` | List all available commands |
| `C-Rename-Project` | Rename a project |
| `C-Relocate-Project` | Move a project to a new location |
| `C-Split-Project` | Split a project into multiple |
| `C-Merge-Project` | Merge projects |

Type `C-<Tab>` in PowerShell to discover all commands.

## External Resources
- [Project Website](https://compze.net/)
- [Semantic Events Documentation](https://compze.net/paradigms/semantic-events/definition.html)
- [Development Setup](../DEVELOPMENT.md)
