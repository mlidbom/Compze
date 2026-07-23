# Storage: the domain database, and what Tessaging keeps in it

This document takes a developer who is new to the Tessaging code from zero to understanding its storage —
whose database it is, which tables Tessaging keeps in it, how an endpoint's storage is segregated from its
neighbors', how one-process-per-endpoint is enforced, and how schemas come to exist. It is a companion to
[Tessaging](tessaging.md), which explains why storage is shaped this way (the consistency
law and the domain/endpoint/process triad).

## The database belongs to the domain; endpoints join it

An exactly-once endpoint declares the **domain database it joins** (`SqliteDomainDatabase(...)`,
`MsSqlDomainDatabase(...)`, `MySqlDomainDatabase(...)`, `PgSqlDomainDatabase(...)`), never a database of
its own. The database is the domain's: the domain's aggregates, query models, and stores live in it, and
the exactly-once machinery's atomicity *is* its co-location with that data — the outbox row commits inside
the sending execution's transaction, the inbox's handled-bookkeeping inside the receiving execution's.

The named declaration registers the whole engine pairing through one call on
`ExactlyOnceEndpointBuilder.DomainDatabase`: the connection pool, the type-id interner, and Tessaging's
SQL layers for that engine — inbox, outbox, durable peer registry, and the endpoint catalog, each
contributing its own schema-creation SQL as it registers.

Any number of endpoints join one domain database. A best-effort endpoint has no database and none of the
below.

## Each endpoint owns a prefixed table-set

`EndpointTableSet` (`Compze.Tessaging.Transport.SqlLayer`) names the seven tables an endpoint owns, each
prefixed with the endpoint's name:

| Table | Holds |
|---|---|
| `«Name»_InboxTessages` | Admitted tessages: dedup identity, delivery stream position, body, handling status |
| `«Name»_InboxDeliveryStreamAdmissions` | Per-sender admission high-water marks: the inbox admits a tessage exactly when the pair's mark equals its declared predecessor |
| `«Name»_OutboxTessages` | Sent tessages: durable rows awaiting delivery |
| `«Name»_OutboxTessageDispatching` | Per-receiver delivery bookkeeping: each row's assigned `DeliveryStreamSequenceNumber`, receipt, and `IsStranded` |
| `«Name»_OutboxDeliveryStreamCounters` | Per-receiver delivery stream counters: the save transaction's increment assigns each dispatching row its sequence number, and the counter row's lock serializes the pair's commits |
| `«Name»_Peers` | Durable peer memory: each remembered peer's identity |
| `«Name»_PeerHandledTessageTypes` | Each remembered peer's advertisement, one row per type |

The prefix is what makes an exactly-once endpoint's name **identifier material**: a letter followed by
letters, digits, or underscores, at most **28 characters**. The cap is derived, not chosen: 63 (PostgreSQL's
identifier byte limit, the strictest of the four backends) minus 35 (the longest identifiers the schemas
generate beyond the name: `IX_«name»_OutboxTessages_Unique_TessageId`, and PostgreSQL's auto-named
`«name»_InboxDeliveryStreamAdmissions_pkey`) — the derivation is documented in
`EndpointTableSet`'s remarks, and whoever adds a longer generated identifier re-derives it. A
non-conforming name fails loud at composition (`EndpointTableSet.For`), never sanitized silently: a
silently altered name would silently re-home the endpoint's storage.

Creating an endpoint needs nothing beyond the `CREATE TABLE` rights the framework's startup
schema-creation already uses, on every backend identically — which is what keeps endpoint creation a
handful of lines with zero operational ceremony. Decommissioning an endpoint's storage means dropping its
prefixed table-set and deleting its catalog entry (refused while its process lock is held); the
administration operation for that act does not exist yet — it is parked as a todo at the catalog surface,
awaiting its first consumer.

## The domain-level tables are deliberately unprefixed

The **type-id interner**, the **tevent store**, the **document db**, and the **endpoint catalog** carry no
endpoint prefix: they are the domain's data, shared by every endpoint that joins. Two endpoints sharing
tevent-store tables in one domain database is the design, not a collision — the database is the domain's.

One interner per domain database means co-located endpoints agree on interned type-ids by construction. On
SQLite the interner is its own database file whose name derives from the domain database's connection
string name (`«name».TypeIdInterner`), so endpoints joining one domain database share one interner file
with no declaration; on the heavier backends the interner table simply lives in the domain database.

## The endpoint catalog

One shared table per domain database — `EndpointCatalog` — is Tessaging's only shared table: domain-level
data *about* endpoints, inherently shared. Columns: `EndpointName` (primary key), `EndpointId` (unique),
`CreatedUtc`, and — advisory bookkeeping — the recorded lock holder (`LockHolderDescription`).
Its SQL surface is `ITessagingSqlLayer.IEndpointCatalogSqlLayer`, implemented per backend. It enforces:

- **Name uniqueness**: a name only ever belongs to one endpoint. A second endpoint claiming an occupied
  name with a different `EndpointId` fails its start loud, immediately.
- **Identity stability**: an `EndpointId` never silently re-keys itself under a new name — renaming an
  endpoint means decommissioning the old storage. Also loud and immediate.
