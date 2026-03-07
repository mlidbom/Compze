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
- `TypermediaController`: All three endpoints deserialize and call executor directly

**Registration fix:** Void Typermedia commands were historically registered in the tessaging handler registry. Moved to `RegisterTypermediaHandlers` where they belong (test fixtures + AccountManagement sample). `HandledRemoteTypermediaTypeIds()` updated to include `_voidTommandHandlers`.

**Residual entanglement:** `TypermediaController` still inherits `ControllerBase` which takes `IInbox` + `HandlerExecutionEngine` constructor args (unused by TypermediaController). Clean up in Phase 3.

### Phase 3 — Clean Up Tessaging Internals

Remove dead Typermedia code from the Tessaging execution pipeline.

- Remove `Inbox.ExecuteAsync` — no longer called by anything.
- Remove Typermedia branches from `QueuedTessage` switch statements.
- Remove Typermedia branches from `Coordinator`.
- `HandlerExecutionEngine` simplifies to Tessaging-only.
- `Inbox.ITessageStorage` no longer touched by Typermedia.
- Break `TypermediaController`'s inheritance of `ControllerBase` — it no longer needs `IInbox` or `HandlerExecutionEngine`. Extract the deserialization helper (`CreateIncomingTessage`) it still uses.

### Phase 4 — Typermedia Gets Its Own Transport

Eliminate shared transport abstractions.

- Replace `ApiEndpointClient` → `ITransportMessagePoster` with a Typermedia-specific transport:
  - **Memory:** Direct function call to `TypermediaHandlerExecutor` (no serialization needed in-process).
  - **HTTP:** Direct `HttpClient` call to existing `/typermedia/*` endpoints.
- Create `TypermediaMemoryTransportServer` to replace the Typermedia branches in `MemoryInboxTransportServer`.
- Typermedia stops using `TransportTessage`, `TransportTessageType`, `ITransportMessagePoster`.
- Remove Typermedia values from `TransportTessageType` enum.
- Remove Typermedia branches from `HttpTransportMessagePoster` and `MemoryInboxTransportServer`.
- `TransportTessage` becomes Tessaging-only (resolves the `//todo: split out TyperMediaTransportTessage`).

### Phase 5 — Separate Hosting

Each paradigm gets its own hosting lifecycle.

- `ServerEndpointBuilder` stops registering Typermedia handler executor infrastructure (or Typermedia gets its own builder).
- `Endpoint` lifecycle decoupled — Typermedia server startup independent of Tessaging outbox/router startup.
- `AspNetInboxTransportServer` either splits into two servers or Typermedia gets its own `WebApplication` setup.
- `EndpointHost` orchestrates both but they don't block each other.

### Recommended next step

Phase 3 is pure cleanup — removing dead code paths that nothing calls anymore. Low-risk, high-clarity improvement. Phase 4 is the next structural change (giving Typermedia its own transport).

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
- Maintains route tables: `Dictionary<Type, TypermediaConnection>` for commands and queries
- `PostAsync(cmd)` → look up `TypermediaConnection` by message type → `connection.ApiClient.PostAsync(cmd)`
- `GetAsync(tuery)` → look up `TypermediaConnection` by tuery type → `connection.ApiClient.GetAsync(tuery)`
- Discovery: `DiscoverAndConnectAsync(seedAddress)` → sends `NetworkTopologyTuery` to seed → gets all endpoint addresses → connects to each
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
- Holds `IRemoteApiEndpointClient` (the `ApiClient`) and `EndpointInformation`
- `InitAsync()` → sends `EndpointInformationTuery` to discover handled types
- Stateless — no queues, no background threads

