"""What a pool database's steps cost in file operations, by kind: how many syncs, creates, deletes, opens, closes, reads
and writes, and the time each kind took. performance_schema's summaries count everything but reads and writes as one
"misc" figure; this tells them apart, from its history of wait events.

That history is off by default: this turns its consumers on for the run and back off at the end, a change to a running
server that it doesn't keep (setup_consumers is reset at a restart). Run it only on a server of your own.

Each step runs alone on a database of its own, dbpool_measurement_file_operations, dropped at the end; the history holds
the last 10,000 events, enough for one step.

Usage: python file_operations_per_step.py <host[:port]>
"""
import sys
from mysql_measurement import Server

server = Server(sys.argv[1])
database = "dbpool_measurement_file_operations"
CONSUMERS = "'events_waits_current', 'events_waits_history_long'"

def measure(name, sql):
    server.run("TRUNCATE TABLE performance_schema.events_waits_history_long")
    server.run(sql)
    rows = server.run("SELECT SUBSTRING_INDEX(event_name, '/', -1), operation, COUNT(*), ROUND(SUM(timer_wait) / 1e9, 1) "
                      "FROM performance_schema.events_waits_history_long WHERE event_name LIKE 'wait/io/file/%' "
                      "GROUP BY 1, 2 ORDER BY 4 DESC")
    print(f"{name}:")
    for file_kind, operation, count, milliseconds in rows:
        print(f"   {file_kind:18s} {operation:8s} {int(count):5d}  {float(milliseconds):8.1f} ms")

server.run(f"UPDATE performance_schema.setup_consumers SET enabled = 'YES' WHERE name IN ({CONSUMERS})")
try:
    server.run(f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
    measure("a test's schema creation: the 15 tables", server.create_pool_tables_sql(database))
    measure("the DbPool's reset of the database", f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
    server.run(f"DROP DATABASE IF EXISTS {database};")
finally:
    server.run(f"UPDATE performance_schema.setup_consumers SET enabled = 'NO' WHERE name IN ({CONSUMERS})")
