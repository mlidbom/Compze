"""The fsyncs MySQL makes, by file kind, and the wall time, for each step a pool database goes through, one at a time on
a database of its own (dbpool_measurement_operations, dropped at the end): a test's schema creation (the 15 pool tables),
a few commits, the DbPool's reset (DROP DATABASE and CREATE DATABASE), and for comparison emptying the tables instead.

The counts are exact only while nothing else runs on the server.

Usage: python fsyncs_per_operation.py <host[:port]>
"""
import sys, time
from mysql_measurement import Server

server = Server(sys.argv[1])
database = "dbpool_measurement_operations"

def measure(name, sql):
    before = server.fsyncs()
    started = time.perf_counter()
    server.run(sql)
    wall = time.perf_counter() - started
    after = server.fsyncs()
    total = sum(after[kind][0] - before[kind][0] for kind in after)
    parts = ", ".join(f"{kind} {after[kind][0] - before[kind][0]} ({(after[kind][1] - before[kind][1]) / 1e9:.0f} ms)" for kind in after)
    print(f"   {name:50s} wall {wall * 1000:5.0f} ms, fsyncs {total:4d}: {parts}")

def emptying_every_table():
    tables = server.run(f"SELECT table_name FROM information_schema.tables WHERE table_schema = '{database}'")
    return (f"USE {database}; SET SESSION foreign_key_checks = 0; START TRANSACTION; "
            + "".join(f"DELETE FROM `{table}`;" for (table,) in tables) + " COMMIT;")

server.run(f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
for round in (1, 2):
    print(f"round {round}:")
    measure("a test's schema creation: the 15 tables", server.create_pool_tables_sql(database))
    measure("5 commits of one row each", f"USE {database}; " + "".join(
        f"INSERT INTO typeids (CurrentName) VALUES ('Type{it}');" for it in range(5)))
    measure("emptying the 15 tables with DELETE, one commit", emptying_every_table())
    measure("the DbPool's reset of the database", f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
    measure("the DbPool's reset of an empty database", f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
server.run(f"DROP DATABASE IF EXISTS {database};")
