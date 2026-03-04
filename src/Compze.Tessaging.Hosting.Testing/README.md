# Compze.Tessaging.Hosting.Testing

Testing support for [Compze](https://github.com/mlidbom/Compze) messaging hosting.

## What is Compze?

Compze is a .NET framework for building expressive domains through **Teventive programming** and **Typermedia APIs**. [Learn more](https://compze.net/)

## What's in this package?

This package provides comprehensive testing infrastructure for Compze applications:

- **`TestEnv`** — Static entry point for configuring pluggable component combinations (SQL provider, DI container, serializer, transport) under test
- **Test host** — `TestingEndpointHost` for spinning up fully configured messaging endpoints in tests
- **Database pool integration** — Automatic database provisioning and cleanup for integration tests
- **Pluggable component testing** — Test your domain against all supported SQL providers, DI containers, serializers, and transports
- **Performance testing utilities** — `TimedExecutionSummary` and `TimedThreadedExecutionSummary` for benchmarking

### Quick start

```csharp
using var host = TestEnv.DIContainer.SetupTestingServiceLocator(builder =>
{
    builder.RegisterEndpoint("MyEndpoint", setup =>
    {
        // Configure your endpoint
    });
});

// Use host to send commands, publish events, run queries
```

## Installation

```shell
dotnet add package Compze.Tessaging.Hosting.Testing
```

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Tessaging.Hosting.AspNetCore](https://www.nuget.org/packages/Compze.Tessaging.Hosting.AspNetCore) | ASP.NET Core hosting |
| [Compze.Must](https://www.nuget.org/packages/Compze.Must) | Fluent assertions |
| [Compze.Utilities.Testing.XUnit](https://www.nuget.org/packages/Compze.Utilities.Testing.XUnit) | xUnit utilities |

## License

Apache-2.0
