# PackageReferenceOrProjectReferenceIfTargetInSolution

Solution-aware MSBuild reference resolution for .NET projects.

## The Problem

When a .NET solution contains many projects that are also published as NuGet packages, you face a choice:
- **ProjectReference** — great for development (source debugging, instant refactoring across all projects), but requires all projects in the solution
- **PackageReference** — works with any subset of projects, but you lose all the above advantages.

## Our Solution

In your `.csproj`:

```xml
<ItemGroup>
    <PackageReferenceOrProjectReferenceIfTargetInSolution Include="MyLibrary" Version="2.0.0" />
</ItemGroup>
```

At build and restore time:
- If `MyLibrary.csproj` is in the current solution this is replaced by a normal project reference.
- If not this is replaced by a normal package reference.

## Installation

1. Copy [`PackageReferenceOrProjectReferenceIfTargetInSolution.targets`](src/PackageReferenceOrProjectReferenceIfTargetInSolution.targets) into your repository (e.g., into a `msbuild/` folder)

2. Import it from your `Directory.Build.targets` (create the file if it doesn't exist):

```xml
<Project>
    <Import Project="$(MSBuildThisFileDirectory)msbuild/PackageReferenceOrProjectReferenceIfTargetInSolution.targets" />
</Project>
```

> **Why a file copy instead of a NuGet auto-import?** This tool must participate in NuGet restore's project graph evaluation. NuGet package imports aren't available until *after* restore completes.

## Primary use case

### Partial Solutions for Faster Development

Create focused solution files for different parts of your codebase:

```
MyFramework.everything.slnx         ← contains all projects. Used for CI, packaging, cross cutting refactoring etc.
MyFramework.top-level-concerns.slnx ← excludes all utility projects to keep the solution small and fast.
MyFramework.Utilities.slnx          ← just utilities + their tests
MyFramework.Samples.slnx            ← just samples (framework = NuGet packages)
```

## Requirements

- Solution files in `.slnx` format

## Compatibility

Confirmed to work with:
- Visual Studio
- VS Code (C# Dev Kit)
- JetBrains Rider
- `dotnet build` / `dotnet restore` CLI

## License

Unlicense
