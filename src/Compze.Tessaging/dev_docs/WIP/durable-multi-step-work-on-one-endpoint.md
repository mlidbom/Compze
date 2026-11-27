# Durable multi-step work on one endpoint: design

## Status

A design, 2026-11-27, with Magnus's decisions at its end being put to him one at a time.

- **Decision 1** is settled by Magnus's words on topic 27: at the registration. Increment 2 waits for him to see the
  design.
- **Decision 2** was rewritten after Magnus's objection on topic 1, that a handler outside any transaction isn't an
  exactly-once handler: it now chooses between at-least-once tommands and none.
- **Change 5 and decision 6 are on hold:** Magnus asked on topic 2 whether an endpoint that converses with no other
  differs enough from one that simply has no peers to be worth building, then came to doubt his own conclusion, and
  holds the decision (2026-11-27).
- **Decision 3** is settled by Magnus on topic 3: per handler registration, "the feature that does everything"; for the
  endpoint as a whole can come later, as a convenience.
- **Decision 5** is settled by compze-agent-rep's judgment, which Magnus can overrule on his topic 73:
  `StopAsync(CancellationToken)`, keeping today's drain.
- **Increment 1 is built** (cancellation, and the stop that lets go), in three steps, with one change to the design
  below: the stop's token stops the inbox's handling the moment it is cancelled, in whichever phase the stop then is,
  not only in the drain. Stopping the listening waits for the requests in flight, and a running handler can hold one up:
  on SQLite, its transaction holds the one write lock that an arriving tessage's admission waits for, and the ASP.NET
  Core transport then waits out its 30-second shutdown timeout. A best-effort arrival's handlers get a token that is
  never cancelled, for now.

Asked for by the Claude tooling's hub: on topic 27 of its session, Magnus chose that the hub drives its long operations
through one exactly-once endpoint of its own, each operation's next step a tommand to that same endpoint. Forwarded by
compze-agent-rep, with Magnus's note that the tooling's four proposals are proposals, not requirements: design from the
need. Where this design departs from them, it says why.

## The headline

An exactly-once endpoint can orchestrate durable multi-step work by sending each step to itself once it has five things.
Each is a choice made where a handler is registered, or where an endpoint is declared or stopped; the defaults stay as
they are today:

1. **A tommand handler may run in a transaction of its own:** never in the sender's, always after the sender commits,
   through the endpoint's own inbox.
2. **An at-least-once tommand may run an outside effect outside any transaction:** the effect at least once, then the recording
   of its outcome exactly once, in the transaction that marks the tommand handled.
3. **A handler may give up its place in the endpoint's order,** so that its tessages are handled as soon as there is
   capacity, at the same time as anything else.
4. **Handlers receive a cancellation token, and an endpoint's stop can let go at once:** it cancels what its inbox is
   running, and leaves it for the endpoint's next start.
5. **An exactly-once endpoint may converse with no other endpoint:** no transport, no discovery, no peer registry and no
   database for one.

The mechanism behind 1 and 2: **a tessage an endpoint sends itself needs no delivery.** It is admitted to the endpoint's
own inbox inside the sender's transaction: no outbox row, no delivery stream, no transport.

With 1 and 2, the consistency law changes: a handler's registration, not only which endpoint it lives in, decides whether
it runs in the sender's execution. That is the first decision below.

## The need

The hub's long operations, such as removing a worktree or reloading sessions, must:

- **be durable step by step:** each step's outcome is recorded, and the next step is sure to run after it, across
  crashes;
- **run in parallel:** ten removals don't queue behind one 60-second git step;
- **survive the endpoint's replacement:** at a publish, the new hub process completes what the old one didn't;
- **stop promptly:** the old hub lets go at once, rather than wait for a running step;
- **start cheaply:** the new hub's start at a publish is on the path of every hook.

A step's outside effect, such as a git worktree removal, can outlast the unit of work's 45-second transaction. On SQLite a
handling transaction also holds the database's one write lock from its claim to its end (the claim is a write:
`SqliteInboxSqlLayer.TryClaimForHandlingAsync`). So such an effect can't run inside a handling transaction.

## What stands in the way today

- **A tommand whose handler is in the sender's roster runs inline,** in the sender's transaction
  (`UnitOfWorkTommandSender`). Nothing an endpoint sends itself is stored, retried or redelivered.
