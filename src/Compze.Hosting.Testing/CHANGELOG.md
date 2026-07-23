# Changelog

All notable changes to Compze.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- The package narrows to the pluggable-component test wiring (DI containers, serializers, database pools, `TestingComponentRegistrar`). The testing endpoint host and its `ITestingEndpointHostFeature` seam are gone: the seam died with the endpoint feature machinery, and the surviving testing host lives in `Compze.Tessaging.Hosting.Testing`, registering the concrete endpoint types with per-tier test wiring — which requires knowing the tiers.
- **The test-matrix vocabulary lives here now**: `DIContainer`, `Serializer`, `SqlLayer`, `Transport` and their `ValueFor` helpers moved from `Compze.Abstractions`, beside the `PluggableComponents` record that aggregates them. Which components a test run exercises is QA vocabulary, not a core abstraction — it sat in `Compze.Abstractions` only because that is the one assembly every test project already referenced.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `TestingEndpointHost` owns a real `InterprocessEndpointRegistry` (`ITestingEndpointHost.EndpointRegistry`), created per host in a unique temp directory and deleted when the host is disposed. The testing features have every endpoint `ParticipateIn` it, so every test runs the production announce/discover pipeline — announcement, signal-driven reconciliation, retraction — instead of a test-only in-memory registry.

## 0.1.0-alpha.1

- Initial release: the `TestingEndpointHost` with its `ITestingEndpointHostFeature` seam, and the pluggable-component test wiring (DI containers, serializers, database pools) extracted from `Compze.Tessaging.Hosting.Testing`.
