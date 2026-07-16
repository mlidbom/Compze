# Changelog

All notable changes to Compze.Internals.Transport.AspNet will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `TransportRequestController` serves the best-effort-tevent route (`internal/tessaging/best-effort-tevent`, `TransportRequestKind.BestEffortTevent`) alongside the existing kinds.
- The protocol declaration composes: `AspNetCoreEndpointTransport()` on `ComposeEndpoint`'s composer returns the endpoint's `EndpointFoundation`.
- `AspNetCoreEndpointTransportServer`: the one Kestrel server per endpoint — consolidating the two near-identical per-style servers that previously lived in `Compze.Tessaging.Hosting.AspNetCore` and `Compze.Typermedia.Hosting.AspNetCore`.
- One dispatch surface instead of per-feature controllers: `TransportRequestController` serves every communication style's routes by rebuilding each `TransportRequest` from route, headers and body and dispatching through the endpoint's `TransportRequestHandlerMap` — the very same contributed request handlers the named-pipe server serves. The per-feature MVC controllers (`TessagingController`, `TypermediaController`) and the `AspNetCoreControllerContribution` application-part mechanism are gone, and with them the `Compze.Tessaging.Hosting.AspNetCore` and `Compze.Typermedia.Hosting.AspNetCore` assemblies.

## 0.1.0-alpha

- Initial pre-release
