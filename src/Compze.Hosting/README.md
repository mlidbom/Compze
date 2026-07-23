# Compze.Hosting

Endpoint hosting for the Compze framework. This package never looks inside an endpoint — the host builds
each registered endpoint-declaration in its environment (`RegisterEndpoint(declaration)`), starts the built
endpoints, and disposes them.

## What's in this package?

- **`EndpointHost`** — a convenience owning several endpoints' lifecycles in one process: starting the host
  starts every endpoint, each driving its own phase ordering (listen → announce → send), and disposing it
  disposes them. Endpoints are first-class; the host adds nothing an endpoint cannot do alone
- **`AppSettingsJsonConfigurationParameterProvider`** — Configuration parameters from `appsettings.json`

## Related packages

| Package | Description |
|---------|-------------|
| [Compze.Tessaging](https://www.nuget.org/packages/Compze.Tessaging) | Tessaging - the endpoint types (`ExactlyOnceEndpoint` / `BestEffortEndpoint`), the LocalTessagingEngine, and the pure client |

## License

Apache-2.0