- **The inbox handles one tessage at a time, endpoint-wide.** Its one dispatching rule,
  `TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint`, dispatches nothing while anything executes.
- **Every handling runs in a unit of work that claims the tessage's row first** (`HandlerExecutionTask.TryHandleAsync`).
- **Handlers get no token, and the stop drains:** it waits up to 30 seconds for everything received to be handled
  (`Coordinator.DrainAndStopAsync`). The coordinator's stopping token ends only the waits between tries.
- **An endpoint must declare a transport** (`EndpointBuilder.AssertTheFoundationIsDeclared`), and on SQLite its peer
  registry needs a database of its own.

## How the hub's flow would use it

```csharp
class HubOperations : ExactlyOnceEndpointDeclaration<HubOperations>, IEndpointIdentity
{
   public static string Name => "Hub.Operations";
   public static EndpointId Id { get; } = new(Guid.Parse("..."));

   protected override void Declare(ExactlyOnceEndpointBuilder endpoint) => endpoint.ConverseWithNoOtherEndpoint();

   protected override void RegisterExactlyOnceTommandHandlers(IExactlyOnceTommandHandlerRegistrar handle) => handle
      //Sent by the hub's front: runs in the front's unit of work, so the operation and its first step commit together.
      .ForTommand<StartWorktreeRemoval>(async (start, unitOfWork, cancellation) =>
       {
          unitOfWork.Resolve<IWorktreeRemovals>().Add(new WorktreeRemoval(start.RemovalId, start.Worktree));
          await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(new RemoveTheWorktreesFiles(start.RemovalId, start.Worktree), cancellation);
       });

   //RemoveTheWorktreesFiles and DeleteTheWorktreesBranch are IAtLeastOnceTommands (change 2).
   protected override void RegisterAtLeastOnceTommandHandlers(IAtLeastOnceTommandHandlerRegistrar handle) => handle
      //The 60-second git step: outside any transaction, at least once; its outcome and the next step, exactly once.
      .ForTommand<RemoveTheWorktreesFiles, GitOutcome>(
          outsideEffect: async (step, scope, cancellation) => await scope.Resolve<IGit>().RemoveWorktreeAsync(step.Worktree, cancellation),
          recordOutcome: async (step, outcome, unitOfWork, cancellation) =>
          {
             var removal = await unitOfWork.Resolve<IWorktreeRemovals>().GetForUpdateAsync(step.RemovalId);
             removal.FilesRemoved(outcome);
             await unitOfWork.Resolve<IUnitOfWorkTommandSender>().SendAsync(new DeleteTheWorktreesBranch(step.RemovalId), cancellation);
          },
          HandlingOrder.None)
      .ForTommand<DeleteTheWorktreesBranch, GitOutcome>(..., HandlingOrder.None);
}
```

- **Ten removals** are ten `RemoveTheWorktreesFiles` in the inbox, each `HandlingOrder.None`: they run at once, up to the
  inbox's 20 at a time. One operation's steps stay in order by themselves: each sends the next only as it commits.
- **At a publish,** the old hub calls `StopAsync` with a cancelled token. The running git steps are cancelled and left
  unhandled, and the process lock is released once each has ended. The new hub builds and starts the endpoint, and its
  inbox's recovery scan runs them again.
- **After a crash,** the same: what wasn't recorded as handled runs again, the git steps at least once, the recordings
  exactly once.
- **The hub writes no machinery of its own** for resuming, retrying, marking failed or deduplicating.

## Where this departs from the tooling's proposals

- **Parallelism per handler, not for the endpoint as a whole.** The endpoint keeps the order for everything whose handler
  doesn't give it up, so the hub can later receive ordered tessages from other endpoints without being split in two. It
  costs one argument per registration.
- **The outside effect and the recording of its outcome are two parts of one registration,** rather than a handler that
  runs outside any transaction and records through an immediate tommand. With one part, a crash between the recording's
  commit and the tommand being marked handled runs the effect and the recording again, so the recording, too, must be
  safe to repeat. With two, the recording commits together with the mark: only the effect ever repeats.