### Tessaging Connection [S]
- `TessagingConnection` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/TessagingConnection.cs`
- Implements `ITessagingInboxConnection`
- Has a background send loop with `Queue<PendingDelivery>`
- `EnqueueForDelivery(tessage)` → serialize → track in-flight → enqueue → signal loop
- `TrySend(pending)` → `_transportMessagePoster.PostAsync(tessage, address)` → mark received on success → retry with backoff on failure
- `StartDelivery()` → loads undelivered from storage → starts loop thread

---

## 4. Serialization & Transport Message Wrapping

### ApiEndpointClient [T]
- **Interface**: `IRemoteApiEndpointClient` — `src/Compze.Tessaging/Implementation/Transport/Client/Internal/IRemoteApiEndpointClient.cs`
- **Impl**: `ApiEndpointClient` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Universal/ApiEndpointClient.cs`
- Wraps the message: `TransportTessage.OutGoing.Create(message, typeMapper, serializer)` → serializes body to JSON, maps type to `TypeId`, classifies to `TransportTessageType`
- Delegates to `ITransportMessagePoster.PostAsync(tessage, address)`
- Also handles `BootstrapConnectionToEndpoint` (sends `EndpointInformationTuery`)

### TransportTessage [BOTH]
- `TransportTessage` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/TransportTessage.cs`
- **`OutGoing`**: `Body` (serialized JSON), `Type` (TypeId), `TessageId`, `TessageTypeEnum`
- **`InComing`**: same fields + `_tessageType` (resolved .NET Type) + lazy `DeserializeTessageAndCacheForNextCall()`
- The envelope type is shared — carries both paradigms' messages

### TransportTessageType [BOTH]
- `TransportTessageType` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/TransportTessageType.cs`
- Enum values mix both paradigms:
  - `ExactlyOnceTevent` (S)
  - `ExactlyOnceTommand` (S)
  - `TypermediaAtMostOnceTommand` (T)
  - `TypermediaAtMostOnceTommandWithReturnValue` (T)
  - `TyperMediaTuery` (T)

---

## 5. Transport Layer

### Transport Message Poster [BOTH]
- **Interface**: `ITransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Abstractions/ITransportMessagePoster.cs`
- Shared abstraction. Both `ApiEndpointClient` (typermedia) and `TessagingConnection` (tessaging) use this.

### Memory Transport Poster [BOTH]
- `MemoryTransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryTransportMessagePoster.cs`
- Converts `OutGoing` → `InComing`, calls `InMemoryTransportNetwork.GetServer(address).PostAsync(incoming)`
- Direct in-process call, no network

### HTTP Transport Poster [BOTH]
- `HttpTransportMessagePoster` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Http/HttpTransportMessagePoster.cs`
- Sends HTTP POST. Routes to different URL paths by `TransportTessageType`:
  - `TyperMediaTuery` → `/typermedia/tuery`
  - `TypermediaAtMostOnceTommandWithReturnValue` → `/typermedia/tommand-with-result`
  - `TypermediaAtMostOnceTommand` → `/typermedia/tommand-no-result`
  - `ExactlyOnceTevent` → `/tessaging/tevent`
  - `ExactlyOnceTommand` → `/tessaging/tommand`
- Response deserialized via `IRemotableTessageSerializer.DeserializeResponse<TResult>(json)`

### In-Memory Transport Network [BOTH]
- `InMemoryTransportNetwork` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/InMemoryTransportNetwork.cs`
- Static `ConcurrentDictionary<EndPointAddress, MemoryInboxTransportServer>`
- Global address book for in-memory endpoints

---

## 6. Server-Side Receiving

### Inbox Transport Server Interface [BOTH]
- `IInboxTransportServer` — `src/Compze.Core/Tessaging/Transport/Internal/IInboxTransportServer.cs`
- `Address`, `StartAsync()`, `StopAsync()`

