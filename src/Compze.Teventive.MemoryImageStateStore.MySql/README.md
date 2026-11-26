# Compze.Teventive.MemoryImageStateStore.MySql

The MySQL tables of [Compze](https://github.com/mlidbom/Compze)'s memory image state store: for a store kept in a MySQL domain database's tables, which one memory image at a time uses, holding a process lock of the database while it does.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## Installation

```shell
dotnet add package Compze.Teventive.MemoryImageStateStore.MySql
```

## What's in this package?

- **`MySqlMemoryImageStateStoreSqlLayer()`** — declared beside the domain database, `registrar.MySqlDomainDatabase(connectionStringName).MySqlMemoryImageStateStoreSqlLayer()`, it contributes the store's tables to the database's schema and registers their SQL, for a store composed with `StoredInTheDomainDatabase(...)`.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Teventive.MemoryImageStateStore](https://www.nuget.org/packages/Compze.Teventive.MemoryImageStateStore) | The memory image state store |

## License

Apache-2.0
