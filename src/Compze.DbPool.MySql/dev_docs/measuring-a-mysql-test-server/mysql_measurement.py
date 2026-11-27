"""What the tools in this folder share: reaching a MySQL server through the mysql client, and the fixture of the 15 tables
a pool database holds after a typical Compze test (pool-tables.sql).

The tools talk to the server through mysql.exe rather than a Python driver, so that they need nothing installed beyond
Python and MySQL's own client. Its per-statement timings ("Query OK, ... (1.612 sec)") are wrong on Windows: the tools
time statements with the wall clock instead.
"""
import os, subprocess

MYSQL_BIN = os.environ.get("MYSQL_BIN", r"C:\Program Files\MySQL\MySQL Server 9.7\bin")
MYSQL = os.path.join(MYSQL_BIN, "mysql.exe")
MYSQLBINLOG = os.path.join(MYSQL_BIN, "mysqlbinlog.exe")
USER = os.environ.get("MYSQL_MEASUREMENT_USER", "root")
PASSWORD = os.environ.get("MYSQL_MEASUREMENT_PASSWORD", "Development!1")
POOL_TABLES_SQL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "pool-tables.sql")


class Server:
    """A MySQL server addressed as host or host:port."""

    def __init__(self, address):
        host, _, port = address.partition(":")
        self.client_arguments = [MYSQL, "-h", host, "-P", port or "3306", f"-u{USER}", f"-p{PASSWORD}", "-N", "-B"]

    def run(self, sql):
        """Runs statements on a connection of their own; returns the result rows, their columns split."""
        result = subprocess.run([*self.client_arguments, "-e", sql], check=True, capture_output=True, text=True)
        return [line.split("\t") for line in result.stdout.splitlines()]

    def session(self):
        """A connection held open, for running statements one batch after another."""
        return Session(self)

    def create_pool_tables_sql(self, database):
        """The statements that create the 15 pool tables in a database, as a test's schema creation does."""
        tables = "".join(line for line in open(POOL_TABLES_SQL, encoding="utf-8") if not line.startswith("--"))
        return f"USE `{database}`; SET SESSION foreign_key_checks = 0; " + tables.replace("{database}", database).replace("\n", " ")

    def fsyncs(self):
        """The fsyncs (count, picoseconds) MySQL has made since it started, by file kind: data, redo log, binary log."""
        rows = self.run("SELECT event_name, count_misc, sum_timer_misc FROM performance_schema.file_summary_by_event_name "
                        "WHERE event_name IN ('wait/io/file/innodb/innodb_data_file', 'wait/io/file/innodb/innodb_log_file', "
                        "'wait/io/file/sql/binlog')")
        return {name.split("/")[-1]: (int(count), int(picoseconds)) for name, count, picoseconds in rows}


class Session:
    def __init__(self, server):
        self._process = subprocess.Popen([*server.client_arguments, "--unbuffered"], stdin=subprocess.PIPE,
                                         stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, bufsize=1)

    def run(self, sql):
        """Runs statements and waits for them; returns the result rows, their columns split."""
        self._process.stdin.write(sql + " SELECT '#END#';\n")
        self._process.stdin.flush()
        rows = []
        for line in self._process.stdout:
            if line.startswith("#END#"): return rows
            rows.append(line.rstrip("\n").split("\t"))
        raise RuntimeError("the mysql client ended: a statement failed")

    def close(self):
        self._process.stdin.close()
        self._process.wait()
