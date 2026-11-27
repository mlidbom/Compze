# Measuring a MySQL test server

Python tools that measure what Compze's tests cost a MySQL server: written for
[why the DbPool's resets take seconds on MySQL](../why-resets-take-seconds-on-mysql.md), and kept for measuring a change
to a test server's settings before and after. They need Python 3 and MySQL's client programs, by default those of MySQL
9.7 (`MYSQL_BIN` names another folder), and connect as root with the test servers' password (`MYSQL_MEASUREMENT_USER`,
`MYSQL_MEASUREMENT_PASSWORD`).

A server is `host` or `host:port`, such as `192.168.0.11` for threadripper's.

| Tool | What it does to the server | What it says |
|---|---|---|
| `ddl_capacity.py <server> [<workers> [<seconds> [<idle connections>]]] [--reset <reset>]` | Creates databases `dbpool_measurement_capacity_<n>` and drops them | The database uses a second it completes under a suite-like load, the syncs a use, and the resets' latency meanwhile, with as many idle connections held open as asked. `--reset` tries another reset than the DbPool's `DROP DATABASE`: `drop-tables`, `truncate`, or `delete`, which empties the tables and keeps them |
| `private_server.py start <MySQL folder> <port> <folder>` | Runs a server of your own, of any version, configured as this machine's test server | Nothing: the other tools measure it, as `127.0.0.1:<port>`. `stop <MySQL folder> <port>` stops it |
| `fsyncs_per_operation.py <server>` | Creates the database `dbpool_measurement_operations` and drops it | The file operations other than reads and writes, mostly syncs, by file kind, and the wall time of each step a pool database goes through |
| `file_operations_per_step.py <server>` | Turns performance_schema's wait history on for the run: **only on a server of your own** | Each step's file operations by kind, syncs, creates, deletes, opens, reads and writes, and their time |
| `observe_mysql.py <server> <folder>` | Reads only | Where the server's time went during a test run: connections and whose they are, statements by kind, waits, syncs, what ran at once, the resets |
| `binlog_resets.py <binlog file>...` | Reads its binary log files | Each past test run's resets and how long they took |
| `suite_run.py <server> <folder> [<C-Test arguments>...]` | Runs the MySql suite against it: C-Test, its DbPool pointed at the server for this run only | The run's time and outcome, and `observe_mysql.py`'s summary of it. `-MaxParallelThreads` and `-MaxParallelAssemblies` try less parallelism |
| `suite_by_engine.py <machine> <folder> [--engines MsSql,PgSql,...] [--together] [-- <C-Test arguments>...]` | Runs the full suite on each engine in turn, or with `--together` on all of them in one run, against all three servers of `<machine>`, an IPv4 address or `this-machine`, for these runs only | Each engine's time, by `C-Measure-Command`, its tests' outcome and failures, the processor time the rest of the machine used, and MySQL's connections and resets during the MySql run. For what a change to C-Test's parallelism costs every engine. Builds nothing: `C-Build` first |

- **`ddl_capacity.py` is the one to compare settings with:** run it before and after, on a quiet server. 16 workers,
  about what a full suite runs at once, for 30 s.
- **Comparing MySQL versions:** unpack a version's zip archive, from `https://cdn.mysql.com/archives/mysql-<major.minor>/`,
  start it with `private_server.py`, and measure it with the others. Reach it as `127.0.0.1:<port>`, not `localhost`:
  it listens on IPv4 only, and on Windows the client's first try, IPv6, takes 2 s to be refused. For MySQL 5.7, set
  `MYSQL_BIN` to its own `bin` folder.
- **`observe_mysql.py` with a test run:** `suite_run.py` does it all; by hand, start it, run
  `C-Test -Combinations 'MySql:Microsoft:Newtonsoft:AspNetCore'`, then create the file `<folder>/stop`. It writes
  `<folder>/summary.txt` and prints it.
- **Other sessions share this machine,** and their builds and runs share its processors and the test servers: a run's
  time is only comparable with another's made while the rest of the machine was as quiet. `C-Measure-Command` says how
  much processor time the rest of the machine used meanwhile.
- **The fixture**, `pool-tables.sql`: the 15 tables a pool database holds after a typical test, as threadripper's
  server showed them on 2026-11-27. Compze's schemas change; the fixture stands for their size, not their columns.
