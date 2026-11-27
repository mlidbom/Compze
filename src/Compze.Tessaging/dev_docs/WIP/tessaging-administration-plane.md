# Tessaging's administration plane: design

## Status

A design, 2026-11-27, by a helper of its own, as Magnus chose on topic 7 of the tessaging-deferred-handling helper. Nothing
is built. The decisions at its end are put to Magnus one at a time; the calls made on judgment are listed beside them, for
him to overrule.

Magnus, on that topic, 2026-11-27:

> Tessaging at current completely lacks a "management bus" (not sure about that name) ... It is not a tevent that any
> domain other than the bus(compze) itself owns, and it can hardly use the existing guaranteed delivery mechanisms since
> that would both be performance prohibitive and a catch 22 if any handler of management tevents or commands ever fail...
>
> So this needs us taking a big leap back and considering the BIG picture of managing tessaging infrastructure in
> production, building administration APIs for it etc. If we just duct tape something onto the bus that can sorta-kinda
> support this use case and nothing else I think we will bitterly regret it in the not too distant future...
>
> my thinking is that the admin API for tessaging will itself be a tessaging endpoint, but NOT a guaranteed delivery one

This document calls it the **administration plane**: everything that lets Tessaging's own infrastructure be seen and acted
on in production, as opposed to **the application's conversation**, the tessages of the application's domains. The word
"administration" is the one Tessaging's documents already use, for peer administration and failed-tessage administration.

## The headline

**The administration plane is the bus's own conversation, beside the application's, and never through its guaranteed
delivery:**

1. **Every endpoint serves an administration API about itself,** on its one transport server, beside discovery: tueries to
   see and tommands to act, at most once, handled outside the inbox and outside the application's roster, in a wire format
   Compze owns, and addressed to the endpoint rather than routed by type.
2. **Any endpoint can administer the endpoints it discovers.** `ITessagingAdministration`, in every endpoint's container,
   gives this endpoint's administration and that of every endpoint it discovers, as one interface,
   `IEndpointAdministration`. An administration UI is a best-effort endpoint that discovers the suite.
3. **The endpoint's own tables are the durable record; a notice only says when to look again.** The record is read with a
   query. Notices are observed in the endpoint's own process, or pulled by the administering endpoint, and never ride the
   delivery machinery.
4. **Only the endpoint that owns state changes it:** a running endpoint through its administration API; a stopped or
   retired endpoint through its storage, by a process that holds its process lock for the act.
5. **Administration never fails, blocks or slows the application's conversation,** and keeps working while the
   application's handling is stuck.
6. **Authorization sits above the transport seam.** Until a transport authenticates its callers, the administration API is
   served where the transport vouches for its caller, the named pipes' current user, and elsewhere only where the endpoint
   declares it.

**The first increment serves the Claude tooling's hub, with no wire:** this endpoint's failed tessages, read in its own
process, their retry, and an observer of the tessages its inbox marks failed.

## The need

### What is gathered, each with its record

| Need | Record | Served by |
|---|---|---|
| See failed tessages, retry them once the problem is fixed | [failed-tessage administration](../TODO/failed-tessage-administration.md) | Increments 1, 2 |
| Hear that a tessage was marked failed: alerting, and a flow that must end | the same; topic 7 | Increments 1, 3 |
| Resolve orphaned tessages per kind; an archive; decommission that refuses while anything is owed | [peer administration](peer-administration.md) | Increment 5 |
| Drop a retired endpoint's storage, refused while its process lock is held | `ITessagingSqlLayer.IEndpointCatalogSqlLayer`; [storage](../storage.md) | Increment 6 |
| Distributed quiescence, readiness's sibling | [the style-substrate evaluation, question 6](../DONE/style-substrate-and-hosting-evaluation.md) | Increment 4 |
| Authorization by tessage type, above the transport seam | [TCP transport and discovery](../TODO/tcp-transport-and-discovery-across-machines.md), "A pipeline belongs above the seam" | Increments 2, 7 |

