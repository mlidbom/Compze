# rules for slnx file and file system structure

## Flat Layout Convention

Each project lives in its own top-level directory under `src/` (library) or `test/` (test):

- **Library projects**: `src/<ProjectName>/<ProjectName>.csproj`
- **Test projects**: `test/<ProjectName>/<ProjectName>.csproj`

Examples:
  - `src/Compze.Core/Compze.Core.csproj`
  - `src/Compze.DependencyInjection.Microsoft/Compze.DependencyInjection.Microsoft.csproj`
  - `test/Compze.Tests.Unit/Compze.Tests.Unit.csproj`

## Solution Folder Organization

Projects are organized into solution folders by category:

| Solution Folder | Contents |
|----------------|----------|
| `/Compze/` | Root-level library projects (e.g., Compze.Core, Compze.Tessaging, Compze.Utilities) |
| `/Compze/<SubGroup>/` | Library projects grouped by first component after "Compze." (e.g., `/Compze/Utilities/`, `/Compze/Sql/`) |
| `/_Tests/` | All test projects |
| `/_Samples/` | Sample applications |
| `/_Websites/` | Documentation website |
| `/~Solution Structure/` | MSBuild and solution structure projects |

## Key Rules

- Directory name MUST match project name exactly
- No project nesting — each project is at the top level of `src/` or `test/`
- Not every folder in a project directory must have a csproj — subdirectories are just folders belonging to the project