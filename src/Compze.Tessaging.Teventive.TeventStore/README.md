# Compze.Tessaging.Teventive.TeventStore

Event store implementation for [Compze](https://github.com/mlidbom/Compze) event sourcing.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the event store that powers Compze's teventive (type-routed event) programming model:

- **Event persistence** — `TeventStore` for storing and retrieving aggregate event streams
- **Aggregate updates** — `TeventStoreUpdater` for transactional aggregate modifications
- **Event caching** — `ITeventCache` for in-memory event stream caching
- **Query model generation** — Self-generating query models that automatically stay in sync with event streams
- **Event migration** — Support for event schema evolution and migration
- **Aggregate history validation** — Built-in validation of aggregate event stream consistency

### Teventive programming

Events use the .NET type system to declare their meaning:

```csharp
interface IUserEvent : IAggregateEvent;
interface IUserRegistered : IUserEvent, IAggregateCreatedEvent;
interface IUserImported : IUserRegistered;
```

The event store routes and stores these events based on type compatibility, enabling expressive domain modeling without manual routing.

## Installation

```shell
dotnet add package Compze.Tessaging.Teventive.TeventStore
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Core](https://www.nuget.org/packages/Compze.Core) | Core abstractions and event store interfaces |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Testing support |

## License

Apache-2.0
