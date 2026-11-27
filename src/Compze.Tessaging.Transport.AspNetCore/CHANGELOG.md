# Changelog

All notable changes to Compze.Tessaging.Transport.AspNetCore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.1-alpha

- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.5.0-alpha (was 0.4.0-alpha), Compze.DependencyInjection 0.11.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.
- **Requires exact versions of the Compze packages whose internals it uses:** Compze.Tessaging 0.15.0-alpha (was 0.9.0-alpha or newer). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs apply to this upgrade as well.

## 0.4.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.DependencyInjection.Extensions.Hosting 0.5.0-alpha (was 0.4.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.4.1-alpha), Compze.Tessaging 0.9.0-alpha (was 0.5.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **The package is `Compze.Tessaging.Transport.AspNetCore`**: the ASP.NET Core transport for Tessaging, kept as its own package so the web stack never rides into consumers that do not want it. Previously published as [Compze.Internals.Transport.AspNet](https://www.nuget.org/packages/Compze.Internals.Transport.AspNet/) 0.2.0-alpha.
- `TransportRequestController` serves the best-effort-tevent route (`internal/tessaging/best-effort-tevent`, `TransportRequestKind.BestEffortTevent`) alongside the existing kinds.
- The protocol declaration composes: `AspNetCoreEndpointTransport()` on an endpoint's `EndpointBuilder` fills the endpoint's transport-protocol parameter.
- `AspNetCoreEndpointTransportClient()`: the client-side transport declaration the package was missing. A pure `TypermediaClient` speaking ASP.NET Core now composes without borrowing the server's declaration.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `TransportRequestController` serves the transient-tevent route (`internal/tessaging/transient-tevent`, `TransportRequestKind.TransientTevent`) alongside the existing kinds.
- The protocol declaration composes: `AspNetCoreEndpointTransport()` on `ComposeEndpoint`'s composer returns the endpoint's `EndpointFoundation`.
- `AspNetCoreEndpointTransportServer`: the one Kestrel server per endpoint — consolidating the two near-identical per-style servers that previously lived in `Compze.Tessaging.Hosting.AspNetCore` and `Compze.Typermedia.Hosting.AspNetCore`.
- One dispatch surface instead of per-feature controllers: `TransportRequestController` serves every communication style's routes by rebuilding each `TransportRequest` from route, headers and body and dispatching through the endpoint's `TransportRequestHandlerMap` — the very same contributed request handlers the named-pipe server serves. The per-feature MVC controllers (`TessagingController`, `TypermediaController`) and the `AspNetCoreControllerContribution` application-part mechanism are gone, and with them the `Compze.Tessaging.Hosting.AspNetCore` and `Compze.Typermedia.Hosting.AspNetCore` assemblies.

## 0.1.0-alpha

- Initial pre-release
