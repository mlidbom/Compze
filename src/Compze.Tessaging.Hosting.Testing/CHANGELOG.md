# Changelog

All notable changes to Compze.Tessaging.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.7.0-alpha

- **Breaking: renamed for Microsoft.VisualStudio.Threading.Analyzers' VSTHRD200,** which asks a method that returns a task to end in Async: `TypermediaTestClient.ConnectTo` is `ConnectToAsync`, `AlsoConnectTo` is `AlsoConnectToAsync`, and `TestingEndpointHost.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest` is `DisposeWithoutWaitingForEndpointsToBeAtRestAsync`.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.10.0-alpha (was 0.9.0-alpha), Compze.Internals.SystemCE 0.10.0-internal (was 0.8.0-internal), Compze.Sql.MicrosoftSql 0.6.0-alpha (was 0.5.0-alpha), Compze.Sql.MySql 0.7.0-alpha (was 0.5.0-alpha), Compze.Sql.PostgreSql 0.6.0-alpha (was 0.5.0-alpha), Compze.Sql.Sqlite 0.6.0-alpha (was 0.5.0-alpha), Compze.Tessaging 0.11.0-alpha (was 0.9.0-alpha), Compze.Teventive.TeventStore.Abstractions 0.7.0-alpha (was 0.6.0-alpha), Compze.Teventive.TeventStore.MicrosoftSql 0.4.0-alpha (was 0.3.0-alpha), Compze.Teventive.TeventStore.MySql 0.4.0-alpha (was 0.3.0-alpha), Compze.Teventive.TeventStore.PostgreSql 0.4.1-alpha (was 0.3.0-alpha), Compze.Teventive.TeventStore.Sqlite 0.4.0-alpha (was 0.3.0-alpha), Compze.Threading 0.13.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.DocumentDb.MicrosoftSql 0.3.0-alpha (was 0.2.1-alpha), Compze.DocumentDb.MySql 0.3.0-alpha (was 0.2.1-alpha), Compze.DocumentDb.PostgreSql 0.3.0-alpha (was 0.2.1-alpha), Compze.DocumentDb.Sqlite 0.3.0-alpha (was 0.2.1-alpha), Compze.Hosting.Testing 0.4.0-alpha (was 0.3.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.7.0-internal), Compze.Internals.Testing 0.3.0-alpha (was 0.2.0-alpha), Compze.Sql.MicrosoftSql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.MySql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.PostgreSql 0.5.0-alpha (was 0.4.0-alpha), Compze.Sql.Sqlite 0.5.0-alpha (was 0.4.0-alpha), Compze.Tessaging 0.9.0-alpha (was 0.6.4-alpha), Compze.Tessaging.MicrosoftSql 0.4.0-alpha (was 0.3.0-alpha), Compze.Tessaging.MySql 0.4.0-alpha (was 0.3.0-alpha), Compze.Tessaging.PostgreSql 0.4.0-alpha (was 0.3.1-alpha), Compze.Tessaging.Sqlite 0.4.0-alpha (was 0.3.0-alpha), Compze.Tessaging.Transport.AspNetCore 0.4.0-alpha (was 0.3.0-alpha), Compze.Teventive.TeventStore.Abstractions 0.6.0-alpha (was 0.5.0-alpha), Compze.Teventive.TeventStore.MicrosoftSql 0.3.0-alpha (was 0.2.1-alpha), Compze.Teventive.TeventStore.MySql 0.3.0-alpha (was 0.2.1-alpha), Compze.Teventive.TeventStore.PostgreSql 0.3.0-alpha (was 0.2.1-alpha), Compze.Teventive.TeventStore.Sqlite 0.3.0-alpha (was 0.2.1-alpha), Compze.Threading 0.12.0-alpha (was 0.11.0-alpha), Compze.TypeIdentifiers.Interning.MicrosoftSql 0.3.0-alpha (was 0.2.1-alpha), Compze.TypeIdentifiers.Interning.MySql 0.3.0-alpha (was 0.2.1-alpha), Compze.TypeIdentifiers.Interning.PostgreSql 0.3.0-alpha (was 0.2.1-alpha), Compze.TypeIdentifiers.Interning.Sqlite 0.4.0-alpha (was 0.3.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.1-alpha

