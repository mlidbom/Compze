# Compze.Core

Core abstractions for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the foundational abstractions that all other Compze packages build upon:

- **Entity and identity types** — `EntityId`, `TentityId`, `TaggregateId`, and strongly-typed ID base classes
- **Messaging contracts** — `ITessage`, `ITevent`, `ITommand`, `ITuery`, and the full message type hierarchy
- **Teventive event interfaces** — `ITaggregate`, `IAggregateEvent`, `IAggregateCreatedEvent`, and related types for modeling event-sourced aggregates
- **Event store abstractions** — `ITeventStore`, `ITeventStoreReader`, `ITeventStoreUpdater`
- **Document DB abstractions** — `IDocumentDb`, `IDocumentDbSession`, `IDocumentDbReader`, `IDocumentDbUpdater`
- **Endpoint and hosting contracts** — `EndpointConfiguration`, `IEndpointHost`, `IEndpoint`
- **Time abstractions** — `IUtcTimeTimeSource`

## Installation

```shell
dotnet add package Compze.Core
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |
| [Compze.Tessaging.Teventive.TeventStore](https://www.nuget.org/packages/Compze.Tessaging.Teventive.TeventStore) | Event store implementation |

## License

Apache-2.0