### Memory Inbox Transport Server [BOTH]
- `MemoryInboxTransportServer` — `src/Compze.Tessaging/Implementation/Transport/Client/Implementation/Memory/MemoryInboxTransportServer.cs`
- Switches on `TessageTypeEnum`:
  - `TypermediaAtMostOnceTommandWithReturnValue` → `TypermediaHandlerExecutor.ExecuteTommandWithResult` via `Task.Run`
  - `TyperMediaTuery` → `TypermediaHandlerExecutor.ExecuteTuery` via `Task.Run`
  - `TypermediaAtMostOnceTommand` → `TypermediaHandlerExecutor.ExecuteVoidTommand` via `Task.Run`
  - `ExactlyOnceTevent` / `ExactlyOnceTommand` → `Inbox.ReceiveAsync(tessage)` (enqueue, fire-and-forget)
- Typermedia branches no longer touch Inbox or HandlerExecutionEngine

### ASP.NET Core Inbox Transport Server [BOTH]
- `AspNetInboxTransportServer` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/AspNetInboxTransportServer.cs`
- Starts `WebApplication` on a random port
- Hosts two controllers:

### Typermedia Controller [T]
- `TypermediaController` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/TypermediaController.cs`
- `/typermedia/tuery` → `TypermediaHandlerExecutor.ExecuteTuery`
- `/typermedia/tommand-with-result` → `TypermediaHandlerExecutor.ExecuteTommandWithResult`
- `/typermedia/tommand-no-result` → `TypermediaHandlerExecutor.ExecuteVoidTommand`
- Still inherits `ControllerBase` (for `CreateIncomingTessage`), taking unused `IInbox`/`HandlerExecutionEngine` args — cleanup target

### Tessaging Controller [S]
- `TessagingController` — `src/Compze.Tessaging.Hosting.AspNetCore/Private/TessagingController.cs`
- `/tessaging/tevent` → `Inbox.ReceiveAsync`
- `/tessaging/tommand` → `Inbox.ReceiveAsync`

---

## 7. Inbox

### Inbox [BOTH → cleanup target]
- **Interface**: `IInbox` — `src/Compze.Tessaging/Implementation/TessageHandling/Abstractions/IInbox.cs`
- **Impl**: `Inbox` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.cs`
- Two entry points:
  - `ReceiveAsync(tessage)` — fire-and-forget: save to storage (dedup) → `HandlerExecutionEngine.Enqueue(tessage)` → return immediately
  - `ExecuteAsync(tessage)` — request-response: **no longer called** — was only used by Typermedia. Dead code, remove in Phase 3.

### Inbox Message Storage [S]
- `Inbox.ITessageStorage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.ITessageStorage.cs`
- `Inbox.TessageStorage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.TessageStorage.cs`
- Tracks incoming messages: save, mark succeeded, record exception, mark failed
- Delegates to `IServiceBusSqlLayer.IInboxSqlLayer`
- Note: Typermedia messages also go through this (via `Inbox.ExecuteAsync`) even though they don't need exactly-once tracking

---

## 8. Handler Execution Engine [BOTH]

### HandlerExecutionEngine [BOTH → cleanup target]
- `Inbox.HandlerExecutionEngine` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine..cs`
- Runs a dedicated thread that dequeues and dispatches `HandlerExecutionTask`s
- `Enqueue(tessage)` → create task → add to queue → return immediately (fire-and-forget)
- `ExecuteAsync(tessage)` → **no longer called** — was only used by Typermedia tueries via MemoryInboxTransportServer. Dead code, remove in Phase 3.

### Coordinator
- `Inbox.HandlerExecutionEngine.Coordinator` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine.Coordinator.cs`
- `TryGetDispatchableTessage` — scans waiting list, applies all dispatching rules, caps at 20 concurrent handlers
- Blocks until a task is dispatchable

### Dispatching Rules [S]
- `Inbox.TessageDispatchingRules` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.TessageDispatchingRules.cs`
- `ITessageDispatchingRule` — `src/Compze.Tessaging/Implementation/TessageHandling/Dispatching/ITessageDispatchingRule.cs`
- One rule: **`TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint`** — serializes ExactlyOnce commands and events. Typermedia messages (`TyperMediaTuery`, `TypermediaAtMostOnceTommand`, `TypermediaAtMostOnceTommandWithReturnValue`) bypass this rule entirely.
- `IExecutingTessagesSnapshot` tracks only `ExactlyOnceTommands` and `ExactlyOnceTevents` — no typermedia state.
- Typermedia commands/queries run in parallel with each other and with tessaging mutations. SQLite in-memory deadlocks under this parallelism (its connection pool uses a single-writer lock), so multithreaded SQLite tests are skipped.

