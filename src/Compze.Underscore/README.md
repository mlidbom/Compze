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

## Pipe forward operator

Chain operations fluently with `._()`:

```csharp
var result = initialValue
    ._(Transform)
    ._(Validate)
    ._(Format);
```

Primary methods:
- `._()` - the pipe forward operator.
- `.__()` — the `then` operator. Ignores the previous value and starts over with a value or by invoking a Func.
- `._tap()` — execute side effect, return original value
- `._mutate()` — intent-declaring alias for `_tap`

`.__` has a named alias `_then()` for contexts where you prefer readability over brevity.

> **Note:** The `_assert()` method used in the examples above comes from [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) — you may want to check it out.


## What's with the naming?

These extensions apply to *every* type, so name collisions and polluting auto-complete lists are a very real concern. _camelCase provides:

- **Near zero collision risk** — no standard .NET method starts with `_`.
- **Clear visuals** — instantly recognizable as something other than regular methods.
- **Great discoverability** — type `._` and all these extensions are right there grouped together.

### Why _ "operators", not words?

`._` and `.__` should be thought of as **operators**:  they glue expressions together, like `+` or `|>` in other languages. Naming them with words (`.Pipe(`, `.Then(`) makes the code stutter. Imagine if `variable.Member.MethodName()` had to be written as `variable.DOT.Member.Invoke(MethodName)` and you should see what we mean:

```csharp
// Words — reads like narrating your own code, constantly removing your focus from the parts that actually matter:
return result.Pipe(DoSomething).Then(returnValue)

// Operators — reads like just the logic:
return result._(DoSomething).__(returnValue)
```

`._(` is the pipe-forward operator. `.__(` is the discard-and-continue operator. One underscore = "transform this value." Two underscores = "ignore this value." The double-underscore echoes C#'s own `_` discard — extended from "I don't care about this variable" to "I don't care about the previous result."

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Unit](https://www.nuget.org/packages/Compze.Unit) | The `Unit` type — unifies `Action` and `Func`, enabling void methods to participate in pipelines |
| [Compze.Contracts](https://www.nuget.org/packages/Compze.Contracts) | Design-by-contract assertions, including the `_assert()` pipeline operator |

## License

Apache-2.0
