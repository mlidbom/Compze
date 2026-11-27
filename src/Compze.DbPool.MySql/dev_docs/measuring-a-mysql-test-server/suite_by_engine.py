"""The full suite on each persistence layer in turn, against the database servers of a machine of your choice, timed by
C-Measure-Command: what a change to how runs are made, such as -MaxParallelThreads, does to each engine's run, and to
MySQL's server while the MySql run goes.

Usage: python suite_by_engine.py <machine> <output folder> [--engines MsSql,PgSql,...] [-- <more C-Test arguments>...]
   <machine> is the IPv4 address of the machine whose SQL Server, PostgreSQL and MySQL the tests are to use, such as
   192.168.0.11 for threadripper's, or this-machine for this one's. The DbPool is pointed at them through this run's
   environment alone, as C-Set-TestDatabaseServers would for every run: the user's own setting is left as it is.

   Builds nothing: C-Build first. Each engine's run is C-Test -NoBuild -Combinations '<engine>:Microsoft:Newtonsoft:AspNetCore'
   with the C-Test arguments given. While the MySql run goes, observe_mysql.py watches the MySQL server.

   Writes <output folder>/<engine>.log, the MySQL observer's files under <output folder>/mysql-observer, and
   <output folder>/summary.md, which it prints: each engine's time, its tests' outcome, the processor time the rest of
   the machine used meanwhile, the failed tests, and the MySQL server's connections and resets.
"""
import argparse, collections, json, os, re, subprocess, sys, time
from xml.etree import ElementTree
from mysql_measurement import PASSWORD

