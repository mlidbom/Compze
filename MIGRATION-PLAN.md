# Migration Plan: Nested → Standard .NET Directory Structure

## Problem Statement

The current structure nests projects inside each other — dots in project names map to directory separators (e.g., `Compze.Tessaging.Hosting.AspNetCore` → `src/Compze/Tessaging/Hosting/AspNetCore/`). This means parent `.csproj` files must exclude child project directories via `<Compile Remove>`, maintained by a custom DevScript. While elegant in principle, it causes:

- Confusing project boundaries (files in `Hosting/` belong to the parent, `Hosting/AspNetCore/` is a separate project)
- Extra build complexity (`Compile Remove` exclusions, a script to maintain them)
- Non-standard layout unfamiliar to contributors
- Tooling friction (IDE navigation, refactoring across project boundaries)

## Target Structure

Standard .NET OSS layout with `src/` and `test/` top-level directories. Each project gets its own flat directory named after the assembly.

### Current → Target Mapping

#### Library Projects: `src/`

| Project | Current Path | Target Path |
|---------|-------------|-------------|
| `Compze.Core` | `src/Compze/Abstractions/` | `src/Compze.Core/` |
| `Compze.Utilities` | `src/Compze/Utilities/` | `src/Compze.Utilities/` |
| `Compze.Utilities.DependencyInjection.Microsoft` | `src/Compze/Utilities/DependencyInjection/Microsoft/` | `src/Compze.Utilities.DependencyInjection.Microsoft/` |
| `Compze.Utilities.DependencyInjection.SimpleInjector` | `src/Compze/Utilities/DependencyInjection/SimpleInjector/` | `src/Compze.Utilities.DependencyInjection.SimpleInjector/` |
| `Compze.Utilities.Logging.Serilog` | `src/Compze/Utilities/Logging/Serilog/` | `src/Compze.Utilities.Logging.Serilog/` |
| `Compze.Utilities.Testing.DbPool` | `src/Compze/Utilities/Testing/DbPool/` | `src/Compze.Utilities.Testing.DbPool/` |
| `Compze.Utilities.Testing.Must` | `src/Compze/Utilities/Testing/Must/` | `src/Compze.Utilities.Testing.Must/` |
| `Compze.Utilities.Testing.XUnit` | `src/Compze/Utilities/Testing/XUnit/` | `src/Compze.Utilities.Testing.XUnit/` |
| `Compze.Tessaging` | `src/Compze/Tessaging/` | `src/Compze.Tessaging/` |
| `Compze.Tessaging.Hosting.AspNetCore` | `src/Compze/Tessaging/Hosting/AspNetCore/` | `src/Compze.Tessaging.Hosting.AspNetCore/` |
| `Compze.Tessaging.Hosting.Testing` | `src/Compze/Tessaging/Hosting/Testing/` | `src/Compze.Tessaging.Hosting.Testing/` |
| `Compze.Tessaging.Teventive.TeventStore` | `src/Compze/Tessaging/Teventive/TeventStore/` | `src/Compze.Tessaging.Teventive.TeventStore/` |
| `Compze.Serialization.Newtonsoft` | `src/Compze/Serialization/Newtonsoft/` | `src/Compze.Serialization.Newtonsoft/` |
| `Compze.Sql.Common` | `src/Compze/Sql/Common/` | `src/Compze.Sql.Common/` |
| `Compze.Sql.MicrosoftSql` | `src/Compze/Sql/MicrosoftSql/` | `src/Compze.Sql.MicrosoftSql/` |
| `Compze.Sql.MySql` | `src/Compze/Sql/MySql/` | `src/Compze.Sql.MySql/` |
| `Compze.Sql.PostgreSql` | `src/Compze/Sql/PostgreSql/` | `src/Compze.Sql.PostgreSql/` |
| `Compze.Sql.Sqlite` | `src/Compze/Sql/Sqlite/` | `src/Compze.Sql.Sqlite/` |

#### Test Projects: `test/`

