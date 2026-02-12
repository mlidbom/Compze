# Compze Repository - Copilot Instructions

## Rules — Follow These First

- **Minimal changes**: Make the smallest possible changes to accomplish the task.
- **Don't fix unrelated issues**: Focus only on the task at hand.
- **Test thoroughly**: Always run the full test suite before finalizing.
- **RULE: A full test run must execute at least 958 tests. Fewer = failure.**
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **Documentation**: Update docs only if directly related to your changes.
- **Before committing**: Run `C-Test`. Run `C-Validate-SolutionStructure` if you've modified project structure.
- **InternalsVisibleTo**: Use to maintain encapsulation; run `C-Remove-RedundantInternalsVisibleTo` to clean up.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test timing expectations on slow machines.

### Do Not Modify Without Asking
- `src/Directory.Build.props`
- `src/msbuild/*.props`
- `src/Compze.slnx` (use DevScripts `C-Create-Project`, `C-Rename-Project`, etc.)
- `src/TestUsingPluggableComponentCombinations` (user's local config)
- `src/solution-file-structure.README.md`

### Common Pitfalls
- **Don't write one test per pluggable component** — use `[PCT]` (see Pluggable Component Testing below).
- **Don't run `dotnet test` without `--no-build`** if you've already built.
- **DevScripts must be imported** before using `C-*` commands — don't assume they're loaded. Import with: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`
- **After editing `.csproj` files**, run `C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders`.

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

```
src/
  Compze.slnx                # Solution file
  Directory.Build.props       # Shared MSBuild properties (do not modify)
  TestUsingPluggableComponentCombinations  # Active test config (do not modify)
  Compze/                     # Main framework code
    Abstractions/             # Core abstractions (Compze.Core)
    Serialization/            # Serialization (Newtonsoft)
    Sql/                      # SQL persistence (Common, MicrosoftSql, MySql, PostgreSql, Sqlite)
    Tessaging/                # Type-based messaging + Teventive + Hosting
    Utilities/                # Diverse utilities
      DependencyInjection/    # DI abstractions and implementations
      Testing/                # Testing infrastructure
      Logging/                # Logging infrastructure
      SystemCE/               # System class extensions
      Contracts/              # Code contracts
      Functional/             # Functional programming helpers
  Tests/
    Common/                   # Shared test base classes (e.g., DocumentDbTestsBase)
    Infrastructure/           # Test infrastructure (XUnit attributes, UniversalTestBase)
    Unit/                     # Unit tests
    Integration/              # Integration tests (pluggable component combinations)
    Performance/              # Performance tests
    ScratchPad/               # Scratch/experimental tests
    Compze.Tests.CodePolicies/  # Code policy enforcement tests
  Samples/                    # Sample applications (AccountManagement)
  Websites/Website/           # DocFX documentation site
DevScripts/                   # PowerShell development automation module
```

### Naming Conventions
- **Projects**: Follow namespace structure (e.g., `Compze.Utilities.DependencyInjection` → `src/Compze/Utilities/DependencyInjection/`)
- **Interfaces**: Prefix with `I` (e.g., `IUserEvent`, `IAggregateEvent`)
- **Variables/Methods**: Use descriptive names; long names are acceptable if they improve clarity

## Pluggable Component Testing Pattern

Tests that need to run against all configured pluggable component combinations use this pattern:

1. **Inherit from `UniversalTestBase`** (in `Tests/Infrastructure/`)
2. **Decorate test methods with `[PCT]`** (Pluggable Component Theory, in `Tests/Infrastructure/XUnit/`)
3. **Access current combination via the static `TestEnv` class** — methods take zero parameters

### Template — copy this for new PCT tests:
```csharp
using Compze.Tessaging.Hosting.Testing;
using Compze.Tests.Infrastructure;
using Compze.Tests.Infrastructure.XUnit;

namespace Compze.Tests.Integration.MyFeature;

public class MyFeatureTests : UniversalTestBase
{
   [PCT]
   public void My_test_method()
   {
      using var serviceLocator = TestEnv.DIContainer.SetupTestingServiceLocator(_ => { });
      // ... test logic using serviceLocator
   }
}
```

### Template — copy this for non-PCT unit tests:
```csharp
using Compze.Tests.Infrastructure;
using Compze.Utilities.Testing.Must;
using Xunit;

namespace Compze.Tests.Unit.MyFeature;

public class MyFeatureTests : UniversalTestBase
{
   [Fact]
   public void My_test_method() => 42.Must().Be(42);
}
```

**Attribute variants:**
- `[PCT]` — runs for all 4 component types (SqlLayer × DIContainer × Serializer × Transport)
- `[PCTSerializer]` — only varies the Serializer component
- `[PCTDIContainer]` — only varies the DIContainer component

**DO NOT** write one test per pluggable component. The `[PCT]` mechanism automatically tests ALL enabled combinations.

**Good examples to reference:**
- Simple: `Tests/Integration/Infrastructure/PluggableComponentsTheoryTests.cs`
- With service locator: `Tests/Common/Sql/DocumentDb/DocumentDbTestsBase.cs`

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
- [Development Setup](DEVELOPMENT.md)
