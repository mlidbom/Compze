# Internalized Source References

Applies to: `src/**/*.csproj`

## What It Is

Some of our utility libraries have circular dependencies. For example, `Compze.Utilities.SystemCE` references `Compze.Utilities.SystemCE.ThreadingCE` via `ProjectReference`, and `ThreadingCE` needs code from `SystemCE`. A normal `ProjectReference` back would create a cycle that MSBuild cannot resolve.

We solve this with **internalized source references**: an MSBuild task (`Compze.InternalizedSourceReferences` NuGet package) that copies the source files from one project into an `InternalizedSource/` folder in another project, rewriting all `public` type declarations to `internal`. This gives the consumer its own private copy of the code — no runtime sharing of static state, and internalized types **must not** appear in the consumer's public API.

## How It Works in a .csproj

A project that internalizes source has these elements:

```xml
<PropertyGroup>
   <InternalizeSourceFrom>..\SourceProjectA;..\SourceProjectB</InternalizeSourceFrom>
   <InternalizeSourceTo>$(MSBuildProjectDirectory)\InternalizedSource</InternalizeSourceTo>
</PropertyGroup>

<ItemGroup>
   <PackageReference Include="Compze.InternalizedSourceReferences" Version="0.1.0-alpha.2" PrivateAssets="all" />
</ItemGroup>
```

- `InternalizeSourceFrom` — semicolon-separated paths to source project directories
- `InternalizeSourceTo` — always `$(MSBuildProjectDirectory)\InternalizedSource`
- The rewriting runs before compilation (`BeforeTargets="CoreCompile"`) and only writes files when content changes
- The `InternalizedSource/` folders are `.gitignore`d — they are regenerated on build

## Current Internalized Dependencies

| Consumer project | Internalizes source from |
|---|---|
| `Compze.Utilities.SystemCE.ThreadingCE` | `Compze.Utilities.SystemCE` |
| `Compze.Utilities.Functional` | `Compze.Utilities.SystemCE`, `Compze.Utilities.SystemCE.ThreadingCE` |
| `Compze.Utilities.Contracts` | `Compze.Utilities.SystemCE`, `Compze.Utilities.SystemCE.ThreadingCE`, `Compze.Utilities.Functional` |

These form a circular dependency cluster: `SystemCE` references `ThreadingCE`, `Functional`, and `Contracts` via `ProjectReference`, while those three projects internalize source from `SystemCE` (and each other) to use its code without creating a `ProjectReference` cycle.

## Adding a New Internalized Source Reference

Use the DevScript command:

```powershell
C-Add-InternedSourceReference -ConsumerCsprojPath <path-to-consumer.csproj> -SourceProjectDir <path-to-source-project-dir>
```

This adds the `PackageReference` and `InternalizeSourceFrom`/`InternalizeSourceTo` properties automatically.

## Key Constraints

- **No public API exposure**: Internalized types are rewritten to `internal` — they cannot appear in the consumer's public method signatures, property types, base classes, or interface implementations.
- **No shared static state**: Each consumer gets its own copy of the code, so static fields/properties are independent across consumers.
- **The `Compze.InternalizedSourceReferences` sub-solution** lives at `Compze.InternalizedSourceReferences/` in the repo root — see its [README](../../Compze.InternalizedSourceReferences/README.md) and [DEVELOPMENT.md](../../Compze.InternalizedSourceReferences/DEVELOPMENT.md) for implementation details, testing, and packaging.
