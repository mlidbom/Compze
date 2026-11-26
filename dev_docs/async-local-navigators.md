# Asynchronous local navigators: what it takes, and what it buys

Magnus's item 40, 2026-11-26: tessaging-and-taggregates found the local navigators synchronous only, so an asynchronous
caller, such as an ASP.NET Core route, blocks a thread on each navigation. A helper of compze-agent-rep looked at what an
asynchronous form takes, on branch `async-local-navigators`, with two spikes.

## The headline

- **The asynchronous form is cheap, because everything it needs is asynchronous already.** Handlers are registered as
  asynchronous, the executor (`TessageHandlerExecutor`) is asynchronous end to end, and the unit-of-work envelope has
  asynchronous forms (`ExecuteUnitOfWorkAsync`, `ExecuteInIsolatedScopeAsync`) that the remote typermedia server uses
  already. The synchronous navigators are three `GetAwaiter().GetResult()` waits over that. Spiked in about 50 lines:
  `ExecuteAsync` beside each `Execute`, on both navigators.
- **It buys a lot when a handler awaits, and nothing when it doesn't.** 200 navigations at once whose handlers await a
  second: 1.0 s through `ExecuteAsync`, 11.6 s through `Execute` on a cold pool, which grew to 201 threads, and 42–58 s
  with other specifications loading the pool. A handler that completes synchronously holds the calling thread for its
  whole run either way.
- **The spike found a hazard that predates it.** A unit of work that awaits while it holds a taggregate of the memory
  image state store, with others waiting for that taggregate, starves the thread pool. The waiters block their pool
  threads in the store's synchronous update-lock wait, and the holder can't get a thread to resume on. 128 such
  navigations at once failed every time: every waiter timed out after 30 s. The synchronous navigator has the same
  hazard, as does any handler run through the asynchronous envelope, such as a remote typermedia tommand's.
- **An asynchronous update-lock wait removes it.** Spiked as `ITaggregateStoreUpdater.GetForUpdateAsync`: the same 128
  completed in 1.4–3.7 s, with the pool steady at 33 threads.
- **What still blocks beneath, by design:** the taggregate programming model itself (a taggregate's methods publish
  synchronously, and the stores and participation handlers react inline), the commit (`System.Transactions` is
  synchronous), and the stores' database I/O. These hold a thread for their own work, but they don't wait for another
  unit of work.

## Recommendation

1. **Build the asynchronous navigators, beside the synchronous ones,** as spiked. About half an agent-day to finish:
   the page `Typermedia/_docs/in-one-process.md`, Compze.Tessaging's changelog, and the specifications in their final
   shape. It breaks nothing.
2. **With them, the asynchronous lock waits of the taggregate stores:** `GetForUpdateAsync` and `TryGetForUpdateAsync` on
   `ITaggregateStoreUpdater`, for the memory image state store as spiked, and for the tevent store, whose lock is the
   database's, taken in a synchronous read of the history today. About 1.5 to 2 agent-days, mostly the tevent store's
   four SQL layers. Additive too.
3. **Not now:** making the stores' writes and the commit asynchronous, levels 3 and 4 below. They cost weeks, and change
   the delivery machinery or the unit of work's foundation. What they remove is a thread held for a database round trip,
   not a wait on other work, and nothing here has measured that mattering. Record them in
   `TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md` as what remains.

For the tooling as it is today, step 1 changes nothing measurable. Its handlers don't await (point 4 of its document), so
its routes would hold their thread for the same milliseconds either way. What step 1 gives it is a simple route for when
an operation's handler someday does await.

## The design: `ExecuteAsync` beside `Execute`

On `IIndependentLocalTypermediaNavigator` and `ILocalTypermediaNavigatorSession` alike:

```csharp
Task<TResult> ExecuteAsync<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;
Task<TResult> ExecuteAsync<TResult>(IStrictlyLocalTommand<TResult> tommand);
Task ExecuteAsync(IStrictlyLocalTommand tommand);
```

