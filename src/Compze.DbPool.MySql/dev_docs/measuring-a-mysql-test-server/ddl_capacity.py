"""How many pool-database uses a MySQL server completes per second when a test suite's worth of them run at once, and how
long the DbPool's reset takes meanwhile: the suite's load on the server, reduced to its DDL.

Each of <workers> workers holds a connection and loops over a database of its own: the DbPool's reset (DROP DATABASE and
CREATE DATABASE), then a test's schema creation (the 15 pool tables), for <seconds>. Its databases are named
dbpool_measurement_capacity_<n>, and are dropped at the end.

Usage: python ddl_capacity.py <host[:port]> [<workers> [<seconds>]]      (16 workers, 30 seconds by default)
"""
import statistics, sys, threading, time
from mysql_measurement import Server

server = Server(sys.argv[1])
workers = int(sys.argv[2]) if len(sys.argv) > 2 else 16
seconds = float(sys.argv[3]) if len(sys.argv) > 3 else 30

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
ordered = sorted(resets)
print(f"{sys.argv[1]}, {workers} workers, {elapsed:.0f} s: {uses[0] / elapsed:.1f} database uses/s; the reset's median "
      f"{statistics.median(ordered):.2f} s, p90 {ordered[int(len(ordered) * 0.9)]:.2f} s, max {ordered[-1]:.2f} s")
