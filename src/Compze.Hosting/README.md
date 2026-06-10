# Compze.Hosting

Paradigm-neutral endpoint hosting for the Compze framework. This package knows nothing about any particular
message paradigm — paradigm pipelines (Tessaging, Typermedia) plug themselves into the endpoint builder as
features, each from its own package.

## What's in this package?

- **`EndpointHost`** — Manages endpoint lifecycle: all endpoints start listening before any starts sending
- **`Endpoint`** — A running endpoint that drives the lifecycle of whatever components its features registered
- **`ServerEndpointBuilder`** — The `IEndpointBuilder` implementation: container, type mapper, features, components
- **`AppSettingsJsonConfigurationParameterProvider`** — Configuration parameters from `appsettings.json`

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Messaging pipeline; plugs in via `AddTessaging()` / `RegisterTessagingHandlers` |
| [Compze.Typermedia.Client](https://www.nuget.org/packages/Compze.Typermedia.Client) | Typermedia pipeline; plugs in via `AddTypermedia()` / `RegisterTypermediaHandlers` |

## License

Apache-2.0
