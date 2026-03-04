# Compze.Unit

**Plugs the whole in the BCL where Unit should be.**

Functional programminc languages do not have void. Just Unit as the value returned by method with no meaningful thing to return.

If C# was designed today, there's good reason to believe that void would never have existed. Because `void` creates a gaping rift in the type system. You can't use it as a generic argument, return it from a `Func<T>`, or store it in a variable. This forces every generic API to maintain parallel versions — one for `Func<T, TResult>`, one for `Action<T>`.

Now .NET can hardly remove void at this point, but the BCL certainly could include a Unit type. Sadly in spite of years and years of discussion, a unit type has still not made it into the BCL.

If you want a Unit type the current choices are to roll your own, or take a dependency on some large library that happens to include a Unit type.

This tiny library's whole purpose is to change that.

## Usage

### Return `Unit` instead of `void`

```csharp
// Before — can't be used with Func<T, TResult> APIs
public void Log(string message) => Console.WriteLine(message);

// After — works everywhere Func<T, TResult> is expected
public Unit Log(string message) => Unit.Invoke(() => Console.WriteLine(message));
```

### Convert between `Action` and `Func<Unit>`

```csharp
Action<string> voidMethod = Console.WriteLine;

// Action → Func
Func<string, Unit> funcVersion = voidMethod.ToFunc();

// Func → Action
Action<string> backToAction = funcVersion.ToAction();
```

### Async conversions

```csharp
Func<string, Task> asyncVoid = WriteToFileAsync;

// Func<Task> → Func<Task<Unit>>
Func<string, Task<Unit>> asyncFunc = asyncVoid.ToAsyncFunc();

// Func<Task<Unit>> → Func<Task>
Func<string, Task> backToAsyncVoid = asyncFunc.ToAsyncAction();
```

### Wrap an action as a `Unit`-returning expression

```csharp
// Sync
Unit result = Unit.Invoke(() => Console.WriteLine("done"));

// Async
Task<Unit> asyncResult = Unit.InvokeAsync(() => SomeAsyncMethod());
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Underscore](https://www.nuget.org/packages/Compze.Underscore) | Pipe-forward operator and fluent chaining — `Unit` enables void methods to participate in pipelines |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions |

## License

[Apache-2.0](../../LICENSE.txt)
