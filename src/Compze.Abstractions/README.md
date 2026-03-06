# Compze.Abstractions

Foundational abstractions for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **Entity and identity types** — `EntityId`, `TentityId`, `TaggregateId`, and strongly-typed ID base classes
- **Messaging contracts** — `ITessage`, `ITevent`, `ITommand`, `ITuery`, and the full message type hierarchy
- **Time abstractions** — `IUtcTimeTimeSource`
- **Serialization interfaces** — `IJsonSerializer`, `IBinarySerializer`
- **Type mapping** — `ITypeMappingRegistar`, type identity infrastructure

## Installation

```shell
dotnet add package Compze.Abstractions
```

## License

Apache-2.0
