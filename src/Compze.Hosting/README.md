# Compze.Hosting

Endpoint hosting for the Compze framework. This package never looks inside an endpoint — the host receives
composed endpoints (`RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))`) and drives
their shared lifecycle.

## What's in this package?

- **`EndpointHost`** — owns a set of composed endpoints and runs each lifecycle phase host-wide: every
  endpoint starts listening before any announces its address, and every address is announced before any
  endpoint starts sending
- **`AppSettingsJsonConfigurationParameterProvider`** — Configuration parameters from `appsettings.json`

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | The Tessaging paradigm - the endpoint types (`ExactlyOnceEndpoint` / `BestEffortEndpoint`), the LocalTessagingEngine, and the pure client |

## License

Apache-2.0
