# Message Delivery Pipeline — Component Inventory

> **CRITICAL PRINCIPLE: Existing entanglement is NOT a sign that entanglement should remain.**
>
> Typermedia must stand completely on its own: its own transport, its own handler execution, its own hosting. Tessaging likewise keeps its own full pipeline. Typermedia messages leave the Tessaging pipeline entirely. Every `[BOTH]` tag below represents work to be done, not architecture to be preserved.

---

## Extraction Strategy — Inside-Out, Five Phases

Work from the handler execution outward. Each phase is independently shippable and leaves tests green.

### Phase 1+2 — Typermedia Handler Executor + Server Redirect ✅ DONE

`TypermediaHandlerExecutor` created in `Compze.Typermedia.Hosting`. All Typermedia messages now bypass Inbox and HandlerExecutionEngine entirely.

**Executor** (`src/Compze.Typermedia.Hosting/TypermediaHandlerExecutor.cs`):
- `ExecuteTuery(message)` — isolated scope, no transaction
- `ExecuteTommandWithResult(message)` — isolated scope + transaction + 5-attempt retry
- `ExecuteVoidTommand(message)` — isolated scope + transaction + 5-attempt retry
- Registered as Singleton via `TypermediaHandlerExecutor.RegisterWith()` in `ServerEndpointBuilder`
- All handler lookup goes through `ITypermediaHandlerRegistry` — no tessaging registry involvement

**Server redirects completed:**
- `MemoryInboxTransportServer`: Typermedia branches deserialize and call executor via `Task.Run` (preserving async dispatch for parallelism tests)
- `TypermediaController`: All three endpoints deserialize directly via `IRemotableTessageSerializer` (no `TransportTessage.InComing`), call executor via `RunOutsideScope` to escape the ASP.NET middleware's DI scope

**Registration fix:** Void Typermedia commands were historically registered in the tessaging handler registry. Moved to `RegisterTypermediaHandlers` where they belong (test fixtures + AccountManagement sample). `HandledRemoteTypermediaTypeIds()` updated to include `_voidTommandHandlers`.

**Residual entanglement:** `TypermediaController` still inherits `ControllerBase` which takes `IInbox` + `HandlerExecutionEngine` constructor args (unused by TypermediaController), and uses shared serialization (`IRemotableTessageSerializer`, `TransportTessage.InComing`). Needs investigation.

**TypermediaController separated from ControllerBase:** `TypermediaController` now inherits `Controller` directly — no longer depends on `ControllerBase`, `IInbox`, `HandlerExecutionEngine`, or `TransportTessage.InComing`. It deserializes HTTP requests itself using `IRemotableTessageSerializer` + `ITypeMapper` and serializes responses directly. `ControllerBase` is now Tessaging-only (removed `Serializer` and `HandlerExecutionEngine` properties). Key detail: the ASP.NET middleware creates a DI scope via `ExecuteInIsolatedScopeAsync`, and since the DI container tracks scopes via `AsyncLocal`, the executor's `ExecuteInIsolatedScope` would fail with nested scope error. Solution: `RunOutsideScope` uses `ExecutionContext.SuppressFlow()` + `Task.Run` to escape the middleware scope, matching what `MemoryInboxTransportServer` does.

### Phase 3 — Clean Up Tessaging Internals ✅ DONE

Removed dead Typermedia code from the Tessaging execution pipeline:

- Removed `IInbox.ExecuteAsync` and `Inbox.ExecuteAsync` — was only used by Typermedia
- Removed `HandlerExecutionEngine.ExecuteAsync` — was only used by Typermedia tueries
- Removed `HandlerExecutionTask.ExecuteTuery` — no tueries flow through the engine
- Removed 3 Typermedia branches from `HandlerExecutionTask.CreateTessageTask` switch: `TypermediaAtMostOnceTommandWithReturnValue`, `TypermediaAtMostOnceTommand`, `TyperMediaTuery`
- Removed Typermedia no-op branches from `Coordinator.Dispatching` and `DoneDispatching`
- Removed `ITypermediaHandlerRegistry` dependency from `HandlerExecutionEngine`, `Coordinator`, and `HandlerExecutionTask` — the Tessaging pipeline no longer knows about Typermedia registries

### Phase 4 — Typermedia Gets Its Own Transport ✅ DONE

Created `ITypermediaTransport` — Typermedia's own transport abstraction replacing `ApiEndpointClient` + `ITransportMessagePoster` for all Typermedia messages.

**New interface** (`src/Compze.Tessaging/Implementation/Transport/Client/Internal/ITypermediaTransport.cs`):
- `GetAsync<TResult>(tuery, address)` — tueries
- `PostAsync<TResult>(command, address)` — commands with results
- `PostAsync(command, address)` — void commands

**Memory transport** (`src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryTypermediaTransport.cs`):
- Looks up `TypermediaHandlerExecutor` from `InMemoryTypermediaNetwork` by address
- Calls executor directly — no `TransportTessage` wrapping
- Round-trip serializes responses (simulates network boundary for reference isolation)
- Runs via `Task.Run` to avoid DI scope issues
- Wraps exceptions in `TessageDispatchingFailedException` (matching previous behavior)

