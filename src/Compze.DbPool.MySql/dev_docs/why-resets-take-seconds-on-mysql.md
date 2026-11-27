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
- **Not MySQL's version.** On this machine's drive, with the same `my.ini` and 16 workers, MySQL 5.7.44 completed 2.6
  database uses a second, 8.0.44 4.0, 8.4.6 3.9, and the installed 9.7.0 4.2 to 4.5, the reset's median between 1.5
  and 1.7 s on each. Alone, each resets a database in 0.13 to 0.22 s. 5.7 syncs less, 177 times for the 15 tables'
  creation against 8.0 and later's 296 to 378, but runs less DDL at once.
- **What would cut it: [less at once](#how-much-a-run-asks-of-the-server-at-once).** A run starts every test assembly
  at once, each running a test class per logical processor, and other sessions' runs add theirs. The server's DDL
  throughput stops rising at about 8 at once; past that, each statement only waits longer. [Emptying the tables
  instead of dropping them](#emptying-the-tables-instead-of-dropping-them) and [Windows' write-cache buffer
  flushing](#windows-write-cache-buffer-flushing) were declined.

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

- **History, from the binary logs** (`binlog_resets.py`): on threadripper, resets were slow from its first run,
  2026-11-17, with 47% taking 3 to 9 s. Since 2026-11-24, 8 to 18% take 10 s or more, against 0 to 2% before, and the
  longest 16 to 37 s. Tables created per reset grew from about 12 to 13 or 14. This machine's logs hold test runs from
  2026-11-17 on only: none ran against its server from 2026-10-15, and older logs have expired. On 2026-11-17 half its
  resets took 3 to 9 s, and on 2026-11-18 a fifth to a third took 10 s or more, the longest 34 s, at 12 to 13 tables a
  reset. The sub-second resets Magnus remembers were years ago, on threadripper, under the Windows 10 installation that
  was replaced 2026-05-30, and an older MySQL.
- **This machine's installed server, quiet, 2026-11-27:** alone, the 15 tables' creation took 0.40 s and the reset
  0.21 s; under 16 workers, the reset's median was 1.6 s.

## What slow resets break

- **The DbPool's lock timed out:** a flaw in `DbPool`, whatever the server, fixed 2026-11-27;
  [below](#the-dbpools-lock-held-across-the-reservation-and-the-reset).
- **Tests run past their reservations.** A test host whose three databases each wait seconds for their reset, and then
  for their schema, spends much of the 65 s reservation starting up, so another pool may take and reset a database it
  still uses. Probably what an `Unknown database 'compze_dbpool_0084'` in the same run was; not verified. Since
  dbpool-reset-mid-test's change, such a pool throws `DatabaseHeldPastItsReservationException` when it is disposed: one
  did in a MySQL run on this branch, 2026-11-27.
- **Two runs at once run out of connections.** threadripper's server reached 401 of its 400 `max_connections` at
  08:46:47 on 2026-11-27 and refused 25 connections, when one run alone peaks at 256.

### The DbPool's lock held across the reservation and the reset

Fixed on this branch, at Magnus's word, 2026-11-27, after merging dbpool-reset-mid-test's branch, which changes `Dispose`
too. `Compze.DbPool.Tests.Reservations.While_a_pools_reset_of_one_database_waits_for_a_lock_on_it` specifies it on the
server engines: before the fix the call for another name was still waiting after 20 s, on MySql, PgSql and MsSql alike.

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
- **The fix, as built:** the pool's lock, an `IAwaitableThreadShared<DbPool.Reservations>`, is held only to read and
  change the pool's `DbPool.Reservation`s, one per reservation name. The first call for a name marks its reservation as
  being prepared, and reserves and resets its database outside the lock; the other calls for the name wait, with
  `UpdateWhen`, until it isn't, for up to 2 minutes. Calls for other names never wait behind another database. A
  reservation whose reset failed keeps its database, and the next call for its name resets it again, where before it was
  handed out unreset. A `LazyCE` per name was the first idea, but `Dispose` can't wait for a `LazyCE`'s factory without
  running it.
- **`Dispose`:** it took the same lock, so it waited for a `ConnectionStringFor` in progress. With the slow work
  outside the lock, it could have released the pool's reservations, which `DbPoolMachineWideState.ReleaseReservationsFor`
  does by pool id, while another thread was still reserving: that reservation would have come after the release, and
  stayed held until it expired. So `Dispose` waits, with `UpdateWhen`, until no reservation is being prepared.

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

## How much a run asks of the server at once

C-Test runs `dotnet test` on the whole solution, so MSBuild runs up to one test assembly per logical processor at once:
32 on this machine, so all 34 test projects. Within each, xUnit runs test classes in parallel, up to one per logical
processor, and Compze sets no limit. CI's runners have 4 logical processors, so there it is 4 per assembly. On this
machine several sessions run suites at once, against the same servers: two runs at once took threadripper's MySQL past
its `max_connections`. When resets took a fraction of a second, the tests didn't run like that (Magnus).

**The server's DDL throughput stops rising at about 8 at once.** On this machine's installed server, quiet
(`ddl_capacity.py`, each use a reset, the 15 tables' creation and 5 commits), 2026-11-27:

| Workers | Database uses/s | fsyncs a use | Reset median / p90 | Schema creation median |
|---|---|---|---|---|
| 1 | 1.6 | 501 | 0.18 / 0.22 s | 0.40 s |
| 2 | 2.2 | 404 | 0.28 / 0.34 s | 0.54 s |
| 4 | 3.3 | 317 | 0.40 / 0.51 s | 0.70 s |
| 8 | 4.1 | 291 | 0.73 / 0.98 s | 1.09 s |
| 16 | 4.2 | 298 | 1.71 / 2.27 s | 1.91 s |
| 32 | 4.1 | 302 | 3.27 / 4.95 s | 3.72 s |

InnoDB's group commit shares a sync among the statements ready for it, so the syncs a use fall as more run at once,
to about 290 by 8, and no further.

**The MySql suite against this machine's server** (`suite_run.py`, C-Test's `-MaxParallelThreads` and
`-MaxParallelAssemblies`), one run each, 2026-11-27, on a machine other sessions' builds and runs shared:

| C-Test | Run | Statements running, median / max | Connections, median / max | Reset average | Resets seen, median / max |
|---|---|---|---|---|---|
| as it is | 126 s | 21 / 55 | 122 / 236 | 2.9 s | 4 / 14 s |
| `-MaxParallelThreads 4` | 161 s | 6 / 22 | 27 / 153 | 0.68 s | 1 / 5 s |
| `-MaxParallelAssemblies 8` | 212 s | 34 / 58 | 153 / 201 | 4.6 s | 5 / 16 s |
| both | 294 s | 6 / 25 | 23 / 123 | 1.2 s | 2 / 4 s |

Fewer assemblies at once only lengthens the run: the assemblies that use the database most still run their classes 32 at
once. A run with `-MaxParallelThreads 8` overlapped another session's test run and is left out.

**The full suite on every engine, against threadripper's servers and this machine's** (`suite_by_engine.py`), 2026-11-27,
14:03 to 14:54. No other session's suite ran meanwhile: from 14:28, `machine-stalls test-runs` lists only the tooling's
own specifications, which use no database server, and before that no test host of another checkout was running when
the runs began. Other sessions' builds shared this machine's processors. The MySql runs:

| Servers | C-Test | Run | Failed | Reset average | Connections, max | Statements running, median / max |
|---|---|---|---|---|---|---|
| threadripper | as it is | 232 s, 222 s | 3, 3 | 5.3 s, 5.3 s | 262, 270 | 37 / 60, 35 / 57 |
| threadripper | `-MaxParallelThreads 8` | 206 s | 0 | 1.36 s | 182 | 10 / 32 |
| threadripper | `-MaxParallelThreads 4` | 234 s | 0 | 0.73 s | 131 | 6 / 20 |
| this machine | as it is | 162 s, 132 s | 0, 0 | 3.65 s, 3.71 s | 247, 248 | 30 / 57, 40 / 58 |
| this machine | `-MaxParallelThreads 8` | 128 s | 0 | 1.12 s | 176 | 10 / 32 |
| this machine | `-MaxParallelThreads 4` | 152 s | 0 | 0.77 s | 121 | 6 / 24 |

- **The same 3 tests failed in both of threadripper's runs as it is:** in Compze.Tests.Integration,
  `Given_a_memory_image_state_store_kept_in_the_domain_database…the_next_memory_image_has_every_sentence`,
  `Tommand_receiver_binding_tests.A_tommand_bound_to_its_known_receiver_waits_for_that_endpoint…` and
  `Given_an_exactly_once_tommand_send_racing_discovery.A_send_while_several_remembered_peers…`. Their messages:
  `DatabaseHeldPastItsReservationException` 2, "Unknown database" 2, `ServerErrorException` 1. The tests ran past their
  reservations, and the DbPool reset their databases under them.
- **The run is Compze.Tests.Integration's:** 3 m 43 s of threadripper's 232 s as it is, and 3 m 46 s with 4 classes at
  once. With 4, it has too few classes running to keep the server busy; with 32, they queue for it.
- **Left out:** a second run of this machine's `-MaxParallelThreads 4`, 380 s, during which this machine's disk took
  1.6 ms a transfer at the median, against 0.5 to 0.7 ms in every other run, with the processors 11% busy. Its repeat
  is in the table.
- **The other engines, with 4 classes at once,** against threadripper's servers: MsSql 72 s became 87 s, as
  Compze.Tests.Integration went from 35 s to 79 s; PgSql 73 s and 71 s, Sqlite 71 s and 72 s, SqliteMemory 71 s and 71
  s. Against this machine's: MsSql 72 s and 74 s, PgSql 72 s and 72 s. All passed. Their floor, about 70 s, is
  Compze.DbPool.Tests' 66 s and Compze.Threading.Specifications' 58 s.

## Emptying the tables instead of dropping them

`MySqlDbPoolSqlLayer.ResetDatabase` drops the database and creates it again. Its comment from 2020 says dropping the
objects instead, as the other layers do, was no faster, and for dropping the tables that still holds. Emptying them and
keeping them is another matter: Compze's schema code creates every table with `CREATE TABLE IF NOT EXISTS`, which then
does next to nothing. On this machine's installed server, 16 workers, each use a reset, the 15 tables' creation and 5
commits (`ddl_capacity.py --reset`), 2026-11-27:

| Reset | Database uses/s | fsyncs a use | Reset median / p90 | Schema creation median |
|---|---|---|---|---|
| `DROP DATABASE` and `CREATE DATABASE`: today's | 4.2 | 295 | 1.67 / 2.07 s | 1.96 s |
| `DROP TABLE` of every table | 4.2 | 297 | 1.60 / 2.22 s | 1.99 s |
| `TRUNCATE TABLE` of every table | 3.6 | 337 | 4.14 / 5.16 s | 0.15 s |
| `DELETE FROM` every table, in one transaction | 97.5 | 9 | 0.01 / 0.01 s | 0.10 s |

**Declined,** Magnus, 2026-11-27: "That is most likely not an option, it would hugely complicate both the dbpool and the
test code compared to today. Reducing the extreme load on environments that cannot handle it is a much more reasonable
approach I think". What emptying would have cost: a reset database would hold the tables its previous user created,
empty, where today it holds none.

- **A table of another schema:** one of the same name that another checkout's code, or an earlier build, created with
  other columns. `CREATE TABLE IF NOT EXISTS` keeps it, and the test fails on its columns.
- **`AUTO_INCREMENT` counters** carry on from the previous use: `DELETE` doesn't reset them. Compze's MySQL tables have
  four such columns.
- **A test's own tables** stay, emptied, and a large table takes longer to empty than to drop.

## Windows' write-cache buffer flushing

A disk's policy "Turn off Windows write-cache buffer flushing on the device" (Device Manager, the disk's Policies tab;
`CacheIsPowerProtected` in the registry) has Windows complete a sync without asking the drive to flush its cache. MySQL
keeps its durability protocol and its settings, and a sync costs what it costs on a drive with power-loss protection,
such as a server's. What it gives up is the drive's guarantee across a power loss, for everything on the disk. It is
off, Windows' default, on every disk of both machines (2026-11-27). Magnus thinks he may have turned it on under
threadripper's earlier Windows 10 installation. Its effect here is not measured.

**Declined,** Magnus, 2026-11-27, for this and for MySQL settings other than the defaults: "Nah, let's stay with normal
configs. Realistic testing is good".

## Measuring again

The tools are in [measuring-a-mysql-test-server](measuring-a-mysql-test-server/README.md).
