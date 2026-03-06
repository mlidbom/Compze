# Plan: Build Standalone Typermedia, Replace, Remove

## Strategy

Build a new, purpose-built Typermedia infrastructure alongside the existing code. Migrate usages to the new infrastructure. Then delete all typermedia paths from the tessaging codebase.

No code sharing with the existing tessaging implementation. Fresh, independent code throughout.

## Why this approach

Typermedia is async request-response. What the current shared infrastructure provides for it — `HandlerExecutionEngine` dispatch thread, `Coordinator` with readers-writer dispatching rules, `Inbox` with exactly-once storage, `TransportTessage` with 5-way type discrimination — is unnecessary machinery. A fresh implementation is small and simple. Gradual disentanglement would mean months of splitting shared classes while keeping both paths working through shared machinery.

## What Typermedia actually needs

| Concern | Current (shared) | New (standalone) |
|---|---|---|
| Handler registry | `TessageHandlerRegistry` (stores all 4 handler types, mixed) | New registry: `ForTuery` + `ForTommand` only. No tevent. No void commands — all commands return a result. |
| Handler dispatch | `HandlerExecutionEngine` + `Coordinator` + dispatching rules + dedicated thread | Async invocation on the request thread. No queuing, no dispatch thread, no concurrency rules. |
| Transport | `ITransportMessagePoster` → `TransportTessage` envelope → `MemoryInboxTransportServer` → `Inbox` → engine | Simple: serialize → HTTP POST (or direct call for in-memory) → deserialize → call handler → return result |
| Server receiving | `Inbox.ExecuteAsync` → storage → engine queue → coordinator → dispatch | Handler invoked directly when request arrives |
| Concurrency | Readers-writer lock across both paradigms | None needed — caller awaits, concurrency is the caller's responsibility |
| Message storage | `Inbox.ITessageStorage` saves every message | None — no delivery tracking needed |
| Discovery | `EndpointInformationTuery` / `NetworkTopologyTuery` through the shared inbox | Same mechanism but served by the new infrastructure |

## Design decisions

- **Two registration methods**: `ForTuery<TTuery, TResult>` and `ForTommand<TTommand, TResult>`. No collision with tessaging's `ForTommand` since the registries are separate.
- **All commands return a result**: No void command handlers. Return `Unit` if there's no meaningful result. Eliminates the void/result split that adds complexity for no value.
- **No AtMostOnce naming**: The `AtMostOnce` distinction is a tessaging concern (vs exactly-once). Typermedia commands are just commands. The existing `IAtMostOnce*` type names get removed from the new infrastructure.
- **Async throughout**: Handler invocation is `async Task<TResult>`, not synchronous.
- **No code sharing**: The existing `TypermediaRouter`, `TypermediaConnection`, `ApiEndpointClient`, `RemoteTypermediaNavigator` stay in the tessaging codebase until Phase 3 deletes them. The new code is fully independent.

## Project structure

New solution with separate projects reflecting distinct concerns:

| Project | Responsibility |
|---|---|
| `Compze.Typermedia` | Core: handler registry, handler dispatch, request handling, message type interfaces |
| `Compze.Typermedia.Client` | Navigator, router, connection, transport client |
| `Compze.Typermedia.Hosting` | Endpoint builder, endpoint host, transport server, in-memory transport |
| `Compze.Typermedia.Hosting.Testing` | Test client, test endpoint host |
| `Compze.Typermedia.Hosting.AspNetCore` | HTTP transport server (ASP.NET Core) |

Plus corresponding `.Specifications` projects. Own solution file. Details will be refined as implementation progresses.

## Phase 1: Build new Typermedia infrastructure

### 1a. Typermedia handler registry
- `TypermediaHandlerRegistry` with two dictionaries:
  - `_tueryHandlers: Dictionary<Type, Func<object, Task<object>>>` — tueries
  - `_tommandHandlers: Dictionary<Type, Func<object, Task<object>>>` — commands (all return a result)
- Registration methods: `ForTuery<TTuery, TResult>`, `ForTommand<TTommand, TResult>`
- `HandledTypeIds()` — for endpoint capability advertisement
- No tevent support. No exactly-once semantics.

### 1b. Typermedia request handler (server side)
- Replaces the inbox + execution engine + coordinator for typermedia messages
- On request: deserialize → look up handler in registry → `await` handler on request thread → serialize result → return
- Commands: wrap in `TransactionScope` + DI scope
- Queries: run in isolated DI scope, no transaction (same as current `ExecuteTuery`)
- No queuing, no dispatch thread, no dispatching rules

### 1c. Typermedia transport
- **In-memory**: direct async call to the request handler, same process
- **HTTP**: ASP.NET Core controller(s) that receive HTTP POST, delegate to the request handler
- Serialization: reuse existing `IRemotableTessageSerializer` and `ITypeMapper` (shared utility — not tessaging-specific code sharing)