**HTTP transport** (`src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Http/HttpTypermediaTransport.cs`):
- Serializes using `IRemotableTessageSerializer` + `ITypeMapper`
- Posts directly to `/typermedia/tuery`, `/typermedia/tommand-with-result`, `/typermedia/tommand-no-result`
- Own error handling with `ProblemDetails` deserialization

**Memory network** (`src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/InMemoryTypermediaNetwork.cs`):
- Static `ConcurrentDictionary<EndPointAddress, TypermediaHandlerExecutor>`
- `MemoryInboxTransportServer.StartAsync/StopAsync` binds/unbinds executor alongside the tessaging server binding

**Router changes:**
- `TypermediaRouter` depends on `ITypermediaTransport` instead of `ITransportMessagePoster` + `IRemotableTessageSerializer`
- `DiscoverAndConnectAsync` sends `NetworkTopologyTuery` directly via `ITypermediaTransport`
- `ConnectAsync` sends `EndpointInformationTuery` via `ITypermediaTransport` (no `ApiEndpointClient.BootstrapConnectionToEndpoint`)
- `PostAsync`/`GetAsync` delegate to `_transport.PostAsync(cmd, connection.Address)` / `_transport.GetAsync(tuery, connection.Address)`

**Connection simplification:**
- `TypermediaConnection` reduced to a data class: just holds `EndPointAddress` + `EndpointInformation` (no longer `IDisposable`)
- `TessagingConnection.InitAsync()` bootstraps via `ITypermediaTransport.GetAsync(EndpointInformationTuery)` instead of `TransportTessage.OutGoing` + `ITransportMessagePoster`

**Dead code removed:**
- Deleted `ApiEndpointClient` and `IRemoteApiEndpointClient`
- Removed `ITransportMessagePoster.PostAsync<TResult>` — no tessaging messages return results
- Removed `MemoryInboxTransportServer.PostAsync<TResult>` — only had Typermedia branches
- Removed Typermedia branches from `MemoryInboxTransportServer.PostAsync` and `HttpTransportMessagePoster.GetRelativeUriForTessage`
- Removed `HttpTransportMessagePoster._serializer` and `MemoryInboxTransportServer._serializer`
- Removed `TransportTessageType.TypermediaAtMostOnceTommand`, `TypermediaAtMostOnceTommandWithReturnValue`, `TyperMediaTuery`
- Removed Typermedia branches from `TessageTypeTranslator`
- Removed `//todo: split out TyperMediaTransportTessage` — resolved; `TransportTessage` is now tessaging-only
- Removed dead Typermedia check from `TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint` dispatching rule

**Residual entanglement:**
- `MemoryInboxTransportServer` still resolves `TypermediaHandlerExecutor` to bind it in `InMemoryTypermediaNetwork` during startup — both paradigms share the same endpoint address. Full decoupling deferred to Phase 5.
- `TessagingRouter` and `TessagingConnection` depend on `ITypermediaTransport` for bootstrap — **this is wrong**. Discovery queries (`EndpointInformationTuery`, `NetworkTopologyTuery`) are mistyped as Typermedia tueries. They are plain address-targeted request/response, not type-routed. See Section 12 for details. Fixing this should precede or be part of Phase 5.

### Phase 4b — Retype Discovery as Infrastructure (prerequisite for Phase 5)

Discovery queries are currently mistyped as Typermedia tueries (`IRemotableTuery<T>`). This creates a false dependency: `TessagingRouter`/`TessagingConnection` depend on `ITypermediaTransport` just to send a request to a known address.

**The type hierarchy now supports this fix.** `IQuery<T>` exists as a plain address-targeted query type that is NOT type-routed (see Type Hierarchy section below). Discovery queries should implement `IQuery<T>` directly, not `IRemotableTuery<T>`.

Steps:
- Retype `EndpointInformationTuery` and `NetworkTopologyTuery` from `IRemotableTuery<T>` to `IQuery<T>`
- Create a simple infrastructure request/response transport (serialize, POST to known address, deserialize) — shared by both paradigms
- Move discovery handler registration out of `TypermediaHandlerRegistrarWithDependencyInjectionSupport` into its own infrastructure handler mechanism
- Remove `ITypermediaTransport` dependency from `TessagingRouter` and `TessagingConnection`
- Remove `ITypermediaTransport` registration from server endpoint containers (only needed there because of discovery mistyping)

### Phase 5 — Separate Hosting

Each paradigm gets its own hosting lifecycle.

- `ServerEndpointBuilder` stops registering Typermedia handler executor infrastructure (or Typermedia gets its own builder).
- `Endpoint` lifecycle decoupled — Typermedia server startup independent of Tessaging outbox/router startup.
- `AspNetInboxTransportServer` either splits into two servers or Typermedia gets its own `WebApplication` setup.
- `EndpointHost` orchestrates both but they don't block each other.
- `MemoryInboxTransportServer` stops binding `TypermediaHandlerExecutor` — that moves to a Typermedia-specific server component.

