# Compze.Tessaging.Hosting.AspNetCore

ASP.NET Core hosting integration for [Compze](https://github.com/mlidbom/Compze) messaging.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package integrates Compze messaging endpoints with ASP.NET Core:

- **ASP.NET Core host integration** — Run Compze messaging endpoints inside an ASP.NET Core application
- **Typermedia API hosting** — Expose Compze Typermedia APIs over HTTP
- **Transport bridge** — Connect Compze's in-memory message bus to ASP.NET Core's request pipeline
- **Structured logging** — Integrated Seq logging support

## Installation

```shell
dotnet add package Compze.Tessaging.Hosting.AspNetCore
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Core](https://www.nuget.org/packages/Compze.Core) | Core abstractions |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | Testing support |

## License

Apache-2.0
