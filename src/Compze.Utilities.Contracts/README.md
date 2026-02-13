# Compze.Utilities.Contracts

Design-by-contract assertion library for [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Fluent, chainable runtime assertions for preconditions, postconditions, invariants, and state checks — with `CallerArgumentExpression` support for clear failure messages.

### Assertion categories

| Entry point | Throws | Use for |
|-------------|--------|---------|
| `Assert.Argument` | `ArgumentException` | Method parameter validation |
| `Assert.State` | `InvalidOperationException` | Object state checks |
| `Assert.Result` | `InvalidResultException` | Return value postconditions |
| `Assert.Invariant` | `InvariantViolatedException` | Class invariant enforcement |

### Assertion methods

- **Boolean** — `Is(condition)` with `CallerArgumentExpression`
- **Null checks** — `NotNull()`, `NotNullOrDefault()`, `NotDefault()`, `ReturnNotNull()`
- **Strings** — `NotNullOrEmpty()`, `NotNullEmptyOrWhitespace()`
- **Enums** — `IsValid<TEnum>()`
- **Numeric** — `IsGreaterThan()`
- **Lifecycle** — `IsNotDisposed()`

### Quick start

```csharp
void Transfer(Account from, Account to, decimal amount)
{
    Assert.Argument.NotNull(from);
    Assert.Argument.NotNull(to);
    Assert.Argument.Is(amount > 0);
    Assert.State.IsNotDisposed(_disposed);
}
```

## Installation

```shell
dotnet add package Compze.Utilities.Contracts
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |
| [Compze.Utilities.Functional](https://www.nuget.org/packages/Compze.Utilities.Functional) | Functional programming primitives |

## License

Apache-2.0
