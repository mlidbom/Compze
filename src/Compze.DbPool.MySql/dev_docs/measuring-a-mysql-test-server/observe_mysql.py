"""Watches a MySQL server while a test run uses it, reading only, and says where the server's time went: the statements
by total time, the waits for metadata locks and file I/O, the fsyncs by file kind, and from the process list, sampled
every 200 ms, what ran at once and how long the DbPool's resets were seen running.

Everything it reads is on by default in MySQL 8 and 9: performance_schema's statement digests, its metadata-lock and
file-I/O instruments, and the process list. It changes nothing on the server.

Usage: python observe_mysql.py <host[:port]> <output-folder>
   Watches until the file <output-folder>/stop exists, then writes <output-folder>/summary.txt and prints it; the
   samples are in <output-folder>/processlist.tsv. For a test run:

   start the observer; C-Test -Combinations 'MySql:Microsoft:Newtonsoft:AspNetCore'; create the stop file
"""
import collections, os, statistics, sys, time
from mysql_measurement import Server

server, out = Server(sys.argv[1]), sys.argv[2]
os.makedirs(out, exist_ok=True)
PICOSECONDS = 1e12

DIGESTS = ("SELECT digest, LEFT(REPLACE(digest_text, '\\n', ' '), 90), count_star, sum_timer_wait "
           "FROM performance_schema.events_statements_summary_by_digest WHERE digest IS NOT NULL")
WAITS = ("SELECT event_name, count_star, sum_timer_wait FROM performance_schema.events_waits_summary_global_by_event_name "
         "WHERE count_star > 0 AND (event_name LIKE 'wait/lock/metadata%' OR event_name LIKE 'wait/io/file/innodb/%' "
         "OR event_name LIKE 'wait/io/file/sql/binlog%')")
FILES = ("SELECT event_name, count_misc, sum_timer_misc, count_write, sum_timer_write "
         "FROM performance_schema.file_summary_by_event_name WHERE count_star > 0")
SAMPLE = ("SELECT NOW(6), id, time, IFNULL(state, ''), LEFT(REPLACE(info, '\\n', ' '), 60) FROM information_schema.processlist "
          "WHERE command = 'Query' AND info IS NOT NULL AND id <> CONNECTION_ID();")

def snapshot(): return {query: {row[0]: row for row in server.run(query)} for query in (DIGESTS, WAITS, FILES)}

def differences(before, after, query, columns):
    """Each row's growth between the snapshots in the given numeric columns, the rows that didn't grow left out."""
    for key, row in after[query].items():
        old = before[query].get(key)
        grown = [int(row[it]) - (int(old[it]) if old else 0) for it in columns]
        if any(grown): yield row, grown

before = snapshot()
sampler = server.session()
started = time.time()
with open(os.path.join(out, "processlist.tsv"), "w", encoding="utf-8") as samples:
    while not os.path.exists(os.path.join(out, "stop")):
        for row in sampler.run(SAMPLE): samples.write("\t".join(row) + "\n")
        time.sleep(0.2)
sampler.close()
after = snapshot()

lines = [f"observed {time.time() - started:.0f} s", "", "Statements by total time (count, total s):"]
statements = sorted(differences(before, after, DIGESTS, (2, 3)), key=lambda it: -it[1][1])
lines += [f"  {count:7d} {total / PICOSECONDS:9.1f}  {row[1]}" for row, (count, total) in statements[:20]]
resets = [grown for row, grown in statements if row[1].upper().startswith(("DROP SCHEMA IF EXISTS `COMPZE_DBPOOL", "DROP DATABASE IF EXISTS `COMPZE_DBPOOL"))]
if resets:
    count, total = sum(it[0] for it in resets), sum(it[1] for it in resets)
    lines += ["", f"The DbPool's resets: {count}, {total / PICOSECONDS:.0f} s in all, {total / PICOSECONDS / count:.2f} s on average"]
lines += ["", "Waits (count, total s):"]
lines += [f"  {count:9d} {total / PICOSECONDS:9.1f}  {row[0]}" for row, (count, total) in differences(before, after, WAITS, (1, 2))]
lines += ["", "Files (fsync count, fsync s, write count, write s):"]
lines += [f"  {fsyncs:9d} {fsync_time / PICOSECONDS:8.1f} {writes:9d} {write_time / PICOSECONDS:8.1f}  {row[0]}"
          for row, (fsyncs, fsync_time, writes, write_time) in differences(before, after, FILES, (1, 2, 3, 4))]

states, longest = collections.Counter(), {}
at_once, resets_at_once = collections.Counter(), collections.Counter()
for line in open(os.path.join(out, "processlist.tsv"), encoding="utf-8"):
    parts = line.rstrip("\n").split("\t")
    if len(parts) < 5: continue
    when, connection, seconds, state, info = parts[:5]
    states[(" ".join(info.split()[:2]), state)] += 1
    at_once[when] += 1
    if info.upper().startswith("DROP DATABASE") and "COMPZE_DBPOOL" in info.upper():
        longest[(connection, info)] = max(longest.get((connection, info), 0), int(seconds))
        resets_at_once[when] += 1
if at_once:
    lines += ["", f"Statements running at once: median {statistics.median(at_once.values())}, max {max(at_once.values())}"]
if longest:
    seen = sorted(longest.values())
    lines += [f"The DbPool's resets seen running (those of a quarter second and longer): {len(seen)}; seen for, whole s: "
              f"median {statistics.median(seen)}, p90 {seen[int(len(seen) * 0.9)]}, max {seen[-1]}; at once: median "
              f"{statistics.median([resets_at_once[it] for it in at_once])}, max {max(resets_at_once.values())}"]
lines += ["", "Statements sampled, by kind and state (samples):"]
lines += [f"  {count:7d}  {kind:24s} {state}" for (kind, state), count in states.most_common(25)]
summary = "\n".join(lines) + "\n"
open(os.path.join(out, "summary.txt"), "w", encoding="utf-8").write(summary)
print(summary)