### Recommended next step

Phase 4b — retype discovery as infrastructure. This unblocks Phase 5 by eliminating the false `ITypermediaTransport` dependency in Tessaging.

---

Every component involved in delivering a message from caller to handler and back, with entanglement status.

**Legend**: [T] = Typermedia only, [S] = Tessaging only, [BOTH] = entangled (to be separated)

---

## 1. Caller-Side Entry Points

### Remote Typermedia Navigator [T]
- **Interface**: `IRemoteTypermediaNavigator` — `src/Compze.Typermedia/IRemoteTypermediaNavigator.cs`
- **Impl**: `RemoteTypermediaNavigator` — `src/Compze.Typermedia/RemoteTypermediaNavigator.cs`
- Validates message via `TessageValidator.AssertValidToSendRemote` (shared validation in `Compze.Abstractions`)
- Delegates to `ITypermediaRouting` — narrow routing interface in `Compze.Typermedia` (no lifecycle methods)
- Sync methods are blocking wrappers over async
- Fast path: `ICreateMyOwnResultTuery<T>` returns instantly without hitting transport

### In-Process Typermedia Navigator [T]
- **Interface**: `IInProcessTypermediaNavigator` — `src/Compze.Typermedia/IInProcessTypermediaNavigator.cs`
- **Impl**: `InProcessTypermediaNavigator` — `src/Compze.Typermedia/InProcessTypermediaNavigator.cs`
- Goes directly to `ITypermediaHandlerRegistry` (tueries, commands with results, void commands) — no router, no transport, no inbox, no serialization
- Must be called within an existing transaction scope (`SingleTransactionUsageGuard`)
- Registered as Scoped

### Service Bus Session [S]
- **Interface**: `IServiceBusSession` — `src/Compze.Abstractions/Tessaging/Public/IServiceBusSession.cs`
- **Impl**: `ServiceBusSession` — `src/Compze.Tessaging/Hosting/ServiceBusSession.cs`
- `Send(cmd)` → validates → `IOutbox.SendTransactionally(cmd)`
- `ScheduleSend(datetime, cmd)` → `TommandScheduler.Schedule(datetime, cmd)`

---

## 2. Client-Side Routing

### Typermedia Router [T]
- **Narrow interface**: `ITypermediaRouting` — `src/Compze.Typermedia/ITypermediaRouting.cs` — `PostAsync`, `GetAsync` only. Used by `RemoteTypermediaNavigator`.
- **Full interface**: `ITypermediaRouter : ITypermediaRouting` — `src/Compze.Tessaging/Implementation/Transport/Client/Internal/ITypermediaRouter.cs` — adds lifecycle: `Start`, `Stop`, `DiscoverAndConnectAsync`
- **Impl**: `TypermediaRouter` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TypermediaRouter.cs`
- DI registration: `Singleton.For<ITypermediaRouter, ITypermediaRouting>()` — both interfaces resolved from same instance
- Client-side only. Registered on the client, not per server endpoint.
- Depends on `ITypermediaTransport` (no `ITransportMessagePoster` dependency)
- Maintains route tables: `Dictionary<Type, TypermediaConnection>` for commands and queries
- `PostAsync(cmd)` → look up `TypermediaConnection` by message type → `_transport.PostAsync(cmd, connection.Address)`
- `GetAsync(tuery)` → look up `TypermediaConnection` by tuery type → `_transport.GetAsync(tuery, connection.Address)`
- Discovery: `DiscoverAndConnectAsync(seedAddress)` → `_transport.GetAsync(NetworkTopologyTuery, seedAddress)` → connects to each
- Route population: `RegisterRoutes` classifies `EndpointInformation.HandledTessageTypes` — keeps `IAtMostOnceTypermediaTommand` and `IRemotableTuery<object>`, silently skips exactly-once types

### Tessaging Router [S]
- **Interface**: `ITessagingRouter` — `src/Compze.Tessaging/Implementation/Transport/Client/Internal/ITessagingRouter.cs`
- **Impl**: `TessagingRouter` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TessagingRouter.cs`
- Server-side. Registered per endpoint.
- Additional dependencies: `ITessagesInFlightTracker`, `Outbox.ITessageStorage`, `ITaskRunner`, `IBackgroundExceptionReporter`
- `ConnectionToHandlerFor(cmd)` → returns single `ITessagingInboxConnection` (1:1 routing)
- `SubscriberConnectionsFor(tevent)` → returns multiple connections (fan-out, `IsInstanceOfType` matching)
- Route population: classifies types — keeps `IExactlyOnceTevent` and `IExactlyOnceTommand`, silently skips typermedia types

---

## 3. Client-Side Connections

### Typermedia Connection [T]
- `TypermediaConnection` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TypermediaConnection.cs`
- Data class: holds `EndPointAddress` + `EndpointInformation`
- No longer `IDisposable` — no resources to manage
- Created by `TypermediaRouter.ConnectAsync` after bootstrap tuery returns endpoint info

### Tessaging Connection [S]
- `TessagingConnection` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TessagingConnection.cs`
- Implements `ITessagingInboxConnection`
- Has a background send loop with `Queue<PendingDelivery>`
- `EnqueueForDelivery(tessage)` → serialize → track in-flight → enqueue → signal loop
- `TrySend(pending)` → `_transportMessagePoster.PostAsync(tessage, address)` → mark received on success → retry with backoff on failure
- `StartDelivery()` → loads undelivered from storage → starts loop thread

