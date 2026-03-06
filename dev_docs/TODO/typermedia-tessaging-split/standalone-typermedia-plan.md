# Plan: Build Standalone Typermedia, Replace, Remove

## Strategy

Build a new, purpose-built Typermedia infrastructure alongside the existing code. Migrate usages to the new infrastructure. Then delete all typermedia paths from the tessaging codebase.

No code sharing with the existing tessaging implementation. Fresh, independent code throughout.

## Why this approach

Typermedia is async request-response. What the current shared infrastructure provides for it — `HandlerExecutionEngine` dispatch thread, `Coordinator` with readers-writer dispatching rules, `Inbox` with exactly-once storage, `TransportTessage` with 5-way type discrimination — is unnecessary machinery. A fresh implementation is small and simple. Gradual disentanglement would mean months of splitting shared classes while keeping both paths working through shared machinery.

## What Typermedia actually needs

| Concern | Current (shared) | New (standalone) |
|---|---|---|
| Handler registry | `ITypermediaHandlerRegistry` / `TypermediaHandlerRegistry` (already split — stores tueries + commands with results) | Keep existing split registry. No tevent. No void commands — all commands return a result. |
| Handler dispatch | `HandlerExecutionEngine` + `Coordinator` + dispatching rules + dedicated thread | Async invocation on the request thread. No queuing, no dispatch thread, no concurrency rules. |
| Transport | `ITransportMessagePoster` → `TransportTessage` envelope → `MemoryInboxTransportServer` → `Inbox` → engine | Simple: serialize → HTTP POST (or direct call for in-memory) → deserialize → call handler → return result |
| Server receiving | `Inbox.ExecuteAsync` → storage → engine queue → coordinator → dispatch | Handler invoked directly when request arrives |
| Concurrency | Readers-writer lock across both paradigms | None needed — caller awaits, concurrency is the caller's responsibility |
| Message storage | `Inbox.ITessageStorage` saves every message | None — no delivery tracking needed |
| Discovery | `NetworkTopologyTuery` → seed returns all addresses → `EndpointInformationTuery` per address → classify types. All flowing through the full handler pipeline. | Client provides addresses. Each endpoint exposes a transport-level info RPC (not a tuery). No topology query. |

## Design decisions

- **Two registration methods**: `ForTuery<TTuery, TResult>` and `ForTommand<TTommand, TResult>`. No collision with tessaging's `ForTommand` since the registries are separate.
- **All commands return a result**: No void command handlers. Return `Unit` if there's no meaningful result. Eliminates the void/result split that adds complexity for no value.
- **No AtMostOnce naming**: The `AtMostOnce` distinction is a tessaging concern (vs exactly-once). Typermedia commands are just commands. The existing `IAtMostOnce*` type names get removed from the new infrastructure.
- **Async throughout**: Handler invocation is `async Task<TResult>`, not synchronous.
- **No code sharing**: The existing `TypermediaRouter`, `TypermediaConnection`, `ApiEndpointClient`, `RemoteTypermediaNavigator` stay in the tessaging codebase until Phase 3 deletes them. The new code is fully independent.
- **No topology discovery**: The client knows which endpoint addresses it wants to talk to. Where it gets the addresses is its own business (config, environment, service discovery sidecar, hardcoded in tests). The framework doesn't model inter-endpoint awareness — that's a tessaging concern (for event distribution). Each endpoint answers "what types do I handle?" when asked, via a transport-level RPC. No `NetworkTopologyTuery`, no seed-based discovery.

