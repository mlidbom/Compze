# Changelog

## 0.2.0-alpha

- `TestingEndpointHost` owns a real `InterprocessEndpointRegistry` (`ITestingEndpointHost.EndpointRegistry`), created per host in a unique temp directory and deleted when the host is disposed. The testing features have every endpoint `ParticipateIn` it, so every test runs the production announce/discover pipeline — announcement, signal-driven reconciliation, retraction — instead of a test-only in-memory registry.

## 0.1.0-alpha.1

- Initial release: the `TestingEndpointHost` with its `ITestingEndpointHostFeature` seam, and the pluggable-component test wiring (DI containers, serializers, database pools) extracted from `Compze.Tessaging.Hosting.Testing`.
