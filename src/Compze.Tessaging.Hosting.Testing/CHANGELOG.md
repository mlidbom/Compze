# Changelog

All notable changes to Compze.Tessaging.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- The host's at-rest wait covers tevent observation: the engines' observation dispatch (off-thread since the observation redesign) reports its queued work to the host's tessages-in-flight tracker, so disposal completes only after every queued observation has dispatched — a test cannot pass with observation work in flight — and a throwing observer's failure, reported to the background-exception reporter, is rethrown at disposal like any other background failure. Pinned in `Tevent_observation_at_host_disposal_tests`.
- **The testing host is per-tier, concrete wiring**: `TestingEndpointHost` (moved here from `Compze.Hosting.Testing` — it registers the concrete endpoint types, which requires knowing the tiers) offers `RegisterExactlyOnceEndpoint` / `RegisterBestEffortEndpoint`, handing each endpoint its test concerns at construction: the host's one tessages-in-flight tracker, the current test's transport protocol, the pooled test database keyed by the endpoint's id (exactly-once tier), and participation in the host's real interprocess endpoint registry — every test runs the production announce/discover pipeline. On dispose the host waits until no tessages are in flight and rethrows background exceptions no assertion observed. The `ITestingEndpointHostFeature` seam and both features (`ExactlyOnceTessagingTestingEndpointHostFeature`, `DistributedTypermediaTestingEndpointHostFeature`) died with the endpoint feature machinery.
- `TypermediaTestClient` rides the pure client (`TypermediaClient` in `Compze.Tessaging`), declaring the current test's transport-client strategy (`CurrentTestsEndpointTransportClient()`) and serializers into it, and connects to an endpoint's one `Address`.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
