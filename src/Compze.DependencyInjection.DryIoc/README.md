# Compze.DependencyInjection.DryIoc

[DryIoc](https://www.nuget.org/packages/DryIoc.dll) integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides a DryIoc adapter for Compze's dependency injection abstractions:

- **Container adapter** — Implementing Compze's DI contracts using DryIoc
- **Lifestyle mapping** — Automatic translation between Compze and DryIoc service lifetimes
- **Pluggable DI** — Use DryIoc as the backing implementation for Compze applications

## Installation

```shell
dotnet add package Compze.DependencyInjection.DryIoc
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.DependencyInjection](https://www.nuget.org/packages/Compze.DependencyInjection) | DI abstractions |
| [Compze.DependencyInjection.DryIoc.Extensions.Hosting](https://www.nuget.org/packages/Compze.DependencyInjection.DryIoc.Extensions.Hosting) | DryIoc hosting integration |
| [Compze.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.DependencyInjection.Autofac](https://www.nuget.org/packages/Compze.DependencyInjection.Autofac) | Autofac DI integration |

## License

Apache-2.0