here = os.path.dirname(os.path.abspath(__file__))
all_engines = ["MsSql", "PgSql", "MySql", "Sqlite", "SqliteMemory"]
server_variables = {
    "COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING":
        lambda address: f"Data Source={address};Initial Catalog=master;User Id=sa;Password={PASSWORD};TrustServerCertificate=True;",
    "COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING":
        lambda address: f"Host={address};Database=postgres;Username=postgres;Password={PASSWORD};",
    "COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING":
        lambda address: f"Server={address};Database=mysql;Uid=root;Pwd={PASSWORD};",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("machine")
    parser.add_argument("output_folder")
    parser.add_argument("--engines", default=",".join(all_engines))
    parser.add_argument("--together", action="store_true",
                        help="one run with every engine's combination in it, as the configuration file's matrix runs them")
    own_arguments, c_test_arguments = sys.argv[1:], []
    if "--" in own_arguments:
        separator = own_arguments.index("--")
        own_arguments, c_test_arguments = own_arguments[:separator], own_arguments[separator + 1:]
    arguments = parser.parse_args(own_arguments)
    out = os.path.abspath(arguments.output_folder)
    os.makedirs(out, exist_ok=True)
    repository = subprocess.run(["git", "rev-parse", "--show-toplevel"], cwd=here, capture_output=True, text=True,
                                check=True).stdout.strip()
    environment = environment_using_the_servers_of(arguments.machine)
    mysql_server = "127.0.0.1" if arguments.machine == "this-machine" else arguments.machine

    engines = arguments.engines.split(",")
    runs = [run_engines(it, repository, out, environment, c_test_arguments, mysql_server)
            for it in ([engines] if arguments.together else [[engine] for engine in engines])]

    with open(os.path.join(out, "runs.json"), "w", encoding="utf-8") as file:
        json.dump(runs, file, indent=2)
    summary = render_summary(arguments.machine, c_test_arguments, runs)
    with open(os.path.join(out, "summary.md"), "w", encoding="utf-8") as file:
        file.write(summary)
    print(summary)


def environment_using_the_servers_of(machine):
    environment = dict(os.environ)
    for variable, connection_string_for in server_variables.items():
        if machine == "this-machine":
            environment.pop(variable, None)  # the DbPool falls back to this machine's servers
        else:
            environment[variable] = connection_string_for(machine)
    return environment


def run_engines(engines, repository, out, environment, c_test_arguments, mysql_server):
    engine = engines[0] if len(engines) == 1 else "+".join(engines)
    combinations = ", ".join(f"'{it}:Microsoft:Newtonsoft:AspNetCore'" for it in engines)
    log = os.path.join(out, f"{engine}.log")
    c_test_script = os.path.join(out, f"c-test-{engine}.ps1")
    measure_script = os.path.join(out, f"measure-{engine}.ps1")
    module = f"{repository}/DevScripts/Compze.psm1"
    with open(c_test_script, "w", encoding="utf-8") as file:
        file.write(f"Import-Module '{module}' -DisableNameChecking\n"
                   f"C-Test -NoBuild -Combinations {combinations}"
                   f" -KeepResultsIn '{os.path.join(out, engine + '-results')}' {' '.join(c_test_arguments)}\n")
    with open(measure_script, "w", encoding="utf-8") as file:
        file.write(f"Import-Module '{module}' -DisableNameChecking\n"
                   f"C-Measure-Command -CommandLine 'pwsh -NoProfile -File \"{c_test_script}\"' -OutputPath '{log}'"
                   f" -WorkingDirectory '{repository}' | ConvertTo-Json -Compress\n")

    observer_folder = os.path.join(out, "mysql-observer")
    observer = None
    if "MySql" in engines:
        os.makedirs(observer_folder, exist_ok=True)
        observer = subprocess.Popen([sys.executable, os.path.join(here, "observe_mysql.py"), mysql_server, observer_folder],
                                    stdout=open(os.path.join(observer_folder, "observer.out"), "w"),
                                    stderr=open(os.path.join(observer_folder, "observer.err"), "w"))
    print(f"{time.strftime('%H:%M:%S')} {engine}: running", flush=True)
    measured = subprocess.run(["pwsh", "-NoProfile", "-File", measure_script], cwd=repository, env=environment,
                              capture_output=True, text=True)
    if observer:
        open(os.path.join(observer_folder, "stop"), "w").close()
        observer.wait()
    measurement = next((json.loads(line) for line in reversed(measured.stdout.splitlines()) if line.startswith("{")), None)
    run = {"engine": engine, "log": log, "measurement": measurement, "measure_errors": measured.stderr.strip()}
    run.update(outcome_of(log))
    if "MySql" in engines:
        run["mysql"] = mysql_lines(os.path.join(observer_folder, "summary.txt"))
    print(f"{time.strftime('%H:%M:%S')} {engine}: {run['outcome']}", flush=True)
    return run


def outcome_of(log):
    text = open(log, encoding="utf-8", errors="replace").read() if os.path.exists(log) else ""
    totals = re.findall(r"^Failed: (\d+), Passed: (\d+), Skipped: (\d+), Total: (\d+), Elapsed: ([\d.]+) seconds", text,
                        re.MULTILINE)
    failed_section = text.split("=== Failed Tests ===")[-1].split("Total unique failed tests")[0] \
        if "=== Failed Tests ===" in text else ""
    failed_tests = [line.strip() for line in failed_section.splitlines() if line.strip()]
    exceptions = failure_kinds(os.path.splitext(log)[0] + "-results")
    if not totals:
        return {"outcome": "no totals: see the log", "failed_tests": failed_tests, "exceptions": exceptions}
    failed, passed, skipped, total, elapsed = totals[-1]
    return {"outcome": f"{failed} failed, {passed} passed, {skipped} skipped, of {total}; C-Test says {float(elapsed):.0f} s",
            "failed_tests": failed_tests, "exceptions": exceptions}


def failure_kinds(results_folder):
    """What the failed tests' messages say, by how many failed tests say it: their exception types, and the database
    errors that show a server overloaded or a database reset under its test."""
    namespace = {"trx": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    markers = re.compile(r"\w+Exception\b|Too many connections|Unknown database|doesn't exist|does not exist|timeout|timed out",
                         re.IGNORECASE)
    kinds = collections.Counter()
    for folder, _, files in os.walk(results_folder):
        for name in (it for it in files if it.endswith(".trx")):
            for result in ElementTree.parse(os.path.join(folder, name)).getroot().iterfind(".//trx:UnitTestResult", namespace):
                if result.get("outcome") != "Failed":
                    continue
                message = result.findtext(".//trx:ErrorInfo/trx:Message", default="", namespaces=namespace)
                kinds.update({it.lower() if " " in it else it for it in markers.findall(message)})
    return kinds.most_common(8)


def mysql_lines(summary):
    if not os.path.exists(summary):
        return ["no observer summary"]
    wanted = re.compile(r"^(Connections open|New connections)|\bdrop_db$|\bcreate_db$")
    return [line.strip() for line in open(summary, encoding="utf-8") if wanted.search(line.strip())]


def render_summary(machine, c_test_arguments, runs):
    lines = [f"# The suite by engine, against {machine}'s servers, C-Test {' '.join(c_test_arguments) or '(as it is)'}", "",
             "| Engine | Run | Tests | Other processes' cores busy |", "|---|---|---|---|"]
    for run in runs:
        measurement = run["measurement"] or {}
        elapsed = f"{measurement['ElapsedSeconds']:.0f} s" if measurement else "not measured"
        other = f"{measurement['OtherAverageCoresBusy']:.1f}" if measurement else ""
        lines.append(f"| {run['engine']} | {elapsed} | {run['outcome']} | {other} |")
    for run in runs:
        if run["failed_tests"]:
            lines += ["", f"**{run['engine']}'s failed tests:**", ""] + [f"    {it}" for it in run["failed_tests"]]
            lines += ["", f"**What {run['engine']}'s failures say, by how many failed tests say it:** " +
                      (", ".join(f"{name} {count}" for name, count in run["exceptions"]) or "no TRX files kept")]
        if run.get("mysql"):
            lines += ["", "**MySQL's server during the MySql run:**", ""] + [f"    {it}" for it in run["mysql"]]
        if run["measure_errors"]:
            lines += ["", f"**{run['engine']}: C-Measure-Command's errors:**", "", run["measure_errors"]]
    return "\n".join(lines) + "\n"


if __name__ == "__main__":
    main()
