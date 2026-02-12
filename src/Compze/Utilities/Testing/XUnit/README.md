# Compze.Utilities.Testing.XUnit

xUnit testing utilities and attributes for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides xUnit integration for Compze's pluggable component testing:

- **Custom test case support** — `ArgumentDiscardingTestCase` and `ConstructorArgumentForwardingTestCase` for pluggable component test execution
- **Test discovery** — Attributes and infrastructure for discovering and running tests across all configured pluggable component combinations
- **xUnit v3 compatible** — Built on xUnit v3 extensibility

## Installation

```shell
dotnet add package Compze.Utilities.Testing.XUnit
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full testing infrastructure |
| [Compze.Utilities.Testing.Must](https://www.nuget.org/packages/Compze.Utilities.Testing.Must) | Fluent assertions |
| [Compze.Utilities.Testing.DbPool](https://www.nuget.org/packages/Compze.Utilities.Testing.DbPool) | Database pool management |

## License

Apache-2.0
