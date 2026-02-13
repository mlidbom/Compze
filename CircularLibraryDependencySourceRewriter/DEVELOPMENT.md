# Development

## Build & Test

```powershell
dotnet build CircularLibraryDependencySourceRewriter.slnx
dotnet test CircularLibraryDependencySourceRewriter.slnx
```

## Project Structure

```
CircularLibraryDependencySourceRewriter.slnx
global.json
src/
  CircularLibraryDependencySourceRewriter/
    SourceRewriter.cs                                  # Core rewriting logic
    RewriteDirectoryTask.cs                            # MSBuild ITask implementation
    CircularLibraryDependencySourceRewriter.targets     # MSBuild integration (packed into NuGet)
    CircularLibraryDependencySourceRewriter.csproj      # Configured as NuGet package
test/
  CircularLibraryDependencySourceRewriter.Tests/
    When_rewriting_source_with_MakeTypesInternal.cs     # Unit tests (BDD style)
    When_rewriting_each_source_file.cs                  # Per-file snapshot tests
    When_rewriting_an_entire_directory.cs               # End-to-end batch test (single directory)
    When_rewriting_multiple_directories.cs              # End-to-end batch test (multiple directories)
    GenerateOutputSource.cs                             # Manual helper to regenerate baseline
    input_source/                                       # Real C# source files (test input, committed)
    output_source/                                      # Expected rewritten output (committed baseline)
```

## How the Tests Work

### Snapshot-based specification

The `input_source/` and `output_source/` directories are the **specification**. Both are committed to source control.

- **`When_rewriting_each_source_file`** — A `[Theory]` that generates one test case per `.cs` file in `input_source/`. Each test runs `MakeTypesInternal` on the input and asserts exact string equality against the corresponding `output_source/` file. Failures show a unified diff.

- **`When_rewriting_an_entire_directory`** — Runs `RewriteDirectory` into a temp folder, verifies the file list and every file's content matches `output_source/`.

- **`When_rewriting_source_with_MakeTypesInternal`** — BDD-style unit tests for individual scenarios (each type keyword, modifiers, nested types, members, etc.).

### Adding a new edge case

1. Add the `.cs` file to `input_source/`
2. Add the expected rewritten version to `output_source/`
3. Run tests — a new test case appears automatically

### Fixing a rewriting bug

1. Edit the file in `output_source/` to show the correct expected output
2. Run tests — the test for that file fails with a diff showing what's wrong
3. Fix `SourceRewriter.cs`
4. Tests go green

### Regenerating `output_source/`

If you need to regenerate the entire baseline (e.g., after changing the header text):

```powershell
dotnet test --filter GenerateOutputSource -- RunConfiguration.SkipReason=""
```

This test is skipped by default to prevent it from wiping `output_source/` during normal test runs.

## Implementation Details

The rewriting is a single regex replacement — no Roslyn dependency, no AST parsing:

```
public(\s+)(modifiers)*(class|interface|struct|enum|record|delegate)
```

Where `modifiers` matches `static`, `abstract`, `sealed`, `partial`, `unsafe`, `readonly`, `ref`, `file`, `new`.

This works because the type keyword (`class`, `interface`, etc.) unambiguously distinguishes type declarations from member declarations. `public string Name` doesn't match because `string` isn't a type keyword. Nested types match independently regardless of nesting depth.

## NuGet Packaging

The project is packaged as a NuGet package containing the MSBuild task DLL and `.targets` file in both `build/` and `buildTransitive/`. To pack locally:

```powershell
dotnet pack src/CircularLibraryDependencySourceRewriter -c Release
```

The package outputs to the `nupkgs/` folder at the consuming repo root, configured as a local NuGet source.

## Tech Stack

- .NET 9+
- xUnit v3
- [Compze.Utilities.Testing.Must](https://www.nuget.org/packages/Compze.Utilities.Testing.Must) — fluent assertions with unified diff output
- [Compze.Utilities.Testing.XUnit](https://www.nuget.org/packages/Compze.Utilities.Testing.XUnit) — `[XF]` attribute for BDD-style nested test classes
