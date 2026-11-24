# Compze.Teventive.MemoryImageStateStore.MicrosoftSql

The SQL Server tables of [Compze](https://github.com/mlidbom/Compze)'s memory image state store: for a store kept in a SQL Server domain database's tables, which several memory images, in other processes or other containers, share.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## Installation

```shell
dotnet add package Compze.Teventive.MemoryImageStateStore.MicrosoftSql
```

## What's in this package?

- **`MsSqlMemoryImageStateStoreSqlLayer()`** — declared beside the domain database, `registrar.MsSqlDomainDatabase(connectionStringName).MsSqlMemoryImageStateStoreSqlLayer()`, it contributes the store's tables to the database's schema and registers their SQL, for a store composed with `StoredInTheDomainDatabase(...)`.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Teventive.MemoryImageStateStore](https://www.nuget.org/packages/Compze.Teventive.MemoryImageStateStore) | The memory image state store |

## License

Apache-2.0
