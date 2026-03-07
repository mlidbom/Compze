# Compze.DependencyInjection.Autofac

[Autofac](https://www.nuget.org/packages/Autofac) integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides an Autofac adapter for Compze's dependency injection abstractions:

- **Container adapter** — `AutofacDependencyInjectionContainer` implementing Compze's DI contracts
- **Lifestyle mapping** — Automatic translation between Compze and Autofac service lifetimes
- **Pluggable DI** — Use Autofac as the backing implementation for Compze applications

## Installation

```shell
dotnet add package Compze.DependencyInjection.Autofac
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities and DI abstractions |
| [Compze.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.DependencyInjection.SimpleInjector](https://www.nuget.org/packages/Compze.DependencyInjection.SimpleInjector) | SimpleInjector DI integration |

## License

Apache-2.0