- **The stop lets go when its token says so,** rather than always cancelling handlers. Cancelling always would end
  today's graceful drain, which a rolling restart or a change of serialization format wants. A token on `StopAsync` lets
  each caller choose, as `IHostedService.StopAsync` does.
- **No outbox, no delivery stream and no transport for what an endpoint sends itself,** where the tooling asked for
  handling through the endpoint's own outbox and inbox. The outbox exists to cross a boundary, and there is none to cross.

## 1. Handling in a transaction of its own

### Public API (sketch)

```csharp
public interface IExactlyOnceTommandHandlerRegistrar
{
   ///Today's verb, with a token: in the sender's transaction when sent from this endpoint, in the inbox's own when sent from another.
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler,
                                                            HandlingOrder order = HandlingOrder.AsAdmitted) where TTommand : IExactlyOnceTommand;

   //review-naming:
   ///Never in the sender's transaction: the inbox handles it in a transaction of its own, after the sender commits, wherever it was sent from.
   IExactlyOnceTommandHandlerRegistrar ForTommandInATransactionOfItsOwn<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler,
                                                                                  HandlingOrder order = HandlingOrder.AsAdmitted) where TTommand : IExactlyOnceTommand;
}
```

`TessageBusHandlerRegistrar` gets the same verbs. A composition with no inbox, a plain container's engine or a
best-effort endpoint, refuses such a registration at build, as it refuses handlers demanding exactly-once delivery today.

### Mechanism: admitted to the inbox in the sender's transaction

`UnitOfWorkTommandSender.SendAsync`, for a tommand whose handler is in the roster and registered in a transaction of its
own, hands it to the inbox instead of running it:

1. **Inside the sender's transaction,** the inbox advances the admission counter of the endpoint's stream to itself: the
   row of the admissions table whose sender is the endpoint itself. It inserts the tessage's row, UnHandled, with that
   sequence number, and reserves the tessage's place at the end of the handling line, not yet ready.
2. **At the sender's commit,** the place becomes ready. **At its rollback,** the place is withdrawn, and the row and the
   counter's advance roll back with the sender.
3. **The inbox handles it as any tessage it received:** in a unit of work that claims the row, runs the handler and marks
   it succeeded, tried again while it fails for as long as the endpoint's `RetryPolicy` allows.

The inbox refuses such a send before it has run its recovery scan at start, and after the endpoint has stopped. Before the
scan, the recovered tessages must enter the line first, being older. After the stop, the endpoint's next process may
already own its inbox.

### Exactly-once in order, walked through

- **Exactly once, by construction.** The row commits in the sender's transaction, so it exists exactly when the send
  happened. The claim and the handler's work commit together, so it is handled once. No delivery exists to fail, retry
  or deduplicate.
- **In order, by construction.** The counter row's lock is held from the send to the sender's commit, so the
  transactions that send to the endpoint itself commit one after another: sequence order is commit order. The place in
  the line is reserved while the lock is held, so the line's order is commit order too, whatever order the commit hooks
  then run in. The line never hands out an ordered tessage while an earlier one of the same pair is still reserved. The
  recovery scan's order, by the inbox table's `GeneratedId`, is the same, since the inserts are made under the same lock.
- **Other pairs don't wait for a reserved place:** the order across pairs is promised nowhere, today or here.
- **What it costs:** on the server engines, transactions that send to the endpoint itself take turns at the counter row,
  as transactions sending to one peer take turns at the outbox's counter row today. SQLite's one writer serializes them
  anyway.
- **Crash windows.** Before the sender's commit, nothing is left. After the commit and before the hook, an UnHandled row,
  which the recovery scan enqueues. During handling, a rolled-back try and the same UnHandled row.
- **Marking failed, as today.** An ordered tessage that keeps failing holds the ordered line until the `RetryPolicy` gives
  up. Marking it failed gives up its place, recorded on its row. For a flow, that is where it stops, visibly, until an
  administrator retries it (`TODO/failed-tessage-administration.md`).
- **Restarts.** The endpoint's next process finds the rows in its table-set, and its recovery scan enqueues them in order,
  with the failing time each recorded.
