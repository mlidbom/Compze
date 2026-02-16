---
applyTo: "**/*.cs"
---

# C# Code Conventions for all repositories

## Design & Refactoring

- When implementing new functionality, if a missing abstraction makes the implementation inconsistent, awkward, or poorly structured — **introduce that abstraction**. Refactoring existing code to better accommodate new changes is expected and encouraged.
- Do not bolt new behavior onto an ill-fitting structure just to avoid creating classes. If the right design calls for a new class, record, interface, or helper — create it.
- The goal is a codebase where each change leaves the design **more** coherent, not less. Treat every feature as an opportunity to improve the surrounding code.

## Formatting

- **Indentation**: 3 spaces.
- **File-scoped namespaces**: Always `namespace Foo.Bar;` — never block-scoped.
- **Namespace = folder path**: The namespace must match the project name + subfolder structure.
- **Braces**: Allman style for type and member declarations. No space between keyword and parenthesis in control flow: `if(condition)`, `foreach(var x in items)`, `while(true)`.

## Type Declarations

- **`var` everywhere**: Use `var` for all local variables — even for built-in types. Avoid explicit types unless needed for clarity.
- **Expression bodies (`=>`)**: Prefer for single-expression methods, properties, constructors, and operators.
- **Primary constructors**: Use when appropriate but create explicit fields for the arguments.  Do **NOT** use primary constructor argument capturing
- **Access modifiers**: Omit default access modifiers. No explicit `private` on fields or methods; no explicit `internal` on classes. Only write modifiers that change the default.
- **`readonly`**: Use on fields for immutable state. Use `readonly struct` and `readonly record struct` for value types.
- **Records**: Use for true value objects. Prefer `record` or `readonly record struct` with positional syntax.

## Naming

| Element          | Convention                                                                 |
| ---------------- | -------------------------------------------------------------------------- |
| Classes          | `PascalCase`                                                               |
| Interfaces       | `I` prefix: `IEndpoint`, `ITessage`                                        |
| Methods          | `PascalCase`                                                               |
| Fields           | `_camelCase` (no explicit `private`)                                       |
| Properties       | `PascalCase`                                                               |
| Constants        | `PascalCase`                                                               |
| Enums/values     | `PascalCase`                                                               |
| Generic params   | `T` prefix: `TEntity`, `TKey`                                              |
| Extension classes | `{TypeName}CE` suffix: `StringCE`, `EnumCE`, `TimeSpanCE`                 |
| Extension 1st param | `@this`: `this string @this`                                           |
| Functional utils | Lowercase for "language-like" helpers: `caf()`, `then()`, `mutate()`, `tap()` |

## Using Directives

- Place at file top, outside namespace.
- No global usings files — each file has its own explicit usings.
- Prefer `using static Compze.Utilities.Contracts.Assert;` to call `Argument.NotNull()`, `State.Is()` etc. without the `Assert.` prefix.

## Null Handling

- **Nullable reference types enabled** in all projects.
- Use the contract assertion system instead of raw null checks:
  - `Assert.Argument.NotNull(value)` — `ArgumentException`
  - `Assert.State.Is(condition)` — `InvalidOperationException`
  - `Assert.Result.NotNull(result)` — `InvalidResultException`
  - `Assert.Invariant.Is(condition)` — `InvariantViolatedException`
- Use `.NotNull()` extension for quick null-dereferencing.

## Collections

- Use collection expression syntax `[]` for initialization: `List<Task> tasks = [];`.
- Prefer `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `IReadOnlySet<T>` for return types and fields.
- Prefer LINQ method syntax when not cumbersome.
- Target-typed `new()` for well-known types: `static readonly ConcurrentDictionary<...> Cache = new();`.

## Async

- **`.caf()`** instead of `.ConfigureAwait(false)` — apply to every `await` in library code.

## Strings

- String interpolation `$"..."` everywhere — no concatenation.
- Raw string literals `$"""..."""` for multi-line strings.
- `nameof()` in exception messages and debug displays.

## Exceptions

- Never swallow exceptions in a catch block without rethrowing.
- Use the fluent `Assert.*` contract system instead of raw `throw` statements where possible.

## Comments & Documentation

- Prefer descriptive names and extracting new methods over explanatory comments.

## XML doc comments

- Be brief and concise. A single line is often sufficient.
- Unless non-obvious, do not document parameters or return values.
- omit on obvious/internal code.

## File Organization

- Generally one primary type per file; nested classes and related small types in the same file are fine.
- Partial classes split across files using `ClassName.Aspect.cs` naming.
- Underscore-prefix filenames (`_TessageTypes..Interfaces.cs`) for grouped/supporting types.
- `_docs/` subdirectories for co-located documentation.

## Generic Constraints

- Multiple or complex constraints on separate lines, indented 3 spaces:
  ```csharp
  public class Foo<T>
     where T : IBar
  ```
