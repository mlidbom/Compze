# Compze.Internals.Logging.Serilog

[Serilog](https://www.nuget.org/packages/Serilog) logging integration for the [Compze](https://github.com/mlidbom/Compze) framework.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides a Serilog adapter for Compze's logging abstractions:

- **Log backend** — `SerilogLogBackend`, which writes Compze's log lines to Serilog as structured events
- **Preconfigured sinks** — Console and [Seq](https://datalust.co/seq) sinks included
- **Environment enrichment** — Automatic enrichment with machine name and environment details
- **Compact JSON formatting** — Structured logging with compact JSON output

## Installation

```shell
dotnet add package Compze.Internals.Logging.Serilog
```

## Usage

Choose it as the process's logging backend. Compze's loggers look the backend up for each line they write, and the
backend looks up Serilog's static `Log.Logger` for each line, so neither assignment has to come before anything else:

```csharp
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
CompzeLogger.Backend = SerilogLogBackend.Global;
```

To write to a Serilog logger of your own instead of `Log.Logger`, use `SerilogLogBackend.WritingTo(serilogLogger)`.

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Internals.Logging](https://www.nuget.org/packages/Compze.Internals.Logging) | The logging abstractions this package is a backend for |

## License

Apache-2.0