| Project | Current Path | Target Path |
|---------|-------------|-------------|
| `Compze.Tests.Unit` | `src/Tests/Unit/` | `test/Compze.Tests.Unit/` |
| `Compze.Tests.Unit.Internals` | `src/Tests/Unit/Internals/` | `test/Compze.Tests.Unit.Internals/` |
| `Compze.Tests.Integration` | `src/Tests/Integration/` | `test/Compze.Tests.Integration/` |
| `Compze.Tests.Common` | `src/Tests/Common/` | `test/Compze.Tests.Common/` |
| `Compze.Tests.Infrastructure` | `src/Tests/Infrastructure/` | `test/Compze.Tests.Infrastructure/` |
| `Compze.Tests.ScratchPad` | `src/Tests/ScratchPad/` | `test/Compze.Tests.ScratchPad/` |
| `Compze.Tests.Performance.Internals` | `src/Tests/Performance/Internals/` | `test/Compze.Tests.Performance.Internals/` |
| `Compze.Tests.CodePolicies` | `src/Tests/Compze.Tests.CodePolicies/` | `test/Compze.Tests.CodePolicies/` |
| `Compze.Utilities.Tests` | `src/Compze/Utilities/Tests/` | `test/Compze.Utilities.Tests/` |
| `Compze.Utilities.Testing.XUnit.Tests` | `src/Compze/Utilities/Testing/XUnit/Tests/` | `test/Compze.Utilities.Testing.XUnit.Tests/` |

#### Unchanged

| Project | Current Path | Notes |
|---------|-------------|-------|
| `Website` | `src/Websites/Website/` | Stays (different naming convention) |
| `AccountManagement.*` | `src/Samples/...` | Deferred — separate migration |
| `Solution.*` | `src/SolutionStructure/`, `src/msbuild/` | Stays |

### Target Directory Tree (simplified)

```
src/
  Compze.Core/
    Compze.Core.csproj
    Configuration/
    DocumentDb/
    Tessaging/          ← abstractions, NOT the Tessaging project
    ...
  Compze.Utilities/
    Compze.Utilities.csproj
    Contracts/
    DependencyInjection/  ← shared DI abstractions (NOT the child projects)
      Abstractions/
      ComponentRegistrar.cs
      ...
    Functional/
    Logging/              ← shared logging abstractions (NOT Serilog project)
    SystemCE/
    ...
  Compze.Utilities.DependencyInjection.Microsoft/
    Compze.Utilities.DependencyInjection.Microsoft.csproj
    ...
  Compze.Utilities.DependencyInjection.SimpleInjector/
    ...
  Compze.Utilities.Logging.Serilog/
    ...
  Compze.Utilities.Testing.DbPool/
    ...
  Compze.Utilities.Testing.Must/
    ...
  Compze.Utilities.Testing.XUnit/
    ...
  Compze.Tessaging/
    Compze.Tessaging.csproj
    Abstractions/
    Hosting/              ← files here belong to Tessaging, NOT child projects
      Endpoint.cs
      Abstractions/
      Configuration/
      Sql/
    Teventive/            ← abstractions, NOT TeventStore project
    ...
  Compze.Tessaging.Hosting.AspNetCore/
    ...
  Compze.Tessaging.Hosting.Testing/
    ...
  Compze.Tessaging.Teventive.TeventStore/
    ...
  Compze.Serialization.Newtonsoft/
    ...
  Compze.Sql.Common/
    ...
  Compze.Sql.MicrosoftSql/
  Compze.Sql.MySql/
  Compze.Sql.PostgreSql/
  Compze.Sql.Sqlite/
  Directory.Build.props     ← NuGet metadata for library projects (moved from src/Compze/)
  Websites/
  Samples/
  msbuild/
  SolutionStructure/
  Compze.slnx
test/
  Compze.Tests.Unit/
  Compze.Tests.Unit.Internals/
  Compze.Tests.Integration/
  Compze.Tests.Common/
  Compze.Tests.Infrastructure/
  Compze.Tests.ScratchPad/
  Compze.Tests.Performance.Internals/
  Compze.Tests.CodePolicies/
  Compze.Utilities.Tests/
  Compze.Utilities.Testing.XUnit.Tests/
  Directory.Build.props     ← marks IsTestProject=true, inherits from parent
```

## What Changes and Why

### 1. Move Projects to Flat Directories (the big one)

**Algorithm** — process leaf projects first, then parents:

1. **Phase 1: Extract leaves** — Move deepest-nested child projects out first (e.g., `Compze.Utilities.Testing.XUnit.Tests` before `Compze.Utilities.Testing.XUnit`). After extraction, the parent directory only contains parent-owned files.
2. **Phase 2: Move parents** — Move remaining parent projects (e.g., `Compze.Utilities`, `Compze.Tessaging`) to their flat locations. All their intermediate directories (with owned files) come along.
3. **Phase 3: Move test projects** — Move all test projects to `test/`.

Use `git mv` to preserve history.

