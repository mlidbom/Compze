# Compze.Sql.Common

Common SQL database abstractions for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the shared SQL infrastructure used by all Compze database providers:

- **Connection pooling** — `DbConnectionPool` for managing database connections
- **ADO.NET extensions** — Helpers for `DbCommand` and `DbDataReader`
- **Database abstractions** — Common interfaces for SQL operations across providers

This package is typically not referenced directly — use one of the provider-specific packages instead.

## Installation

```shell
dotnet add package Compze.Sql.Common
```

## Database provider packages

| Package | Database |
|---------|----------|
| [Compze.Sql.MicrosoftSql](https://www.nuget.org/packages/Compze.Sql.MicrosoftSql) | SQL Server |
| [Compze.Sql.PostgreSql](https://www.nuget.org/packages/Compze.Sql.PostgreSql) | PostgreSQL |
| [Compze.Sql.MySql](https://www.nuget.org/packages/Compze.Sql.MySql) | MySQL |
| [Compze.Sql.Sqlite](https://www.nuget.org/packages/Compze.Sql.Sqlite) | SQLite |

## License

Apache-2.0
