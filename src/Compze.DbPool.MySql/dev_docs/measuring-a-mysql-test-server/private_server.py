r"""A MySQL server of one's own, for measuring: any version's mysqld, from an installation or an unpacked zip archive, on a
port and a folder of its own, configured as a test server is. What a measurement does to it, such as a setting under
trial or file_operations_per_step.py's wait history, touches no one's shared server.

Its configuration is a test server's my.ini, this machine's installed MySQL 9.7's by default, with the port, the data
folder and the base folder made the instance's own, the server reachable from this machine only, and the options its
version doesn't know left out: so that different versions run as alike as they can, as each ships them otherwise. The
data folder is initialized at the first start, with the password the other tools use for root.

Usage:
   python private_server.py start <MySQL folder> <port> <folder> [<my.ini>]
   python private_server.py stop <MySQL folder> <port>
<MySQL folder> holds bin\mysqld.exe, such as an unpacked mysql-8.4.6-winx64.zip from
https://cdn.mysql.com/archives/mysql-8.4/. <folder> gets the instance's my.ini, its data folder and the log of its start.
The other tools reach it as 127.0.0.1:<port>, not localhost, which on Windows tries IPv6 first and is refused only
after 2 s; with MYSQL_BIN set to <MySQL folder>\bin for a version before 8.0.
"""
import os, re, subprocess, sys, time
from mysql_measurement import USER, PASSWORD

INSTALLED_CONFIG = r"C:\ProgramData\MySQL\MySQL Server 9.7\my.ini"
OPTIONS_THE_INSTANCE_SETS = {"port", "datadir", "basedir", "bind_address", "mysqlx", "mysqlx_port",
                             "named_pipe", "enable_named_pipe", "socket", "shared_memory", "shared_memory_base_name"}


def normalized(option):
    return option.strip().lower().replace("-", "_")


def options_known_to(mysqld):
    """The option names a mysqld accepts, from its own help."""
    help_text = subprocess.run([mysqld, "--no-defaults", "--verbose", "--help"], capture_output=True, text=True).stdout
    return {normalized(it) for it in re.findall(r"^\s+(?:-\w, )?--([a-z0-9][a-z0-9_-]*)", help_text, re.MULTILINE)}


def is_known(option, known):
    name = normalized(option)
    return name in known or any(name.startswith(prefix) and name[len(prefix):] in known
                                for prefix in ("loose_", "skip_", "enable_", "disable_"))


def server_section(config_path):
    """The lines of a my.ini's [mysqld] section."""
    lines, in_server_section = [], False
    for line in open(config_path, encoding="utf-8", errors="replace"):
        stripped = line.strip()
        if stripped.startswith("["):
            in_server_section = stripped.lower() in ("[mysqld]", "[server]")
        elif in_server_section and stripped and not stripped.startswith("#"):
            lines.append(stripped)
    return lines


def write_config(mysql_folder, port, folder, template):
    mysqld = os.path.join(mysql_folder, "bin", "mysqld.exe")
    known = options_known_to(mysqld)
    kept, left_out = [], []
    for line in server_section(template):
        option = line.split("=", 1)[0]
        if normalized(option) in OPTIONS_THE_INSTANCE_SETS: continue
        (kept if is_known(option, known) else left_out).append(line)
    own = [f"port={port}", f"basedir={mysql_folder.replace(os.sep, '/')}",
           f"datadir={os.path.join(folder, 'data').replace(os.sep, '/')}", "bind-address=127.0.0.1"]
    if "mysqlx" in known: own.append("mysqlx=OFF")
    config = os.path.join(folder, "my.ini")
    with open(config, "w", encoding="utf-8") as file:
        file.write("[mysqld]\n" + "\n".join(own + kept) + "\n")
    if left_out: print(f"left out, unknown to this version: {', '.join(left_out)}")
    return config


def client(mysql_folder, program, port, *arguments, password=PASSWORD):
    command = [os.path.join(mysql_folder, "bin", program), "-h", "127.0.0.1", "-P", str(port), f"-u{USER}"]
    if password: command.append(f"-p{password}")
    return subprocess.run([*command, *arguments], capture_output=True, text=True)


def is_running(mysql_folder, port):
    """mysqladmin ping answers 0 while a server runs, whether or not it lets us in."""
    return client(mysql_folder, "mysqladmin.exe", port, "ping", password=None).returncode == 0


def wait_until(condition, what, seconds=120):
    deadline = time.monotonic() + seconds
    while not condition():
        if time.monotonic() > deadline: sys.exit(f"gave up after {seconds} s waiting until {what}")
        time.sleep(0.5)


def start(mysql_folder, port, folder, template=INSTALLED_CONFIG):
    mysql_folder, folder = os.path.abspath(mysql_folder), os.path.abspath(folder)
    if is_running(mysql_folder, port): sys.exit(f"a server already answers on port {port}")
    os.makedirs(folder, exist_ok=True)
    config = write_config(mysql_folder, port, folder, template)
    mysqld = os.path.join(mysql_folder, "bin", "mysqld.exe")
    is_new = not os.path.exists(os.path.join(folder, "data"))
    if is_new:
        initialized = subprocess.run([mysqld, f"--defaults-file={config}", "--initialize-insecure", "--console"],
                                     capture_output=True, text=True)
        if initialized.returncode != 0: sys.exit(f"initializing the data folder failed:\n{initialized.stderr}")
    detached = subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP
    with open(os.path.join(folder, "start.log"), "w") as log:
        try:
            subprocess.Popen([mysqld, f"--defaults-file={config}"], stdout=log, stderr=log, stdin=subprocess.DEVNULL,
                             creationflags=detached | subprocess.CREATE_BREAKAWAY_FROM_JOB)
        except OSError:  # a job that allows no breakaway: the server then ends with the job
            subprocess.Popen([mysqld, f"--defaults-file={config}"], stdout=log, stderr=log, stdin=subprocess.DEVNULL,
                             creationflags=detached)
    wait_until(lambda: is_running(mysql_folder, port), "the server answers")
    if is_new:
        set_password = client(mysql_folder, "mysql.exe", port, "-e",
                              f"ALTER USER '{USER}'@'localhost' IDENTIFIED BY '{PASSWORD}'", password=None)
        if set_password.returncode != 0: sys.exit(f"setting root's password failed:\n{set_password.stderr}")
    version = client(mysql_folder, "mysql.exe", port, "-N", "-B", "-e", "SELECT VERSION()").stdout.strip()
    print(f"MySQL {version} runs on 127.0.0.1:{port}, its data in {os.path.join(folder, 'data')}")


def stop(mysql_folder, port):
    mysql_folder = os.path.abspath(mysql_folder)
    stopped = client(mysql_folder, "mysqladmin.exe", port, "shutdown")
    if stopped.returncode != 0: sys.exit(f"shutdown failed:\n{stopped.stderr}")
    wait_until(lambda: not is_running(mysql_folder, port), "the server has stopped")
    print(f"the server on port {port} has stopped")


if __name__ == "__main__":
    if len(sys.argv) >= 5 and sys.argv[1] == "start": start(*sys.argv[2:6])
    elif len(sys.argv) == 4 and sys.argv[1] == "stop": stop(*sys.argv[2:4])
    else: sys.exit(__doc__)
