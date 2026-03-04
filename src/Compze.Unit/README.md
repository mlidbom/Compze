# Compze.Unit

**The functional programming `Unit` type for .NET — unifies `Action` and `Func` so void methods participate fully in generic pipelines.**

## The problem

In .NET `Action<T>` and `Func<T, TResult>` are not compatible, resulting in tons of code needing one version for Action and one for Func. `Unit` is the solution. Methods with no meaningful return value return `Unit` removing the incompatibility since `Unit` is a value — a single uniform value.

## Painlessly convert void methods to Unit-returning methods

```csharp
public Unit Log(string message) => Unit.Invoke(() => Console.WriteLine(message));
```

## Convert between `Action` and `Func<Unit>`

Extension methods on `Action` and `Func<Unit>` (and their async equivalents) make conversions trivial:

```csharp
Action anAction = SomeVoidMethod;
Func<Unit> aUnitFunc = anAction.ToFunc();
Action backToAction = aUnitFunc.ToAction();
```

Async conversions follow the same pattern:

```csharp
Func<Task> asyncAction = SomeAsyncMethod;
Func<Task<Unit>> asyncFunc = asyncAction.ToAsyncFunc();
Func<Task> backToAsyncAction = asyncFunc.ToAsyncAction();
```

All conversions support 0, 1, and 2 parameter arities.

## API summary

| Extension method | Conversion |
|---|---|
| `Action.ToFunc()` | `Action` → `Func<Unit>` |
| `Func<Unit>.ToAction()` | `Func<Unit>` → `Action` |
| `Func<Task>.ToAsyncFunc()` | `Func<Task>` → `Func<Task<Unit>>` |
| `Func<Task<Unit>>.ToAsyncAction()` | `Func<Task<Unit>>` → `Func<Task>` |

| Static method | Purpose |
|---|---|
| `Unit.Invoke(action)` | Execute an `Action` and return `Unit` |

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Underscore](https://www.nuget.org/packages/Compze.Underscore) | Pipe forward operator and friends — `Unit` enables void methods to participate in pipelines |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions |

## License

Apache-2.0