### 1d. Typermedia endpoint hosting
- `TypermediaEndpoint` — owns a handler registry + request handler + transport server
- Registers handlers during setup (`ForTuery`/`ForTommand` fluent API)
- Discovery: handle `EndpointInformationTuery` and `NetworkTopologyTuery` natively so clients can discover capabilities

### 1e. Client side
- New `TypermediaRouter` — client-side routing, maintains route tables per message type
- New `TypermediaConnection` — represents a connection to one remote endpoint
- New `RemoteTypermediaNavigator` — validates, delegates to router
- New `TestClient` — `ConnectTo(address)` factory, returns navigator

### 1f. Tests
- New specifications projects
- Port typermedia-relevant tests from the integration suite
- Performance tests for the new path

## Phase 2: Migrate usages

### 2a. Endpoint registration
- Typermedia handlers (`ForTuery`, `ForTommand`) register with the new `TypermediaEndpoint`
- Tessaging handlers (`ForTevent`, `ForTommand` for exactly-once) stay with the existing tessaging endpoint
- An endpoint host may contain both a typermedia endpoint and a tessaging endpoint — they share a network address and DI container but have separate handler registries and request handling paths

### 2b. Navigate to new infrastructure
- `IRemoteTypermediaNavigator` implementation now goes through the new typermedia transport path
- `IInProcessTypermediaNavigator` implementation now goes directly to the new typermedia handler registry
- Consumer code changes minimally — same interfaces, same behavior

### 2c. Sample apps
- AccountManagement already uses `IRemoteTypermediaNavigator` directly (after `IClient` removal)
- Update endpoint setup to use new typermedia endpoint builder
- Handler code stays the same

## Phase 3: Remove typermedia from tessaging

### 3a. TransportTessageType
- Remove `TypermediaAtMostOnceTommand`, `TypermediaAtMostOnceTommandWithReturnValue`, `TyperMediaTuery`
- Only `ExactlyOnceTevent` and `ExactlyOnceTommand` remain

### 3b. HandlerExecutionTask
- Remove the `TypermediaAtMostOnceTommand*` and `TyperMediaTuery` branches
- Only tevent fan-out and exactly-once command dispatch remain

### 3c. Dispatching rules
- Remove `TueriesExecuteAfterAllTommandsAndTeventsAreDone` entirely — no more queries in the tessaging engine
- `TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint` stays

### 3d. Inbox
- Remove `ExecuteAsync` (the request-response path) — only `ReceiveAsync` (fire-and-forget enqueue) remains
- `Inbox.ITessageStorage` no longer stores typermedia messages

### 3e. Transport servers
- `MemoryInboxTransportServer` — remove typermedia branches from `PostAsync`
- `TypermediaController` — delete entirely (served by new infrastructure)
- `AspNetInboxTransportServer` — only hosts `TessagingController`

### 3f. Handler registry
- Remove `ForTuery`, `ForTommandWithResult` from `ITessageHandlerRegistrar` / `TessageHandlerRegistry`
- Remove `_tueryHandlers` and `_tommandHandlersReturningResults` dictionaries
- Only `_tommandHandlers` (void, exactly-once) and `_teventHandlers` remain
- Rename to `TessagingHandlerRegistry`

### 3g. Transport
- `ITransportMessagePoster` becomes tessaging-only
- `TransportTessage` envelope simplifies — only two message kinds
- `HttpTransportMessagePoster` removes typermedia URL paths

### 3h. Endpoint hosting
- `ServerEndpointBuilder` no longer registers typermedia components
- `IEndpointBuilder.RegisterHandlers` only exposes tessaging registration methods

### 3i. Cleanup
- Delete `ITypermediaRouter`, `TypermediaRouter`, `TypermediaConnection`, `ApiEndpointClient`, `RemoteTypermediaNavigator` from the tessaging codebase
- Delete typermedia-related extension methods from `RemoteHypermediaNavigatorRegistrar`
- Delete `IAtMostOnceTypermediaTommand`, `IAtMostOnceTommand<TResult>` and related types from `Compze.Abstractions` (replaced by new typermedia message types)

## Open questions

1. **Mixed endpoints**: After the split, a single address may host both a typermedia endpoint and a tessaging endpoint. Need to decide: same `IEndpointBuilder` that exposes two registrars, or two separate builders?

2. **In-process navigator + tessaging**: `IInProcessTypermediaNavigator` is currently resolved within tessaging handler scopes (e.g., a tevent handler that reads data via a local tuery). After the split, tessaging handlers need access to the typermedia handler registry. This is the one legitimate cross-paradigm dependency — tessaging handlers consuming typermedia read APIs. Solution: `IInProcessTypermediaNavigator` is registered in the shared DI container, backed by the new typermedia registry.

3. **Discovery tueries**: `EndpointInformationTuery` and `NetworkTopologyTuery` are currently internal typermedia tueries handled by the shared inbox. After the split, these are served by the new typermedia infrastructure. But tessaging's `TessagingRouter.ConnectAsync` also uses `EndpointInformationTuery` for route building. Solution: both routers connect to the typermedia endpoint for discovery — tessaging clients need a lightweight typermedia client just for bootstrap.
