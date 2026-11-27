"""How many pool-database uses a MySQL server completes per second when a test suite's worth of them run at once, and how
long the DbPool's reset takes meanwhile: the suite's load on the server, reduced to its DDL.

Each of <workers> workers holds a connection and loops over a database of its own: the DbPool's reset (DROP DATABASE and
CREATE DATABASE), then a test's schema creation (the 15 pool tables), for <seconds>. Its databases are named
dbpool_measurement_capacity_<n>, and are dropped at the end.

<idle connections> more connections are opened first and held, doing nothing, until the end: a test run keeps a
hundred or more of them open, with the pools of every test process on every database it has used. Each is a mysql.exe
process, a few megabytes.

Usage: python ddl_capacity.py <host[:port]> [<workers> [<seconds> [<idle connections>]]]
   16 workers, 30 seconds and no idle connections by default.
"""
import statistics, sys, threading, time
from mysql_measurement import Server

server = Server(sys.argv[1])
workers = int(sys.argv[2]) if len(sys.argv) > 2 else 16
seconds = float(sys.argv[3]) if len(sys.argv) > 3 else 30
idle_connections = int(sys.argv[4]) if len(sys.argv) > 4 else 0

idle = [server.session() for _ in range(idle_connections)]
for it in idle: it.run("SELECT 1;")

resets, uses = [], [0]
tally = threading.Lock()
deadline = time.perf_counter() + seconds

def worker(number):
    database = f"dbpool_measurement_capacity_{number}"
    session = server.session()
    session.run(f"CREATE DATABASE IF NOT EXISTS {database};")
    while time.perf_counter() < deadline:
        started = time.perf_counter()
        session.run(f"DROP DATABASE IF EXISTS {database}; CREATE DATABASE {database};")
        reset = time.perf_counter() - started
        session.run(server.create_pool_tables_sql(database))
        with tally:
            resets.append(reset)
            uses[0] += 1
    session.run(f"DROP DATABASE IF EXISTS {database};")
    session.close()

started = time.perf_counter()
threads = [threading.Thread(target=worker, args=(it,)) for it in range(workers)]
for it in threads: it.start()
for it in threads: it.join()
elapsed = time.perf_counter() - started
for it in idle: it.close()
ordered = sorted(resets)
print(f"{sys.argv[1]}, {workers} workers, {idle_connections} idle connections, {elapsed:.0f} s: {uses[0] / elapsed:.1f} database "
      f"uses/s; the reset's median {statistics.median(ordered):.2f} s, p90 {ordered[int(len(ordered) * 0.9)]:.2f} s, "
      f"max {ordered[-1]:.2f} s")