---

## 4. Serialization & Transport Message Wrapping

### TransportTessage [S]
- `TransportTessage` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/TransportTessage.cs`
- **`OutGoing`**: `Body` (serialized JSON), `Type` (TypeId), `TessageId`, `TessageTypeEnum`
- **`InComing`**: same fields + `_tessageType` (resolved .NET Type) + lazy `DeserializeTessageAndCacheForNextCall()`
- Now tessaging-only — Typermedia no longer uses `TransportTessage`

### TransportTessageType [S]
- `TransportTessageType` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/TransportTessageType.cs`
- Tessaging-only values:
  - `ExactlyOnceTevent`
  - `ExactlyOnceTommand`

---

## 5. Transport Layer

### Typermedia Transport [T]
- **Interface**: `ITypermediaTransport` — `src/Compze.Tessaging/Implementation/Transport/Client/Internal/ITypermediaTransport.cs`
- `GetAsync<TResult>(tuery, address)`, `PostAsync<TResult>(command, address)`, `PostAsync(command, address)`
- Registered in both client containers and server endpoint containers (server needs it for `TessagingConnection` bootstrap)

### Memory Typermedia Transport [T]
- `MemoryTypermediaTransport` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryTypermediaTransport.cs`
- Looks up `TypermediaHandlerExecutor` from `InMemoryTypermediaNetwork` by address
- Calls executor directly, round-trip serializes responses, wraps exceptions

### HTTP Typermedia Transport [T]
- `HttpTypermediaTransport` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Http/HttpTypermediaTransport.cs`
- Serializes via `IRemotableTessageSerializer` + `ITypeMapper`, posts to `/typermedia/*` routes
- Deserializes responses, handles `ProblemDetails` errors

### In-Memory Typermedia Network [T]
- `InMemoryTypermediaNetwork` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/InMemoryTypermediaNetwork.cs`
- Static `ConcurrentDictionary<EndPointAddress, TypermediaHandlerExecutor>`
- Bound/unbound by `MemoryInboxTransportServer.StartAsync/StopAsync`

### Tessaging Transport Message Poster [S]
- **Interface**: `ITransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/ITransportMessagePoster.cs`
- Tessaging-only. Single method: `PostAsync(tessage, address)` — no generic return variant.

### Memory Transport Poster [S]
- `MemoryTransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryTransportMessagePoster.cs`
- Converts `OutGoing` → `InComing`, calls `InMemoryTransportNetwork.GetServer(address).PostAsync(incoming)`

### HTTP Transport Poster [S]
- `HttpTransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Http/HttpTransportMessagePoster.cs`
- Routes by `TransportTessageType`: `ExactlyOnceTevent` → `/tessaging/tevent`, `ExactlyOnceTommand` → `/tessaging/tommand`
- No serializer dependency (fire-and-forget, no result deserialization)

### In-Memory Transport Network [S]
- `InMemoryTransportNetwork` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/InMemoryTransportNetwork.cs`
- Static `ConcurrentDictionary<EndPointAddress, MemoryInboxTransportServer>`
- Tessaging-only address book for in-memory endpoints

---

## 6. Server-Side Receiving

### Inbox Transport Server Interface [BOTH]
- `IInboxTransportServer` — `src/Compze.Core/Tessaging/Transport/Internal/IInboxTransportServer.cs`
- `Address`, `StartAsync()`, `StopAsync()`

### Memory Inbox Transport Server [S+binding]
- `MemoryInboxTransportServer` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryInboxTransportServer.cs`
- Tessaging-only dispatch: `ExactlyOnceTevent` / `ExactlyOnceTommand` → `Inbox.ReceiveAsync(tessage)`
- No `PostAsync<TResult>` — removed (was Typermedia-only)
- No `_serializer` dependency — removed
- On startup, also binds `TypermediaHandlerExecutor` in `InMemoryTypermediaNetwork` (pragmatic coupling, Phase 5 will decouple)

### ASP.NET Core Inbox Transport Server [BOTH]
- `AspNetInboxTransportServer` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/AspNetInboxTransportServer.cs`
- Starts `WebApplication` on a random port
- Hosts two controllers:

### Typermedia Controller [T]
- `TypermediaController` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/TypermediaController.cs`
- `/typermedia/tuery` → `TypermediaHandlerExecutor.ExecuteTuery`
- `/typermedia/tommand-with-result` → `TypermediaHandlerExecutor.ExecuteTommandWithResult`
- `/typermedia/tommand-no-result` → `TypermediaHandlerExecutor.ExecuteVoidTommand`
- Inherits `Controller` directly — no dependency on `ControllerBase`, `IInbox`, or `HandlerExecutionEngine`
- Deserializes HTTP requests using `IRemotableTessageSerializer` + `ITypeMapper` (no `TransportTessage.InComing`)
- Uses `RunOutsideScope` (`ExecutionContext.SuppressFlow()` + `Task.Run`) to escape ASP.NET middleware DI scope

### Tessaging Controller [S]
- `TessagingController` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/TessagingController.cs`
- `/tessaging/tevent` → `Inbox.ReceiveAsync`
- `/tessaging/tommand` → `Inbox.ReceiveAsync`

