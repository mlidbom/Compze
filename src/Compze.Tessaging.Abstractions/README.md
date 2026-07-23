# Compze.Tessaging.Abstractions

The tessage type system for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Tessaging is conversation through **tessages** — messages routed by their .NET type. A tessage's type *is*
its whole contract: its kind, its delivery guarantee, its transactionality, its remotability and its
synchrony are all declared by the interfaces the type extends. This package is that type system, and
nothing else:

- **The tessage hierarchy** — `ITessage` and its kinds `ITevent`, `ITommand`, `ITuery`
- **Delivery-guarantee markers** — `IRemotableTevent`, `IAtMostOnceTessage`, `IAtLeastOnceTessage`, `IExactlyOnceTevent`, `IExactlyOnceTommand`, and the `IStrictlyLocal*` family
- **Transactional markers** — `IMustBeSentTransactionally`, `IMustBeHandledTransactionally`, `ICannotBeSentRemotelyFromWithinTransaction`
- **Base types** — the `TessageTypes` families implementing the markers, and `TessageId`, the identity deduplication is done on
- **Publisher-identifying tevents** — `IPublisherTevent<TTevent>` and `PublisherTevent<TTevent>`, the wrapper every tevent is routed by
- **The send/publish contracts** — `IUnitOfWorkTeventPublisher`/`IIndependentTeventPublisher` and `IUnitOfWorkTommandSender`/`IIndependentTommandSender`
- **The design rules** — `TessageTypeDesignRules`, the rules above as a testing surface: run your own tessage types
  through them and a type that breaks one fails your suite instead of a deployment

## Why it is separate from Compze.Tessaging

So that declaring tessages does not mean depending on the machinery that delivers them. An application's
contracts assembly — shared between the processes that converse — needs the type system alone. `Compze.Teventive`
depends on it for the same reason: a taggregate raises tevents without any endpoint being involved.

The engine that routes and delivers these tessages is `Compze.Tessaging`.

## Installation

```shell
dotnet add package Compze.Tessaging.Abstractions
```

## License

Apache-2.0
