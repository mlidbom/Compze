# Compze.Utilities.Functional

**Stop writing code that must be read inside out, and start writing code that reads the way you would describe the algorithm in words.**

Sometimes very little goes a very long way. With just a handful of trivially simple extension methods we enable you to write code like this:

```csharp
public OperationResult SomeBusinessMethod(Guid userId) =>
    userId
    ._(LoadFromDatabase)
    ._assert(MayExecuteThisOperation)
    ._(ActualOperationLogic)
    ._assert(ResultIsWhatWeExpected);
```

## Pipe operator

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
- `_assert()` — assert a condition and return original
- `_then()` — discard the current value and continue with a new one. Enables one-liner implementations when the previous value is irrelevant.


## The Unit type

### Like void but without splitting `Action<T>` from `Func<T,T2>`
In .NET `Action<T>` and `Func<T, T2>` are not compatible, resulting in tons of code needing one version for action and one for Func. Unit is the solution. Methods with no meaningful return value return unit removing incompatibility since unit is a value, a single uniform value. 

### Painlessly convert void methods to unit returning methods that will participate frictionlessly in piping:
```csharp
public unit Log(string message) => unit.From(() => Console.WriteLine(message));
```

### Convert between `Action<T>` and `Func<T,unit>`
```csharp
Action anAction = SomeMethod;
Func<unit> aUnitFunc = anAction.AsFunc();
var anotherAction = aUnitFunc.AsAction(); 
```


## What's with the naming?

These extensions apply to *every* type, so name collisions and polluting auto-complete lists are a very real concern. _camelCase provides:

- **Near zero collision risk** — no standard .NET method starts with `_`.
- **Clear visuals** — instantly recognizable as something other than regular methods.
- **Great discoverability** — type `._` and autocomplete shows only these extensions

`._(` as the pipeline operator has the same advantages and keeps something you will be using constantly as short as possible.

Lower case `unit` is sort of a wish for the future and the way we think it should be. We hope our unit will eventually be replaced by a language keyword and built in type.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.Contracts](https://www.nuget.org/packages/Compze.Utilities.Contracts) | Design-by-contract assertions |

## License

Apache-2.0
