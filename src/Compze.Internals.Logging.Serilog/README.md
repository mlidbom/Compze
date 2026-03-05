# Compze.Internals.Logging.Serilog

[Serilog](https://www.nuget.org/packages/Serilog) logging integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides a Serilog adapter for Compze's logging abstractions:

- **Logger adapter** — `SerilogLogger` implementing Compze's logging contracts
- **Preconfigured sinks** — Console and [Seq](https://datalust.co/seq) sinks included
- **Environment enrichment** — Automatic enrichment with machine name and environment details
- **Compact JSON formatting** — Structured logging with compact JSON output

## Installation

```shell
dotnet add package Compze.Internals.Logging.Serilog
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Utilities](https://www.nuget.org/packages/Compze.Utilities) | Core utilities and logging abstractions |

## License

Apache-2.0
