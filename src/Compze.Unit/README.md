# Compze.Unit

**Plugs the hole in the BCL where `Unit` should be.**

Functional programming languages do not have void. Just Unit as the value returned by methods with no meaningful thing to return. A struct with just one value.

If C# were designed today, there's good reason to believe that void would never have existed. Because `void` creates a gaping rift in the type system. It is not a type, so you can't use it as a generic argument, return it from a `Func<T>`, or store it in a variable. This forces every generic API to maintain parallel versions — one for `Func<T, TResult>`, one for `Action<T>`.

Despite many years of debate, a unit type has still not made it into the BCL. If you want a Unit type, the current choices are to roll your own, or take a dependency on some large library that happens to include a Unit type.

This tiny library, obviously with zero dependencies, exists to change that.

It consists of just the Unit struct, with a single possible value, and some static extension methods on the static class `UnitConvert` to help with conversions.

## Usage

### Return `Unit` instead of declaring the method `void`

```csharp
// Before — can't be used with Func<T, TResult> APIs
public void DoSomething(string message) 
{
    /*do something*/
}

// After — works everywhere Func<T, TResult> is expected
public Unit DoSomething(string message) => Unit.Invoke(() => 
{
    /*do something*/
});

// If your code is called in extremely tight loops and you are worried about performance
public Unit DoSomething(string message)
{
    /*do something*/
    return Unit.Value;
}
```

### Convert between `Action` and `Func<Unit>`

```csharp
Action<string> stringAction = StringTakingAction;

// Action → Func
Func<string, Unit> stringFunc1 = UnitConvert.ToFunc(StringTakingAction);
Func<string, Unit> stringFunc2 = UnitConvert.ToFunc((string it) => StringTakingAction(it));
Func<string, Unit> stringFunc3 = stringAction.ToFunc();


// Func → Action
Action<string> backToAction1 = UnitConvert.ToAction(stringFunc1);
Action<string> backToAction2 = UnitConvert.ToAction((string it) => Unit.Value);
Action<string> backToAction3 = stringFunc3.ToAction();
```

### Convert between `Func<Task>` and `Func<Task<Unit>>`

```csharp
Func<string, Task> asyncAction = StringTakingAsyncAction;

// Func<Task> → Func<Task<Unit>>
Func<string, Task<Unit>> asyncFunc1 = UnitConvert.ToAsyncFunc(StringTakingAsyncAction);
Func<string, Task<Unit>> asyncFunc2 = UnitConvert.ToAsyncFunc((string it) => StringTakingAsyncAction(it));
Func<string, Task<Unit>> asyncFunc3 = asyncAction.ToAsyncFunc();


// Func<Task<Unit>> → Func<Task>
Func<string, Task> backToAsyncAction1 = UnitConvert.ToAsyncAction(asyncFunc1);
Func<string, Task> backToAsyncAction2 = UnitConvert.ToAsyncAction((string it) => Task.FromResult(Unit.Value));
Func<string, Task> backToAsyncAction3 = asyncFunc3.ToAsyncAction();
```


## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Underscore](https://www.nuget.org/packages/Compze.Underscore) | Code that reads the way you would describe the algorithm in words. Powered by the pipe forward operator and friends. |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions |

## License

[Apache-2.0](../../LICENSE.txt)
