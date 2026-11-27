# A pool service shared across machines

Decided by Magnus 2026-11-17: the DbPool becomes a service. Nothing is built: it waits for Tessaging's TCP transport to
reach `main`.

## The problem

- **The DbPool coordinates its reservations per machine only.** `DbPoolMachineWideState` keeps them in an interprocess
  object under this machine's LocalAppData, shared by every test process on the machine and seen by no other.
- **Every machine names the pool databases the same:** `Compze_DbPool_v3_0001` and on.
- **So two machines that use the same database servers reset each other's databases** in the middle of their tests:
  failures that look like flakes.

It matters now because since 2026-11-17 Magnus's development machine runs its tests against threadripper's SQL Server,
PostgreSQL and MySQL (`C-Set-TestDatabaseServers`, in DevScripts), to take their load off the development machine.
Threadripper has a Compze checkout of its own. **Until the service exists, no tests run on threadripper, or on any other
machine, against the servers the development machine uses.**

## The decision

- **A pool service:** a process beside the database servers that owns the DbPool's reservations, which tests reserve
  their databases from over Tessaging. Sharing the servers is then safe by construction, for any number of machines.
- **No interim fix.** Per-machine database names would close the hazard, but the service will exist before any tests
  run on threadripper, so they would be dug in only to be filled in again (Magnus).
- **Test runs depending on the code under test is accepted.** Compze's tests getting their databases through the
  Tessaging they test was accepted long ago: "It's turtles all the way down" (Magnus).

## What it waits for

Tessaging's TCP transport: [the TCP transport and discovery across machines](../../../Compze.Tessaging/dev_docs/TODO/tcp-transport-and-discovery-across-machines.md),
built on the `tcp-protocol` branch. The AspNetCore transport binds `127.0.0.1` only, and the named-pipe transport stays on
one machine.

## What its design must handle

- **One build of Tessaging serves every branch's tests.** A branch that changes the wire format can't talk to the
  service. The service checks the format of what it receives and refuses a mismatch with a message that says so, and it
  is redeployed from `main`.
- **Running it:** a Windows service on threadripper, deployed when it changes. When it is down, every test that uses a
  database server fails, as when the servers themselves are down.
- **One setting instead of three:** test runs know only the service's address. The servers' addresses and credentials
  live in the service, where the three `COMPOSABLE_*_DATABASE_POOL_MASTER_CONNECTIONSTRING` variables hold them now.
- **Resets off the tests' critical path:** the service can clean a database after it is released, so a reservation gets
  an empty one at once. Whether that matters is to be measured first: against threadripper, the server combinations
  (4,940 tests) took 172.2 s, against 96.6 s on the development machine's own servers. A round trip there takes 0.33 ms
  against 0.10 ms locally, and threadripper's 1950X is much slower per thread. On MySQL, measured 2026-11-27: the resets
  and the tests' schema creation are most of the server's work, DDL that queues for the drive's flushes, so a reset done
  after the release would still take the same capacity from the tests. Three server settings cut it five times:
  [why the DbPool's resets take seconds on MySQL](../../../Compze.DbPool.MySql/dev_docs/why-resets-take-seconds-on-mysql.md).
- **Sqlite stays local.** Its pool databases are files on the machine that runs the tests.
