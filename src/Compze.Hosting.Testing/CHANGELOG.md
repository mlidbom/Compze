# Changelog

## 0.2.0-alpha

- The package narrows to the pluggable-component test wiring (DI containers, serializers, database pools, `TestingComponentRegistrar`). The testing endpoint host and its `ITestingEndpointHostFeature` seam are gone: the seam died with the endpoint feature machinery, and the surviving testing host lives in `Compze.Tessaging.Hosting.Testing`, registering the concrete endpoint types with per-tier test wiring — which requires knowing the tiers.

## 0.1.0-alpha.1

- Initial release: the `TestingEndpointHost` with its `ITestingEndpointHostFeature` seam, and the pluggable-component test wiring (DI containers, serializers, database pools) extracted from `Compze.Tessaging.Hosting.Testing`.
