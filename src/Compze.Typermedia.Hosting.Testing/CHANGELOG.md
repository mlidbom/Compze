# Changelog

All notable changes to Compze.Typermedia.Hosting.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `DistributedTypermediaTestingEndpointHostFeature` has every endpoint `ParticipateIn` the host's real interprocess endpoint registry (`ITestingEndpointHost.EndpointRegistry`), so every endpoint's typermedia router connects to every endpoint in the host through the production announce/discover pipeline — endpoints in a testing host navigate each other's typermedia exactly as they would across processes.
- `CurrentTestsTypermediaTransport()` is removed: the distributed Typermedia feature registers its own client side now, so an endpoint needs only the endpoint transport of the current test's protocol (`CurrentTestsEndpointTransport()`). `CurrentTestsTypermediaClientTransport()` remains for test-client containers that connect to endpoints without hosting one.

## 0.1.0-alpha

- Initial pre-release
