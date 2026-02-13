# Compze.Utilities.Functional

Functional programming primitives for [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Essential functional programming building blocks for C# — pipe operators, `Option<T>`, discriminated unions, and the `unit` type.

### Pipe operator

Chain operations fluently with `._()`:

```csharp
var result = initialValue
    ._( transform )
    ._( validate )
    ._( format );
```

Additional pipe variants:
- `tap()` — execute side effect, return original value
- `mutate()` — modify in place, return original
- `then()` — chain void operations
- `assert()` — validate with assertion, return original

### Option type

```csharp
Option<User> user = Option.Some(foundUser);
Option<User> missing = Option.None<User>();
```

### Discriminated unions

Type-safe unions with 2–6 cases:

```csharp
class Result : DiscriminatedUnion<Result, Success, Error>
{
    public Result(Success success) : base(success) { }
    public Result(Error error) : base(error) { }
}
```

### Unit type

The functional equivalent of `void` — a type with exactly one value:

```csharp
Func<unit> wrapped = unit.Func(() => Console.WriteLine("hello"));
```

## Installation

```shell
dotnet add package Compze.Utilities.Functional
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.Contracts](https://www.nuget.org/packages/Compze.Utilities.Contracts) | Design-by-contract assertions |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
