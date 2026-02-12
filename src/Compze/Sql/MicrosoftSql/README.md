# Compze.Sql.MicrosoftSql

Microsoft SQL Server support for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides SQL Server integration for Compze persistence:

- **SQL Server connection management** — `CompzeMsSqlConnection` and `IMsSqlConnectionPool`
- **Event store persistence** — SQL Server storage for Compze event stores
- **Document DB persistence** — SQL Server storage for Compze document databases
- **Pluggable provider** — Drop-in SQL Server support via Compze's pluggable component architecture

## Installation

```shell
dotnet add package Compze.Sql.MicrosoftSql
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Sql.Common](https://www.nuget.org/packages/Compze.Sql.Common) | Shared SQL abstractions |
| [Compze.Sql.PostgreSql](https://www.nuget.org/packages/Compze.Sql.PostgreSql) | PostgreSQL provider |
| [Compze.Sql.MySql](https://www.nuget.org/packages/Compze.Sql.MySql) | MySQL provider |
| [Compze.Sql.Sqlite](https://www.nuget.org/packages/Compze.Sql.Sqlite) | SQLite provider |

## License

Apache-2.0
