# Asynchronous local navigators and getting taggregates for update asynchronously

Magnus's item 40, 2026-11-26: tessaging-and-taggregates found the local navigators synchronous only, so an asynchronous
caller, such as an ASP.NET Core route, blocks a thread on each navigation. A helper of compze-agent-rep looked at what an
asynchronous form takes, with two spikes, on branch `async-local-navigators`. Magnus's go, the same day (item 48,
forwarded by compze-agent-rep): build levels 1 and 2 below, and not 3 or 4. Both are built on the branch.

## What was built

1. **`ExecuteAsync` beside each `Execute`,** on `IIndependentLocalTypermediaNavigator` and
   `ILocalTypermediaNavigatorSession`, with `Execute`'s semantics. Documented in "Typermedia in one process"
   (`src/Compze.Tessaging/Typermedia/_docs/in-one-process.md`).
2. **`GetForUpdateAsync` and `TryGetForUpdateAsync` on `ITaggregateStoreUpdater`,** which wait while another unit of work
   holds the taggregate holding no thread:
   - the memory image state store waits for its lock of the taggregate with `SemaphoreSlim.WaitAsync`, and reads nothing;
   - the tevent store waits for the taggregate's lock in its database, and reads its history, asynchronously, in each of
     its four SQL layers: SQL Server's `sp_getapplock`, MySQL's row in the lock table, PostgreSQL's advisory lock, and
     SQLite's write gate, which a transaction now takes asynchronously when its first command is created asynchronously
     (`ICompzeDbConnection.CreateCommandAsync`).

The synchronous forms stay as they were. The changelogs of the packages concerned say what each changed.

## What it buys

- **The navigator:** while a handler awaits, `ExecuteAsync` holds no thread. 200 navigations at once, each started on a
  pool thread, whose handlers await a second: 1.0 s through `ExecuteAsync`; 11.6 s through `Execute` on a cold pool,
  which grew to 201 threads; 42–58 s through `Execute` beside other specifications. A handler that completes without
  awaiting holds its thread for its whole run either way, which is the tooling's case today.
- **Getting for update:** a unit of work that awaits while it holds a taggregate, with others waiting for that taggregate,
  starved the thread pool through `GetForUpdate`. The waiters held their pool threads in the store's lock wait, and the
  holder got no thread to resume on. 128 such units of work at once, through the navigator, on the memory image state
  store, failed 4 runs of 4: each waiter timed out after 30 s. Through `GetForUpdateAsync` the same 128 completed in
  1.4–3.7 s, with the pool steady at 33 threads. The hazard is the same whether the unit of work is begun by
  `ExecuteAsync`, by `Execute`, or by the remote typermedia server, which runs handlers through the same envelope.

Measured on Windows, 32 processors, SQLite in memory. The specifications of both stores' asynchronous forms ran on all
five SQL layers.

## What still blocks beneath a navigation

Each part either holds a thread for its own work, a database round trip say, or holds it waiting on other work, which is
what turns load into a stall: that other work needs a thread to finish, and the waiting threads took them.

| What | Holds a thread |
|---|---|
| `Execute`'s wait on the handler's task | waiting, for the handler's whole run. `ExecuteAsync` doesn't |
| The executor, the handlers, the unit-of-work envelope | no |
| Disposing the scope, through `using` | only for a scoped component that disposes asynchronously: `TODO/container-disposal-of-async-only-components.md` |
| A taggregate store's first use in a unit of work | the memory image state store loads on first use, and waits while a reload runs |
| `GetForUpdate`'s lock | **waiting on another unit of work.** `GetForUpdateAsync` doesn't |
| A taggregate's method publishing a tevent | synchronous by design: the tevent store writes it, and participation handlers run inline. `UnitOfWorkTeventPublisher.Publish` waits for handlers that await |
| An exactly-once tevent a taggregate publishes | waiting: `TaggregateUnitOfWork.Dispatch` blocks on `PublishAsync`, which writes the outbox, "the one deliberate bridge" |
| A handler's own `PublishAsync`, outbox writes, remote navigation | no |
| The document DB, type-identifier interning | their own work: synchronous only |
| The commit, `TransactionScope.Dispose` | its own work: `System.Transactions` has no asynchronous commit. The participants prepare, the memory image state store writes, the database commits as the decision |

On Windows a thread held for its own work blocks in the kernel, and the pool grows around it. On Linux,
`TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md` found that a synchronous socket read completes only when
a pool thread handles the socket's event, so under a pool already starved every synchronous database call waits for the
pool to grow too. Nothing here measured that case.

## Found on the way: the tevent store's pool deadlocks when more units of work wait for one taggregate than it has connections

On PostgreSQL and MySQL, 128 units of work at once, each getting one user of the tevent store for update, deadlocked the
connection pool of ten: those waiting for their first connection timed out after 15 s. Synchronous `GetForUpdate` on
threads of their own, with no await, failed the same way, so it predates this work. The unit of work holding the
taggregate's lock needs a second connection, and those waiting for the lock in the database hold all ten:

- **PostgreSQL:** the history is read outside the transaction (suppressed), on a fresh connection, after the advisory
  lock is taken. Marked with a `bug:` comment at `PgSqlTeventStoreSqlLayer.GetTaggregateHistory`.
- **MySQL:** a second connection not yet located; the type-identifier interner, which also runs outside the transaction,
  is the first suspect.

The specification of the tevent store's asynchronous contention stays at eight units of work, below the pool's size.

Running all five SQL layers' combinations in one process at once also failed three of main's specifications on MySQL,
and the memory image state store's contention specification on PostgreSQL. Each combination run on its own passed
everything, 412 of 412. CI runs one SQL layer per job.

## Not built: the levels beyond

3. **Asynchronous writes before the commit:** an asynchronous prepare phase in `ExecuteUnitOfWorkAsync`, before
   `TransactionScope.Complete`, where the memory image state store's prepared write, the tevent store's writes and the
   outbox rows of exactly-once tevents that taggregates publish would be written. About 5 to 10 agent-days, a guess. It
   changes the delivery machinery, so it would first be walked through the exactly-once in-order guarantee. It changes
   when the tevent store's writes happen too: a participation handler reading the tevent store in the same unit of work
   would no longer find the tevents published before it ran.
4. **An asynchronous commit:** a unit-of-work coordinator of Compze's own in place of `System.Transactions`, on which
   every participant in Compze is built. Weeks, 15 to 30 agent-days, a guess. It breaks consumers that use
   `TransactionScope` or `Transaction.Current` themselves.

What either would remove is a thread held for a database round trip, not a wait on other work, and nothing has measured
that mattering. The taggregate's synchronous methods and the inline dispatch to participation handlers stay at every
level: making those asynchronous would be another programming model.

## How the remote navigators relate

`IRemoteTypermediaNavigator` is asynchronous at heart, with synchronous wrappers: each operation both ways, the pattern
the local navigators now follow. Its server, `TypermediaHandlerExecutor`, runs each arriving tommand through the
asynchronous envelope, which is what `IIndependentLocalTypermediaNavigator.ExecuteAsync` does in-process.

## The spikes

`f196180c4` (the navigators) and `3f34583b9` (the memory image state store's asynchronous lock wait) on this branch hold
the measurements above as specifications: the 200 navigations through `Execute`, and the 128 through `GetForUpdate`,
which starve the pool, skipped. To see the numbers again, run them on their own without the skip.