- **Replacement topologies:**
  - **The endpoint's next process,** same `EndpointId`: the process lock hands over at the stop and the start, and the
    rows wait in the table-set. A claim is refused while another process holds the lock, so the successor starts once
    the predecessor's stop has returned.
  - **A successor endpoint,** another `EndpointId`: a tessage an endpoint sent itself is bound to it, as an outbox row is
    bound to its receiver. It never moves to a successor. Rows left behind are the retired endpoint's, for
    administration (`WIP/peer-administration.md`).
  - **A registration changed across a restart:** a waiting row is handled by the handler the roster now has for its type,
    however that is now registered. A removed handler fails each try with the no-handler exception until the row is
    marked failed: drain the endpoint before removing a handler whose tessages wait.

### What the existing handling policies give

The inbox's dispatch loop already handles several tessages at once, up to `MaxConcurrentlyExecutingHandlers` (20), each on
a task of its own, and consults a list of dispatching rules (`ITessageDispatchingRule`) for each waiting tessage. Change 1
needs nothing more of it but the reserved place: today an entry is ready when it is enqueued.

## 2. At-least-once tommands, handled outside any transaction

No longer recommended: see decision 2. With SQLite left behind and a registration's own transaction timeout, a long
step is an ordinary handler in a transaction of its own, and this rung has hardly any motivation left (Magnus, topic 1).

Magnus, on topic 1, 2026-11-27: "A handler outside any transactions is not an ExactlyOnceTommandHandler." The first draft
registered such a handler on the exactly-once registrar. The type hierarchy already has the rung that tells the truth:
`IAtLeastOnceTessage`, "delivered until it arrives, and whose handling is retried while it fails", without the
transactional handling `IExactlyOnceTessage` adds.

### Public API (sketch)

```csharp
//review-naming:
///A tommand delivered exactly once to its receiver's inbox, and handled at least once: its handler's effect runs outside any
///transaction, and runs again when a try fails or a crash or a stop cuts it off, so it must be safe to repeat.
public interface IAtLeastOnceTommand : IAtLeastOnceTessage, IMustBeSentTransactionally, ITommand;

//review-naming:
public interface IAtLeastOnceTommandHandlerRegistrar
{
   ///The inbox runs outsideEffect outside any transaction, then recordOutcome in the unit of work that marks the tommand handled.
   IAtLeastOnceTommandHandlerRegistrar ForTommand<TTommand, TOutcome>(
      Func<TTommand, IScopeResolver, CancellationToken, Task<TOutcome>> outsideEffect,
      Func<TTommand, TOutcome, IUnitOfWorkResolver, CancellationToken, Task> recordOutcome,
      HandlingOrder order = HandlingOrder.AsAdmitted) where TTommand : IAtLeastOnceTommand;
}
```

The tommand senders accept it beside `IExactlyOnceTommand`. Sent from the endpoint itself, it is admitted to the inbox as
in change 1: it never runs inline, since its effect can't run in the sender's transaction. Sent from another endpoint, it
travels the outbox and arrives in the inbox as an exactly-once tommand does today.

### Mechanism

1. **The inbox dispatches it as any tessage,** in its place in the order its registration chose.
2. **The outside effect runs in a scope with no ambient transaction,** with the endpoint's stopping token. The row isn't
   claimed: nothing else can handle it at the same time, since one process runs the endpoint (the process lock), the
   line hands a tessage out once, and the stop keeps the process lock until the effect has ended (decision 4).
3. **The recording runs in a unit of work that claims the row, records the outcome and marks the tommand handled:** one
   commit.
4. **A failure of either part is a failed try,** and the next try runs both parts again: the effect's outcome is not kept
   between tries.

### The guarantee, walked through

- **The effect: at least once, never twice at the same time.** It must be safe to repeat: the contract the tommand's type
  declares, which its sender sees.
- **The recording: exactly once,** atomically with the mark.
- **The tommand: admitted exactly once,** deduplicated and in its pair's order, as every tessage the inbox admits.
- **Crash windows.** During the effect: the row stays UnHandled, and the effect runs again at the next start. After the
  effect and before the recording commits: both run again. After the recording commits: nothing repeats.
- **Order.** An ordered one holds the ordered line for as long as its effect runs, which is why the hub gives the order up.
- **Marking failed, restarts and replacement topologies:** as in change 1.

## 3. Handling order

### Public API (sketch)

