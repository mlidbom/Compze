# Compze.Internals.Logging

Lightweight logging abstractions for [Compze](https://github.com/mlidbom/Compze).

## Usage

Code logs through an `ILogger`, made for the type that logs:

```csharp
static readonly ILogger Log = CompzeLogger.For<OrderProcessor>();
Log.Info($"Processed order {orderId}");
```

Where the lines go is the process's `CompzeLogger.Backend`: the console unless the host assigns another, such as
`SerilogLogBackend.Global` from [Compze.Internals.Logging.Serilog](https://www.nuget.org/packages/Compze.Internals.Logging.Serilog).
Loggers look the backend up for each line they write, so a logger made before the host assigns it still writes to it.

## License

Apache-2.0
