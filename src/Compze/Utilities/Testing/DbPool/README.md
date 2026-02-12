# Compze.Utilities.Testing.DbPool

Database pool management for [Compze](https://github.com/mlidbom/Compze) integration testing.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides database lifecycle management for integration tests:

- **Database pooling** — `DbPool` manages a pool of test databases, automatically provisioning and cleaning them between tests
- **Strict resource management** — Ensures databases are properly disposed, detecting leaks
- **Cross-provider support** — Works with all Compze SQL providers (SQL Server, PostgreSQL, MySQL, SQLite)
- **Shared state serialization** — Thread-safe state management for concurrent test execution

## Installation

```shell
dotnet add package Compze.Utilities.Testing.DbPool
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Full testing infrastructure |
| [Compze.Utilities.Testing.Must](https://www.nuget.org/packages/Compze.Utilities.Testing.Must) | Fluent assertions |
| [Compze.Utilities.Testing.XUnit](https://www.nuget.org/packages/Compze.Utilities.Testing.XUnit) | xUnit utilities |

## License

Apache-2.0