```csharp
//review-naming:
///Whether a handler's tessages keep their place in the endpoint's order.
public enum HandlingOrder
{
   ///The default: handled one at a time, in admission order, with every other tessage of the endpoint whose handlers keep
   ///their place. One that keeps failing holds up those behind it.
   AsAdmitted,

   ///Handled as soon as there is capacity, at the same time as anything else, whatever was admitted before it. One that
   ///keeps failing holds up nothing.
   None
}
```

An argument of every handler verb, as sketched in changes 1 and 2. A tevent's handlers run together in one unit of work,
so a tevent's tessage gives up its place only when every handler it runs gives it up.

### Mechanism

One dispatching rule replaces today's: a waiting tessage may be dispatched when it is ready, and either its order is
`None`, or no `AsAdmitted` tessage is executing and none of its pair's is reserved ahead of it. Each tessage's order is
looked up in the roster as it is enqueued.

### The guarantee, walked through

- **Exactly once: unchanged.** Admission deduplicates, and the claim keeps a tessage to one handling.
- **Admission: unchanged,** still in stream order per pair. Only the order of handling relaxes.
- **For whom.** A peer that sends a tessage whose handler gives up its place loses in-order handling for that tessage, and
  only for it: its other tessages keep their order among themselves. The tessage's type doesn't say so; the receiving
  handler's registration does, as for the choices of changes 1 and 2.
- **At the same time as other handlers.** A handler that gives up its place runs at the same time as any handler of the
  endpoint, so what it shares with them in memory needs locking; the database's isolation covers the rest. On SQLite the
  transactional ones still take turns at the one writer; the outside effects of change 2 run in parallel.
- **Marking failed.** A tessage that gives up its place and keeps failing holds up nothing, and is marked failed when the
  `RetryPolicy` gives up.
- **Restarts.** The recovery scan enqueues in admission order, and those whose handlers gave up their place are
  dispatched as capacity allows.

### The alternatives

- **The endpoint as a whole,** one declaration making every handler `None`: smaller (Magnus: "an even smaller change"),
  but it gives up the order for everything the endpoint receives, from peers whose tessages need it too, and an endpoint
  needing both must be split.
- **By a key the tessage gives,** such as the taggregate it concerns: the most expressive, with `AsAdmitted` and `None`
  as its two extremes. No consumer needs it yet.

## 4. Cancellation

### Public API (sketch)

- **Every TessageBus handler verb's delegate gains a trailing `CancellationToken`,** as sketched above. Today's shapes stay,
  as convenience overloads in `TessageBusHandlerRegistrarCE`, for handlers that wait on nothing cancellable.
- **The TessageBus senders and publishers take a token,** last and defaulted, as .NET does:
  `IUnitOfWorkTommandSender.SendAsync(tommand, cancellationToken)`, and the same for `IIndependentTommandSender`,
  `IUnitOfWorkTeventPublisher.PublishAsync` and `IIndependentTeventPublisher.PublishAsync`. It reaches every handler the
  send or publish runs in the caller's execution, and the wait for a bindable receiver.
- **The stop takes a token:**

```csharp
public interface IEndpoint
{
   ///Stops the endpoint. Its inbox first handles what it has received, for up to 30 seconds. Cancelling the token ends that
   ///at once: the handlers its inbox is running are cancelled, and what is left waits for the endpoint's next start.
   Task StopAsync(CancellationToken cancellationToken = default);
}
```

### Which token a handler receives: the one of the execution it serves

- **Handling by the inbox,** of what arrives from other endpoints and of what the endpoint sends itself: the endpoint's
  stopping token, cancelled when the stop's grace ends.
- **Participation,** an inline tommand handler or a tevent handler in its publisher's unit of work: the token of the
  execution it takes part in. That is the stopping token inside a handling by the inbox, and otherwise the token the
  caller passed to the send or the publish.
- **Best-effort arrivals:** the endpoint's stopping token. Not built in increment 1: their handlers get a token that is
  never cancelled, since nothing yet tells the best-effort dispatch that the endpoint is stopping.
- **Typermedia handlers:** unchanged here. [Remote typermedia calls: deadlines, cancellation and
  stopping](../TODO/remote-typermedia-calls-deadlines-cancellation-and-stopping.md) would give them the caller's token,
  carried across the wire, together with the endpoint's stop.
