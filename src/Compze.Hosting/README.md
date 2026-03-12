# Compze.Hosting

Combined hosting for Tessaging and Typermedia endpoints — shared DI container, separate transports.

## What's in this package?

- **`EndpointHost`** — Manages endpoint lifecycle (start/stop listening and sending)
- **`Endpoint`** — A running endpoint with both tessaging and typermedia capabilities
- **`ServerEndpointBuilder`** — Fluent builder for configuring endpoints with DI, message handlers, and typermedia handlers

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging infrastructure |
| [Compze.Typermedia](https://www.nuget.org/packages/Compze.Typermedia) | Typermedia API infrastructure |

## License

Apache-2.0
