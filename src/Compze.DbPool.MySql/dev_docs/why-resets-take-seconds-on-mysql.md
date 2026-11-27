# Why the DbPool's resets take seconds on MySQL

Investigated 2026-11-27 for compze-agent-rep's topic 69, after a trace of a full MySQL suite run against threadripper's
server showed resets of 3.8 s at the median, 11 s at p90 and 21 s at most. Magnus: "it used to take a tiny fraction of
a second. Something is utterly FUBAR if it takes 21 seconds".

## The answer

**The reset isn't slow. The MySQL server is saturated with DDL, and each reset waits in its queue.** On MySQL, a pool
database's use is DDL from end to end: a test creates its 15 tables, and the next reservation's reset drops them. With
InnoDB's defaults, that is about 500 fsyncs a use, against 15 for a test's few commits. The suite's ~490 uses a run
need more flushes than the server's drive completes, so DDL statements queue: a median of 33 statements ran at once,
14 of them resets. Nothing in `MySqlDbPoolSqlLayer` or `DbPool` makes them slower than that.

- **MySQL only.** On the same server machine and the same suite, PgSql's resets took 0.48 s at the median (p90 0.77 s),
  MsSql's 0.88 s (p90 1.36 s), and their suites 81 s and 69 s, against MySQL's 3.8 s and 212 s.
- **Threadripper halves the capacity.** Its MySQL completes 1.9 database uses a second under 16 concurrent workers; the
  same MySQL 9.7 with the same `my.ini`, on this machine's drive, 4.0 to 4.2, with this machine busy. Raw write-and-flush:
  threadripper's Samsung 970 EVO Plus 2.65 ms alone and 761 flushes/s from 8 threads; this machine's 990 PRO 1.35 ms and
  1,424/s.
- **Three server settings give five times the capacity:** see [the fix](#the-fix-three-settings-on-the-test-server).

## What was measured

| A pool database's steps, on a quiet threadripper | fsyncs | wall |
|---|---|---|
| A test's schema creation: 15 tables | ~290 | 330–600 ms |
| The DbPool's reset: `DROP DATABASE` of the 15 tables, `CREATE DATABASE` | ~190 | 170 ms |
| A test's 5 commits | 14 | 50 ms |

- **A suite run, observed on the server** (`observe_mysql.py`, 213 s): ~148,000 fsyncs, data files 84,761, redo log
  51,827, binary log 11,101, at 2 to 5 ms each against 1 ms idle. mysqld kept under one processor busy: it waited for the
  drive. The resets ran their time in `DROP DATABASE`'s own work: of about 11,700 samples of one, 3 saw it waiting for
  a metadata lock.
- **Since the server started, 31 hours:** 129,097 `CREATE TABLE`, 8,875 `DROP DATABASE`, 29,721 commits.
- **Concurrency on an idle server:** one reset 0.14 s; 16 at once 1.0 s each.
- **The same suite on a private MySQL 9.7 on this machine** (port 3399, data in a temporary folder):

  | Server | Suite | fsyncs | Resets seen, median / p90 / max |
  |---|---|---|---|
  | threadripper, as configured | 212 s, 1 timeout | ~148k | 5 / 11 / 18 s |
  | this machine's drive, as configured | 150 s | ~139k | 4 / 7 / 11 s |
  | this machine's drive, the three settings | 78 s | ~28k | under 1 / 1 / 3 s |

  The samples, 200 ms apart, see only resets of a quarter second and longer.

- **History, from threadripper's binary logs** (`binlog_resets.py`): resets there were slow from the first run,
  2026-11-17, with 47% taking 3 to 9 s. Since 2026-11-24, 8 to 18% take 10 s or more, against 0 to 2% before, and the
  longest 16 to 37 s. Tables created per reset grew from about 12 to 13 or 14.

## What was ruled out

- **Metadata locks:** 3 of about 11,700 samples of a reset saw it waiting for one. The server counted 218 s of
  metadata-lock waits in the run, `GET_LOCK` waits among them, which the MDL subsystem implements.
- **The drive's health and Defender:** healthy; Defender excludes `C:\ProgramData\MySQL\` and `mysqld.exe` there.
- **Windows' timer resolution:** an autocommit insert takes ~5 ms there, fsyncs 1 ms each.
- **The DbPool's lock across the reset:** a pool's three resets are serial because an endpoint creates its schema in
  each database before it asks for the next one, not because of the lock.
- **The `mysql` client's own timings:** they are wrong on Windows, reporting 1.6 s for a statement that took 0.24 s of
  wall time. Every number here is wall time or the server's own counters.

## The fix: three settings on the test server

Magnus's to decide: threadripper's MySQL is a test server, and these trade crash durability for speed. In its
`my.ini`, `[mysqld]`, then a restart of the MySQL97 service, or the same with `SET PERSIST` without one:

- `innodb_file_per_table=0`: tables live in the system tablespace, so creating and dropping one creates and deletes no
  file of its own, each with its fsyncs. Of a use's ~500 fsyncs, about 200 go.
- `innodb_flush_log_at_trx_commit=2`: a commit writes the redo log, which is flushed once a second. A crash of mysqld
  loses nothing; a crash of Windows or a power loss, up to a second of commits.
- `sync_binlog=0`: the binary log is flushed when Windows chooses. No replica reads it. `disable-log-bin` instead would
  write none at all; it wasn't measured.

On this machine's drive, with 16 workers: 4.0–4.2 database uses a second became 19–20, and the reset's median 1.6 s
became 0.19 s; the suite, 150 s became 78 s. Not yet measured on threadripper.

CI's MySQL container gets the same three settings, from `.github/actions/mysql-skips-crash-durability`, since
2026-11-27. There they made no difference the runs could show: Compze.Tests.Integration on the two MySql jobs took 159
and 129 s with them, against 137, 184, 223 and 112 s in two runs without. CI's MySQL isn't held up by its flushes: its
jobs take about as long as PgSql's and MsSql's.

The system tablespace, `ibdata1`, 12 MB on threadripper now, grows to hold the tables and never shrinks; for 100
databases of 15 small tables, by an estimate, tens of megabytes, and what a drop frees is reused. Settings like these can't come from Compze's code: each is
global only, and test code must never change a developer's shared server.

## Measuring again

The tools are in [measuring-a-mysql-test-server](measuring-a-mysql-test-server/README.md).