### HandlerExecutionTask [BOTH → cleanup target]
- `Inbox.HandlerExecutionEngine.Coordinator.QueuedTessage` — `src/Compze.Tessaging/Implementation/TessageHandling/Inbox/Inbox.HandlerExecutionEngine.Coordinator.QueuedTessage.cs`
- Constructor switches on `TransportTessageType` to build the handler invocation delegate:
  - `ExactlyOnceTevent` → get all tevent handlers → call each in sequence
  - `TypermediaAtMostOnceTommandWithReturnValue` → **dead branch** (no longer enqueued)
  - `TypermediaAtMostOnceTommand` → **dead branch** (no longer enqueued)
  - `ExactlyOnceTommand` → get void tommand handler → call
  - `TyperMediaTuery` → **dead branch** (no longer enqueued)
- Execution paths:
  - **Queries** → `ExecuteTuery()`: **dead code** (no tueries flow through engine anymore)
  - **Everything else** → `ExecuteTransactionalTessage()`: `TransactionScope` + `BeginScope()`, retry policy on failure

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

## 12. Discovery [T uses, S uses, mechanism is T]

### Internal Tueries
- `TessageTypesInternal` — `src/Compze.Tessaging/Implementation/Abstractions/_TessageTypesInternal.cs`
- `EndpointInformationTuery : IRemotableTuery<EndpointInformation>` — returns endpoint ID + handled message type IDs
- `NetworkTopologyTuery : IRemotableTuery<NetworkTopology>` — returns all endpoint addresses
- Both are typermedia tueries, registered as tuery handlers in every endpoint
- Used by both `TypermediaRouter` (topology discovery) and `TessagingConnection` (endpoint info bootstrap)

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

### Dead code to remove (Phase 3)
| Component | Dead code |
|---|---|
| `Inbox.ExecuteAsync` | No longer called — was Typermedia's entry point |
| `HandlerExecutionEngine.ExecuteAsync` | No longer called — was Typermedia tuery's entry point |
| `QueuedTessage` Typermedia branches | `TypermediaAtMostOnceTommand`, `TypermediaAtMostOnceTommandWithReturnValue`, `TyperMediaTuery` — never enqueued |
| `QueuedTessage.ExecuteTuery` | No tueries flow through engine |
| `TypermediaController` → `ControllerBase` inheritance | Takes `IInbox` + `HandlerExecutionEngine` it doesn't use |
| `Inbox.ITessageStorage` | Typermedia no longer goes through inbox storage |

### Still entangled (transport + hosting — Phases 4+5)
| Component | Why it's entangled |
|---|---|
| `ITransportMessagePoster` | Single abstraction carrying both paradigms' messages |
| `TransportTessage` / `TransportTessageType` | Single envelope type with enum discriminating 5 message kinds across both paradigms |
| `InMemoryTransportNetwork` | Single address book for all endpoints regardless of paradigm |
| `MemoryInboxTransportServer` | Still dispatches both paradigms (executor for Typermedia, Inbox for Tessaging) |
| `AspNetInboxTransportServer` | Hosts both `TypermediaController` and `TessagingController` |
| `ServerEndpointBuilder` | Wires inbox, outbox, tessaging router, in-process navigator, and typermedia executor all together |
| `Endpoint` | Lifecycle manages both typermedia and tessaging components |
| `EndpointHost` | Starts both paradigms' components in a single two-phase startup |
