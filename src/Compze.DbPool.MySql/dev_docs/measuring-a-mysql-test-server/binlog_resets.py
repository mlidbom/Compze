r"""The history of the DbPool's resets on a MySQL server, from its binary logs: for each test run (activity two quiet
minutes or more apart), how many resets and CREATE TABLE statements ran, and how long the resets took.

The binary log records each DROP DATABASE with its start and exec_time, the whole seconds until it was written, which
for DDL is the statement's duration. It reads the files only: a server's binary logs, such as threadripper's through
//192.168.0.11/C$/ProgramData/MySQL/MySQL Server 9.7/Data/THREADRIPPER-bin.0000NN, can be read while it runs.

Usage: python binlog_resets.py <binlog-file> [<binlog-file> ...]
"""
import datetime, re, subprocess, sys
from mysql_measurement import MYSQLBINLOG

header = re.compile(r"^#(\d{6})\s+(\d+:\d\d:\d\d) server id \d+\s+end_log_pos \d+.*\tQuery\tthread_id=\d+\texec_time=(\d+)")

events = []  # (start, "reset" or "create", exec_time)
for path in sys.argv[1:]:
    reader = subprocess.Popen([MYSQLBINLOG, "--base64-output=DECODE-ROWS", path], stdout=subprocess.PIPE,
                              stderr=subprocess.DEVNULL, text=True, encoding="utf-8", errors="replace")
    pending = None
    for line in reader.stdout:
        match = header.match(line)
        if match:
            pending = (datetime.datetime.strptime(f"{match.group(1)} {match.group(2).zfill(8)}", "%y%m%d %H:%M:%S"), int(match.group(3)))
        elif pending and not line.startswith(("SET ", "/*", "#", "use ")):
            statement = line.upper()
            if statement.startswith("DROP DATABASE") and "COMPZE_DBPOOL" in statement: events.append((pending[0], "reset", pending[1]))
            elif statement.startswith("CREATE TABLE"): events.append((pending[0], "create", pending[1]))
            pending = None
    reader.wait()

events.sort()
runs, current = [], []
for event in events:
    if current and (event[0] - current[-1][0]).total_seconds() > 120:
        runs.append(current)
        current = []
    current.append(event)
if current: runs.append(current)

print(f"{'run start':19s} {'minutes':>7s} {'resets':>6s} {'creates':>7s}   the resets' exec_time: 0 s, 1-2 s, 3-9 s, 10 s or more, max")
for run in runs:
    resets = [seconds for _, kind, seconds in run if kind == "reset"]
    if len(resets) < 50: continue
    share = lambda low, high: sum(1 for it in resets if low <= it <= high) / len(resets)
    print(f"{run[0][0]:%Y-%m-%d %H:%M:%S} {(run[-1][0] - run[0][0]).total_seconds() / 60:7.1f} {len(resets):6d} "
          f"{sum(1 for _, kind, _ in run if kind == 'create'):7d}   {share(0, 0):5.0%} {share(1, 2):6.0%} {share(3, 9):6.0%} "
          f"{share(10, 10 ** 9):7.0%} {max(resets):5d}")