### 2. Remove `<Compile Remove>` Exclusions

After flattening, no project contains child projects in subdirectories. All `<Compile Remove>` sections auto-generated by `C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders` become unnecessary and should be removed. 

The `_docs` handling stays (documentation co-location pattern is independent of project nesting).

### 3. Restructure `Directory.Build.props` Hierarchy

**Current:**
```
src/Directory.Build.props           ← detects test projects by path, imports msbuild props
src/Compze/Directory.Build.props    ← NuGet metadata
```

**Target:**
```
src/Directory.Build.props           ← NuGet metadata (moved from Compze/)
                                      No longer needs path-based IsTestProject detection
test/Directory.Build.props          ← <IsTestProject>true</IsTestProject>, imports src/Directory.Build.props
```

### 4. Update `ProjectReference` Paths

All `<ProjectReference>` relative paths need recalculating from the new project locations. The migration script handles this automatically.

### 5. Regenerate Solution File (`Compze.slnx`)

Update all `<Project Path="...">` entries and solution folder structure.

### 6. Update MSBuild Props

- `IncludeTestAppSettings.props` — Update path to `test-common-appsettings.json` (move it or adjust relative path)
- `IncludeTestConfigurationFiles.props` — Adjust relative path to `TestUsingPluggableComponentCombinations`

### 7. Update Website Project

`Website.csproj` globs for `_docs` files using `Compze\**\_docs\*.cs`. Since library projects now live at `src/Compze.*/`, the glob patterns need updating to scan all `src/Compze.*/` directories (or use a junction/symlink approach).

### 8. Update DevScripts

- `C-Create-Project` — New projects go to `src/ProjectName/` (library) or `test/ProjectName/` (test), no dot-splitting
- `C-Relocate-Project` — Becomes simpler (just move to `src/` or `test/` flat dir)
- `C-Ensure-CsprojfilesExclude...` — Simplify: only `_docs` handling needed, no more child project exclusions
- Update any hardcoded path assumptions

### 9. Move `test-common-appsettings.json` and Test Config

Currently at `src/Compze/test-common-appsettings.json`. Move to `test/test-common-appsettings.json` or a shared location, and update the MSBuild props accordingly.

### 10. Move `.editorconfig` for Tests

Currently `src/Tests/.editorconfig`. Move to `test/.editorconfig`.

---

## Migration Script Approach

A PowerShell migration script is the best approach because:
- The project already has PowerShell DevScripts with XML manipulation patterns
- `git mv` operations preserve history
- The existing `C-Relocate-Project` script demonstrates the reference-updating logic

### Script Algorithm (pseudocode)

```
1. Parse Compze.slnx to build project list with current paths
2. Compute target paths for each project
3. Sort by nesting depth (deepest first)
4. For each project (deepest first):
   a. Create target directory
   b. git mv <source-dir-contents> <target-dir>/
   c. Update <ProjectReference> paths in the moved csproj
5. For ALL csproj files: recalculate ProjectReference relative paths
6. Remove <Compile Remove>, <EmbeddedResource Remove>, <None Remove> sections 
   that were for child project exclusions (keep _docs exclusions)
7. Rewrite Compze.slnx with updated paths
8. Restructure Directory.Build.props files
9. Update msbuild .props paths
10. Update Website.csproj _docs globs
11. Clean up empty directories in old src/Compze/ and src/Tests/
12. Run: dotnet build to verify
```

### Risk Mitigation

- **Run on a clean branch** — easy to reset if something goes wrong
- **Verify with `dotnet build`** after migration
- **Run full test suite** (`C-Test`) as final validation
- **Git history preserved** via `git mv`

---

## Estimated Scope

| Category | Items | Complexity |
|----------|-------|------------|
| Project moves | 28 projects | Mechanical (scriptable) |
| ProjectReference updates | ~100+ references | Mechanical (scriptable) |
| Solution file rewrite | 1 file | Mechanical (scriptable) |
| Directory.Build.props restructure | 2-3 files | Manual design needed |
| MSBuild props updates | 2 files | Small |
| Website _docs references | 1 file | Small |
| DevScripts updates | 4-5 functions | Medium |
| Remove Compile Remove exclusions | ~5 csproj files | Mechanical (scriptable) |

**Recommendation:** Build the migration script first. The mechanical parts (moves, reference updates, solution file) are ~80% of the work and fully automatable. The remaining ~20% (Directory.Build.props, DevScripts, Website) require manual thought but are small in volume.
