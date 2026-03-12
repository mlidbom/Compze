# Compze.Underscore

**Code that reads the way you would describe the algorithm in words. Powered by the pipe forward operator and friends.**

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

## `._(` The Pipe forward operator. Invokes the next step in a pipeline, passing the previous value as the `it` parameter.

```csharp
var result = initialValue
    ._(it => Transform(it))
    ._(Validate)
    ._(Format);
```


## `.__(` The then/discard operator. Ignores the previous value in the pipeline and starts over with a value or by invoking a Func or Action


```csharp
string ValidatedName(string name) => Argument.Assert(!string.IsNullOrWhiteSpace(name))
    .__(DoSomething(name));

IDisposable StartOperation(string name) => _logger.Log($"Starting {name}")
    .__(() => new OperationScope(name));

Unit EnsureInitialized() => State.Assert(!_disposed).__(() => 
{
    /*logic here */
});
```

## `._tap()` — side effect, then continue

Executes a side effect on `it` without changing the pipeline value:

```csharp
return userId
    ._tap(it => _logger.Log($"processing user {it}"))
    ._(DoSomething);
```

## `._mutate()` — mutate and return

Declares that you're modifying `it` in-place and returning `it` for further chaining:

```csharp
public static Command SetText(this Command @this, string text) => @this._mutate(it => it.CommandText = text);
```


## What's with the naming?

These extensions apply to *every* type, so name collisions and polluting auto-complete lists are a very real concern. _camelCase provides:

- **Near zero collision risk** — no standard .NET method starts with `_`.
- **Clear visuals** — instantly recognizable as something other than regular methods.
- **Great discoverability** — type `._` and all these extensions are right there grouped together.

### _ "operators", vs words

`._(` and `.__(` should be thought of as **operators**:  they glue expressions together, like `.`, `()`, or `|>` in other languages. Naming them with words (`.Pipe(`, `.Then(`) makes the code stutter. Imagine if `file.Open()` had to be written as `file.DOT.Invoke(Open)` and you should see what we mean. You don't want words for `.` and `()`, nor do you want them for `._(` or `.__(`:

```csharp
// Compare:
return startValue.Pipe(DoSomething).Then(returnValue)

// And
return startValue._(DoSomething).__(returnValue)
```

Once you have internalized the meaning of `._(` and `.__(` the readability of the two play in different leagues.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Unit](https://www.nuget.org/packages/Compze.Unit) | The `Unit` type — unifies `Action` and `Func`, enabling void methods to participate in pipelines |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions, including the `_assert()` pipeline operator |

## License

Apache-2.0
