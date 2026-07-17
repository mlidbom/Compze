> **The `Compze.Typermedia.Hosting.Testing` package folded into `Compze.Tessaging` on 2026-07-17** - this README describes it as
> it was packaged separately; the prose is rewritten when the paradigm's docs are.

# Compze.Typermedia.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) Typermedia hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

Typermedia's plug-in for the testing endpoint host in `Compze.Hosting.Testing`, plus a remote test client:

- **`DistributedTypermediaTestingEndpointHostFeature`** — wires the distributed Typermedia pipeline and transport into every endpoint a `TestingEndpointHost` registers.
- **`TypermediaTestClient`** — a remote Typermedia client running in its own container, connecting to an endpoint's typermedia address over HTTP exactly as an external client application would.
- **Transport test wiring** — `CurrentTestsTypermediaTransport()` for endpoints, `CurrentTestsTypermediaClientTransport()` for clients.

### Quick start

```csharp
using var host = TestingEndpointHost.Create(new DistributedTypermediaTestingEndpointHostFeature());
var endpoint = host.RegisterEndpoint("MyEndpoint", endpointId, builder =>
{
   builder.RegisterTypermediaHandlers.ForTuery((MyTuery tuery) => HandleTuery(tuery));
});
await host.StartAsync();

await using var client = await TypermediaTestClient.ConnectTo(endpoint.TypermediaAddress!, mapper => mapper.RegisterMyDomainTypeMappings());
var result = client.Navigator.Get(new MyTuery());
```

## Installation

```shell
dotnet add package Compze.Typermedia.Hosting.Testing
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Hosting.Testing](https://www.nuget.org/packages/Compze.Hosting.Testing) | The testing endpoint host this feature plugs into |
| [Compze.Typermedia](https://www.nuget.org/packages/Compze.Typermedia) | The Typermedia navigation model |
| [Compze.Typermedia.Hosting.AspNetCore](https://www.nuget.org/packages/Compze.Typermedia.Hosting.AspNetCore) | ASP.NET Core hosting |

## License

Apache-2.0
