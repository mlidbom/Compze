# Compze.Abstractions

Foundational abstractions for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

- **Entity and identity types** — `EntityId`, `TentityId`, `TaggregateId`, the `Entity<>` base classes, and `ValueWrapper<>`
- **Time abstractions** — `IUtcTimeTimeSource`, and the testing time source that overrides it
- **Serialization interfaces** — `IJsonSerializer`, `IDocumentDbSerializer`
- **Configuration** — `IConfigurationParameterProvider`

The tessage type system (`ITessage`, `ITevent`, `ITommand`, `ITuery` and the full hierarchy) is **not** here —
it lives in `Compze.Tessaging.Abstractions`.

## Installation

```shell
dotnet add package Compze.Abstractions
```

## License

Apache-2.0
