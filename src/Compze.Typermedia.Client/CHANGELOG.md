# Changelog

All notable changes to Compze.Typermedia.Client will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Typermedia no longer runs a transport server of its own: `NamedPipeTypermediaTransportServer()` now contributes Typermedia's request handling to the endpoint's one named-pipe transport server, and `DistributedTypermediaEndpointFeature` composes the shared `EndpointTransportServerFeature` — an endpoint speaking both styles serves both on one address. The feature gains `AnnounceAddressTo(...)`, delegating to the shared feature.
- Named-pipe Typermedia transport: `NamedPipeTypermediaTransport()` (client) and `NamedPipeTypermediaTransportServer()` (server) implement the Typermedia transport over the same-machine named-pipe transport — no ASP.NET Core anywhere in the process.
- Typermedia's transport is protocol-free: one `TypermediaTransport` client and one `TypermediaRequestHandlers` server contribution (`TypermediaTransportServer()`), both over the endpoint transport — the per-protocol variants and the `Compze.Typermedia.Hosting.AspNetCore` controller assembly are gone. The protocol-named registrars now only pick the protocol plumbing around them.

## 0.1.0-alpha

- Initial pre-release
