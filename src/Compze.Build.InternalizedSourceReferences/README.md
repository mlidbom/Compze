# Compze.Build.InternalizedSourceReferences

An MSBuild task that enables circular dependencies between .NET library projects. The library using thing task gets a local "internalized" copy of the other library's code (or any number of other libraries' code).

### Key Constraints and gotchas

**Internalized types cannot appear in the public API** of the consuming project. Since they are rewritten to `internal`, they can't be used in public method signatures, property types, base classes, or interface implementations that are exposed to consumers.

**Static state** Any static state in the "referenced" library will NOT be shared. The local source results in entirely different types and data as far as the runtime is concerned.
 

## What Gets Rewritten

| Input | Output |
|-------|--------|
| `public class Foo` | `internal class Foo` |
| `public interface IBar` | `internal interface IBar` |
| `public struct Baz` | `internal struct Baz` |
| `public enum Color` | `internal enum Color` |
| `public record Person` | `internal record Person` |
| `public delegate void Handler` | `internal delegate void Handler` |
| `public static class Extensions` | `internal static class Extensions` |
| `public abstract class Base` | `internal abstract class Base` |
| `public sealed partial class Widget` | `internal sealed partial class Widget` |


## MSBuild Integration

Add this to your .csproj file

```xml
<PropertyGroup>
  <InternalizeSourceFrom>..\LibraryA;..\LibraryB;..\LibraryC</InternalizeSourceFrom>
  <InternalizeSourceTo>$(MSBuildProjectDirectory)\InternalizedSource</InternalizeSourceTo>
</PropertyGroup>
```

The target:
- Runs `BeforeTargets="CoreCompile"` so rewriting happens before your code compiles
- Only writes files when content has actually changed
- Auto-includes the generated files in compilation
- Only runs when both properties are set

The `InternalizedSource` folder can be `.gitignore`d since it's regenerated on build.

## API

### `SourceRewriter.MakeTypesInternal(string source) → string`

Rewrites a single C# source string. Prepends the auto-generated header and replaces `public` type declarations with `internal`.

### `SourceRewriter.RewriteDirectory(string inputDirectory, string outputDirectory)`

Convenience overload — calls `RewriteDirectories` with a single input directory.

### `SourceRewriter.RewriteDirectories(string[] inputDirectories, string outputDirectory)`

Rewrites all `.cs` files from multiple input directories into `outputDirectory`, preserving directory structure per input. Only writes files whose content has actually changed. Removes stale output files that no longer have a corresponding input.

## License

Apache-2.0

## Development

See [DEVELOPMENT.md](DEVELOPMENT.md) for project structure, build instructions, testing workflow, and implementation details.
