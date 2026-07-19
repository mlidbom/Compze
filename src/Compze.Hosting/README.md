# Compze.Hosting

Endpoint hosting for the Compze framework. This package never looks inside an endpoint — the host receives
composed endpoints (`RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))`), starts
them, and disposes them.

## What's in this package?

- **`EndpointHost`** — a convenience owning several endpoints' lifecycles in one process: starting the host
  starts every endpoint, each driving its own phase ordering (listen → announce → send), and disposing it
  disposes them. Endpoints are first-class; the host adds nothing an endpoint cannot do alone
- **`AppSettingsJsonConfigurationParameterProvider`** — Configuration parameters from `appsettings.json`

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | The Tessaging paradigm - the endpoint types (`ExactlyOnceEndpoint` / `BestEffortEndpoint`), the LocalTessagingEngine, and the pure client |

## License

Apache-2.0
