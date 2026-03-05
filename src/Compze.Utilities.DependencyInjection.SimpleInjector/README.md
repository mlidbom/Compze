# Compze.Utilities.DependencyInjection.SimpleInjector

[SimpleInjector](https://www.nuget.org/packages/SimpleInjector) integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides a SimpleInjector adapter for Compze's dependency injection abstractions:

- **Container adapter** — `SimpleInjectorDependencyInjectionContainer` implementing Compze's DI contracts
- **Lifestyle mapping** — Automatic translation between Compze and SimpleInjector service lifetimes
- **Pluggable DI** — Use SimpleInjector as the backing implementation for Compze applications

## Installation

```shell
dotnet add package Compze.Utilities.DependencyInjection.SimpleInjector
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities and DI abstractions |
| [Compze.Utilities.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.Utilities.DependencyInjection.Microsoft) | Microsoft DI integration |

## License

Apache-2.0
