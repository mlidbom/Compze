# Changelog

All notable changes to Compze.Typermedia.Client will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Typermedia dynamic-topology parity with Tessaging: an endpoint declares the registry it discovers other endpoints through on its distributed-Typermedia feature (`DiscoverEndpointsThrough`/`ParticipateIn`), and its `TypermediaRouter` reconciles its connections against the registry's live membership exactly as the Tessaging router does — an endpoint that appears is connected (its identity and handled typermedia types learned through the typermedia discovery query) and its tommand/tuery routes registered, one whose address disappears is dropped and its routes with it, and one that returns at a new address — addresses are per-instance, identity is the `EndpointId` — has its connection replaced. The feature now registers the endpoint's whole client side (the `TypermediaTransport` client, the router, and a scoped `IRemoteTypermediaNavigator`), so an endpoint navigates other endpoints' typermedia through its own container — proven across real OS processes with no database in either process. Declaring no registry means the endpoint only serves: navigating from it fails loud naming the missing declaration, while an external client keeps the explicit-address path (`ITypermediaRouter.ConnectAsync`).
- The router's routes are re-derived from its live connections whenever membership changes — previously routes could only ever be added, so a departed endpoint left dead routes targeting its old address.
- `AddDistributedTypermedia(compose)` on a composed endpoint's `EndpointFoundation`, with `DistributedTypermediaComposition` as the compose surface (its serializer slot). Adding the feature asserts that the transport protocol and the Typermedia serializer are declared, each failure naming the missing declaration.
- Typermedia no longer runs a transport server of its own: it contributes its request handling to the endpoint's one transport server, and `DistributedTypermediaEndpointFeature` composes the shared `EndpointTransportServerFeature` — an endpoint speaking both styles serves both on one address. The feature gains `AnnounceAddressTo(...)`, delegating to the shared feature.
- Typermedia's transport is protocol-free: one `TypermediaTransport` client (registered by `TypermediaTransport()`) and one `TypermediaRequestHandlers` server contribution, both over the endpoint transport — the per-protocol variants and the `Compze.Typermedia.Hosting.AspNetCore` controller assembly are gone. `DistributedTypermediaEndpointFeature` registers its request handling itself; the composing layer declares only the endpoint's transport protocol (`NamedPipeEndpointTransport()` / `AspNetCoreEndpointTransport()`), so Typermedia works cross-process with no ASP.NET Core anywhere in the process when the endpoint speaks named pipes.

## 0.1.0-alpha

- Initial pre-release