### Controller Base [S]
- `ControllerBase` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/ControllerBase.cs`
- Now Tessaging-only: provides `IInbox` and `CreateIncomingTessage()` to `TessagingController`
- `Serializer` and `HandlerExecutionEngine` properties removed

---

## 7. Inbox

### Inbox [S]
- **Interface**: `IInbox` — `src/Compze.Tessaging/Implementation/TessageHandling/Abstractions/IInbox.cs`
- **Impl**: `Inbox` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.cs`
- Single entry point:
  - `ReceiveAsync(tessage)` — fire-and-forget: save to storage (dedup) → `HandlerExecutionEngine.Enqueue(tessage)` → return immediately
- `ExecuteAsync` removed — was only used by Typermedia

### Inbox Message Storage [S]
- `Inbox.ITessageStorage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.ITessageStorage.cs`
- `Inbox.TessageStorage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.TessageStorage.cs`
- Tracks incoming messages: save, mark succeeded, record exception, mark failed
- Delegates to `IServiceBusSqlLayer.IInboxSqlLayer`
- Now Tessaging-only — Typermedia no longer flows through inbox storage

---

## 8. Handler Execution Engine [S]

### HandlerExecutionEngine [S]
- `Inbox.HandlerExecutionEngine` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine..cs`
- Runs a dedicated thread that dequeues and dispatches `HandlerExecutionTask`s
- `Enqueue(tessage)` → create task → add to queue → return immediately (fire-and-forget)
- `ExecuteAsync` removed — was only used by Typermedia
- No longer depends on `ITypermediaHandlerRegistry`

### Coordinator [S]
- `Inbox.HandlerExecutionEngine.Coordinator` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine.Coordinator.cs`
- `TryGetDispatchableTessage` — scans waiting list, applies all dispatching rules, caps at 20 concurrent handlers
- Blocks until a task is dispatchable
- `Dispatching`/`DoneDispatching` now only handle `ExactlyOnceTevent` and `ExactlyOnceTommand` — Typermedia branches removed
- No longer depends on `ITypermediaHandlerRegistry`

