# Compze.Sql.Sqlite

SQLite database support for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides SQLite integration for Compze persistence:

- **SQLite connection management** — `CompzeSqliteConnection` and `SqliteConnectionPool`
- **In-memory database support** — Ideal for unit and integration testing
- **Event store persistence** — SQLite storage for Compze event stores
- **Document DB persistence** — SQLite storage for Compze document databases
- **Pluggable provider** — Drop-in SQLite support via Compze's pluggable component architecture

## Installation

```shell
dotnet add package Compze.Sql.Sqlite
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Sql.Common](https://www.nuget.org/packages/Compze.Sql.Common) | Shared SQL abstractions |
| [Compze.Sql.MicrosoftSql](https://www.nuget.org/packages/Compze.Sql.MicrosoftSql) | SQL Server provider |
| [Compze.Sql.PostgreSql](https://www.nuget.org/packages/Compze.Sql.PostgreSql) | PostgreSQL provider |
| [Compze.Sql.MySql](https://www.nuget.org/packages/Compze.Sql.MySql) | MySQL provider |

## License

Apache-2.0
