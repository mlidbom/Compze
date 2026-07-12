# Compze.Tessaging.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) Tessaging hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Tessaging's plug-in for the testing endpoint host in `Compze.Hosting.Testing`:

- **`DistributedTessagingTestingEndpointHostFeature`** — wires the distributed Tessaging pipeline, transport, and persistence into every endpoint a `TestingEndpointHost` registers, tracks tessages in flight host-wide, and makes the host wait until everything is at rest before disposing — so tests cannot silently drop in-flight work.
- **Transport test wiring** — `CurrentTestsTessagingTransport()` registers the ASP.NET Core Tessaging transport for the current test configuration.
- **Persistence test wiring** — `CurrentTestsConfiguredSqlLayer()` registers the Tessaging vertical's storage stack (type-id interner, document db, tessaging inbox/outbox, tevent store) against the SQL backend the current test runs against.

### Quick start

```csharp
using var host = TestingEndpointHost.Create(new DistributedTessagingTestingEndpointHostFeature());
var endpoint = host.RegisterEndpoint("MyEndpoint", endpointId, builder =>
{
   builder.RegisterTessagingHandlers.ForTommand((MyTommand tommand) => Handle(tommand));
});
await host.StartAsync();
```

## Installation

```shell
dotnet add package Compze.Tessaging.Hosting.Testing
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Hosting.Testing](https://www.nuget.org/packages/Compze.Hosting.Testing) | The testing endpoint host this feature plugs into |
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Tessaging.Hosting.AspNetCore](https://www.nuget.org/packages/Compze.Tessaging.Hosting.AspNetCore) | ASP.NET Core hosting |

## License

Apache-2.0
