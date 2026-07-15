# Changelog

All notable changes to Compze.Internals.Transport will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- A request kind no contribution handles fails loud naming the gap: the endpoint's transport server refuses it with an error saying the endpoint's composition does not wire the capability that serves the kind, and listing the kinds it does serve — a peer's request must fail on the sender, never be silently dropped. (Reachable e.g. when an exactly-once tessage arrives at an endpoint composing only guarantee-free transient Tessaging.)
- `TransportRequestKind.TransientTevent`: the wire kind for a transient tevent — a remotable tevent whose type declares no exactly-once guarantee — dispatched directly to the receiving endpoint's handlers with no inbox (see `src/Compze.Tessaging/_docs/tevent-delivery-model.md`). The acknowledgement is written after the handlers have executed. Named pipes carry the new kind as-is; the HTTP client and the ASP.NET Core controller gained its route (`internal/tessaging/transient-tevent`).
- The protocol declarations compose: `NamedPipeEndpointTransport()` on `ComposeEndpoint`'s composer returns the endpoint's `EndpointFoundation`.
- One transport server per endpoint: `IEndpointTransportServer` and `EndpointTransportServerFeature` — every distributed communication style contributes its request handling to the endpoint's single server instead of running one of its own, giving each endpoint a single address (what lets an endpoint registry map an `EndpointId` to one address). The server answers endpoint-discovery queries itself, and its component owns announce/retract of the endpoint's address. The named-pipe implementation (`NamedPipeEndpointTransportServer`) serves the endpoint's `TransportRequestHandlerMap` through one `NamedPipeTransportServer`.
- The named-pipe transport substrate (`Compze.Internals.Transport.NamedPipes`): framed request/response conversations over `System.IO.Pipes` — base runtime only, no web stack. `NamedPipeTransportServer` serves connections through a fixed pool of listener loops (bounded concurrency, backpressure through pending connects); `NamedPipeTransportClient` sends one request per connection; handler exceptions travel back as error frames rethrown client-side as `MessageDispatchingFailedException`. Includes the named-pipe `IEndpointTransportClient` implementation.
- The request envelope and request-handler contribution are protocol-neutral: `TransportRequest`, `TransportRequestKind`, `ITransportRequestHandlerContribution` — carried by named pipes as framed messages and by HTTP as route + headers + body.
- One client for every conversation: `IEndpointTransportClient` sends a `TransportRequest` to a remote endpoint's transport server, with one named-pipe and one HTTP implementation (the HTTP one owns the kind-to-route table). The per-protocol endpoint-discovery query transports collapse onto it — `EndpointDiscoveryQueryTransport` is one implementation for every protocol.
- `TransportRequestHandlerMap`: everything the endpoint's transport server serves — every communication style's contributed request handlers plus endpoint discovery — dispatched identically by the named-pipe and ASP.NET Core servers.
- Protocol declarations: `NamedPipeEndpointTransport()` (here) and `AspNetCoreEndpointTransport()` (in the AspNet package) declare an endpoint's transport protocol in one call — endpoint transport client, endpoint-discovery query transport, and the endpoint's one transport server. The communication styles register nothing protocol-specific.

## 0.1.0-alpha

- Initial pre-release