### The big picture: what running Tessaging in production asks of it

- **Seeing.** Which endpoints make up the suite, which run, and where. What each remembers of its peers. What each holds:
  failed tessages, tessages owed to a peer that is down, stranded ones, best-effort queues. Who holds a process lock.
- **Acting.** Retry a failed tessage. Resolve what will never be delivered: archive it, with the decision recorded. Forget a
  peer that is gone for good. Drop a retired endpoint's storage. Later, perhaps, pause sending ahead of an update.
- **Hearing.** A tessage was marked failed; tessages were stranded; an endpoint lost its process lock's session. For
  alerting, for a console, and for a flow that waits on a step.
- **Winding down and coming up.** Readiness: safe to take traffic. Quiescence: safe to stop.
- **The same, securely,** across processes and machines.

### The waiting consumer: the Claude tooling's hub

A deferred step that fails until its `RetryPolicy` gives up is marked failed, and the operation's request then waits for
ever ([durable multi-step work on one endpoint](durable-multi-step-work-on-one-endpoint.md)). Until it can hear of it, the
hub's handler catches the failure itself and records the operation's end, which gives up the retries. The hub's need is in
its own process: its own endpoint marks its own tessage failed.

## What the plane holds to, and why

- **The plane is the bus's own.** Its tueries, tommands and notices belong to no domain: they are never in the
  application's roster, advertisement, routes, peer memory or best-effort queues. Mixed in, a burst of failure notices
  would share the application's queue bounds and could make its publishes fail with
  `BestEffortTeventQueueOverflowException`.
- **Never guaranteed delivery.** Magnus's two reasons: it would cost too much, and a failing handler of an administration
  tessage would be a catch-22. So tueries and tommands are at most once, and every act is safe to repeat: it acts only when
  its precondition holds, such as a retry only of a tessage marked failed.
- **The durable record is the endpoint's own tables,** read through the endpoint, never by the administering side from the
  tables (agreed in the failed-tessage todo): the endpoint owns its inbox, its handling claim and its handlers.
- **Nothing is held for the administering side.** An administered endpoint keeps no subscriptions, queues or connections
  for it, so an administration UI that comes and goes costs the suite nothing.
- **Administration works while the application's handling is stuck.** It is served by the transport server, not by the
  inbox, so a tessage holding the ordered line holds up nothing of it.

## 1. Every endpoint's administration API

### Public API (sketch)

```csharp
namespace Compze.Tessaging.Administration;

//review-naming:administration:
///The administration of this endpoint and of the endpoints it discovers. In every endpoint's container, as a singleton.
public interface ITessagingAdministration
{
   ///This endpoint's own administration, in its own process: no wire, no authorization.
   IEndpointAdministration ThisEndpoint { get; }

   ///Every endpoint this endpoint discovers, besides itself, each administered through its administration API.
   IReadOnlyCollection<IEndpointAdministration> DiscoveredEndpoints { get; }
}

///One endpoint's administration, the same interface whether it is this endpoint or another.
public interface IEndpointAdministration
{
   EndpointId Id { get; }
   string Name { get; }

   ///The tessages the endpoint's inbox has marked failed, in pages. No order is promised. A best-effort endpoint has none.
   IAsyncEnumerable<IFailedTessage> FailedTessagesAsync(CancellationToken cancellationToken = default);
}

///A tessage the endpoint's inbox marked failed: its retry policy gave up on it, and it waits for an administrator.
public interface IFailedTessage
{
   TessageId Id { get; }
   ///The endpoint itself, for what it sent itself.
   EndpointId Sender { get; }
   ///Text: the administering side need not map the application's types.
   string TessageTypeName { get; }
   ///As the endpoint's serializer wrote it.
   string Body { get; }
   int FailedTries { get; }
   TimeSpan FailingTime { get; }
   ///New: recorded by the mark, for display only.
   DateTime MarkedFailedUtc { get; }
   ///The last try's exception: its type, message and stack trace, as text.
   HandlingFailure LastFailure { get; }

   ///Retries the tessage: it is handled again, out of its pair's order. Succeeds when the tessage is no longer marked
   ///failed, by this retry or an earlier one.
   Task RetryAsync(CancellationToken cancellationToken = default);
}
```

