# Why the DbPool's resets take seconds on MySQL

Investigated 2026-11-27 for compze-agent-rep's topic 69, after a trace of a full MySQL suite run against threadripper's
server showed resets of 3.8 s at the median, 11 s at p90 and 21 s at most. Magnus: "it used to take a tiny fraction of
a second. Something is utterly FUBAR if it takes 21 seconds".

## The answer

**The reset isn't slow. The MySQL server is saturated with DDL, and each reset waits in its queue.** On MySQL, a pool
database's use is DDL from end to end: a test creates its 15 tables, and the next reservation's reset drops them. With
InnoDB's defaults, that is about 440 syncs to disk a use, against 15 for a test's few commits: InnoDB makes DDL durable
step by step, about 14 syncs of its redo log for each table created and 9 for each dropped. The suite's ~490 uses a run
need more syncs than the server's drive completes, so DDL statements queue: a median of 33 statements ran at once, 14 of
them resets. Nothing in `MySqlDbPoolSqlLayer` or `DbPool` makes them slower than that.

- **The suite's DDL has grown about eightfold since March.** The `CREATE TABLE` statements in Compze's MySQL schema code
  went from 5 (2026-03-01) to 8 (06-01), 13 (09-01) and 17 (11-27), and the test declarations from about 1,400 to about
  3,400. A test's database held a few tables, not 15, when a reset took a tiny fraction of a second.
- **Threadripper halves the capacity.** Its MySQL completes 1.9 database uses a second under 16 concurrent workers; the
  same MySQL 9.7 with the same `my.ini`, on this machine's drive, 4.0 to 4.2, with this machine busy. Raw write-and-flush:
  threadripper's Samsung 970 EVO Plus 2.65 ms alone and 761 flushes/s from 8 threads; this machine's 990 PRO 1.35 ms and
  1,424/s. Same NVMe driver, Microsoft's, same power plan, Balanced, same write-cache settings.
- **MySQL only.** On the same server machine and the same suite, PgSql's resets took 0.48 s at the median (p90 0.77 s),
  MsSql's 0.88 s (p90 1.36 s), and their suites 81 s and 69 s, against MySQL's 3.8 s and 212 s.
- **Not the connections.** A run keeps up to 256 open, of the 400 `max_connections` allows, and 250 idle connections
  held open didn't slow the DDL. Two runs at once pass 400: see [what slow resets break](#what-slow-resets-break).

## What was measured

| A pool database's steps, on a quiet threadripper | syncs and other file operations | wall |
|---|---|---|
| A test's schema creation: 15 tables | ~290 | 330–600 ms |
| The DbPool's reset: `DROP DATABASE` of the 15 tables, `CREATE DATABASE` | ~190 | 170 ms |
| A test's 5 commits | 14 | 50 ms |

- **What the operations are:** performance_schema's summaries count every file operation but reads and writes as one
  figure. Broken down on a private instance (`file_operations_per_step.py`): of the schema creation's 306, 269 were
  syncs, 212 of them the redo log's; of the reset's 204, 168 were syncs, and deleting the 15 files took 5 ms in all.
  The numbers below that say fsyncs are that figure.
- **A suite run, observed on the server** (`observe_mysql.py`, 213 s): ~148,000 fsyncs, data files 84,761, redo log
  51,827, binary log 11,101, at 2 to 5 ms each against 1 ms idle. mysqld kept under one processor busy: it waited for the
  drive. The resets ran their time in `DROP DATABASE`'s own work: of about 11,700 samples of one, 3 saw it waiting for
  a metadata lock.