- **The same semantics as `Execute`:** the independent navigator's tommand is its own unit of work, committed before the
  task completes, and its tuery runs in a fresh scope with no transaction. The session's run in the caller's scope and
  unit of work, whose ambient transaction flows across the handler's awaits
  (`TransactionScopeAsyncFlowOption.Enabled`, which every Compze unit of work uses). The assertions are the same: no
  ambient transaction for the independent navigator, the caller's for the session's tommands.
- **Beside, not replacing:**
  - `IRemoteTypermediaNavigator` already has each operation both ways (`Post`/`PostAsync`, `Get`/`GetAsync`).
  - Synchronous callers are real: the tooling's command-line commands, specifications, and code driven by a taggregate's
    synchronous methods. Without `Execute`, each would write the `GetAwaiter().GetResult()` itself.
  - `Execute` costs nothing extra while the handler completes synchronously: it waits only while the handler awaits.
- **The analyzers lead asynchronous callers to it:** once `ExecuteAsync` exists, CA1849 and VSTHRD103 flag an `Execute`
  in asynchronous code. They flagged the AccountManagement sample's three exactly-once tevent handlers, which the spike
  moved to `ExecuteAsync`. The same will happen in consumers that enable these analyzers: warnings, not breaks.
- **No cancellation token.** Handlers take none, so a token could stop a navigation only before it starts. A token that
  reaches the handler means a token in the handler's signature, a decision of its own, and a cancelled tommand would
  only roll back.
- **What it would break:** nothing that compiles against Compze today, unless a consumer implements one of the two
  interfaces itself, which nothing known does. JAStudio uses neither navigator; the tooling uses
  `IIndependentLocalTypermediaNavigator`.

## What runs beneath a navigation, and what blocks

What an asynchronous navigation runs, top to bottom. Each item says whether it can hold a thread waiting on something
else (a wait), or only for its own work (its own work).

| What | Today | Holds a thread |
|---|---|---|
| The navigator's own wait on the handler's task | synchronous `Execute`: `GetAwaiter().GetResult()` | a wait, for the handler's whole run. Gone with `ExecuteAsync` |
| The executor, the handler roster, handlers | asynchronous | no |
| The scope: `BeginScope`, and disposal by `using` | synchronous. Microsoft's adapter blocks on `DisposeAsync` | only for a scoped component that disposes asynchronously. `container-disposal-of-async-only-components.md` covers it |
| The transaction's begin | synchronous, in memory | no |
| A taggregate store's session, at its first use in the unit of work | the memory image state store: load on first use, the gate against reloads, catching up with other memory images (database read) | its own work, and a wait while a reload runs |
| `GetForUpdate`: the update lock | the memory image state store: `SemaphoreSlim.Wait`, up to 30 s | **a wait on another unit of work.** The hazard below. `GetForUpdateAsync` removes it (spiked) |
| `GetForUpdate`: the read | the memory image state store: its lock in the storage (database). The tevent store: the history, read with the database's update lock | its own work. The tevent store's database lock wait is **a wait on another unit of work** too, inside the read |
| A taggregate's method publishing a tevent | synchronous by design: the tevent store writes it (database), and participation handlers run inline. `UnitOfWorkTeventPublisher.Publish` blocks on the handlers' task | its own work, and a wait on any participation handler that awaits |
| An exactly-once tevent published by a taggregate | `TaggregateUnitOfWork.Dispatch` blocks on `PublishAsync`, which writes the outbox (asynchronous database I/O) | a wait: "the one deliberate bridge" |
| A handler's own `PublishAsync`, outbox writes, remote navigation | asynchronous | no |
| The document DB, type-identifier interning | synchronous only | its own work |
| The commit: `TransactionScope.Dispose` | synchronous by design: the participants prepare (the memory image state store writes its rows), the database commits as the decision (`DatabaseTransactionAsTheDecision.SinglePhaseCommit`), the participants are told | its own work: a round trip or two |