The names marked `review-naming:` are chosen, not decisions. `TessageTypeName` is text on purpose, and meets the existing
todo on `PersistedPeer.HandledTessageTypes` that type ids travel as plain strings.

"Retry" is the administrative act. "Try again" stays the retry policy's word for its own tries, as in the inbox today.

### Mechanism: this endpoint, in its own process

`ThisEndpoint` calls the inbox directly: no tessage, no transport. Its reads are queries on the inbox's table, outside any
transaction. `RetryAsync` asserts that there is no ambient transaction, as `IRemoteTypermediaNavigator` does, since the
same call on another endpoint can't take part in the caller's transaction.

### Mechanism: another endpoint, on the wire

- **A request kind of its own,** `TransportRequestKind.Administration`, beside `EndpointDiscoveryQuery`. The
  `TransportRequestHandlerMap` serves it itself, as it serves discovery, so every endpoint serves it, whatever it speaks.
- **Compze's own administration tueries and tommands,** internal types under an `_private` namespace, in the fixed wire
  format discovery already uses (`EndpointInformationQuerySerializer`), renamed for what it then is: the format of the
  bus's own conversation. So an administration UI administers endpoints whatever serializer each declares.
- **Addressed by `EndpointId`,** never routed by type. Each call goes to the address the administering endpoint's router
  has for that id now, so a handle such as an `IFailedTessage` keeps working across the endpoint's restart, which moves its
  generated address. An endpoint not reachable now fails the call at once, without the handler-availability patience: an
  administrator wants to see that it is down.
- **Outside the roster and the advertisement:** nothing changes any router's routes, and `IRemoteTypermediaNavigator` never
  finds an administration type. This also keeps out of the way of the open question of [addressing one endpoint among
  several](../TODO/addressing-one-endpoint-among-several.md).
- **Run once.** The administration executor is its own, so the retries of `TypermediaHandlerExecutor`, which runs a handler
  that threw up to five times, don't apply.
- **Listening and stopping:** the API is served while the transport server listens. In-process acts before the inbox's
  recovery scan, or after the stop, are refused, as the deferred-handling design refuses a send to the endpoint itself.

### What each tier serves

The administration API follows the tier ladder: a best-effort endpoint serves its identity, its process-lifetime peers and
its best-effort queues; an exactly-once endpoint adds its inbox, its outbox and its durable peer memory. Increment 1 serves
only the failed tessages.

## 2. Administering the suite

**The suite** here means the endpoints that discover each other through one registry.

- **Any endpoint administers what it discovers.** `DiscoveredEndpoints` is the endpoints its router connects to now, and
  those its peer memory remembers. The cost is nothing until it is called.
- **An administration UI is a best-effort endpoint:** Magnus's "a tessaging endpoint, but NOT a guaranteed delivery one". It
  discovers through the registry and needn't announce itself, which `DiscoverEndpointsThrough` without `AnnounceAddressTo`
  allows today. Then no endpoint of the suite connects to it or remembers it. The failed-tessage todo's Blazor app, run in
  an endpoint, or an Avalonia app, sits on this interface.
- **Its `EndpointId` is stable,** since a fresh id at each start leaves a peer remembered at each start wherever it is
  remembered.

## 3. Notices

A **notice** is what an endpoint says about itself as it happens. Notices are never handled in a transaction and never
retried; an observer's failure goes to the background-exception reporter (`IBackgroundExceptionReporter`), as a tevent
observer's does. Two reaches, each with semantics it can state honestly:

### In the endpoint's own process: told at least once, with no delivery machinery

