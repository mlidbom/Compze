---
name: diagnosing-ci-flakiness
description: >-
  How to find the cause of a test that flakes or fails in CI: reading the load summary and the thread pool state a CI
  run records, what a healthy runner measures, the fingerprint of thread-pool starvation, hunting a flake that won't
  reproduce locally with the flake-hunt workflow, and reading the dumps and the PostgreSQL log in the job log. Use it
  when a CI run fails, or a test times out, runs slowly or fails only sometimes, on the runner or locally: before
  concluding anything about the runner's load, and before rerunning CI to get it green.
---

# Diagnosing CI flakiness: read what the run recorded, then hunt the flake in CI

A CI test that flakes has a cause in the code, and the runner being overloaded is the last hypothesis: the
always-loaded rule on diagnosing CI flakiness says why. This is how to find the cause.

The evidence is already captured — read it before theorising. The CI `Build and Test` step
(`.github/actions/build-and-test/action.yml`) prints a `vmstat` load summary every run, pass or fail:
peak/avg run-queue (`r`), min idle CPU%, iowait, and the busiest samples — judge them against the printed
`logical CPUs`. Separately, `NamedPipeTransportClient` puts the ThreadPool state (free workers, pending
items) into the exception when a connect times out. Saturated looks like run-queue far above the CPU count
with idle near 0, sustained; healthy looks like run-queue near/below it with idle to spare.

Measured baseline, 2026-11-14: the 35 Linux test jobs of three green runs, each job testing four combinations on
**4 logical CPUs**.

- **Over the whole test run, idle averaged 38–78% and the run-queue 1.2–5.1.** MySql jobs are the busiest (idle
  38–60%), SqliteMemory jobs the calmest (62–78%).
- **Every green job has busy seconds too:** idle touches 0–8% in each, the run-queue peaks at 11–26, and at least ten
  of a job's 240–430 seconds have a run-queue of 7 or more. So the peak and the minimum tell nothing by themselves.

Judge by the averages, and by how long the busy stretch lasted. With that much idle on average, a 10-second timeout
is something waiting on an event that isn't arriving, a code or DB cause — not the machine. The box takes ~1.5× a
dev machine's time on pure-CPU work, by an earlier measurement. (`COMPOSABLE_MACHINE_SLOWNESS=5.0` is conservative
padding for perf-test thresholds only, not a measurement of the box.)

A second fingerprint the instruments catch: ThreadPool **pending work draining at ~1 item/second while live
threads climb by ~1/second** is thread-pool starvation from *blocking* code — the machine is fine; the process
is strangling itself, because the pool injects threads that slowly once its minimum is busy. Async code must
never block a pool thread. The 2026-07 case: Windows' named-pipe connect is synchronous under the hood and ran
per-send; fixed by the transport's connection pooling. Reproduce such cases locally with
`DOTNET_PROCESSOR_COUNT=4` — small-machine starvation is invisible on a many-core dev box.

That local reproduction can fail where CI's succeeds: the 2026-11-12 case reproduced in none of ~200 local runs,
`DOTNET_PROCESSOR_COUNT=1` included, and in 11% of Linux CI runs. When a flake won't reproduce locally, hunt it in CI
rather than theorise further:

- **The flake-hunt workflow** (`.github/workflows/flake-hunt.yml`): push a branch named `flake-hunt/<what>` with the
  workflow's combination and test project set, and one push runs that project 20 times on each of 8 Linux runners,
  each run from a fresh runner's state. `ci.yml` skips such branches.
- **Make a test's own timeout leave a dump:** `WaitAsyncDumpingTheProcessOnTimeout` (Compze.Tests.Infrastructure)
  dumps the process while the awaited work is still stuck, and names the thread pool's state in the exception.
- **Read the dumps in the job log,** not the artifact: on failure CI prints each dump's thread pool, threads,
  pending awaits (`dumpasync` — where a stuck async operation is suspended) and stacks. Pool workers all parked
  in blocking waits, work items queued and the CPU idle is starvation; the parked frames name the culprits.
  `dumpasync` sees only code compiled with the compiler's state machines, and Compze is compiled with runtime async
  since .NET 11 (dotnet-dump 10.0.750501, 2026-11-14): a flake hunt that needs it sets `RUNTIME_ASYNC` to `false`.
- **PostgreSQL logs lock waits and statements over a second** in CI; its container log is printed when the job
  ends. A timeout with nothing logged there was not waiting in the database.

