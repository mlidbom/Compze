# Changelog

All notable changes to Compze.Tessaging.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- `AwaitEndpointsHaveMetEachOtherAsync()`: awaits every endpoint of the host remembering every other as a peer — mutual first contact. Starting the host completes when every endpoint has started; whether they have *discovered each other* is topology convergence, completing at signal latency after the start. A specification whose very next act rides that discovery — an exactly-once tevent whose fan-out membership is the remembered subscribers (first contact is the boundary), an assertion over peer memory, an ambiguity pin needing every advertising handler visible — awaits this after starting instead of racing the reconciliation; a production composition awaits what it needs through the production surfaces instead (readiness, `RequirePeers`, waiting sends).
- `RegisterExactlyOnceEndpointInDomainDatabase(name, id, domainDatabaseName, declare)`: registers an exactly-once endpoint joined to the named shared domain database instead of a database of its own — the composition for several endpoints storing side by side, each with its prefixed table-set, sharing the endpoint catalog and the type-id interner. `RegisterExactlyOnceEndpoint` (the id-keyed default) now delegates to it.
- The host's at-rest wait covers tevent observation: the engines' observation dispatch (off-thread since the observation redesign) reports its queued work to the host's tessages-in-flight tracker, so disposal completes only after every queued observation has dispatched — a test cannot pass with observation work in flight — and a throwing observer's failure, reported to the background-exception reporter, is rethrown at disposal like any other background failure. Pinned in `Tevent_observation_at_host_disposal_tests`.
- **The testing host is per-tier, concrete wiring**: `TestingEndpointHost` (moved here from `Compze.Hosting.Testing` — it registers the concrete endpoint types, which requires knowing the tiers) offers `RegisterExactlyOnceEndpoint` / `RegisterBestEffortEndpoint`, handing each endpoint its test concerns at construction: the host's one tessages-in-flight tracker, the current test's transport protocol, the pooled test database keyed by the endpoint's id (exactly-once tier), and participation in the host's real interprocess endpoint registry — every test runs the production announce/discover pipeline. On dispose the host waits until no tessages are in flight and rethrows background exceptions no assertion observed. The `ITestingEndpointHostFeature` seam and both features (`ExactlyOnceTessagingTestingEndpointHostFeature`, `DistributedTypermediaTestingEndpointHostFeature`) died with the endpoint feature machinery.
- `TypermediaTestClient` rides the pure client (`TypermediaClient` in `Compze.Tessaging`), declaring the current test's transport-client strategy (`CurrentTestsEndpointTransportClient()`) and serializers into it, and connects to an endpoint's one `Address`.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
