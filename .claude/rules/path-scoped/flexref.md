---
paths:
  - "**/*.csproj"
  - "**/Directory.Build.props"
  - "**/FlexRef.config.xml"
---

# FlexRef — Flexible Project/Package References

## What It Is

FlexRef ([Compze.Build.FlexRef](https://www.nuget.org/packages/Compze.Build.FlexRef/)) is a dotnet tool that generates MSBuild boilerplate so that references to packable projects become **flex references** — `ProjectReference` when the referenced project is in the current solution, `PackageReference` when it is not.

This lets us maintain multiple `.slnx` solutions of any size: a monolithic one with everything, and focused ones with just a subset of projects. The same csproj works in all of them without modification.

## How It Works

### At build time (MSBuild)

1. `build/FlexRef.props` (imported by root `Directory.Build.props`) reads the current `.slnx` file and extracts project filenames into `$(_FlexRef_SolutionProjects)`.
2. Per-dependency `UsePackageReference_*` properties in root `Directory.Build.props` evaluate to `true` when the referenced project is **absent** from the solution.
3. Each csproj has conditional `ItemGroup` pairs that switch between `ProjectReference` and `PackageReference` based on the flag.

### At sync time (CLI tool)

`flexref sync` scans all `.csproj` files under the configured directory, detects references to packable projects, and generates/updates:
- `build/FlexRef.props` — the solution-parsing MSBuild infrastructure
- `Directory.Build.props` — the `UsePackageReference_*` property declarations
- All `.csproj` files — the conditional `ItemGroup` pairs
- `.v3.ncrunchsolution` files — NCrunch `CustomBuildProperties`

## Repository Layout

| File | Location | Managed by |
|---|---|---|
| `FlexRef.config.xml` | Repo root | Developer (configuration) |
| `build/FlexRef.props` | Repo root | `flexref sync` (generated) |
| `Directory.Build.props` | Repo root | `flexref sync` (FlexRef section) |
| `.csproj` flex references | `src/`, `test/`, `samples/` | `flexref sync` (generated) |

**Important**: `FlexRef.config.xml` and `flexref sync` operate from the **repository root**, not from `src/`. This is required so that `flexref sync` discovers and manages csproj files in `src/`, `test/`, and `samples/` — all of which reference packable projects.

## What a Flex Reference Looks Like in a .csproj

```xml
<!-- Compze.Contracts — flex reference -->
<ItemGroup Condition="'$(UsePackageReference_Compze_Contracts)' == 'true'">
  <PackageReference Include="Compze.Contracts" Version="*-*" />
</ItemGroup>
<ItemGroup Condition="'$(UsePackageReference_Compze_Contracts)' != 'true'">
  <ProjectReference Include="..\..\src\Compze.Contracts\Compze.Contracts.csproj" />
</ItemGroup>
```

The `Version="*-*"` wildcard resolves to the highest pre-release version available in the local `nupkgs/` feed.

## Configuration

`FlexRef.config.xml` at the repo root controls which packages become flex references:

```xml
<FlexRef>
  <AutoDiscover />
  <ExcludeDirectory Path="src/Websites/Website/Compze" />
</FlexRef>
```

`<AutoDiscover />` scans all `.csproj` files to find packable projects.

`<ExcludeDirectory Path="…" />` points scanning away from a directory subtree (path relative to the repo
root; multiple allowed). **We need it because of the docs-website junctions.**
`src/Websites/Website/Ensure-CoLocatedDocsJunctions.ps1` creates a git-ignored directory junction per
documented project under `src/Websites/Website/Compze/` (e.g. `Compze/Teventive` → `../../Compze.Teventive`)
so DocFX can read co-located `_docs`. Those junctions resolve **back into `src/`**, so without the exclusion
`flexref sync` discovers every junctioned project a second time (same file, two paths) and:
- injects a bogus second `ProjectReference` (to the `…\Website\Compze\…` path) into every consumer, and
- rewrites the real project's own references with junction-relative `..\..\..\..\` paths — the "corrupts
  every csproj" symptom.

Excluding the junction root fixes both while leaving genuine symlinks/junctions elsewhere scannable. This is
the supported fix (FlexRef ≥ the version that adds `<ExcludeDirectory>`); do **not** hand-author flex blocks
to work around it.

## Syncing After Changes

Run the DevScript command after adding/removing/renaming projects or changing references:

```powershell
C-FlexRef-Sync
```

This runs `flexref sync` from the repo root. **Never run `flexref sync` from a subdirectory** — it must see all projects to generate correct relative paths.

## Key Rules

- **Never hand-edit the *content* of flex reference sections** — `flexref sync` generates and normalizes it. But in `<AutoDiscover />` mode the csproj flex blocks are themselves the source of truth (there is no central dependency list), so adding or removing a dependency happens in the csproj, followed by a sync.
- **Removing a dependency = delete its whole flex block, then sync**: remove the three-part block (the `<!-- X — flex reference -->` comment plus both conditional `ItemGroup`s) from the csproj and run `C-FlexRef-Sync`, which renormalizes everything and updates `build/FlexRef.props` and the `.v3.ncrunchsolution` files.
- **Never hand-edit the FlexRef section in `Directory.Build.props`** — it is generated by `flexref sync`.
- **Always run `C-FlexRef-Sync` after structural changes** — adding projects, renaming projects, changing project references.
- **Focused solutions require `C-Pack` first** — the `PackageReference` path needs packages in `nupkgs/`. Run `C-Pack` to populate them.
- **The monolithic solution (`Compze.AllProjects.slnx`) needs no packages** — all references resolve as `ProjectReference` (except ISR which is always a `PackageReference`).