- **One process per endpoint**: the process lock, below.

The catalog also tells administration which endpoints inhabit the database
(`IEndpointCatalogSqlLayer.GetEntriesAsync`).

## The process lock

An endpoint runs in exactly one process at a time, enforced by a **process lock** — exclusivity a live
holder holds, never a time-bounded lease (`EndpointProcessLock`,
`Compze.Tessaging._private.EndpointCatalog`). A heartbeat lease was the previous design and was abandoned
on principle: no timeout can distinguish a paused-but-alive holder from a dead one, so under enough load a
live holder's lease went stale and a claimant legitimately split the endpoint across two processes.

- **Claimed as the first act of starting to listen** — whether this process may run the endpoint at all is
  decided before anything else touches the database, and before any endpoint state mutates, so a refused
  claim leaves the endpoint fully un-started.
- **The lock is a session-scoped database lock on a dedicated held connection** (MS SQL `sp_getapplock`
  session-owned, PostgreSQL `pg_try_advisory_lock`, MySQL `GET_LOCK`): it lives exactly as long as the
  session, so a crashed process's lock is released when the server notices its connection die — the
  endpoint's next process claims it with **no waiting and no manual cleanup** — and no pause, however
  long, can lose a live holder's lock: there is no timeout for load, a debugger, or a machine sleep to
  overrun. SQLite has no server sessions, so there the lock is an OS-level machine-wide mutex keyed on the
  database's identity (`SqliteEndpointProcessLockHold`) — a sqlite database is machine-local by nature, so
  a machine-wide OS lock covers every process that could open it, and the OS releases a dead process's
  mutex the same way the servers release a dead session's locks.
- **A claimant finding the lock held is refused immediately and loudly**
  (`EndpointAlreadyRunningInAnotherProcessException`, naming the holding process): the lock being held is
  itself proof of a live holder, so there is nothing to wait out.
- **The held session is kept alive by a periodic ping** (`ProcessLockSession`), defeating infrastructure
  that reaps idle database sessions. The ping failing means the domain database is unreachable from the
  holding process — the one way a live holder can lose the lock — and is reported loud through the
  background-exception machinery (`EndpointProcessLockSessionLostException`).
- **The catalog row records the holder's description** — advisory bookkeeping for the refusal message and
  administration; the lock itself is the enforcement, so a crashed process's lingering record blocks
  nothing.
- **Released at disposal**, after the observation drain — once nothing in the process writes to the domain
  database.
- Every catalog act runs with the ambient transaction suppressed — the lock is its own act, never part of
  a business transaction. No clock synchronization is assumed: nothing about the lock is a timestamp.

## Schema creation

Each SQL layer contributes its schema-creation SQL as part of registering itself (the per-backend
`«Engine»SchemaContribution` registrars), and all contributions run as one suppressed-transaction batch
before the database's first use — before any business transaction takes a lock.

Several endpoints joining one fresh domain database create the shared schemas concurrently — from one
process (a host starts endpoints in parallel) or many — and IF-NOT-EXISTS guards are not concurrency-safe
DDL on the heavy backends. Schema creation therefore serializes under the engine's advisory lock: MS SQL
`sp_getapplock`, PostgreSQL `pg_advisory_lock`, MySQL `GET_LOCK` — acquired, batched, and released on
**one** connection, because the locks are session-scoped. That is correct across connections and across
processes. SQLite needs no lock: it is single-writer by nature.

## Per-backend specifics worth knowing

- **SQLite stores datetimes as INTEGER ticks** (UTC `DateTime.Ticks`) — its driver has no datetime affinity
  worth trusting for comparisons.
- **MySQL datetime columns are `datetime(6)`**: plain `datetime` has second precision.
- **MS SQL uses `datetime2`; PostgreSQL uses `timestamptz`.** All backends read timestamps back as UTC.
- **The catalog's race-safe insert differs per engine**: SQLite `INSERT ... SELECT ... WHERE NOT EXISTS`
  (safe under the single writer), MS SQL `WITH (UPDLOCK, HOLDLOCK)`, MySQL `INSERT IGNORE` (with a
  pre-insert id-consistency read covering its swallow-anything caveat), PostgreSQL
  `ON CONFLICT (EndpointName) DO NOTHING`.
- **On SQLite a domain database is one single-writer file**: co-located busy endpoints share its write
  gate. That is the accepted price of the domain being one database, for SQLite's role; the heavier
  backends have no equivalent coupling beyond the shared commit log every one-database domain implies.

## Where the behavior is pinned

`test/Compze.Tessaging.Specifications/Storage/EndpointTableSet_specification.cs` pins the prefix and name
rules; `test/Compze.Tests.Integration/Tessaging/Given_two_exactly_once_endpoints_joined_to_one_domain_database.cs`
proves two endpoints conversing exactly-once inside one domain database, the catalog listing both, and the
catalog's identity rules;
`Given_two_hosts_each_starting_an_endpoint_with_the_same_name_and_id.cs` and
`Given_a_domain_database_remembering_a_crashed_processes_catalog_entry.cs` pin the lock refusal and the
crash-recovery start — the lock, not the lingering bookkeeping, decides.
