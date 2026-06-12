---
paths:
  - "**/*.cs"
---

# C# code conventions for Compze

The design principles — OO, everything-in-its-place, naming, comments — live in the universal rules
(`01-universal-shared`); this file is the C# and Compze mechanics: formatting, idiom, and the Compze
helper-library conventions.

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
- **`readonly`**: Use on fields for immutable state. Use `readonly struct` for value types.

## Naming conventions

| Element          | Convention                                                                 |
| ---------------- | -------------------------------------------------------------------------- |
| Classes          | `PascalCase`                                                               |
| Interfaces       | `I` prefix: `IEndpoint`, `IRepository`                                     |
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
- Prefer `using static Compze.Contracts.Assert;` to call `Argument.NotNull()`, `State.Is()` etc. without the `Assert.` prefix.

## Contracts & null handling

- **Nullable reference types enabled** in all projects.
- Use Compze.Contracts:
  - `Assert.Argument.NotNull(value)` — `ArgumentException`
  - `Assert.State.Is(condition)` — `InvalidOperationException`
  - `Assert.Result.NotNull(result)` — `InvalidResultException`
  - `Assert.Invariant.Is(condition)` — `InvariantViolatedException`
- Prefer `Assert.*` or `._assert` over manual if clauses and throwing.
- Use `.NotNull()` extension for quick null-dereferencing.

## Extension Methods

- Cross-cutting helpers, and logic that operates on a type it doesn't own, become extension methods —
  usually on that type or its interface — for discoverability.
- **Use extension blocks, not the old syntax `@this`**
- **Always name the receiver parameter `@this`** — making it instantly recognizable.
  ```csharp
  extension(ContractAsserter @this)
  {
     public ContractAsserter NotNull<T>([NotNull] T? value, ...) { ... @this.ThrowNull(...); ... }
  }
  ```


## No Records
- **Do not use `record` or `record struct`.**
- Records encourage treating types as dumb data bags, discouraging proper OO design with encapsulation and behavior.
- If value equality is genuinely needed, implement `IEquatable<T>` explicitly.

## Default Interface Methods (Mixins)

This codebase uses **default interface methods and extension methods extensively** as a mixin pattern. Interfaces often contain many convenience overloads and helper methods implemented as defaults that delegate to a small number of abstract members.

**Always check interfaces for default method implementations AND extension methods** before assuming a method doesn't exist or writing workarounds.

```csharp
// Abstract — the only members implementors need to provide
IDisposable TakeReadLock(TimeSpan? timeout = null);
IDisposable TakeUpdateLock(TimeSpan? timeout = null);

// Default — delegates Action overload to Func overload
unit Read(Action action, TimeSpan? timeout = null) => Read(action.ToFunc(), timeout);

// Default — delegates to abstract TakeReadLock
TReturn Read<TReturn>(Func<TReturn> func, TimeSpan? timeout = null)
{
   using(TakeReadLock(timeout)) return func();
}
```

## Collections

- Use collection expression syntax `[]` for initialization: `List<Task> tasks = [];`.
- Use immutable types for exposed data `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `IReadOnlySet<T>`
- Prefer LINQ method syntax when not cumbersome.
- Target-typed `new()` for well-known types: `static readonly ConcurrentDictionary<...> Cache = new();`.

## Async

- **`.caf()`** instead of `.ConfigureAwait(false)` — apply to every `await` in library code.

## Strings

- String interpolation `$"..."` everywhere — no concatenation.
- Raw string literals `$"""..."""` for multi-line strings.
- `nameof()` in exception messages and debug displays.

## File Organization

- Generally one primary type per file; nested classes and related small types in the same file are fine.
- Partial classes split across files using `ClassName.Aspect.cs` naming.
- Underscore-prefix filenames (`_MessageTypes..Interfaces.cs`) for grouped/supporting types.
- `_docs/` subdirectories for co-located documentation.

## Generic Constraints

- Multiple or complex constraints on separate lines, indented 3 spaces:
  ```csharp
  public class Foo<T>
     where T : IBar
  ```