```csharp
public abstract class EndpointBuilder
{
   //review-naming:administration:
   ///Declares observers of this endpoint's administration notices, in its own process, from its start.
   public EndpointBuilder ObserveAdministrationNotices(Action<AdministrationNoticeObservationRegistrar> observe);
}

public sealed class AdministrationNoticeObservationRegistrar
{
   ///Observes each tessage compatible with TTessage that this endpoint's inbox marks failed: told off-thread once the mark
   ///has committed, and again at each later start of the endpoint while the tessage stays marked failed.
   public AdministrationNoticeObservationRegistrar ForTessageMarkedFailed<TTessage>(
      Func<TTessage, IFailedTessage, IScopeResolver, Task> observer) where TTessage : ITessage;
}
```

- **Typed, by type compatibility,** as tevent observation is, since in its own process the endpoint can deserialize what it
  stored. Declared at composition, through the builder idiom, so it observes from the first moment.
- **At least once, across crashes, by construction.** A crash between the mark's commit and the observer's end would lose a
  notice. So, after each start, off the start's path, the endpoint pages through its tessages marked failed and tells each
  observer of the compatible ones again. No row, column or outbox is added: the failed mark is the record, and the
  start's scan reads it. An observer must then be safe to repeat, as a handler's effect outside its database already must.
- **No catch-22:** nothing waits for an observer. One that throws is reported and not told again until the next start.

### To the administering endpoint: pulled, with every gap said

- **Each endpoint keeps the notices of its current process** in a journal of fixed size, in memory, each with a sequence
  number. The bound is fixed, so the journal can't grow with the application's load or fail it.
- **The administering endpoint pulls,** with a long-poll tuery: "notices after this sequence number, waiting up to 20
  seconds". The administered endpoint holds nothing for it between calls.
- **A gap is said, never hidden.** When notices may have been missed, because the journal overflowed, the endpoint
  restarted, or a call failed, the feed yields a notice saying so. The consumer then reads the record again.

```csharp
public interface IEndpointAdministration
{
   ///The endpoint's notices as they happen, until cancelled. INoticesMayHaveBeenMissed comes wherever some may have been.
   IAsyncEnumerable<IAdministrationNotice> NoticesAsync(CancellationToken cancellationToken = default);
}
```

## 4. Authorization

- **Today no transport authenticates its callers.** The named pipes admit only the current user
  (`PipeOptions.CurrentUserOnly`); the ASP.NET Core transport listens on `127.0.0.1`, open to every user of the machine.
- **What the administration API adds to the exposure:** bodies of failed tessages, which may hold personal data, and acts.
- **Increment 2's default:** the administration API is served where the transport vouches for its caller, which today means
  the named pipes, and on other transports only where the endpoint declares it, in a lab on an isolated network say:

```csharp
//review-naming:administration:
public EndpointBuilder ServeAdministrationToCallersTheTransportCannotIdentify();
```

- **Where it is checked:** in `TransportRequestHandlerMap`, above the seam, before dispatch, so it serves every transport.
  The transport hands the map what it knows of the caller: today "this machine's user" or nothing.
- **Later, increment 7,** when a transport authenticates its callers: authorization by act, such as reading bodies apart
  from retrying, on the general pipeline above the seam. Akka Management serves its management API on an HTTP server of its
  own, "in most cases" on another network interface; an administration address of its own is the option for network
  isolation once a transport crosses machines.

## 5. Administration at rest

A stopped or retired endpoint serves nothing, but its storage stays in its domain database: failed tessages, rows it owes
its peers. The endpoint's process lock already makes "one process per endpoint" hold, so:

- **Any process whose endpoint joins the same domain database can act on another endpoint's storage while holding that
  endpoint's process lock,** for the act's length, through the SQL layers for that endpoint's table-set.
- **By construction, never both:** while the act holds the lock, the endpoint's start is refused loudly, with
  `EndpointAlreadyRunningInAnotherProcessException` naming the administering process; while the endpoint runs, the act is
  refused.
- **What it serves:** the failed tessages of a stopped endpoint, retried by marking them unhandled for its next start's
  recovery scan, which is today's manual workaround made safe; archiving them; and the storage drop, which refuses while
  the table-set holds anything not finished.

