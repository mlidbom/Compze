---
paths:
  - "**/*.cs"
---

# C# Code Conventions for all repositories

## Design & Refactoring

- When implementing new functionality, if a missing abstraction makes the implementation inconsistent, awkward, or poorly structured — **introduce that abstraction**. Refactoring existing code to better accommodate new changes is expected and encouraged.
- Do not bolt new behavior onto an ill-fitting structure just to avoid creating classes. If the right design calls for a new class, record, interface, or helper — create it.
- The goal is a codebase where each change leaves the design **more** coherent, not less. Treat every feature as an opportunity to improve the surrounding code.
- **Behavior belongs with data.** If a static method's first parameter is an object it primarily operates on, that method should probably be an instance method on that type instead. `StaticHelper.DoSomething(object, ...)` is C-style procedural code — prefer `object.DoSomething(...)`. Static utility classes are for genuinely cross-cutting operations that don't belong to any specific type. And those should usually be extension methods for discoverability.
- **Logic belongs where it fits, not where it's first needed.** If a method operates on a type it doesn't belong to — move it to that type as an instance method, or make it an extension method on that type or interface. Don't let helper logic accumulate in unrelated classes just because that's where the need first arose.

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

## Naming

 - **Make them however long they need to be** By far the most common problem with code we see is that trying to keep names short means they do NOT describe what they do. Names should be as long as they need to be to clearly convey their purpose. If you find yourself needing to add a comment to explain what a method or variable does, it's a strong sign the name should be improved instead.


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

## Null Handling

- **Nullable reference types enabled** in all projects.
- Use Compze.Contracts:
  - `Assert.Argument.NotNull(value)` — `ArgumentException`
  - `Assert.State.Is(condition)` — `InvalidOperationException`
  - `Assert.Result.NotNull(result)` — `InvalidResultException`
  - `Assert.Invariant.Is(condition)` — `InvariantViolatedException`
- Use `.NotNull()` extension for quick null-dereferencing.

## Extension Methods

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

## Exceptions

- **CRITICAL**: Never swallow exceptions in a catch block without rethrowing.
  - Only catch an exception at all if you have a specific recovery strategy or need to add context before re-throwing.
- Prefer `Assert.*` or `._assert` from Compze.Contracts over manual if clauses and throwing.

## Comments & Documentation

- Prefer self-documenting code over comments
    - Extract a well named method that explains what is happening instead of writing a comment
    - Rename a method to explain what it does rather than add a documentation comment

## XML doc comments

- Follow [documentation-comments](../universal/03-code-standards/040-documentation-comments.md): a
  plain-language `<summary>` floor, why-first `<remarks>`, build-validated `<see cref>`s, and a comment on
  every member whose name alone doesn't tell a codebase-newcomer what it's for.

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
