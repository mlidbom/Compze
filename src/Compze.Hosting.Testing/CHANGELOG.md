# Changelog

All notable changes to Compze.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.5.0-alpha (was 0.4.0-alpha), Compze.DbPool 0.4.0-alpha (was 0.3.0-alpha), Compze.DbPool.MicrosoftSql 0.4.1-alpha (was 0.3.0-alpha), Compze.DbPool.MySql 0.5.1-alpha (was 0.3.0-alpha), Compze.DbPool.PostgreSql 0.4.1-alpha (was 0.3.0-alpha), Compze.DbPool.Sqlite 0.4.0-alpha (was 0.3.0-alpha), Compze.DependencyInjection 0.11.0-alpha (was 0.9.0-alpha), Compze.Hosting 0.5.1-alpha (was 0.4.0-alpha), Compze.Internals.SystemCE 0.14.0-internal (was 0.8.0-internal), Compze.Internals.Testing 0.4.0-alpha (was 0.3.0-alpha), Compze.Serialization.Newtonsoft 0.6.2-alpha (was 0.5.0-alpha), Compze.Tessaging 0.15.0-alpha (was 0.9.0-alpha), Compze.Tessaging.Transport.AspNetCore 0.5.0-alpha (was 0.4.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.
- **Requires exact versions of the Compze packages whose internals it uses:** Compze.Sql.MicrosoftSql 0.8.1-alpha (was 0.5.0-alpha or newer), Compze.Sql.MySql 0.9.1-alpha (was 0.5.0-alpha or newer), Compze.Sql.PostgreSql 0.8.1-alpha (was 0.5.0-alpha or newer), Compze.Sql.Sqlite 0.8.1-alpha (was 0.5.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.DbPool 0.3.0-alpha (was 0.2.2-alpha), Compze.DbPool.MicrosoftSql 0.3.0-alpha (was 0.2.2-alpha), Compze.DbPool.MySql 0.3.0-alpha (was 0.2.2-alpha), Compze.DbPool.PostgreSql 0.3.0-alpha (was 0.2.2-alpha), Compze.DbPool.Sqlite 0.3.0-alpha (was 0.2.2-alpha), Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.DependencyInjection.Autofac 0.6.0-alpha (was 0.5.1-alpha), Compze.DependencyInjection.Autofac.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.DependencyInjection.DryIoc 0.6.0-alpha (was 0.5.1-alpha), Compze.DependencyInjection.DryIoc.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.DependencyInjection.LightInject 0.6.0-alpha (was 0.5.1-alpha), Compze.DependencyInjection.LightInject.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.DependencyInjection.Microsoft 0.6.0-alpha (was 0.5.1-alpha), Compze.DependencyInjection.Microsoft.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.Hosting 0.4.0-alpha (was 0.3.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.Internals.Testing 0.3.0-alpha (was 0.1.1-alpha), Compze.Serialization.Newtonsoft 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.MicrosoftSql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.MySql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.PostgreSql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.Sqlite 0.5.0-alpha (was 0.4.0-alpha), Compze.Tessaging 0.9.0-alpha (was 0.5.0-alpha), Compze.Tessaging.Transport.AspNetCore 0.4.0-alpha (was 0.3.0-alpha), Compze.TypeIdentifiers 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- The package narrows to the pluggable-component test wiring (DI containers, serializers, database pools, `TestingComponentRegistrar`). The testing endpoint host and its `ITestingEndpointHostFeature` seam are gone: the seam died with the endpoint feature machinery, and the surviving testing host lives in `Compze.Tessaging.Hosting.Testing`, registering the concrete endpoint types with per-tier test wiring — which requires knowing the tiers.
- **The test-matrix vocabulary lives here now**: `DIContainer`, `Serializer`, `SqlLayer`, `Transport` and their `ValueFor` helpers moved from `Compze.Abstractions`, beside the `PluggableComponents` record that aggregates them. Which components a test run exercises is QA vocabulary, not a core abstraction — it sat in `Compze.Abstractions` only because that is the one assembly every test project already referenced.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `TestingEndpointHost` owns a real `InterprocessEndpointRegistry` (`ITestingEndpointHost.EndpointRegistry`), created per host in a unique temp directory and deleted when the host is disposed. The testing features have every endpoint `ParticipateIn` it, so every test runs the production announce/discover pipeline — announcement, signal-driven reconciliation, retraction — instead of a test-only in-memory registry.

## 0.1.0-alpha.1

- Initial release: the `TestingEndpointHost` with its `ITestingEndpointHostFeature` seam, and the pluggable-component test wiring (DI containers, serializers, database pools) extracted from `Compze.Tessaging.Hosting.Testing`.
