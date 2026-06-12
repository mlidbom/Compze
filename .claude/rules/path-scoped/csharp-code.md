---
paths:
  - "**/*.cs"
---

# C# code conventions for Compze

The design principles — OO, everything-in-its-place, naming, comments — live in the universal rules
(`01-universal-shared`). Formatting and idiom-level style — indentation, braces, `var`, expression bodies,
casing, namespace = folder path, collection expressions, and the like — is enforced by the ReSharper/Rider
profile and surfaces automatically through the code-intelligence MCPs as inspections (with quick-fixes), so
it is not documented here. This file holds only what the tooling can't know: Compze's library conventions
and design rules no inspection covers.

## Contracts & null handling

- **Nullable reference types enabled** in all projects.
- Use Compze.Contracts:
  - `Assert.Argument.NotNull(value)` — `ArgumentException`
  - `Assert.State.Is(condition)` — `InvalidOperationException`
  - `Assert.Result.NotNull(result)` — `InvalidResultException`
  - `Assert.Invariant.Is(condition)` — `InvariantViolatedException`
- Prefer `Assert.*` or `._assert` over manual if clauses and throwing.
- Prefer `using static Compze.Contracts.Assert;` to call `Argument.NotNull()`, `State.Is()` etc. without the `Assert.` prefix.
- Use `.NotNull()` extension for quick null-dereferencing.

## Extension methods

- Cross-cutting helpers, and logic that operates on a type it doesn't own, become extension methods —
  usually on that type or its interface — for discoverability.
- Extension classes carry the `{TypeName}CE` suffix: `StringCE`, `EnumCE`, `TimeSpanCE`.
- **Use extension blocks, not the old `this T @this` syntax**, and always name the receiver `@this` —
  making it instantly recognizable:
  ```csharp
  extension(ContractAsserter @this)
  {
     public ContractAsserter NotNull<T>([NotNull] T? value, ...) { ... @this.ThrowNull(...); ... }
  }
  ```
- "Language-like" functional helpers are lowercase: `caf()`, `then()`, `mutate()`, `tap()`.

## Async

- **`.caf()`** instead of `.ConfigureAwait(false)` — apply to every `await` in library code.

## No records

- **Do not use `record` or `record struct`.** Records encourage treating types as dumb data bags,
  discouraging proper OO design with encapsulation and behavior.
- If value equality is genuinely needed, implement `IEquatable<T>` explicitly.

## Primary constructors

- Create explicit fields for the arguments; do **NOT** use primary-constructor argument capturing.

## Default interface methods (mixins)

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

- Expose immutable types for outward-facing data: `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `IReadOnlySet<T>`.

## File organization

- Generally one primary type per file; nested classes and related small types in the same file are fine.
- Partial classes split across files using `ClassName.Aspect.cs` naming.
- Underscore-prefix filenames (`_MessageTypes..Interfaces.cs`) for grouped/supporting types.
- `_docs/` subdirectories for co-located documentation.
