# Changelog

All notable changes to Compze.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **Hosts build endpoint-declarations in an environment.** `EndpointHost.Production.Create(containerFactory, environment)` takes the deployment's `IEndpointEnvironment`; `RegisterEndpoint(declaration)` builds each registered declaration in it, and `RegisterEndpoint(declaration, environment)` overrides it per registration — for an endpoint whose environment differs from its co-hosted neighbors', usually a decorating environment wrapping the host's (`Environment` is public for exactly that). The callback registration (`RegisterEndpoint(container => ...)`) leaves the public surface.
- **The Host demotion: endpoints are first-class; `EndpointHost` is a convenience.** Starting the host starts every registered endpoint — each driving its own phase ordering (`IEndpoint.StartAsync`: listen → announce → send) — and disposing it disposes them, each endpoint's disposal driving its own mirror phases. The host-wide phase barrier (every endpoint's phase N before any endpoint's phase N+1) is gone: its headline guarantee — a router's first look at the registry sees every endpoint the host announced — held only inside one process's `StartAsync` and silently degraded to reconciliation-based convergence the moment a second host existed; per-endpoint ordering keeps what is honest in every process topology (an announced address is always one that is actually listening), and startup completeness is carried by the mechanisms that already carry it cross-process — readiness, waiting sends, queue-while-down, `RequirePeers`.
- The host never looks inside an endpoint anymore: `EndpointHost` builds registered endpoint-declarations (the concrete endpoint types live in `Compze.Tessaging`), starts them, and disposes them. The generic `Endpoint` that drove `IEndpointComponent` lists and the `ServerEndpointBuilder` behind `IEndpointBuilder` are deleted with the endpoint feature machinery.
- The announcing/retracting lifecycle phases: an endpoint announces its address after its listening phase and before its sending phase, and retracts it before its sending stops — so an announced address is always one that is actually listening, in every process topology.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.2.0-alpha

- `InterprocessEndpointRegistry` implements `IEndpointRegistry.AwaitPossibleMembershipChange` on its backing interprocess object's cross-process change signal: every announcement and retraction — from any process sharing the registry — wakes every waiting router on the machine, so topology changes propagate at signal latency (tens of milliseconds at most) instead of at the routers' periodic reconciliation interval. A crashed process raises no signal — its addresses just stop being listed — which is one reason the periodic pass remains.
- The host drives the new announcing/retracting lifecycle phases: every endpoint's listening completes before any endpoint announces its address, and every announcement before any endpoint's sending components start; on stop, addresses are retracted before any sending stops. A router's first reconciliation against an `IEndpointRegistry` therefore sees every endpoint its host announced.
- `InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`): a same-machine `IEndpointRegistry` and `IEndpointAddressAnnouncer` backed by an `IInterprocessObject` — endpoints announce the address they listen on, every process opening the same registry name and directory sees them, with no configuration and no server. Each entry records its announcing process's `ProcessIdentity` (process id + start time, because the OS recycles ids — with cross-OS-tolerant start-time comparison, since Unix reconstructs another process's start time from a per-reader boot-time estimate), so a crashed process's stale addresses are never routed to and are pruned on the next announcement.

## 0.1.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.1.0-alpha

- Initial pre-release
