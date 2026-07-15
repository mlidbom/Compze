# Compze.Tessaging

Messaging infrastructure for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the messaging infrastructure that powers Compze's type-routed communication:

- **Tommand, tuery, and tevent handling** — Type-routed message dispatch for `ITommand`, `ITuery`, and `ITevent`
- **Message routing** — Automatic routing based on .NET type compatibility
- **Three compositions, each containing the one below** — `InProcessTessaging()` composes the synchronous in-process core into a plain container with no transports at all; `AddTransientTessaging()` adds guarantee-free conversation across endpoints with nothing persisted anywhere; `AddExactlyOnceTessaging()` adds the full inbox/outbox pipeline through which endpoints converse with delivery guarantees

### Type-routed messaging example

```csharp
registrar
  .ForTevent<IUserTevent>(userTevent => Console.WriteLine("User tevent"))
  .ForTevent<IUserRegistered>(userRegistered => Console.WriteLine("User registered"));
```

When an `IUserRegistered` tevent (which implements `IUserTevent`) is published, **both handlers** are called — no manual routing needed.

## Installation

```shell
dotnet add package Compze.Tessaging
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Abstractions](https://www.nuget.org/packages/Compze.Abstractions) | Shared abstractions |
| [Compze.Teventive.TeventStore](https://www.nuget.org/packages/Compze.Teventive.TeventStore) | The tevent store implementation |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Testing support |

## License

Apache-2.0
