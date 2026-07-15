# Compze.Teventive.TeventStore

The tevent store: event-sourcing persistence for [Compze](https://github.com/mlidbom/Compze) taggregates.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the tevent store that powers Compze's teventive (type-routed event) programming model:

- **Tevent persistence** — `TeventStore` for storing and retrieving taggregate tevent streams
- **Taggregate updates** — `TeventStoreUpdater` for transactional taggregate modifications
- **Tevent caching** — `ITeventCache` for in-memory tevent stream caching
- **Query model generation** — Self-generating query models that automatically stay in sync with tevent streams
- **Tevent migration** — Support for tevent schema evolution and migration
- **Taggregate history validation** — Built-in validation of taggregate tevent stream consistency

### Teventive programming

Tevents use the .NET type system to declare their meaning:

```csharp
interface IUserTevent : ITaggregateTevent;
interface IUserRegistered : IUserTevent, ITaggregateCreatedTevent;
interface IUserImported : IUserRegistered;
```

The tevent store routes and stores these tevents based on type compatibility, enabling expressive domain modeling without manual routing.

## Installation

```shell
dotnet add package Compze.Teventive.TeventStore
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Teventive](https://www.nuget.org/packages/Compze.Teventive) | The Teventive programming model: taggregates, tevents and tevent dispatching |
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Testing support |

## License

Apache-2.0
