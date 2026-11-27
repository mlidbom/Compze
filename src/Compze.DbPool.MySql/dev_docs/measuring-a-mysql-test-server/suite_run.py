"""A run of Compze's tests on MySQL against a server of your choice, observed: observe_mysql.py watches the server while
C-Test runs the tests on MySql:Microsoft:Newtonsoft:AspNetCore with their DbPool pointed at <server>, for this run
only. The machine's own setting, the environment variable COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING, is left
as it is, and so is every other run's.

Usage: python suite_run.py <host[:port]> <output folder> [<more C-Test arguments>...]
   Writes the run's log, <output folder>/c-test.log, and the observer's files, and prints how long the run took, the end
   of its log and the observer's summary.
"""
import os, subprocess, sys, time
from mysql_measurement import PASSWORD, USER

here = os.path.dirname(os.path.abspath(__file__))
server, out, more_arguments = sys.argv[1], os.path.abspath(sys.argv[2]), sys.argv[3:]
os.makedirs(out, exist_ok=True)
host, _, port = server.partition(":")
repository = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=here, capture_output=True, text=True,
                            check=True).stdout.strip()
environment = dict(os.environ, COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING=
                   f"Server={host};Port={port or 3306};Database=mysql;Uid={USER};Pwd={PASSWORD};")

with open(os.path.join(out, "observer.out"), "w") as observer_out, open(os.path.join(out, "observer.err"), "w") as observer_err:
    observer = subprocess.Popen([sys.executable, os.path.join(here, "observe_mysql.py"), server, out],
                                stdout=observer_out, stderr=observer_err)
    command = (f"Import-Module '{repository}/DevScripts/Compze.psm1' -DisableNameChecking; "
               f"C-Test -Combinations 'MySql:Microsoft:Newtonsoft:AspNetCore' {' '.join(more_arguments)}")
    started = time.monotonic()
    with open(os.path.join(out, "c-test.log"), "w", encoding="utf-8") as log:
        test = subprocess.run(["pwsh", "-NoProfile", "-Command", command], cwd=repository, env=environment, stdout=log,
                              stderr=subprocess.STDOUT)
    elapsed = time.monotonic() - started
    open(os.path.join(out, "stop"), "w").close()
    observer.wait()

print(f"C-Test against {server} exited {test.returncode} after {elapsed:.0f} s; its log is {os.path.join(out, 'c-test.log')}")
log_lines = open(os.path.join(out, "c-test.log"), encoding="utf-8", errors="replace").read().splitlines()
print("\n".join(log_lines[-15:]))
summary = os.path.join(out, "summary.txt")
print(open(summary, encoding="utf-8").read() if os.path.exists(summary) else f"no summary: see {out}/observer.err")
