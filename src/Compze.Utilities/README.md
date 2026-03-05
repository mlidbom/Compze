# Compze.Utilities

Core utilities for the [Compze](https://github.com/mlidbom/Compze) framework.

## What's in this package?

This package provides foundational utilities used throughout the Compze framework:

- **Dependency injection abstractions** — Container-agnostic DI contracts
- **Threading helpers** — Resource access synchronization, task extensions
- **System extensions** — Extensions for common .NET types (`String`, `Type`, `DateTime`, collections, etc.)
- **Functional programming helpers** — Option types and functional composition utilities
- **Code contracts** — Runtime assertions and argument validation
- **Logging abstractions** — Framework-agnostic logging contracts

## Installation

```shell
dotnet add package Compze.Utilities
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Core](https://www.nuget.org/packages/Compze.Core) | Core abstractions |
| [Compze.DependencyInjection.Microsoft](https://www.nuget.org/packages/Compze.DependencyInjection.Microsoft) | Microsoft DI integration |
| [Compze.DependencyInjection.SimpleInjector](https://www.nuget.org/packages/Compze.DependencyInjection.SimpleInjector) | SimpleInjector integration |
| [Compze.Utilities.Logging.Serilog](https://www.nuget.org/packages/Compze.Utilities.Logging.Serilog) | Serilog integration |

## License

Apache-2.0