- **Since the server started, 31 hours:** 129,097 `CREATE TABLE`, 8,875 `DROP DATABASE`, 29,721 commits.
- **Concurrency on an idle server:** one reset 0.14 s; 16 at once 1.0 s each.
- **Connections in a suite run** (`observe_mysql.py`, 222 s): a median of 167 open, 256 at most, 128 of them one test
  process's; 1,587 new ones, 7 a second; none refused. The DbPool's MySQL connection strings keep one connection open in
  every pool (`MinimumPoolSize=1`), a pool per database a process has used, and replace each connection 10 s after it
  was opened (`ConnectionLifeTime=10`, its whole age, where PgSql's `ConnectionIdleLifetime=10` is its idle time).
- **Idle connections and DDL** (`ddl_capacity.py`, private instance, 16 workers): 4.0 and 4.2 database uses a second
  without idle connections, 3.8 and 4.2 with 250.
- **The same suite on a private MySQL 9.7 on this machine** (port 3399, data in a temporary folder):

  | Server | Suite | fsyncs | Resets seen, median / p90 / max |
  |---|---|---|---|
  | threadripper, as configured | 212 s, 1 timeout | ~148k | 5 / 11 / 18 s |
  | this machine's drive, as configured | 150 s | ~139k | 4 / 7 / 11 s |
  | this machine's drive, the three settings below | 78 s | ~28k | under 1 / 1 / 3 s |

  The samples, 200 ms apart, see only resets of a quarter second and longer.

- **History, from threadripper's binary logs** (`binlog_resets.py`): resets there were slow from the first run,
  2026-11-17, with 47% taking 3 to 9 s. Since 2026-11-24, 8 to 18% take 10 s or more, against 0 to 2% before, and the
  longest 16 to 37 s. Tables created per reset grew from about 12 to 13 or 14. This machine's binary logs from before
  2026-11-17 would show how fast resets were here, and with how many tables; they are readable only elevated.

## What slow resets break

- **The DbPool's lock times out:** a flaw in `DbPool`, whatever the server, marked by a `bug:` comment at
  `DbPool.ConnectionStringFor`; [below](#the-dbpools-lock-held-across-the-reservation-and-the-reset).
- **Tests run past their reservations.** A test host whose three databases each wait seconds for their reset, and then
  for their schema, spends much of the 65 s reservation starting up, so another pool may take and reset a database it
  still uses. Probably what an `Unknown database 'compze_dbpool_0084'` in the same run was; not verified. Since
  dbpool-reset-mid-test's change, such a pool throws `DatabaseHeldPastItsReservationException` when it is disposed: one
  did in a MySQL run on this branch, 2026-11-27.
- **Two runs at once run out of connections.** threadripper's server reached 401 of its 400 `max_connections` at
  08:46:47 on 2026-11-27 and refused 25 connections, when one run alone peaks at 256.

### The DbPool's lock held across the reservation and the reset

Being fixed on this branch, at Magnus's word, 2026-11-27, after merging dbpool-reset-mid-test's branch, which changes
`Dispose` too.

- **The flaw:** `DbPool.ConnectionStringFor` runs entirely inside `_reservedDatabases.Locked`, a lock created with
  `LockTimeout.Seconds(30)`. Inside it run `DbPoolMachineWideState.ReserveDatabase`, which waits up to 45 s for a free
  database, and `IDbPoolSqlLayer.ResetDatabase`, which on MySQL took up to 37 s, with `EnsureDatabaseExistsAndIsEmpty`
  after a failed reset. A call for any other reservation name on the same pool, even one already reserved and only
  looked up, waits behind them and throws `TakeMonitorLockTimeoutException` after 30 s.
- **Who calls concurrently:** `DbPool` is one instance per host, shared by the endpoints' containers, which are clones
  of the host's (`DelegateToParentServiceLocatorWhenCloning`). `EndpointHost.StartAsync` starts the endpoints together,
  with `Task.WhenAll`, and each asks for its databases' connection strings as it starts. In the Integration tests'
  trace, a pool's three resets came one after another, because those endpoints create their schema in each database
  before they ask for the next: there the lock made no difference.
- **Seen once, 2026-11-27,** in a full local run against threadripper's servers, in
  `AccountManagement.UserStories._020_After_a_user_has_registered_an_account` on MySql:Microsoft:Newtonsoft:AspNetCore.
  The waiting call: `ExactlyOnceEndpoint.ClaimTheProcessLockAsync`, `EndpointProcessLock.AcquireAsync`,
  `MySqlEndpointCatalogSqlLayer.InitAsync`, `MySqlSqlLayerSchemaManager.EnsureSchemaInitialized`,
  `MySqlConnectionPool`'s lazy connection string, `TestingComponentRegistrar.MySqlDbPoolRegistrar`, then
  `DbPool.ConnectionStringFor`. The exception's stack of the blocking thread's release showed it releasing the lock in
  `DbPool.ConnectionStringFor` too, after more than 30 s inside it.
- **The fix:** take the lock only to find or record an entry for the reservation name, and reserve and reset outside
  it, once for all the callers of that name. A `LazyCE<string>` per name, whose factory reserves, resets and returns the
  connection string, does that: its waiters wait up to `LockTimeout.Default`, 2 minutes (45 s under NCrunch), and a
  factory that throws is run again by the next read, where today a database whose reset and fallback both failed stays
  cached and is handed out unreset. Callers for other names never wait behind another database.
- **What the fix must handle: `Dispose`.** It takes the same lock now, so it waits for a `ConnectionStringFor` in
  progress. Once the slow work runs outside the lock, `Dispose` could release the pool's reservations, which
  `DbPoolMachineWideState.ReleaseReservationsFor` does by pool id, while another thread is still reserving: that
  reservation would come after the release, and stay held until it expires. So `Dispose` waits for the entries still in
  progress before it releases, or a reservation that finishes after `Dispose` releases itself.

## What was ruled out

- **Metadata locks:** 3 of about 11,700 samples of a reset saw it waiting for one. The server counted 218 s of
  metadata-lock waits in the run, `GET_LOCK` waits among them, which the MDL subsystem implements.
- **The open connections:** see above; 250 idle ones changed nothing.
- **Creating and deleting the tables' files:** 5 ms for deleting 15; the time is in the syncs.
- **The drive's health and Defender:** healthy; Defender excludes `C:\ProgramData\MySQL\` and `mysqld.exe` there.
- **Windows' timer resolution:** an autocommit insert takes ~5 ms there, fsyncs 1 ms each.
- **The `mysql` client's own timings:** they are wrong on Windows, reporting 1.6 s for a statement that took 0.24 s of
  wall time. Every number here is wall time or the server's own counters.

## Three settings that would hide it: declined

`innodb_file_per_table=0`, `innodb_flush_log_at_trx_commit=2` and `sync_binlog=0` made the suite take 78 s instead of
150 s on this machine's drive, with five times the DDL capacity, by giving up crash durability: a commit is flushed a second
later, and a table has no file of its own. Magnus declined them, 2026-11-27: "I'm very hesitant to run our tests using
nonstandard mysql settings that I assume are some sort of optimization hack that loses us some guarantees and makes our
tests non-representative for how a production MySql database works".

**CI's MySQL isn't held up by its flushes,** and keeps the stock `mysql:8` as it is. The three settings were tried there,
with `SET GLOBAL` in a step before the tests (`f2c02798b`), and removed again (`a8bd997ec`), since they made no
difference the runs could show. Compze.Tests.Integration on the two MySql jobs:

| Run | AspNetCore | NamedPipes |
|---|---|---|
| main, `966e54b96` | 137 s | 184 s |
| this branch, without the settings, `85b11f7d2` | 223 s | 112 s |
| this branch, with them, `f2c02798b` | 159 s | 129 s |

The same code varies from 112 to 223 s, and CI's MySql jobs take about as long as its PgSql and MsSql jobs. A cloud VM's
disk completes a sync at once, which a consumer NVMe drive on Windows doesn't: that is my understanding, not measured.

## Measuring again

The tools are in [measuring-a-mysql-test-server](measuring-a-mysql-test-server/README.md).
