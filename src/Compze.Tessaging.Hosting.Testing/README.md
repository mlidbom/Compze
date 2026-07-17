# Compze.Tessaging.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) Tessaging hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

The endpoint host tests use, plus the per-tier test wiring it hands each endpoint:

- **`TestingEndpointHost`** — registers the concrete endpoint types with the current test's concerns handed in at construction: `RegisterExactlyOnceEndpoint` / `RegisterBestEffortEndpoint` supply the host's one tessages-in-flight tracker, the current test's transport protocol and serializers, the pooled test database keyed by the endpoint's id (exactly-once tier), and participation in the host's own interprocess registry. On dispose the host waits until no tessages are in flight and rethrows background exceptions — so tests cannot silently drop in-flight work.
- **`TypermediaTestClient`** — the pure client composed for tests (see the Typermedia README beside this one).
- **Persistence test wiring** — `CurrentTestsConfiguredSqlLayer()` registers the Tessaging vertical's storage stack (type-id interner, document db, tessaging inbox/outbox, tevent store) against the SQL backend the current test runs against. (The endpoint's transport protocol comes from `CurrentTestsEndpointTransport()` in `Compze.Hosting.Testing`.)

### Quick start

```csharp
using var host = TestingEndpointHost.Create();
var endpoint = host.RegisterExactlyOnceEndpoint("MyEndpoint", endpointId, endpoint =>
{
   endpoint.RegisterTessageHandlers(handle => handle.ForTommand(async (MyTommand tommand, IUnitOfWorkResolver unitOfWork) => await HandleAsync(tommand)));
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