## The catalogue of acts

| Act | Tier | Who performs it | Increment |
|---|---|---|---|
| Read failed tessages | Exactly-once | The running endpoint | 1 (in-process), 2 (wire) |
| Retry a failed tessage | Exactly-once | The running endpoint | 1, 2 |
| Observe tessages marked failed | Exactly-once | The endpoint's own process | 1 |
| Pull notices | Both | The running endpoint | 3 |
| Read work counts; await the suite's quiescence | Both | Each running endpoint; the administering endpoint aggregates | 4 |
| Archive a failed tessage | Exactly-once | The running endpoint | 5 |
| Resolve a stranded or undelivered tessage: archive it, or send it again | Exactly-once | The sending endpoint | 5 |
| Forget a peer, refused while anything is owed to it | Both | The remembering endpoint | 5 |
| Retry or archive a stopped endpoint's failed tessages | Exactly-once | A process holding its process lock | 6 |
| Drop a retired endpoint's storage | Exactly-once | A process holding its process lock | 6 |

## Exactly-once in order, walked through

### A retry

- **The act, in a transaction of its own:** a conditional update from `Failed` to `UnHandled`, the failing time back to
  zero. The earlier failures' count and last exception stay, for display, until the next failed try replaces them. After
  the commit, the inbox enqueues the tessage at the end of its handling line.
- **Exactly once, by construction.** A row marked failed was never handled successfully: every try's unit of work rolled
  back. The claim still requires `UnHandled`, and the claim, the handler's work and the success mark still commit together.
  So the retry starts its first and only successful handling, whatever happens around it.
- **Two retries at once,** or one delivered twice: only the update that changes the row enqueues the tessage. The other
  finds nothing marked failed and succeeds, doing nothing.
- **The mark-failure window.** The inbox marks a tessage failed in the database, then releases it from its handling line. A
  retry between the two would enqueue the tessage while the line still holds it. So the retry is refused while the line
  holds the tessage, before it touches the row, and the caller tries again. Before the mark, the row is `UnHandled` and the
  retry finds nothing to do.
- **Out of order, as agreed.** Marking failed gave up the tessage's place in its pair's order. A retry handles it after
  whatever its pair's later tessages have reached. The pair's admission is untouched: the high-water mark never moves back,
  so the sender's stream goes on.
- **Crash windows.** Before the commit: still failed, nothing lost. After it and before the enqueue: an `UnHandled` row,
  which the next start's recovery scan enqueues, in admission order among the unhandled ones: out of order either way.
- **Restarts:** the failed rows wait in the table-set and are listed again.
- **Replacement topologies:**
  - **The endpoint's next process,** same `EndpointId`: the handle addresses the id, so a retry reaches the new process.
  - **A successor endpoint,** another `EndpointId`: a failed tessage is bound to the endpoint whose inbox admitted it, as an
    outbox row is bound to its receiver, and never moves. The retired endpoint's are for administration at rest.
  - **A handler removed or changed across a restart:** the retried tessage is handled by what the roster has now. With no
    handler, it fails each try until it is marked failed again.

### A notice

- **It changes no row and delays nothing:** the mark commits first, the notice follows off-thread. Delivery, admission and
  handling are untouched.
- **Lost at a crash, told again at the next start** in the endpoint's own process. Pulled notices that may have been
  missed are said with the gap notice.
- **A retry racing an observer** is the observer's domain question: a flow told that its step failed may end the operation
  while an administrator retries the step. The flow decides what a step succeeding after its operation ended means.

### The later acts, in outline

- **Archiving** moves a row to the archive table-set in one transaction of the same database, with the decision and who
  made it. It is never handled or delivered after that: at most once, by a recorded decision.
- **An archived or stranded outbox row must leave its pair's stream for good:** the declared predecessor computation
  (`GetDeliveryStreamPredecessorSequenceNumberAsync`) excludes it, as it excludes stranded rows today, or the rows behind it
  wait for ever.
