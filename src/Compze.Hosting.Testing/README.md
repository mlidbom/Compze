# Compze.Hosting.Testing

Paradigm-neutral testing support for [Compze](https://github.com/mlidbom/Compze) endpoint hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

The testing counterpart of `Compze.Hosting`: a testing endpoint host that knows no paradigm, plus the pluggable-component wiring that builds test containers from the current test configuration.

- **`TestingEndpointHost`** — the endpoint host tests use. Paradigm features (`Compze.Tessaging.Hosting.Testing`, `Compze.Typermedia.Hosting.Testing`) plug into it, and every endpoint it registers gets their wiring plus the current test's pluggable components.
- **`ITestingEndpointHostFeature`** — the seam those paradigm packages implement.
- **`TestingComponentRegistrar`** — the component registrar all test containers are built with; routes connection-string lookups through the test database pool.
- **Pluggable-component wiring** — DI container, serializer, and database pool selection driven by the current test's `PluggableComponents` configuration.

### Quick start

```csharp
using var host = TestingEndpointHost.Create(new TessagingTestingEndpointHostFeature(),
                                            new TypermediaTestingEndpointHostFeature());
var endpoint = host.RegisterEndpoint("MyEndpoint", endpointId, builder =>
{
   // Register handlers; both paradigm pipelines are already wired in.
});
await host.StartAsync();
```

## Installation

```shell
dotnet add package Compze.Hosting.Testing
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Hosting](https://www.nuget.org/packages/Compze.Hosting) | The endpoint hosting mechanism this package tests against |
| [Compze.Tessaging.Hosting.Testing](https://www.nuget.org/packages/Compze.Tessaging.Hosting.Testing) | The Tessaging testing feature |
| [Compze.Typermedia.Hosting.Testing](https://www.nuget.org/packages/Compze.Typermedia.Hosting.Testing) | The Typermedia testing feature |

## License

Apache-2.0