### Dispatching Rules [S]
- `Inbox.TessageDispatchingRules` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.TessageDispatchingRules.cs`
- `ITessageDispatchingRule` — `src/Compze.Tessaging/Implementation/TessageHandling/Dispatching/ITessageDispatchingRule.cs`
- One rule: **`TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint`** — serializes ExactlyOnce commands and events.
- `IExecutingTessagesSnapshot` tracks only `ExactlyOnceTommands` and `ExactlyOnceTevents`.

### HandlerExecutionTask [S]
- `Inbox.HandlerExecutionEngine.Coordinator.QueuedTessage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine.Coordinator.QueuedTessage.cs`
- Constructor switches on `TransportTessageType` to build the handler invocation delegate:
  - `ExactlyOnceTevent` → get all tevent handlers → call each in sequence
  - `ExactlyOnceTommand` → get void tommand handler → call
- Execution: `ExecuteTransactionalTessage()` — `TransactionScope` + `BeginScope()`, retry policy on failure
- No longer depends on `ITypermediaHandlerRegistry`

---

## 9. Handler Registration & Lookup [SPLIT]

### Tessaging Handler Registrar [S]
- **Interface**: `ITessageHandlerRegistrar` — `src/Compze.Tessaging.Abstractions/Tessaging/Hosting/TessageHandling/Registration/Public/ITessageHandlerRegistrar.cs`
- `ForTevent<TTevent>(handler)` — tevent handlers
- `ForTommand<TTommand>(handler)` — void command handlers

### Typermedia Handler Registrar [T]
- **Interface**: `ITypermediaHandlerRegistrar` — `src/Compze.Typermedia/HandlerRegistration/ITypermediaHandlerRegistrar.cs`
- `ForTommand<TTommand>(handler)` — void commands
- `ForTommand<TTommand, TResult>(handler)` — commands with results
- `ForTuery<TTuery, TResult>(handler)` — tuery handlers

### Tessaging Handler Registry [S]
- **Interface**: `ITessageHandlerRegistry` — `src/Compze.Tessaging/Implementation/TessageHandling/Abstractions/ITessageHandlerRegistry.cs`
- **Impl**: `TessageHandlerRegistry` — `src/Compze.Tessaging/Implementation/TessageHandling/Dispatching/TessageHandlerRegistry.cs`
- `_tommandHandlers` — `Dictionary<Type, Action<object>>` — void commands
- `_teventHandlers` — `Dictionary<Type, IReadOnlyList<Action<ITevent>>>` — events, 1:N, `IsAssignableFrom` lookup (Teventive type routing)
- `HandledRemoteTessageTypeIds()` — advertises tessaging-handled types

### Typermedia Handler Registry [T]
- **Interface**: `ITypermediaHandlerRegistry` — `src/Compze.Typermedia/HandlerRegistration/ITypermediaHandlerRegistry.cs`
- **Impl**: `TypermediaHandlerRegistry` — `src/Compze.Typermedia/HandlerRegistration/TypermediaHandlerRegistry.cs`
- `_voidTommandHandlers` — `Dictionary<Type, Action<object>>` — void commands
- `_tommandHandlersReturningResults` — `Dictionary<Type, HandlerWithResultRegistration>` — commands with results
- `_tueryHandlers` — `Dictionary<Type, HandlerWithResultRegistration>` — tueries
- `HandledRemoteTypermediaTypeIds()` — advertises typermedia-handled types
- Filters out `IInternalInfrastructureTessage` types from remote advertisement

### Tessaging Handler Registrar With DI Support [S]
- `TessageHandlerRegistrarWithDependencyInjectionSupport` — `src/Compze.Tessaging.Abstractions/Tessaging/Hosting/TessageHandling/Registration/Public/TessageHandlerRegistrarWithDependencyInjectionSupport.cs`
- Wraps `ITessageHandlerRegistrar` + `IServiceLocator`
- Extension methods: `ForTevent`, `ForTommand` (void)
- Exposed as `IEndpointBuilder.RegisterTessagingHandlers`

### Typermedia Handler Registrar With DI Support [T]
- `TypermediaHandlerRegistrarWithDependencyInjectionSupport` — `src/Compze.Typermedia/HandlerRegistration/TypermediaHandlerRegistrarWithDependencyInjectionSupport.cs`
- Wraps `ITypermediaHandlerRegistrar` + `IServiceLocator`
- Extension methods: `ForTuery`, `ForTommand` (void), `ForTommandWithResult`
- Exposed as `IEndpointBuilder.RegisterTypermediaHandlers`

---

## 10. Outbox [S]

### Outbox
- **Interface**: `IOutbox` — (in `Outbox` namespace)
- **Impl**: `Outbox` — `src/Compze.Tessaging/Implementation/Outbox/Outbox..cs`
- `SendTransactionally(cmd)` → `ITessagingRouter.ConnectionToHandlerFor(cmd)` → save to storage → on commit: `connection.EnqueueForDelivery(cmd)`
- `PublishTransactionally(tevent)` → `ITessagingRouter.SubscriberConnectionsFor(tevent)` → save to storage with all receiver endpoint IDs → on commit: each `connection.EnqueueForDelivery(tevent)`

### Outbox Message Storage [S]
- `Outbox.ITessageStorage` — `src/Compze.Tessaging/Implementation/Outbox/Outbox.ITessageStorage.cs`
- Tracks outgoing messages per receiver: save, mark received, record failure, get undelivered

### Command Scheduler [S]
- `TommandScheduler` — `src/Compze.Tessaging/Implementation/TommandScheduler.cs`
- In-memory timer, polls every 100ms, sends due commands via `Outbox.SendTransactionally`

---

## 11. Endpoint Hosting [BOTH]

### Endpoint Host
- **Interface**: `IEndpointHost` — `src/Compze.Core/Tessaging/Hosting/Public/IEndpointHost.cs`
- **Impl**: `EndpointHost` — `src/Compze.Tessaging/Hosting/EndpointHost.cs`
- `RegisterEndpoint(name, id, setup)` → creates `ServerEndpointBuilder` → calls `setup` → `builder.Build()`
- `StartAsync()` → phase 1: start inboxes + schedulers. Phase 2: connect tessaging routers + start outboxes.

### Server Endpoint Builder [BOTH]
- `ServerEndpointBuilder` — `src/Compze.Tessaging/Hosting/ServerEndpointBuilder.cs`
- `Build()` registers in DI: `TessagingTransport` (TessagingRouter), `Inbox`, `Outbox`, `ServiceBusSession`, `InProcessHypermediaNavigator`, `TommandScheduler`, internal tessage type handlers
- Note: `ITypermediaRouter`/`ITypermediaRouting` and `IRemoteTypermediaNavigator` are NOT registered here — they are client-side only

### Endpoint [BOTH]
- `Endpoint` — `src/Compze.Tessaging/Hosting/Endpoint.cs`
- `StartListeningComponentsAsync()` → starts inbox + scheduler
- `StartSendingComponentsAsync()` → connects tessaging router to all known addresses → starts outbox

---

## 12. Discovery [INFRASTRUCTURE — currently mistyped as Typermedia]

### ⚠️ Key architectural insight: Discovery is NOT Typermedia

`EndpointInformationTuery` and `NetworkTopologyTuery` are currently typed as `IRemotableTuery<T>` (Typermedia tueries). **This is wrong.** They are plain infrastructure request/response calls sent to a specific known address — they are NOT type-routed. Typermedia's defining characteristic is type-based routing; these queries go to a specific already-known endpoint, which is just plain HTTP + serialization.

Consequences of the current mistyping:
- Tessaging depends on Typermedia's internal transport (`ITypermediaTransport`) just to bootstrap connections
- Discovery handlers are registered as Typermedia tuery handlers in every endpoint, creating a hard dependency from hosting to Typermedia
- It spreads the false impression that "discovery is fundamentally a Typermedia mechanism" — it is not

**Fix (Phase 4b):** Retype these as `IQuery<T>` (the plain address-targeted query type from the message hierarchy — see Section 13). Create a simple infrastructure request/response mechanism (serialize, POST to known address, deserialize response). No type routing, no Typermedia registry, no `ITypermediaTransport`. Both Tessaging and Typermedia routers can then bootstrap independently using this shared infrastructure layer.

### Internal Tueries (currently mistyped)
- `TessageTypesInternal` — `src/Compze.Tessaging/Implementation/Abstractions/_TessageTypesInternal.cs`
- `EndpointInformationTuery : IRemotableTuery<EndpointInformation>` — returns endpoint ID + handled message type IDs
- `NetworkTopologyTuery : IRemotableTuery<NetworkTopology>` — returns all endpoint addresses
- Both are currently registered as typermedia tuery handlers in every endpoint — **should not be**
- Used by both `TypermediaRouter` (topology discovery) and `TessagingRouter` (endpoint info bootstrap)

---

## 13. Message Type Hierarchy

Defined in `src/Compze.Abstractions/Tessaging/Public/_TessageTypes..Interfaces.cs`.

The hierarchy has three layers: plain messages (no routing semantics), type-routed tessages, and paradigm-specific specializations.

```
IMessage                          — any message, no routing implied
├── IEvent                        — something happened
├── ICommand                      — do something (void)
│   └── ICommand<TResult>         — do something, return result
├── IQuery<TResult>               — ask something, get answer
│
└── ITessage : IMessage            — TYPE-ROUTED messages (routing by .NET type hierarchy)
    ├── ITevent : IEvent           — type-routed event
    ├── ITommand : ICommand        — type-routed command (void)
    │
    ├── ITypermediaTessage         — Typermedia paradigm marker
    │   └── ITyperMediaTessage<TResult>
    │       ├── ITommand<TResult> : ITommand, ICommand<TResult>  — type-routed command with result
    │       └── ITuery<TResult> : IQuery<TResult>                — type-routed query (Tuery IS-A Query)
    │
    ├── IRemotableTessage          — can cross endpoint boundaries
    │   ├── IRemotableTevent
    │   ├── IRemotableTommand / IRemotableTommand<TResult>
    │   └── IRemotableTuery<TResult>
    │
    ├── IAtMostOnceTessage          — at-most-once delivery guarantee (has TessageId)
    │   ├── IAtMostOnceTypermediaTommand
    │   └── IAtMostOnceTommand<TResult>
    │
    └── IExactlyOnceTessage         — exactly-once delivery (inbox/outbox)
        ├── IExactlyOnceTevent
        └── IExactlyOnceTommand
