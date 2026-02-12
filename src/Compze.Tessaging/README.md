# Compze.Tessaging

Messaging infrastructure for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides the messaging infrastructure that powers Compze's type-routed communication:

- **Command, query, and event handling** — Type-routed message dispatch for `ITommand`, `ITuery`, and `ITevent`
- **Endpoint hosting** — `EndpointConfiguration` and `IEndpointHost` for composing messaging endpoints
- **Message routing** — Automatic routing based on .NET type compatibility
- **In-memory and distributed transport** — Pluggable transport layer supporting both in-process and remote messaging
- **Typermedia API support** — Type-based hypermedia-style API navigation

### Type-routed messaging example

```csharp
registrar
  .ForEvent<IUserEvent>(e => Console.WriteLine("User event"))
  .ForEvent<IUserRegistered>(e => Console.WriteLine("User registered"));
```

When `IUserRegistered` (which implements `IUserEvent`) is published, **both handlers** are called — no manual routing needed.

## Installation

```shell
dotnet add package Compze.Tessaging
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Core](https://www.nuget.org/packages/Compze.Core) | Core abstractions |
| [Compze.Tessaging.Teventive.TeventStore](https://www.nuget.org/packages/Compze.Tessaging.Teventive.TeventStore) | Event store implementation |
| [Compze.Tessaging.Hosting.AspNetCore](https://www.nuget.org/packages/Compze.Tessaging.Hosting.AspNetCore) | ASP.NET Core hosting |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Testing support |

## License

Apache-2.0