The 2026-11-12 case: an endpoint's router parked a pool thread in its registry wait for the endpoint's whole life,
and the PostgreSQL sql layers prepared statements synchronously from async code; on a 4-processor runner the
pool's 4–5 threads were all parked and a recording hung in a synchronous socket read for over 10 seconds.

**A synchronous socket read that the server never answers, with the server idle, is a send that never left.**
Npgsql 9 sends the second and later statements of a *synchronous* command, and flushes the whole command, from the
thread pool, while the calling thread waits in `recv`. With the pool starved, `pg_stat_activity` shows as many idle
backends in wait event `ClientRead` as the client has threads in `NpgsqlReadBuffer.Ensure`: each side waits for the
other. The 2026-11-24 case: the memory image state store's two-statement reads stalled 1.6–30 s under forty units of
work from `Task.Run`, on a 32-core machine, until each statement became a command of its own. `psql` from
`C:\Program Files\PostgreSQL\18\bin` reads `pg_stat_activity` on any of the test servers during a stall.

**On Linux, a synchronous socket read waits for a thread-pool thread too, even when the server has answered.** .NET
emulates a synchronous operation on a socket that has gone non-blocking: the epoll thread hands the socket's event to
the thread pool, and only a pool thread wakes the reader. The fingerprint, in a dump: the reader in
`SocketAsyncContext.PerformSyncOperation`, on a `ManualResetEventSlim`, while the `SocketAsyncEngine.EventLoop` thread
sits idle in `WaitForSocketEvents`; and the database logs the statement as done at once (PostgreSQL: the lock taken
with no wait logged). Windows' synchronous socket reads block for real, so it never reproduces there. The 2026-11-25
case: the memory image state store's deck specification ran forty synchronous units of work through `Task.Run`, all
but one blocked their pool threads waiting for the deck, and the one holding it waited 7–30 s for a reply that had
arrived; endpoint tests beside it timed out in the same starvation. Blocking actors of a test belong on threads of
their own (`TaskCE.RunOnDedicatedThreadAsync`, `TestingTaskRunner`).

**A dump taken on a timer comes late when the pool is starved:** a timer's callback runs on the pool, so it fires once
the pool frees up, after the stall. `WaitAsyncDumpingTheProcessOnTimeout` has a thread of its own write the dump at the
deadline since 2026-11-25; a test that dumps another way needs the same. Taking a dump freezes the process for
seconds, so other tests of the run may fail beside the one that dumped.

**On SQL Server, a reader that throws from `TrySetMetaData` read two responses.** SqlClient reads on past a response's
final DONE token when more bytes are already in its buffer, so an error followed by another result set in one
`ExecuteReader` means two requests reached the server on one session before the reader read: two users of one physical
connection. The 2026-11-26 case, CI run 45886748771: "Incorrect syntax near '쑀'" for a single-statement SELECT on a
fresh connection of its own, seen once in a few hundred SQL Server jobs. 320 runs of its project in a flake hunt didn't
repeat it, and its cause wasn't found. Hunting it found and fixed two real defects: `AsyncLockCE` let work started under
a hold enter after the hold had ended, and the sql layer's pool put a transaction's connection back in SqlClient's pool
before SqlClient's own completion handler had run, which then doomed the connection in the next transaction that took
it: "The requested operation cannot be completed because the connection has been broken".

What that hunt left for the next one:

- **The flake-hunt workflow runs on SQL Server** (persistence `MsSql`), and with `di: every` on the four DI containers in
  one process, as each CI test job does.
- **A probe for two users of one SQL Server physical connection,** on the branch `flake-hunt/mssql-garbage-probe-perf`:
  it listens to SqlClient's diagnostic events and reports two commands in progress at once on one `ClientConnectionId`,
  and a close while one is, with both commands' SQL, parameters, transactions and stacks. Two traps it hit: SqlClient
  completes an async command's task before it writes the command's After event, and under runtime async the awaiting
  code may run its next command on another thread meanwhile, so a command ends when Compze's code returns from it; and
  `RuntimeHelpers.GetHashCode` collides among the tens of thousands of SqlConnections a test process makes, so it
  identifies them by a serial number in a `ConditionalWeakTable`. The endpoint catalog's lock session, a connection of
  its own outside the pool, still gives the first trap's false positive.
- **SqlClient's diagnostic events also start work at an exact moment inside SqlClient,** for a deterministic
  specification of a race: `WhenSqlClientHasClosedTheConnectionThatRan` in Compze.Sql.InternalSpecifications runs an
  action once a connection is back in SqlClient's pool, before its close returns.