- **Sending a stranded tessage again** can't reuse its sequence number: the receiver's high-water mark has passed it, and
  admission would take it for a duplicate. It goes at the end of its pair's stream with a new number, out of order. A
  stranded row was never received, so the receiver admits it once.
- **Forgetting a peer** refuses while any row is bound to it, undelivered or stranded. With that refusal, rows owed to a
  forgotten peer can't exist, which is what makes the first-contact assertion in `Outbox.PeerLifecycleObserver` a true
  invariant, as peer administration says.
- **At rest,** the process lock makes the act and the endpoint exclusive: the act sees the table-set exactly as the
  endpoint's last process left it, and the next process sees exactly what the act left.

## The hub's flow with increment 1

```csharp
class HubOperations : ExactlyOnceEndpointDeclaration<HubOperations>, IEndpointIdentity
{
   protected override void Declare(ExactlyOnceEndpointBuilder builder) => builder
      .ObserveAdministrationNotices(observe => observe
         //Told when the step is marked failed, and again at each later start while it stays failed: safe to repeat.
         .ForTessageMarkedFailed(async (IWorktreeRemovalStep step, IFailedTessage failed, IScopeResolver scope) =>
             await scope.Resolve<IIndependentTommandSender>().SendAsync(new EndTheRemovalAsFailed(step.RemovalId, failed.LastFailure))));
}
```

- **The hub drops its stand-in,** and its steps keep the retry policy's retries.
- **`EndTheRemovalAsFailed` does nothing for a removal that has ended,** since it is told again at each start while the step
  stays failed.
- **Retrying a step from the hub's own tooling** is `ThisEndpoint`'s `FailedTessagesAsync` and `RetryAsync`, until a UI
  exists.

## Beside the plane, not in it

- **Metrics and traces** belong to the BCL's `System.Diagnostics.Metrics` and `ActivitySource`, which every monitoring tool
  reads: Compze adds nothing that improves on them. Their place is the pipeline above the transport seam, an effort of its
  own.
- **Readiness** stays `IEndpoint.AwaitReadinessAsync`: the application's own wait at its start, for what it can reach.
- **The testing host's tessages-in-flight tracker** stays the tests' at-rest wait. It sees what is in flight between
  endpoints on a shared heap, which no endpoint's counts can.
- **Discovery** is the one conversation of the plane that exists today, served the same way. It stays open to every
  endpoint, since endpoints must find each other before anything else, so it keeps its request kind.

## Prior art

- **NServiceBus's ServiceControl** ([contracts](https://docs.particular.net/servicecontrol/contracts)) is an endpoint of its
  own that takes in failed messages and publishes `MessageFailed` for alerting. It retries by returning a message to the
  endpoint's queue. Tessaging can't: the endpoint owns its inbox rows, so the retry is the endpoint's own act, through its
  administration API.