- **Observers:** none. Observation is drained at the stop, never discarded.

The rest of Compze's public asynchronous members are [the cancellation
sweep](../../../../dev_docs/TODO/URGENT-cancellation-tokens-missing-from-the-public-apis.md)'s. This design answers two of
its questions to settle first: whose token handlers receive, and what cancelling one means under exactly-once.

### What a cancelled handling means under exactly-once

- **The handler's unit of work rolls back, and the tessage stays UnHandled.** That isn't a failed try: it is the endpoint
  stopping, not the tessage failing. The next start handles it again, in its place, as it handles a tessage that waited to
  be tried again at the stop today.
- **An `OperationCanceledException` while the endpoint isn't stopping** is an ordinary failure.

### The stop, step by step

1. Retract the address, stop sending, stop listening: as today.
2. **The drain:** the inbox handles what it has received until all of it is handled, 30 seconds have passed, or the token
   is cancelled. Cancelling the token stops the inbox's handling at once, whichever step the stop is at, as built in
   increment 1. What the endpoint sends itself during the drain waits for the next start: otherwise a flow whose steps
   each send the next would keep the drain going.
3. **Stop handling:** cancel the running handlers. Wait for the transactional ones up to the drain's patience, as today:
   a successor that tries one again waits at the row's claim until the predecessor's transaction ends. Wait for the
   outside effects until they end (decision 4).
4. Release the process lock: as today.

## 5. An endpoint that converses with no other

### Public API (sketch)

```csharp
public sealed class ExactlyOnceEndpointBuilder
{
   //review-naming:
   ///Declares that the endpoint converses with no other endpoint: it has no transport, discovers and announces nothing, and
   ///remembers no peers. Every tessage it handles is one it sends itself.
   public ExactlyOnceEndpointBuilder ConverseWithNoOtherEndpoint();
}
```

Declared in the endpoint's declaration, through its `Declare` override: conversing with nobody is what the endpoint is,
not a choice of its deployment. The environment's transport and discovery declarations are then left unused, so one
environment serves it and its process's other endpoints alike.

### What it leaves out, and what it keeps

- **Left out:** the transport server and client, the router's connections, discovery and announcing, the durable peer
  registry and its database (an empty process-lifetime registry instead), and the outbox with its tevent delivery legs:
  nothing crosses the boundary, so publishing is participation alone, the deliberately in-process composition.
- **Kept:** the engine, the inbox, the serializer and the type map, since the inbox stores serialized tessages, and the
  type-id interner, with its database on SQLite.
- **A tommand whose handler isn't in the roster fails at once,** saying that the endpoint converses with no other: no
  30-second wait for a handler that can't appear.
- **Guarantees: unchanged.** Nothing crosses the boundary.
- **The start's cost:** measured in its increment, the hub's composition today (named pipes, the registry, three SQLite
  databases) against one that converses with no other endpoint.

## The tevent counterparts

`ForTeventInATransactionOfItsOwn` and `ForTeventOutsideAnyTransaction` would follow the same design:

- **A local publish** admits the tevent to the inbox in the publisher's transaction when such a handler matches it. Its row
  runs only those handlers, since the participating ones ran in the publisher's unit of work.
- **An arrival from another endpoint** runs all of the endpoint's handlers in the inbox's unit of work, as today.
- **On the delivery ladder,** such a local subscriber takes the exactly-once rung instead of participation: one rung down,
  where the delivery model today lets a subscription opt down only all the way, to observation.

Designed so that they fit, and built when a consumer needs them. The hub doesn't.

## What changes in the current-state documents

- **[Tessaging](../tessaging.md), the consistency law:** "In-boundary is immediate and transactional" becomes "unless the
  handler's registration takes it out of the sender's execution, into a transaction of its own or outside any". The law's
  "It is not a mode, not an endpoint type, and has no opt-out" goes.
- **[The tevent delivery model](../tevent-delivery-model.md), "Tommands are different":** the handler's registration now
  has a say. "Ordering" gains the handling order.
- **The public `_docs`:** the new verbs, the handling order, the tokens, the stop and the endpoint that converses with no
  other.

## Increments

