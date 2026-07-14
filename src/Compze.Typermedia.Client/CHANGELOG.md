# Changelog

All notable changes to Compze.Typermedia.Client will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- `AddDistributedTypermedia(compose)` on a composed endpoint's `EndpointFoundation`, with `DistributedTypermediaComposition` as the compose surface (its serializer slot). Adding the feature asserts that the transport protocol and the Typermedia serializer are declared, each failure naming the missing declaration.
- Typermedia no longer runs a transport server of its own: it contributes its request handling to the endpoint's one transport server, and `DistributedTypermediaEndpointFeature` composes the shared `EndpointTransportServerFeature` — an endpoint speaking both styles serves both on one address. The feature gains `AnnounceAddressTo(...)`, delegating to the shared feature.
- Typermedia's transport is protocol-free: one `TypermediaTransport` client (registered by `TypermediaTransport()`) and one `TypermediaRequestHandlers` server contribution, both over the endpoint transport — the per-protocol variants and the `Compze.Typermedia.Hosting.AspNetCore` controller assembly are gone. `DistributedTypermediaEndpointFeature` registers its request handling itself; the composing layer declares only the endpoint's transport protocol (`NamedPipeEndpointTransport()` / `AspNetCoreEndpointTransport()`), so Typermedia works cross-process with no ASP.NET Core anywhere in the process when the endpoint speaks named pipes.

## 0.1.0-alpha

- Initial pre-release
