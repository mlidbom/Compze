# Compze.DependencyInjection.Microsoft

[Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection) integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides a Microsoft DI adapter for Compze's dependency injection abstractions:

- **Container adapter** — `MicrosoftDependencyInjectionContainer` implementing Compze's DI contracts
- **Lifestyle mapping** — Automatic translation between Compze and Microsoft DI service lifetimes
- **Pluggable DI** — Use Microsoft's DI container as the backing implementation for Compze applications

## Installation

```shell
dotnet add package Compze.DependencyInjection.Microsoft
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities and DI abstractions |
| [Compze.DependencyInjection.SimpleInjector](https://www.nuget.org/packages/Compze.DependencyInjection.SimpleInjector) | SimpleInjector integration |

## License

Apache-2.0
