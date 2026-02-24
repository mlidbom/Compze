# FlexRef Adoption Plan

## Goal

Enable working on subsets of the Compze solution with fast builds and fast
NCrunch feedback. Open a subset `.slnx` and projects not in it automatically
resolve as NuGet PackageReferences instead of ProjectReferences.

Uses [Compze.Build.FlexRef](https://github.com/mlidbom/Compze.Build.FlexRef) — 
a solution-aware MSBuild `.props` file that auto-detects which projects are in
the current `.slnx` and sets `UsePackageReference_*` flags accordingly.

## Scope

- **23 src projects** (22 have src→src references, 228 switchable references total)
- **10 test projects** (93 switchable src references)
- **7 sample projects** (33 switchable src references)
- **4 subset solutions** planned (plus the existing full solution)

## Phases

### Phase 1: Add FlexRef Infrastructure

- [x] Copy `FlexRef.props` into `src/msbuild/FlexRef.props`
- [x] Import it from `src/Directory.Build.props` (before any `UsePackageReference_*` declarations)
- [x] Add one `UsePackageReference_*` property per switchable src project (24 properties)

Property naming convention: `UsePackageReference_{PackageName_with_dots_replaced_by_underscores}`

The 23 properties needed:

```xml
<PropertyGroup>
  <!-- Utilities (leaf → high-level) -->
  <UsePackageReference_Compze_Contracts        Condition="..." />
  <UsePackageReference_Compze_Functional                  Condition="..." />
  <UsePackageReference_Compze_Utilities                  Condition="..." />
  <UsePackageReference_Compze_Utilities_SystemCE         Condition="..." />
  <UsePackageReference_Compze_Utilities_SystemCE_ThreadingCE Condition="..." />
  <UsePackageReference_Compze_Utilities_Logging          Condition="..." />
  <UsePackageReference_Compze_Utilities_DependencyInjection Condition="..." />
  <UsePackageReference_Compze_Utilities_DependencyInjection_Microsoft Condition="..." />
  <UsePackageReference_Compze_Utilities_DependencyInjection_SimpleInjector Condition="..." />
  <UsePackageReference_Compze_Utilities_Logging_Serilog  Condition="..." />
  <UsePackageReference_Compze_Utilities_Testing_Must     Condition="..." />
  <UsePackageReference_Compze_Utilities_Testing_XUnit    Condition="..." />
  <UsePackageReference_Compze_Utilities_Testing_DbPool   Condition="..." />

  <!-- Core + Serialization -->
  <UsePackageReference_Compze_Core                       Condition="..." />
  <UsePackageReference_Compze_Serialization_Newtonsoft   Condition="..." />

  <!-- SQL -->
  <UsePackageReference_Compze_Sql_Common                 Condition="..." />
  <UsePackageReference_Compze_Sql_MicrosoftSql           Condition="..." />
  <UsePackageReference_Compze_Sql_MySql                  Condition="..." />
  <UsePackageReference_Compze_Sql_PostgreSql             Condition="..." />
  <UsePackageReference_Compze_Sql_Sqlite                 Condition="..." />

  <!-- Tessaging -->
  <UsePackageReference_Compze_Tessaging                  Condition="..." />
  <UsePackageReference_Compze_Tessaging_Hosting_AspNetCore Condition="..." />
  <UsePackageReference_Compze_Tessaging_Hosting_Testing  Condition="..." />
  <UsePackageReference_Compze_Tessaging_Teventive_TeventStore Condition="..." />
</PropertyGroup>
```

Each property's condition follows the pattern:
```xml
<UsePackageReference_Compze_Core
    Condition="'$(UsePackageReference_Compze_Core)' != 'true'
             And '$(_SwitchRef_SolutionProjects)' != ''
             And !$(_SwitchRef_SolutionProjects.Contains('|Compze.Core.csproj|'))">true</UsePackageReference_Compze_Core>
```

### Phase 2: Convert ProjectReferences to Conditional Pairs

For every switchable `ProjectReference` in every `.csproj`, replace:
```xml
<ProjectReference Include="..\Compze.Core\Compze.Core.csproj" />
```
with:
```xml
<ItemGroup Condition="'$(UsePackageReference_Compze_Core)' == 'true'">
  <PackageReference Include="Compze.Core" Version="0.1.0-alpha.3" />
</ItemGroup>
<ItemGroup Condition="'$(UsePackageReference_Compze_Core)' != 'true'">
  <ProjectReference Include="..\Compze.Core\Compze.Core.csproj" />
</ItemGroup>
```

**Scope by project type:**

| Area | Projects | Switchable refs | Notes |
|------|----------|----------------|-------|
| src→src | 22 projects | 102 refs | All src cross-refs become switchable |
| test→src | 10 projects | 93 refs | Only src refs; test→test stays as ProjectReference |
| samples→src | 7 projects | 33 refs | Only src refs; sample→sample stays as ProjectReference |
| **Total** | **39 projects** | **228 refs** | |

**Important**: Each converted reference needs the correct `Version` matching the
current package version (`0.1.0-alpha.3`). Consider `Directory.Packages.props`
(Central Package Management) later when version bumps become painful.

### Phase 3: Create Subset Solutions

Recreate subset `.slnx` files (relative to `src/`). Planned solutions:

| Solution | Projects | Use case |
|----------|----------|----------|
| `Compze.slnx` (existing) | All 51 | Full development, CI |
| `Compze.Utilities.slnx` | 13 src + 3 test | Working on utility libraries |
| `Compze.Utilities.Testing.slnx` | 3 src + 2 test | Working on test infrastructure |
| `Compze.WithoutUtilities.slnx` | 11 src + 0 test | Working on higher-level Compze libs |
| `Compze.Samples.slnx` | 4 sample src + 3 sample test + 1 test infra | Working on samples only |

### Phase 4: DevScripts — `C-Verify-FlexRef`

Create a validation script that checks:
- Every `.csproj` with a switchable `ProjectReference` has the matching
  conditional `PackageReference` pair
- Every subset `.v3.ncrunchsolution` has `CustomBuildProperties` for all src
  projects NOT in its corresponding `.slnx`
- Package versions in conditional `PackageReference` blocks are consistent
- No orphaned `UsePackageReference_*` properties (project was renamed/removed)

### Phase 5: DevScripts — `C-Update-NCrunchSolutionConfigs`

Create a script that automatically generates/updates `.v3.ncrunchsolution`
files to match their corresponding `.slnx` files:
- Parse each subset `.slnx` to extract which src projects are included
- Derive which `UsePackageReference_*` flags must be `true` (for all src
  projects NOT in that solution)
- Write/overwrite the `.v3.ncrunchsolution` file with the correct
  `CustomBuildProperties`
- For the full solution, ensure `CustomBuildProperties` is empty
- Preserve other NCrunch settings (`AllowParallelTestExecution`, `EnableRDI`,
  etc.) if the file already exists

This eliminates the manual sync burden: when you add/remove a project from a
subset solution, just re-run `C-Update-NCrunchSolutionConfigs`.

### Phase 6: DevScripts — `C-Pack` / `C-Clear-NuGetCache`

Ensure local package workflow works smoothly:
- `C-Pack`: Build and pack all src projects to the local `nupkgs/` feed
- `C-Clear-NuGetCache`: Clear NuGet HTTP + global-packages cache for Compze
  packages so subset solutions pick up freshly packed versions

### Phase 7: Verify

- [x] Full solution (`Compze.slnx`) builds and all tests pass (1164/1164)
- [x] Each subset solution builds with correct PackageReferences
- [x] NCrunch works in full solution
- [x] NCrunch works in subset solutions
- [x] `dotnet build src/Compze.slnx` (CLI, no solution context) still works
  (falls through to ProjectReference — the safe default)
- [x] CI (`dotnet build src/Compze.slnx`) passes

### Phase 8: Consider Moving Scripts to FlexRef Project

Evaluate which scripts are Compze-specific vs. generally reusable:

| Script | Compze-specific? | Move to FlexRef? |
|--------|-----------------|-----------------|
| `C-Verify-FlexRef` | Mostly generic — validates the FlexRef pattern | **Yes** — any FlexRef consumer would need this |
| `C-Update-NCrunchSolutionConfigs` | Mostly generic — reads `.slnx` + generates ncrunch configs | **Yes** — the ncrunch config generation is a FlexRef concern |
| `C-Pack` | Compze-specific (knows which projects to pack) | **No** — repo-specific |
| `C-Clear-NuGetCache` | Partially generic (cache clearing is universal) | **Maybe** — generic enough but trivial |

The FlexRef project could ship these as scripts alongside the `.props` file
(in the NuGet content package or as a companion PowerShell module). This would
benefit any repo adopting FlexRef, not just Compze.

If moved, Compze's DevScripts would import/wrap them rather than owning them
directly.

## Dependency Graph (src→src)

```
Compze.Contracts (leaf)
├── Compze.Functional
│   ├── Compze.Utilities
│   ├── Compze.Utilities.SystemCE.ThreadingCE
│   │   └── Compze.Utilities.SystemCE
│   │       └── Compze.Utilities.Logging
│   │           └── Compze.Utilities.DependencyInjection
│   │               ├── Compze.Utilities.DependencyInjection.Microsoft
│   │               ├── Compze.Utilities.DependencyInjection.SimpleInjector
│   │               ├── Compze.Utilities.Logging.Serilog
│   │               ├── Compze.Utilities.Testing.Must
│   │               ├── Compze.Utilities.Testing.XUnit
│   │               └── Compze.Core
│   │                   ├── Compze.Serialization.Newtonsoft
│   │                   ├── Compze.Sql.Common
│   │                   │   ├── Compze.Sql.MicrosoftSql
│   │                   │   ├── Compze.Sql.MySql
│   │                   │   ├── Compze.Sql.PostgreSql
│   │                   │   ├── Compze.Sql.Sqlite
│   │                   │   └── Compze.Tessaging
│   │                   │       ├── Compze.Tessaging.Hosting.AspNetCore
│   │                   │       └── Compze.Tessaging.Teventive.TeventStore
│   │                   └── Compze.Utilities.Testing.DbPool
│   │                       (also refs: Serialization.Newtonsoft, Sql.Common)
│   └── Compze.Tessaging.Hosting.Testing
│       (refs: most of Tessaging + Sql + DI + Testing)
```

## Notes

- **Package version**: All src projects are currently at `0.1.0-alpha.3`.
  FlexRef conditional `PackageReference` blocks will use this version. When
  bumping, all conditional blocks need updating — consider Central Package
  Management (`Directory.Packages.props`) if this becomes painful.
- **test→test and sample→sample references**: Stay as regular
  `ProjectReference` — they are never published as NuGet packages.
- **`Compze.Build.InternalizedSourceReferences`**: Separate sub-solution,
  already has its own build. Not part of the FlexRef switching.
- **`Compze.Utilities.SystemCE.ThreadingCE`**: Uses
  `Compze.Build.InternalizedSourceReferences` to internalize source from
  `Compze.Utilities.SystemCE`. This PackageReference is NOT switchable
  (it's a build tool, not a library reference).
- **Duplicate ProjectReference**: `Compze.Tests.Infrastructure` has a
  duplicate reference to `Compze.Utilities.Testing.XUnit` — should be
  cleaned up during Phase 2.