- **Orleans's `IManagementGrain`**
  ([API](https://learn.microsoft.com/dotnet/api/orleans.runtime.imanagementgrain)) is "an interface for system management
  functions of silos, exposed as a grain for receiving remote requests or commands": the administration API is a
  participant of the system it administers, as Magnus remembers for Tessaging.
- **Akka Management** ([health checks](https://doc.akka.io/docs/akka-management/1.1.1/healthchecks.html)) is served by every
  node, with readiness and liveness checks, on an HTTP server of its own, usually on another network interface.

## What changes in the current-state documents

- **[Tessaging](../tessaging.md), "Peer memory and administration":** the administration surfaces become the plane,
  pointing here.
- **[Storage](../storage.md)** and the todo at `IEndpointCatalogSqlLayer`: the storage drop is increment 6's.
- **[Peers](../peers.md), "Removing peers from memory":** increment 5's.
- **The failed-tessage todo and peer administration** are absorbed into this design once it is agreed, at the merge of the
  tessaging-deferred-handling branch, whose helper still works on the first.
- **The public `_docs`:** a page on administration, with increment 1.

## Increments

Each builds with no warnings, passes the full suite on MsSql, PgSql, MySql, Sqlite and SqliteMemory, and is releasable
alone. Specified through the public API only.

1. **This endpoint's failed tessages, in its own process:** `ITessagingAdministration.ThisEndpoint`, `FailedTessagesAsync`,
   `RetryAsync`, `ObserveAdministrationNotices` with `ForTessageMarkedFailed`, and the mark's time on the inbox row. No
   wire. For the hub.
2. **The administration API on the wire:** the request kind, the fixed format, `DiscoveredEndpoints`, and authorization's
   default. For a UI or a tool, such as the hub's.
3. **Pulled notices,** with the gap notice.
4. **Work counts and the suite's quiescence,** each count tagged durable or volatile, as the evaluation sketched.
5. **The archive table-set, resolving per kind, and forgetting a peer.**
6. **Administration at rest,** and the storage drop.
7. **Authorization by act,** when a transport authenticates its callers.

Increment 1 waits for Magnus's agreement to the design. The others are built when a consumer needs them.

## Decisions for Magnus

1. **Where an endpoint's administration is served:**
   - **(a) By every endpoint itself,** on its one transport server, beside discovery, addressed by `EndpointId`.
     Recommended: the endpoint owns what is administered, no endpoint is added, and endpoints of the suite connect to
     nothing new. The administering side is itself a best-effort endpoint.
   - **(b) By an administration endpoint of its own in each process,** a best-effort endpoint with its own identity and
     address, administering the process's endpoints from inside it. It needs a process-level owner that Tessaging doesn't
     have, since endpoints are first-class and the host is optional, and it reaches into other containers. Every router
     would connect to it and remember it, unless it had a registry of its own.
   - **(c) By a central administration endpoint** that endpoints report to, as ServiceControl does. It still needs (a) to
     act, since only the endpoint can retry its tessages.
2. **The public contract:**
   - **(a) Interfaces,** `ITessagingAdministration`, `IEndpointAdministration`, `IFailedTessage`, over administration
     tessages that stay internal. Recommended: the wire stays Compze's to change, and a UI binds to interfaces as well as
     to resources.
   - **(b) Public typermedia resources and tessages,** navigated with a navigator, links and forms in the typermedia style.
     Every wire shape is then public API.
3. **Authorization's default, until a transport authenticates its callers:**
   - **(a) Served where the transport vouches for the caller,** the named pipes' current user, and elsewhere only where the
     endpoint declares it. Recommended.
   - **(b) Served on every transport,** as the application's tessages are today.
   - **(c) Never served unless the endpoint declares it.**

## Calls made on judgment, for Magnus to overrule

- **Remote notices are pulled, with every gap said,** not pushed to subscribers: an administered endpoint then holds
  nothing for an administering side, and a UI that comes and goes costs nothing.
- **An observer in the endpoint's own process is told again at each start** while a tessage stays marked failed: at least
  once across crashes, from the record, with no delivery machinery. The point from the other helper, notices that may be
  lost, holds for the remote reach; in-process it would leave a flow waiting for ever after a crash in a window of
  milliseconds.
- **Administration at rest under the endpoint's process lock,** which extends the storage-drop equation already settled.
- **A retry is idempotent,** handled at the end of the line, its failing time from zero, and refused while the line still
  holds the tessage, before the recovery scan and after the stop.
- **Failed tessages come in pages, with no order promised,** since an inbox may hold any number.
- **Metrics and traces are left to the BCL.**
- **"Administration plane"** for the concept, as the brief and the existing documents say, rather than "management bus".

## Not in this design

- **Handing a retired endpoint's failed tessage to its successor's inbox.** Possible within one domain database, outside any
  delivery stream, once a consumer needs it.
- **Pausing sends** ahead of an update: an application's act first, as the evaluation says.
- **Failover,** two processes for one endpoint: [Tessaging](../tessaging.md) leaves it to an effort of its own.
- **A UI.** Magnus expects a Blazor app, run in an endpoint, or an Avalonia app: either sits on `ITessagingAdministration`.
