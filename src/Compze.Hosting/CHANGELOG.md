# Changelog

All notable changes to Compze.Hosting will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The host never looks inside an endpoint anymore: `EndpointHost` receives composed endpoints (`RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, ...))` — the concrete endpoint types live in `Compze.Tessaging`) and drives their lifecycle phases host-wide. The generic `Endpoint` that drove `IEndpointComponent` lists and the `ServerEndpointBuilder` behind `IEndpointBuilder` are deleted with the endpoint feature machinery.
- `InterprocessEndpointRegistry` implements `IEndpointRegistry.AwaitPossibleMembershipChange` on its backing interprocess object's cross-process change signal: every announcement and retraction — from any process sharing the registry — wakes every waiting router on the machine, so topology changes propagate at signal latency (tens of milliseconds at most) instead of at the routers' periodic reconciliation interval. A crashed process raises no signal — its addresses just stop being listed — which is one reason the periodic pass remains.
- The host drives the new announcing/retracting lifecycle phases: every endpoint's listening completes before any endpoint announces its address, and every announcement before any endpoint's sending components start; on stop, addresses are retracted before any sending stops. A router's first reconciliation against an `IEndpointRegistry` therefore sees every endpoint its host announced.
- `InterprocessEndpointRegistry` (`Compze.Hosting.SameMachine`): a same-machine `IEndpointRegistry` and `IEndpointAddressAnnouncer` backed by an `IInterprocessObject` — endpoints announce the address they listen on, every process opening the same registry name and directory sees them, with no configuration and no server. Each entry records its announcing process's `ProcessIdentity` (process id + start time, because the OS recycles ids — with cross-OS-tolerant start-time comparison, since Unix reconstructs another process's start time from a per-reader boot-time estimate), so a crashed process's stale addresses are never routed to and are pruned on the next announcement.

## 0.1.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.1.0-alpha

- Initial pre-release