Of the asynchronous parts, the executor and the envelope were built so ("async-capable from birth", the executor's
documentation says), and the transport server has run handlers through them all along. Of the synchronous ones, three
are so by design: the taggregate's synchronous methods, the inline dispatch to participation handlers, and
`System.Transactions`, which has no asynchronous commit at all.

### Its own work and waits

A thread held for its own work, a database round trip say, is ordinary blocking I/O. On Windows it blocks in the kernel,
and the thread pool grows around it. On Linux, `synchronous-paths-that-need-a-free-thread-pool-thread.md` found that a
synchronous socket read completes only when a pool thread handles the socket's event. So under a pool already starved,
every synchronous database call waits for the pool to grow as well. The spikes ran on Windows, with SQLite in memory, so
they say nothing about that case.

A thread held waiting on other work is what turns load into a stall. That work needs a thread to finish, and the waiting
threads are what took them.

### The hazard: a taggregate held across an await

Measured with the memory image state store kept in a SQLite in-memory domain database, 32 processors, a pool minimum of
32 threads. 128 navigations were started at once, each on a pool thread, as a route's is, and each adds a card to one
deck:

| The handler | 128 at once |
|---|---|
| gets the deck for update and adds the card, not awaiting | 0.14–0.23 s alone, 2.8 s beside other specifications |
| gets the deck for update, awaits 10 ms, adds the card | **fails every time, 4 runs of 4:** `TimeoutException` after 30 s, "Waited 00:00:30 for another unit of work to finish changing taggregate …" |
| the same, through `GetForUpdateAsync` | 1.4–3.7 s, the pool steady at 33 threads |

The waiters each block a pool thread in `TaggregateUpdateLocks.Take`. The holder's continuation after its await needs a
pool thread, and gets one only as the pool grows, about one a second past its minimum here. Then the next holder needs
one too.

It isn't new. `Execute` blocks the holder's calling thread on the handler, so its continuation needs a pool thread
just the same. The remote typermedia server runs handlers through the same envelope (`TypermediaHandlerExecutor`), so a
remote tommand handler that awaits while holding a taggregate has it too. The inbox runs exactly-once tommand and tevent
handlers one at a time (`TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint`), so it can't meet
itself there, only the other paths. The awaits that do this in practice are ordinary ones: a handler that changes a
taggregate and then awaits `PublishAsync` of an exactly-once tevent, or any asynchronous I/O of its own.

The tevent store has the same hazard by another lock: its waiters wait for the database's row lock inside a synchronous
read of the history, so they hold their threads the same way. Reasoned from the code, not run.

## How the remote navigators do it, and what that tells

- **The client, `IRemoteTypermediaNavigator`:** asynchronous at heart, with synchronous wrappers
  (`WaitUnwrappingException`, `ResultUnwrappingException`). Each operation exists both ways. That is the pattern the
  local navigators follow.
- **The server, `TypermediaHandlerExecutor`:** runs each arriving tommand through
  `TessageHandlerExecutor.ExecuteTommandHandlerWithResultInOwnUnitOfWorkAsync`, the asynchronous envelope. That is
  what `IIndependentLocalTypermediaNavigator.ExecuteAsync` now does in-process, minus the wire and the retry. So the
  remote path has always been "an asynchronous navigator honest about the parts that still block". Its handlers meet the
  same synchronous stores and commit beneath, and the hazard above.

So the remote navigators settle the design question: asynchronous beside synchronous, over the one asynchronous
executor, with the synchronous parts beneath left as they are and documented.

## The levels, and what each costs

The costs are guesses in agent-days, said as such.

1. **The asynchronous navigators** (spiked, `f196180c4`). Removes the navigator's own wait. To finish: the documentation
   page, the changelog, the specifications in their final BDD shape. **About half a day.** Breaks nothing.
