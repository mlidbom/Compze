# Compze.Hosting

Endpoint hosting for the Compze framework. This package knows nothing of Tessaging, Typermedia, or any other
capability — each plugs its pipeline into the endpoint builder as a feature, from its own package.

## What's in this package?

- **`EndpointHost`** — Manages endpoint lifecycle: all endpoints start listening before any starts sending
- **`Endpoint`** — A running endpoint that drives the lifecycle of whatever components its features registered
- **`ServerEndpointBuilder`** — The `IEndpointBuilder` implementation: container, type mapper, features, components
- **`AppSettingsJsonConfigurationParameterProvider`** — Configuration parameters from `appsettings.json`

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging pipeline; plugs in via `AddDistributedTessaging()` / `RegisterTessagingHandlers` |
| [Compze.Typermedia.Client](https://www.nuget.org/packages/Compze.Typermedia.Client) | Typermedia pipeline; plugs in via `AddDistributedTypermedia()` / `RegisterTypermediaHandlers` |

## License

Apache-2.0
