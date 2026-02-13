# Compze.Utilities.Logging

Lightweight logging abstractions for [Compze](https://github.com/mlidbom/Compze).

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

A pluggable logging abstraction with console default, log level filtering, suppression support, and structured exception formatting.

### Core API

```csharp
// Get a logger for your class
var log = CompzeLogger.For<MyService>();

log.Info("Processing order");
log.Debug("Order details: {0}", orderId);
log.Warning("Slow query detected");
log.Error("Failed to process", exception);
```

### Features

- **Log levels** — `Error`, `Warning`, `Info`, `Debug`
- **Level loggers** — Scoped single-level loggers via `log.Info()`, `log.Debug()`, `log.Warning()`
- **Scope logging** — `LogMethodEntryExit()` and `LogMethodExecutionTime()` for automatic method tracing
- **Suppression** — `CompzeLogger.SuppressLoggingWhileRunningAsync()` for silencing logs in tests
- **Exception formatting** — Rich, indented exception messages with `AggregateException` unwrapping
- **Pluggable factory** — Replace `CompzeLogger.LoggerFactoryMethod` to integrate any logging framework

### Scope logging

```csharp
using(log.Info().LogMethodEntryExit())
{
    // Logs "Entering MyMethod" and "Exiting MyMethod" automatically
}
```

### Default: console

Ships with `ConsoleLogger` as the default — thread-safe `Console.WriteLine` output. Replace with [Serilog integration](https://www.nuget.org/packages/Compze.Utilities.Logging.Serilog) or your own `ILogger` implementation.

## Installation

```shell
dotnet add package Compze.Utilities.Logging
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities.Logging.Serilog](https://www.nuget.org/packages/Compze.Utilities.Logging.Serilog) | Serilog integration |
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities |

## License

Apache-2.0
