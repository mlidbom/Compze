> **The `Compze.Typermedia.Hosting.Testing` package folded into `Compze.Tessaging` on 2026-07-17** - this README describes it as
> it was packaged separately; the prose is rewritten when the paradigm's docs are.

# Compze.Typermedia.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) Typermedia hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

The pure client composed for tests:

- **`TypermediaTestClient`** — the pure client (`TypermediaClient`) running in its own container, connecting to an endpoint's address over the current test's transport exactly as an external client application would.
- **Transport test wiring** — `CurrentTestsEndpointTransportClient()`, the transport-client strategy a pure client's composition declares.

Every endpoint the testing host registers serves typermedia — both endpoint types serve all four tessage kinds, unconditionally — so there is no typermedia-specific endpoint wiring here.

### Quick start

```csharp
class MyEndpointDeclaration : BestEffortEndpointDeclaration<MyEndpointDeclaration>, IEndpointIdentity
{
   public static string Name => "MyEndpoint";
   public static EndpointId Id { get; } = new(Guid.Parse("..."));

   protected override void RegisterTueryHandlers(ITueryHandlerRegistrar handle) =>
      handle.ForTuery((MyTuery tuery) => HandleTuery(tuery));
}

using var host = TestingEndpointHost.Create();
var endpoint = host.RegisterEndpoint(new MyEndpointDeclaration());
await host.StartAsync();

await using var client = await TypermediaTestClient.ConnectTo(endpoint.Address!, mapper => mapper.RegisterMyDomainTypeMappings());
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
