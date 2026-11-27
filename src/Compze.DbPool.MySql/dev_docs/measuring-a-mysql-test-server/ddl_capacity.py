"""How many pool-database uses a MySQL server completes per second when a test suite's worth of them run at once, and how
long the DbPool's reset takes meanwhile: the suite's load on the server, reduced to its DDL.

Each of <workers> workers holds a connection and loops over a database of its own: the DbPool's reset, a test's schema
creation (the 15 pool tables, CREATE TABLE IF NOT EXISTS as Compze's schema code creates them), and a test's 5 commits of
a row each, for <seconds>. Its databases are named dbpool_measurement_capacity_<n>, and are dropped at the end.

The reset is one of:
   drop-database   DROP DATABASE and CREATE DATABASE: what the DbPool's MySQL layer does.
   drop-tables     DROP TABLE of every table in the database, which stays.
   truncate        TRUNCATE TABLE of every table: the tables stay, emptied, and the schema creation finds them.
   delete          DELETE FROM every table, in one transaction: the same, with DML instead of DDL.

<idle connections> more connections are opened first and held, doing nothing, until the end: a test run keeps a
hundred or more of them open, with the pools of every test process on every database it has used. Each is a mysql.exe
process, a few megabytes.

The syncs per use are the server's own counters, so they include whatever else it did meanwhile: measure on a quiet
server.

Usage: python ddl_capacity.py <host[:port]> [<workers> [<seconds> [<idle connections>]]] [--reset <reset>]
   16 workers, 30 seconds, no idle connections and drop-database by default.
"""
import argparse, statistics, threading, time
from mysql_measurement import Server

RESETS = ("drop-database", "drop-tables", "truncate", "delete")
arguments = argparse.ArgumentParser(usage=__doc__)
arguments.add_argument("server")
arguments.add_argument("workers", nargs="?", type=int, default=16)
arguments.add_argument("seconds", nargs="?", type=float, default=30)
arguments.add_argument("idle_connections", nargs="?", type=int, default=0)
arguments.add_argument("--reset", choices=RESETS, default="drop-database")
arguments = arguments.parse_args()
server = Server(arguments.server)


def reset_sql(session, database):
    """The statements of the reset under trial, for a database that holds what the previous use left."""
    if arguments.reset == "drop-database": return f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};"
    tables = [f"`{database}`.`{table}`" for (table,) in session.run(f"SHOW TABLES FROM {database};")]
    if not tables: return "DO 0;"
    if arguments.reset == "drop-tables": return "SET SESSION foreign_key_checks = 0; DROP TABLE " + ", ".join(tables) + ";"
    if arguments.reset == "truncate": return "SET SESSION foreign_key_checks = 0; " + "".join(f"TRUNCATE TABLE {it};" for it in tables)
    return "SET SESSION foreign_key_checks = 0; START TRANSACTION; " + "".join(f"DELETE FROM {it};" for it in tables) + " COMMIT;"


idle = [server.session() for _ in range(arguments.idle_connections)]
for it in idle: it.run("SELECT 1;")

resets, schema_creations, uses = [], [], [0]
tally = threading.Lock()
server.version()  # before the clock starts: the fixture depends on it


def worker(number):
    database = f"dbpool_measurement_capacity_{number}"
    session = server.session()
    session.run(f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
    while time.perf_counter() < deadline:
        started = time.perf_counter()
        session.run(reset_sql(session, database))
        reset = time.perf_counter() - started
        session.run(server.create_pool_tables_sql(database))
        schema_creation = time.perf_counter() - started - reset
        session.run(f"USE {database}; " + "".join(f"INSERT INTO typeids (CurrentName) VALUES ('Type{it}');" for it in range(5)))
        with tally:
            resets.append(reset)
            schema_creations.append(schema_creation)
            uses[0] += 1
    session.run(f"DROP DATABASE IF EXISTS {database};")
    session.close()


def percentile(values, share): return sorted(values)[int(len(values) * share)]


syncs_before = sum(count for count, _ in server.fsyncs().values())
deadline = time.perf_counter() + arguments.seconds
started = time.perf_counter()
threads = [threading.Thread(target=worker, args=(it,)) for it in range(arguments.workers)]
for it in threads: it.start()
for it in threads: it.join()
elapsed = time.perf_counter() - started
syncs = sum(count for count, _ in server.fsyncs().values()) - syncs_before
for it in idle: it.close()
print(f"{arguments.server} MySQL {server.version()}, --reset {arguments.reset}, {arguments.workers} workers, "
      f"{arguments.idle_connections} idle connections, {elapsed:.0f} s: {uses[0] / elapsed:.1f} database uses/s, "
      f"{syncs / uses[0]:.0f} fsyncs a use; the reset's median {statistics.median(resets):.2f} s, p90 {percentile(resets, 0.9):.2f} s, "
      f"max {max(resets):.2f} s; the schema creation's median {statistics.median(schema_creations):.2f} s")