Each builds with no warnings, passes the full suite on MsSql, PgSql, MySql, Sqlite and SqliteMemory, and is releasable
alone:

1. **Cancellation, and a stop that lets go:** the tokens of change 4, in the handler verbs and the TessageBus senders and
   publishers, and `IEndpoint.StopAsync(CancellationToken)`.
2. **Tommand handlers in a transaction of their own:** change 1.
3. **The handling order:** change 3.
4. **At-least-once tommands, handled outside any transaction:** change 2.
5. **The endpoint that converses with no other:** change 5, with the start's cost measured before and after.

Later, when a consumer needs them: the tevent counterparts, the order by a key, and a successor's claim that waits for its
predecessor's release (below).

## Decisions for Magnus

1. **May a handler's registration take it out of the sender's execution?**
   - **(a) Yes, at the registration, for tommands now and tevents later.** Recommended: which endpoint a handler lives in
     stays what decides, now with how it is registered there, and the choice is visible where the handler is declared.
   - **(b) Yes, but by the tommand's type,** such as a marker interface meaning "handled in a transaction of its own": the
     sender sees it in the type, and moving the handler never changes the sender's consistency. It mirrors
     `IStrictlyLocalTessage`, the type-level promise in the other direction. Tevents can't have it: a publisher mustn't
     decide what its subscribers need.
   - **(c) No:** the law stays, and multi-step flows ping-pong between two endpoints.
2. **A long outside effect, such as a git step** (rewritten twice after Magnus's objections on topic 1: that a handler
   outside any transaction isn't exactly-once handling, then that SQLite should go and a registration could set its
   timeout):
   - **(a) A handler's registration sets its transaction timeout,** and no at-least-once tommands. On a server engine a
     long step is then an ordinary handler in a transaction of its own (change 1), its order given up (change 3), and its
     claim locks only its own inbox row. What the long transaction still costs, a pooled connection, its locks and on
     PostgreSQL an old snapshot, doesn't matter at the hub's scale. Today's timeout is one 45-second backstop for every
     unit of work (`TransactionScopeCE.TransactionTimeout`); above 10 minutes, `TransactionManager.MaximumTimeout` must be
     raised too, process-wide. For the hub this means leaving SQLite (the tooling's topic 26). Recommended.
   - **(b) At-least-once tommands,** as change 2 sketches.
3. **Where the order is given up:**
   - **(a) Per handler registration.** Recommended.
   - **(b) For the endpoint as a whole.**
4. **An outside effect still running when the stop's grace has ended:**
   - **(a) The process lock is kept until it ends.** Recommended: an effect never runs twice at the same time. A hung
     effect delays the successor's start, loudly.
   - **(b) The lock is released after the drain's patience, as for transactional handlers:** the successor starts on time,
     and may run the effect while the predecessor still does.
5. **How the stop lets go:**
   - **(a) `StopAsync(CancellationToken)`:** the drain as today, ended at once by the token. Recommended: the .NET
     convention, and each caller chooses.
   - **(b) The stop always cancels running handlers,** without a drain.
6. **How an endpoint that converses with no other is declared:**
   - **(a) By the endpoint,** `ConverseWithNoOtherEndpoint()` in its declaration, the environment's transport and
     discovery left unused. Recommended: it is what the endpoint is.
   - **(b) By the environment leaving out the transport:** absent means no wire, as an absent discovery registry means no
     discovery today.

The names marked `review-naming:` above are chosen, not decisions: `ForTommandInATransactionOfItsOwn`,
`IAtLeastOnceTommand` with its registrar, `HandlingOrder` with `AsAdmitted` and `None`, and `ConverseWithNoOtherEndpoint`.

## Not in this design

- **A successor's claim that waits for its predecessor's release.** Today a claim finding the process lock held is refused
  at once. The hub starts the new endpoint once the old one's stop has returned, so it needs no wait; a claim that waits
  would let the two processes overlap.
- **The type-id interner's database.** An endpoint that converses with no other still has it on SQLite, since the inbox
  interns type ids. Interning every mapped type at start, before any transaction, could let it live in the domain
  database.
- **Declaring how many tessages the inbox handles at once.** 20, fixed, suits the hub.
- **"A disposed store that refuses writes,"** on topic 27's list of what the hub needs from Compze: not in this brief.
