# Compze.TypeIdentifiers.Interning

Database-local integer interning for stable type identity. Maps a Compze `TypeId` to a small, per-database `int` and back, so storage tables can reference a type with a 4-byte int instead of a GUID or a long string — durably linking every spelling a type has been persisted as (reclassification and renames) behind a small, storage-agnostic provider interface. Depends only on `Compze.TypeIdentifiers`.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## Installation

```shell
dotnet add package Compze.TypeIdentifiers.Interning
```

## License

Apache-2.0
