# Synchronous paths that silently need a free thread-pool thread

Put in the backlog by Magnus 2026-11-24. Not started.

## Why

A synchronous call that, somewhere below it, waits for work on the thread pool stalls whenever every pool thread is
busy, and the busier the process, the longer. It shows as timeouts that come and go with load: the 2026-11-12 CI
starvation, and the memory image state store's deck flake of 2026-11-24.

The deck flake: forty units of work from `Task.Run`, queued on one deck, kept every pool thread blocked. The store's
PostgreSQL reads were synchronous commands of two statements, and Npgsql 9 sends the second statement, and flushes the
whole command, from the thread pool (`NpgsqlCommand.Write`, its `ConstrainedConcurrencyScheduler`). PostgreSQL received
nothing: `pg_stat_activity` showed the backends idle in `ClientRead` while the client threads waited in `recv`, for 1.6
to 30 seconds. Fixed in the store by one statement per command (df4d81e8d). The CI-flakiness skill has the fingerprint.

There are two kinds, and they need different defences:

- **Compze's own:** a synchronous member that blocks on an asynchronous one, with `.Wait()`, `.Result` or
  `GetAwaiter().GetResult()`. Visible in our code, and easy to write without noticing.
- **Inside a library's synchronous API,** as Npgsql's above. Invisible in our code: only running it under a starved pool
  shows it.

## What there is, 2026-11-24

Fixed on main the same day by commit-as-the-decision (0c941e289 to dfc0a2e9d): the four schema managers'
`EnsureSchemaInitialized()`, which blocked on the asynchronous form and is synchronous now; the PostgreSQL tevent store's
two-statement insert; every PostgreSQL schema script, now a list of statements, each a command of its own; a deadlock
in `DbConnectionPool`, which opened a transaction's connection under the pool-wide lock; and Npgsql's own pool, whose
synchronous waiter needs a pool thread to be woken, which Compze's PostgreSQL connections no longer wait in.

Synchronous waits on a task in library code, outside tests, that remain:

- **Publishing tevents synchronously:** `TaggregateUnitOfWork.Dispatch` for exactly-once tevents, "the one deliberate
  bridge", and `UnitOfWorkTeventPublisher`.
- **`LocalTypermediaNavigatorSession`:** three, synchronous tommands and tueries over asynchronous handlers.
- **The tessaging delivery streams,** `TessagingConnection.BestEffortDeliveryStream` and `.ExactlyOnceDeliveryStream`:
  seven. Not yet checked whether they run on threads of their own, where blocking costs no pool thread.
- **`IAsyncShared.Locked`:** blocks on the shared object's task, which the synchronous path creates already completed.
  It waits only when the asynchronous path opened the transaction's connection and it is still opening.
- **The DI containers' synchronous disposals:** covered by `container-disposal-of-async-only-components.md`.

## Not `SyncOrAsync`

Compze once had `SyncOrAsync` (2020, first named `AsyncMode`): a flag passed down the database call chain, so that one
asynchronous implementation served both modes, with the synchronous ADO.NET calls at the bottom in sync mode, and
`SyncResult()` asserting each task complete instead of waiting on it. Magnus removed it 2024-11-27 (a4f585f73, "Get rid
of convoluted SyncFlex code that was causing strange errors"). It would cover only Compze's own kind: in sync mode it
calls the library's synchronous API, Npgsql's included. Its assertion would also fire, under contention only, wherever
something in the chain is truly asynchronous, such as the contended `SemaphoreSlim.WaitAsync` in `IAsyncLockCE`:
possibly the strange errors, which the commit doesn't describe.

## The task

1. **A code policy in Compze.Tests.CodePolicies** that lists every synchronous wait on a task in library code. Each is
   fixed, or allowed with its reason, at the code and in the policy. A new one fails the policy until it is decided.
2. **Starved-pool specifications for each synchronous surface:** the memory image state store, the tevent store, schema
   creation, the typermedia navigator, publishing. Each runs the synchronous call on a thread of its own while every pool
   thread is held, and requires it to finish in time. This catches the library-internal kind too. A helper in
   Compze.Tests.Infrastructure that holds the pool belongs with the first.
3. **The fixes** of what the policy and the specifications find.
