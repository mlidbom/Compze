# PackageReferenceOrProjectReferenceIfTargetInSolution

Solution-aware MSBuild reference resolution for .NET projects.

## The Problem

When a .NET solution contains many projects that are also published as NuGet packages, you face a choice:
- **ProjectReference** — great for development (source debugging, instant refactoring across all projects), but requires all projects in the solution
- **PackageReference** — works with any subset of projects, but you lose all the above advantages.

## Our Solution

Use standard `ProjectReference` items with metadata that enables automatic conversion:

```xml
<ItemGroup>
    <ProjectReference Include="../MyLibrary/MyLibrary.csproj"
                      ConvertToPackageWhenNotInSolution="true"
                      PackageFallbackVersion="2.0.0" />
</ItemGroup>
```

At build and restore time:
- If `MyLibrary.csproj` is in the current solution → stays as a normal ProjectReference.
- If not → the ProjectReference is replaced by a PackageReference (using `PackageFallbackVersion` as the version).

## Installation

1. Copy [`PackageReferenceOrProjectReferenceIfTargetInSolution.targets`](src/PackageReferenceOrProjectReferenceIfTargetInSolution.targets) into your repository (e.g., into a `msbuild/` folder)

2. Import it from your `Directory.Build.targets` (create the file if it doesn't exist):

```xml
<Project>
    <Import Project="$(MSBuildThisFileDirectory)msbuild/PackageReferenceOrProjectReferenceIfTargetInSolution.targets" />
</Project>
```

> **Why a file copy instead of a NuGet auto-import?** This tool must participate in NuGet restore's project graph evaluation. NuGet package imports aren't available until *after* restore completes.

## Primary use cases

### Partial Solutions for Faster Development

Create focused solution files for different parts of your codebase:

```
MyFramework.everything.slnx         ← contains all projects. Used for CI, packaging, cross cutting refactoring etc.
MyFramework.top-level-concerns.slnx ← excludes all utility projects to keep the solution small and fast.
MyFramework.Utilities.slnx          ← just utilities + their tests
MyFramework.Samples.slnx            ← just samples (framework = NuGet packages)
```

### Switching Between Consuming and Contributing to a Library

When your application depends on a library you also maintain, create two solutions with the same project files:

```
MyApp.slnx              ← just your app projects. Library references resolve from NuGet packages.
MyApp.WithLibrary.slnx  ← your app projects + library projects. Library references become live ProjectReferences.
```

Switch between "consuming the library" and "developing the library" by opening a different solution — no changes to any `.csproj` files.

## Requirements

- Solution files in `.slnx` format

## Compatibility

Confirmed to work with:
- Visual Studio 2026
- Visual Studio Code (C# Dev Kit and Resharper)
- JetBrains Rider
- `dotnet build` / `dotnet restore` CLI

### Ncrunch
- Under NCrunch no replacement occurs. NCrunch still works, but will build using the original project references

## License

Unlicense