2. **The asynchronous lock waits of the taggregate stores:** `GetForUpdateAsync` and `TryGetForUpdateAsync` (spiked for
   the memory image state store: `TaggregateUpdateLocks.TakeAsync` over `SemaphoreSlim.WaitAsync`). To finish: the tevent
   store's `GetTaggregateHistoryForUpdateAsync` in its four SQL layers, so that its database lock is waited for
   asynchronously; specifications for both stores; documentation. **About 1.5 to 2 days.** Breaks nothing; the analyzers
   lead asynchronous handlers from `GetForUpdate` to it.
   - Beyond the lock: the memory image state store's catch-up read and its lock in the storage could become
     asynchronous too, through asynchronous forms of `ITaggregateStorage` (tables and files), and its gate against
     reloads through Compze.Threading's `UpdateWhenAsync`. **About 2 to 3 days more.** That removes only a thread held for
     its own work.
3. **Asynchronous writes before the commit:** an asynchronous prepare phase in `ExecuteUnitOfWorkAsync`, before
   `TransactionScope.Complete`. The memory image state store's prepared write, the tevent store's writes, which it makes
   at each publish today and would hold until then, and the outbox rows of exactly-once tevents that taggregates publish
   would be written there. That leaves the commit's own round trip as the one synchronous step. **About 5 to 10 days.** It
   changes the delivery machinery, so it must first be walked through the exactly-once in-order guarantee. It changes
   when the tevent store's writes happen, too: a participation handler that reads the tevent store in the same unit of
   work would no longer find the tevents published before it ran. Whether anything relies on that, I don't know.
4. **An asynchronous commit:** replacing `System.Transactions` with a unit-of-work coordinator of Compze's own, with
   asynchronous prepare and commit. Every participant in Compze is built on it: `VolatileTransactionParticipant`,
   `DatabaseTransactionAsTheDecision`, `FilesAsADurableParticipant`, `TaggregateUnitOfWork`, the outbox, and the
   connection pool's connection for each transaction. **Weeks: 15 to 30 days.** It breaks consumers that use
   `TransactionScope` or `Transaction.Current` themselves. Not recommended.

The taggregate's synchronous methods and the inline dispatch to participation handlers stay at every level: making
those asynchronous would be another programming model, not a change to this one.

## The spikes, on this branch

- `f196180c4`, **Spike: ExecuteAsync on the local typermedia navigators:** the API, the sample's handlers, and
  `Given_a_container_composing_a_LocalTessagingEngine.AsynchronousTypermedia.cs` and
  `Given_a_memory_image_state_store_whose_deck_tommands_are_navigated_asynchronously.cs` in Compze.Tests.Integration.
  The two specifications that starve the pool are skipped, and their numbers are above. To see them again, run them on
  their own, without the skip.
- The next commit, **Spike: GetForUpdateAsync, waiting for the update lock asynchronously:**
  `ITaggregateStoreUpdater.GetForUpdateAsync`, the memory image state store's `TryGetForUpdateAsync` over
  `TaggregateUpdateLocks.TakeAsync`, and the tevent store's over its synchronous read, marked `wip:`. The deck
  specification's context through `GetForUpdateAsync` passes.

If the work goes ahead, the specifications to keep are the asynchronous forms' and the deck's three contexts. The
navigator's 200-at-once measurement could stay as a skipped measurement, or go.

## Open

- **Names, for Magnus:** `ExecuteAsync`, which mirrors `Execute` as `PostAsync` mirrors `Post`. `GetForUpdateAsync` and
  `TryGetForUpdateAsync`, whose `Task<TTaggregate?>` replaces the out parameter, since an asynchronous method can't
  have one.
- **Not measured:** PostgreSQL, MySQL or SQL Server, and Linux, where synchronous socket reads need a pool thread to
  complete. The spikes ran only on the default combination, SQLite in memory, on Windows.
