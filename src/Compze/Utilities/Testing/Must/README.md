# Compze.Utilities.Testing.Must

Fluent assertion library for [Compze](https://github.com/mlidbom/Compze) testing.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

A strict-by-default fluent assertion library with rich failure diagnostics:

- **Fluent API** — Chain assertions naturally: `value.Must().Be(expected)`
- **Rich failure output** — JSON rendering of involved objects, the failing expression, and unified diffs for equality comparisons
- **Deep equality** — `DeepEqual` support for comparing complex object graphs
- **Type-safe** — Generic assertion context with full type inference
- **Extensible** — Trivial to add custom assertions via extension methods on `IAssertionContext<T>`

### Assertion categories

- **Equality** — `Be`, `NotBe`, `DeepEqual`
- **Nullability** — `NotBeNull`, `BeNull`
- **Boolean** — `BeTrue`, `BeFalse`
- **Strings** — `Contain`, `NotBeNullOrEmpty`, string-specific assertions
- **Collections** — `HaveCount`, `Contain`, enumerable assertions
- **Comparison** — `BeGreaterThan`, `BeLessThan`, and other `IComparable` assertions
- **General** — `Satisfy` for custom predicates, `BeOneOf` for value sets

### Quick start

```csharp
using Compze.Utilities.Testing.Must;

user.Email.Must().NotBeNullOrEmpty();
result.Must().Be(expected);
users.Must().HaveCount(3);
actual.Must().DeepEqual(expected); // Shows unified diff on failure
```

## Installation

```shell
dotnet add package Compze.Utilities.Testing.Must
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full testing infrastructure |
| [Compze.Utilities.Testing.XUnit](https://www.nuget.org/packages/Compze.Utilities.Testing.XUnit) | xUnit utilities |
| [Compze.Utilities.Testing.DbPool](https://www.nuget.org/packages/Compze.Utilities.Testing.DbPool) | Database pool management |

## License

Apache-2.0