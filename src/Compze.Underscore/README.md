# Compze.Underscore

**Code that reads the way you would describe the algorithm in words, not code that must be read inside out. Powered by the pipe forward operator and friends.**

Sometimes very little goes a very long way. With just a handful of trivially simple extension methods we enable you to write code like this:

```csharp
public OperationResult SomeBusinessMethod(Guid userId) =>
    userId
    ._(LoadFromDatabase)
    ._tap(it => { /*log*/ })
    ._assert(MayExecuteThisOperation)
    ._(ActualOperationLogic)
    ._tap(it => { /*log*/ })
    ._assert(ResultIsWhatWeExpected);
```

## Pipe forward operator

Chain operations fluently with `._()`:

```csharp
var result = initialValue
    ._(Transform)
    ._(Validate)
    ._(Format);
```

Primary methods:
- `_tap()` — execute side effect, return original value
- `_mutate()` — intent declaring alias for _tap
- `_then()` — discard the current value and continue with a new one. Enables one-liner implementations when the previous value is irrelevant.

> **Note:** The `_assert()` method used in the examples above comes from [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) — you may want to check it out.


## What's with the naming?

These extensions apply to *every* type, so name collisions and polluting auto-complete lists are a very real concern. _camelCase provides:

- **Near zero collision risk** — no standard .NET method starts with `_`.
- **Clear visuals** — instantly recognizable as something other than regular methods.
- **Great discoverability** — type `._` and all these extensions are right there grouped together.

`._(` as the pipeline operator has the same advantages and keeps something you will be using constantly as short as possible.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Unit](https://www.nuget.org/packages/Compze.Unit) | The `Unit` type — unifies `Action` and `Func`, enabling void methods to participate in pipelines |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions, including the `_assert()` pipeline operator |

## License

Apache-2.0
