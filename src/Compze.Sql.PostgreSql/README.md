# Compze.Sql.PostgreSql

PostgreSQL database support for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides PostgreSQL integration for Compze persistence:

- **PostgreSQL connection management** — `CompzeNpgsqlConnection` and `PgSqlConnectionPool`
- **Event store persistence** — PostgreSQL storage for Compze event stores
- **Document DB persistence** — PostgreSQL storage for Compze document databases
- **Pluggable provider** — Drop-in PostgreSQL support via Compze's pluggable component architecture

## Installation

```shell
dotnet add package Compze.Sql.PostgreSql
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Sql.Common](https://www.nuget.org/packages/Compze.Sql.Common) | Shared SQL abstractions |
| [Compze.Sql.MicrosoftSql](https://www.nuget.org/packages/Compze.Sql.MicrosoftSql) | SQL Server provider |
| [Compze.Sql.MySql](https://www.nuget.org/packages/Compze.Sql.MySql) | MySQL provider |
| [Compze.Sql.Sqlite](https://www.nuget.org/packages/Compze.Sql.Sqlite) | SQLite provider |

## License

Apache-2.0
