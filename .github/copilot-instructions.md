# Compze Repository - Copilot Instructions

## Repository Overview

Compze is a .NET framework for building expressive domains through:
- **Teventive programming**: Type-routed events that leverage .NET type compatibility for elegant event modeling
- **Typermedia APIs**: Type-based message routing that extends hypermedia principles with .NET types

## Tech Stack

- **Language**: C# (.NET)
- **Testing**: xUnit
- **Build System**: MSBuild (.NET SDK)
- **Dependency Injection**: Pluggable (Microsoft DI, SimpleInjector)
- **Persistence**: Pluggable (SQLite in-memory, SQL Server, PostgreSQL, MySQL)
- **Documentation**: DocFX
- **Development Tools**: PowerShell module (`DevScripts/`)

## Build and Test Instructions

### Prerequisites
- Visual Studio 2022, JetBrains Rider, or Visual Studio Code
- .NET SDK (version specified in `global.json`)
- PowerShell (for DevScripts commands)

### Building the Solution
```bash
# Using DevScripts PowerShell module (preferred)
C-Build

# Using .NET CLI
dotnet build src/Compze.slnx
```

### Running Tests
```bash
# Using DevScripts (preferred)
C-Test                    # Run tests without building
C-Test -Build             # Build then test
C-Test -SingleThreadedTesting  # For debugging

# Using .NET CLI
dotnet test src/Compze.slnx
```

**Important**: A full test run should execute at least 958 tests. If fewer tests run, something is wrong.

### Test Configuration
- Default: SQLite in-memory with Microsoft DI
- Configuration file: `TestUsingPluggableComponentCombinations` (auto-created from `.example` if missing)
- Edit this file to test against external databases (SQL Server, PostgreSQL, MySQL)

## Project Structure

### Directory Layout
```
/src/Compze/           # Main framework code
  /Abstractions/       # Core abstractions (Compze.Core)
  /Tessaging/          # Type-based messaging
  /Utilities/          # Utilities and testing infrastructure
/Tests/                # Tests organized by component
/DevScripts/           # PowerShell development automation
/Websites/Website/     # DocFX documentation website
```

### Naming Conventions
- **Projects**: Follow namespace structure (e.g., `Compze.Wiring.Testing` → `src/Compze/Wiring/Testing/`)
- **Interfaces**: Prefix with `I` (e.g., `IUserEvent`, `IAggregateEvent`)
- **Variables/Methods**: Use descriptive names; long names are acceptable if they improve clarity
- **Documentation**: Co-located with code in `_docs/` folders within project directories

## Key Architectural Patterns

### Pluggable Components Testing
- **DO** test pluggable components (DI containers, persistence layers) using `DuplicateByPluggableComponentTest` structure
- **DO NOT** write one test per pluggable component; the structure automatically tests ALL combinations including future versions
- Configuration: `TestUsingPluggableComponentCombinations` file

### Teventive Programming
- Events use interface inheritance for type-based routing
- Example: `IUserImported : IUserRegistered : IUserEvent : IAggregateEvent`
- Subscribers receive events they're compatible with through type hierarchy

### InternalsVisibleTo
- **DO** use `InternalsVisibleTo` to maintain encapsulation within framework code
- Use the PowerShell command `C-Remove-RedundantInternalsVisibleTo` to clean up unnecessary attributes

## Code Style and Conventions

### Comments
- **DO NOT** add `// Arrange`, `// Act`, `// Assert` comments in tests
- **DO NOT** sprinkle explanatory comments everywhere; use descriptive names instead
- **DO** add comments only when necessary to explain complex logic or when they match existing comment style

### Exception Handling
- **DO NOT** swallow exceptions in catch blocks without rethrowing

### Logging (DevScripts)
- **DO NOT** write log spam in PowerShell functions
- **Success should be silent** - only output when something goes wrong
- This keeps the console clean and makes problems easy to spot

### Performance Tests
- If performance tests fail, rerun them
- Repeated failures are NOT acceptable
- Use `COMPOSABLE_MACHINE_SLOWNESS` environment variable to adjust expectations if needed

## Documentation

### Co-Location Pattern
- Documentation lives in `_docs/` folders next to the code it documents
- Files are excluded from compilation via `Directory.Build.props`
- Included as `None` items for visibility in Solution Explorer
- See `src/Documentation-CoLocation.README.md` for details

### DocFX
- Main documentation site in `Websites/Website/`
- Generated from both markdown and code comments
- See `Websites/Website/README.md` for building instructions

## Development Workflow

### DevScripts Commands
The repository includes a PowerShell module with helpful commands:
- `C-Test` - Run tests with proper configuration
- `C-Build` - Build the solution
- `C-Clean` - Deep clean the solution
- `C-Create-Project` - Create new projects with proper structure
- `C-Validate-SolutionStructure` - Validate solution structure
- `C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders` - Fix .csproj exclusions
- `C-Get-Commands` - List all available commands

See `DevScripts/README.md` and `DevScripts/QUICKSTART.md` for complete documentation.

### Before Committing
- Run the full test suite (`C-Test`)
- Ensure at least 958 tests execute successfully
- Run `C-Validate-SolutionStructure` if you've modified project structure

## Important Notes

- **Minimal changes**: Make the smallest possible changes to accomplish the task
- **Test thoroughly**: Validate that your changes don't break existing behavior
- **Full test suite**: Always run all tests before finalizing changes
- **Don't fix unrelated issues**: Focus only on the task at hand
- **Documentation updates**: Update docs if directly related to your changes

## External Resources

- [Project Website](http://compze.net/)
- [Semantic Events Documentation](https://compze.net/paradigms/semantic-events/definition.html)
- [Development Setup](DEVELOPMENT.md)