- **Disposing the host waits for its endpoints to be at rest without holding a thread.** The wait blocked the disposing thread, so a test that disposed the host while it held an observer at a gate had to dispose on a thread of its own. Requires Compze.Tessaging 0.6.4-alpha, whose at-rest wait it calls.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.SystemCE 0.7.0-internal (was 0.4.0-internal), Compze.Internals.Testing 0.2.0-alpha (was 0.1.1-alpha), Compze.Tessaging 0.6.4-alpha (was 0.5.0-alpha), Compze.Threading 0.11.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- **Registration is by endpoint-declaration**: `RegisterEndpoint(declaration)` builds an `ExactlyOnceEndpointDeclaration` / `BestEffortEndpointDeclaration` (`Compze.Tessaging`) in the host's environment — the current test's concerns as an `IEndpointEnvironment`: the host's one tessages-in-flight tracker, the current test's transport protocol, the pooled test database keyed by the endpoint's id (exactly-once tier), and participation in the host's real interprocess endpoint registry. The host's `Environment` is public, so a test needing the shared environment plus something extra wraps it in a decorating environment and registers with `RegisterEndpoint(declaration, environment)` — which is also how several endpoints join one shared domain database. The callback registrations (`RegisterExactlyOnceEndpoint`, `RegisterExactlyOnceEndpointInDomainDatabase`, `RegisterBestEffortEndpoint`) are deleted.
- `AwaitEndpointsHaveMetEachOtherAsync()`: awaits every endpoint of the host remembering every other as a peer — mutual first contact. Starting the host completes when every endpoint has started; whether they have *discovered each other* is topology convergence, completing at signal latency after the start. A specification whose very next act rides that discovery — an exactly-once tevent whose fan-out membership is the remembered subscribers (first contact is the boundary), an assertion over peer memory, an ambiguity pin needing every advertising handler visible — awaits this after starting instead of racing the reconciliation; a production composition awaits what it needs through the production surfaces instead (readiness, `RequirePeers`, waiting sends).
- The host's at-rest wait covers tevent observation: the engines' observation dispatch (off-thread since the observation redesign) reports its queued work to the host's tessages-in-flight tracker, so disposal completes only after every queued observation has dispatched — a test cannot pass with observation work in flight — and a throwing observer's failure, reported to the background-exception reporter, is rethrown at disposal like any other background failure. Pinned in `Tevent_observation_at_host_disposal_tests`.
- **The testing host is per-tier, concrete wiring**: `TestingEndpointHost` (moved here from `Compze.Hosting.Testing` — its environment binds the pooled test database, which requires knowing the tiers) hands each registered endpoint its test concerns at build — every test runs the production announce/discover pipeline. On dispose the host waits until no tessages are in flight and rethrows background exceptions no assertion observed. The `ITestingEndpointHostFeature` seam and both features (`ExactlyOnceTessagingTestingEndpointHostFeature`, `DistributedTypermediaTestingEndpointHostFeature`) died with the endpoint feature machinery.
- `TypermediaTestClient` rides the pure client (`TypermediaClient` in `Compze.Tessaging`), declaring the current test's transport-client strategy (`CurrentTestsEndpointTransportClient()`) and serializers into it, and connects to an endpoint's one `Address`.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- `ExactlyOnceTessagingTestingEndpointHostFeature` has every endpoint `ParticipateIn` the host's real interprocess endpoint registry (`ITestingEndpointHost.EndpointRegistry`) instead of discovering through a test-only registry listing the host's addresses — every test now runs the production announce/discover pipeline.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