```

### Key design insight: `IMessage` vs `ITessage`

- **`IMessage`** is the root. `IQuery<T>`, `ICommand`, `IEvent` live here. These carry no routing semantics — they are plain messages sent to a specific known address.
- **`ITessage : IMessage`** adds type routing. Everything below `ITessage` uses .NET type compatibility for routing decisions.
- **`ITuery<T> : IQuery<T>`** — a Tuery IS-A Query, but a type-routed one. The converse is not true: a plain `IQuery<T>` is NOT a Tuery and NOT type-routed.

This distinction is critical for discovery: `EndpointInformationTuery` and `NetworkTopologyTuery` are address-targeted queries (`IQuery<T>`) that are currently mistyped as type-routed tueries (`IRemotableTuery<T>`). See Section 12.

---

## Entanglement Summary

### Clean separation (already done)
- Routers: `TypermediaRouter` [T] vs `TessagingRouter` [S]
- Connections: `TypermediaConnection` [T] vs `TessagingConnection` [S]
- ASP.NET controllers: `TypermediaController` [T] vs `TessagingController` [S]
- Entry points: `IRemoteTypermediaNavigator` [T] vs `IServiceBusSession` [S]
- Handler execution: `TypermediaHandlerExecutor` [T] vs `Inbox`/`HandlerExecutionEngine` [S]
- Outbox / command scheduler [S] — no typermedia involvement
- Handler registrars: `ITypermediaHandlerRegistrar` [T] vs `ITessageHandlerRegistrar` [S]
- Handler registries: `ITypermediaHandlerRegistry` / `TypermediaHandlerRegistry` [T] vs `ITessageHandlerRegistry` / `TessageHandlerRegistry` [S]
- DI wrappers: `TypermediaHandlerRegistrarWithDependencyInjectionSupport` [T] vs `TessageHandlerRegistrarWithDependencyInjectionSupport` [S]
- Capability advertisement: `HandledRemoteTypermediaTypeIds()` [T] vs `HandledRemoteTessageTypeIds()` [S]
- `IEndpointBuilder` exposes `RegisterTypermediaHandlers` [T] and `RegisterTessagingHandlers` [S] as separate properties

### Moved to correct projects (recently completed)
- `IRemoteTypermediaNavigator`, `RemoteTypermediaNavigator` → `Compze.Typermedia` (was in `Compze.Tessaging`)
- `IInProcessTypermediaNavigator`, `InProcessTypermediaNavigator` → `Compze.Typermedia` (was in `Compze.Tessaging`)
- `ITypermediaHandlerRegistrar`, `ITypermediaHandlerRegistry`, `TypermediaHandlerRegistry`, `TypermediaHandlerRegistrarWithDependencyInjectionSupport` → `Compze.Typermedia` (was in `Compze.Tessaging`/`Compze.Tessaging.Abstractions`)
- `TessageTypeInspector`, `TommandValidator`, `TessageValidator` → `Compze.Abstractions` (shared validation, no cross-dependency)
- `ITypermediaRouting` created in `Compze.Typermedia` — narrow routing interface (`PostAsync`/`GetAsync`). `ITypermediaRouter` extends it, adding lifecycle methods. `RemoteTypermediaNavigator` depends only on `ITypermediaRouting`.
- `IInternalInfrastructureTessage` marker added in `Compze.Abstractions` — lets `TypermediaHandlerRegistry` filter infrastructure types without depending on tessaging internals
- Void command support added to `ITypermediaHandlerRegistrar`/`TypermediaHandlerRegistry`
- `InProcessTypermediaNavigator` dispatches void commands through typermedia registry (no longer needs `ITessageHandlerRegistry`)
- Dispatching rules disentangled: removed `TueriesExecuteAfterAllTommandsAndTeventsAreDone` rule entirely. Remaining rule (`TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint`) now only serializes ExactlyOnce tessaging messages — typermedia commands and queries bypass it. `IExecutingTessagesSnapshot` no longer tracks AtMostOnce commands.
- `TypermediaHandlerExecutor` created in `Compze.Typermedia.Hosting` — gives Typermedia its own handler execution path, bypassing Inbox/HandlerExecutionEngine
- Void Typermedia commands (`IAtMostOnceTypermediaTommand`) moved from `RegisterTessagingHandlers` to `RegisterTypermediaHandlers` (test fixtures + AccountManagement sample). `HandledRemoteTypermediaTypeIds()` updated to include void command handlers.
- Phase 3 dead code removal: `Inbox.ExecuteAsync`, `HandlerExecutionEngine.ExecuteAsync`, `HandlerExecutionTask.ExecuteTuery`, Typermedia branches from `HandlerExecutionTask.CreateTessageTask`/`Coordinator.Dispatching`/`Coordinator.DoneDispatching` all removed. `ITypermediaHandlerRegistry` dependency removed from `HandlerExecutionEngine`, `Coordinator`, and `HandlerExecutionTask`.
- `TypermediaController` separated from `ControllerBase`: now inherits `Controller` directly, deserializes via `IRemotableTessageSerializer` + `ITypeMapper` (no `TransportTessage.InComing`), uses `RunOutsideScope` to escape middleware DI scope. `ControllerBase` is now Tessaging-only (removed `Serializer` and `HandlerExecutionEngine` properties, removed `IInbox`/`HandlerExecutionEngine` from `TessagingController` constructor).

### Transport separated (Phase 4 — done)
- `ITypermediaTransport` [T] — Typermedia's own transport (`MemoryTypermediaTransport`, `HttpTypermediaTransport`)
- `ITransportMessagePoster` [S] — now tessaging-only (no `PostAsync<TResult>`, no Typermedia branches)
- `TransportTessage` / `TransportTessageType` [S] — tessaging-only (Typermedia values removed from enum)
- `InMemoryTypermediaNetwork` [T] — Typermedia's own address book
- `InMemoryTransportNetwork` [S] — tessaging-only address book
- `ApiEndpointClient` / `IRemoteApiEndpointClient` — deleted
- `TessagingConnection` bootstraps via `ITypermediaTransport` — **wrong**: see Section 12, discovery is not Typermedia

### Still entangled (discovery mistyping + hosting — next phases)
| Component | Why it's entangled |
|---|---|
| `EndpointInformationTuery` / `NetworkTopologyTuery` | Mistyped as `IRemotableTuery<T>` (Typermedia) — forces Tessaging to depend on `ITypermediaTransport` for plain address-targeted request/response. Should be infrastructure messages, not Typermedia. |
| `TessagingRouter` / `TessagingConnection` | Depends on `ITypermediaTransport` solely because discovery queries are mistyped as Typermedia tueries |
| `MemoryInboxTransportServer` | Binds `TypermediaHandlerExecutor` in `InMemoryTypermediaNetwork` during startup |
| `AspNetInboxTransportServer` | Hosts both `TypermediaController` and `TessagingController` |
| `ServerEndpointBuilder` | Wires inbox, outbox, tessaging router, in-process navigator, and typermedia executor all together |
| `Endpoint` | Lifecycle manages both typermedia and tessaging components |
| `EndpointHost` | Starts both paradigms' components in a single two-phase startup |
