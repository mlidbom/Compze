# Compze.DependencyInjection.LightInject

[LightInject](https://www.nuget.org/packages/LightInject) integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides a LightInject adapter for Compze's dependency injection abstractions:

- **Container adapter** — Implementing Compze's DI contracts using LightInject
- **Lifestyle mapping** — Automatic translation between Compze and LightInject service lifetimes
- **Pluggable DI** — Use LightInject as the backing implementation for Compze applications

## Installation

```shell
dotnet add package Compze.DependencyInjection.LightInject
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection) | DI abstractions |
| [Compze.DependencyInjection.LightInject.Extensions.Hosting](https://www.nuget.org/packages/Compze.DependencyInjection.LightInject.Extensions.Hosting) | LightInject hosting integration |
| [Compze.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.DependencyInjection.Autofac](https://www.nuget.org/packages/Compze.DependencyInjection.Autofac) | Autofac DI integration |

## License

Apache-2.0
