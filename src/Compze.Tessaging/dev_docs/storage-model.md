# The storage model: the domain database, and what Tessaging keeps in it

This document takes a developer who is new to the Tessaging code from zero to understanding its storage —
whose database it is, which tables Tessaging keeps in it, how an endpoint's storage is segregated from its
neighbors', how one-process-per-endpoint is enforced, and how schemas come to exist. It is a companion to
[the Tessaging model](tessaging-model.md), which explains why storage is shaped this way (the consistency
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

`EndpointTableSet` (`Compze.Tessaging.Transport.SqlLayer`) names the five tables an endpoint owns, each
prefixed with the endpoint's name:

| Table | Holds |
|---|---|
| `«Name»_InboxTessages` | Received tessages: dedup identity, body, handling status |
| `«Name»_OutboxTessages` | Sent tessages: durable rows awaiting delivery |
| `«Name»_OutboxTessageDispatching` | Per-receiver delivery bookkeeping (including `IsStranded`) |
| `«Name»_Peers` | Durable peer memory: each remembered peer's identity |
| `«Name»_PeerHandledTessageTypes` | Each remembered peer's advertisement, one row per type |

The prefix is what makes an exactly-once endpoint's name **identifier material**: a letter followed by
letters, digits, or underscores, at most **28 characters**. The cap is derived, not chosen: 63 (PostgreSQL's
identifier byte limit, the strictest of the four backends) minus 35 (the longest identifier the schemas
generate beyond the name, `IX_«name»_OutboxTessages_Unique_TessageId`) — the derivation is documented in
`EndpointTableSet`'s remarks, and whoever adds a longer generated identifier re-derives it. A
non-conforming name fails loud at composition (`EndpointTableSet.For`), never sanitized silently: a
silently altered name would silently re-home the endpoint's storage.

Creating an endpoint needs nothing beyond the `CREATE TABLE` rights the framework's startup
schema-creation already uses, on every backend identically — which is what keeps endpoint creation a
handful of lines with zero operational ceremony. Decommissioning an endpoint's storage means dropping its
prefixed table-set and deleting its catalog entry (refused while its process lease is held); the
administration door for that act does not exist yet — it is parked as a todo at the catalog surface,
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
`CreatedUtc`, and the process lease (`LeaseHolderId`, `LeaseHolderDescription`, `LeaseHeartbeatUtc`).
Its SQL surface is `ITessagingSqlLayer.IEndpointCatalogSqlLayer`, implemented per backend. It enforces:

- **Name uniqueness**: a name only ever belongs to one endpoint. A second endpoint claiming an occupied
  name with a different `EndpointId` fails its start loud, immediately.
- **Identity stability**: an `EndpointId` never silently re-keys itself under a new name — renaming an
  endpoint means decommissioning the old storage. Also loud and immediate.
- **One process per endpoint**: the process lease, below.

The catalog also tells administration which endpoints inhabit the database
(`IEndpointCatalogSqlLayer.GetEntriesAsync`).

## The process lease

An endpoint runs in exactly one process at a time, enforced by a **heartbeat lease** in the catalog row
(`EndpointProcessLease`, `Compze.Tessaging.Implementation.EndpointCatalog`):

- **Claimed as the first act of starting to listen** — whether this process may run the endpoint at all is
  decided before anything else touches the database, and before any endpoint state mutates, so a refused
  claim leaves the endpoint fully un-started.
- **The knob is `ExactlyOnceEndpointBuilder.ProcessLeaseDuration`** (default 15 seconds). The holder
  heartbeats at a fifth of the duration; a lease whose heartbeat is older than one duration is stale.
- **A claimant finding the lease held waits out one lease duration.** A crashed predecessor's lease goes
  stale within that window and is **taken over silently** — crash recovery needs no manual cleanup. A
  holder proven alive by its heartbeats fails the claimant's start loud
  (`EndpointAlreadyRunningInAnotherProcessException`, naming the holding process and its heartbeat age).
- **A live holder whose lease is stolen** (a debugger pause or machine sleep long enough for its lease to
  go stale under a waiting claimant) discovers the loss at its next heartbeat and reports through the
  background-exception machinery.
- **Released at disposal**, after the observation drain — once nothing in the process writes to the domain
  database.
- Every catalog act runs with the ambient transaction suppressed — the lease is its own act, never part of
  a business transaction. Claims and heartbeats are single conditional statements, so racing claimants
  serialize on the row without read-then-write races. Lease timestamps come from the claimants' clocks: the
  processes sharing a domain database are assumed clock-synchronized to within a fraction of the lease
  duration (same machine, or NTP-disciplined).

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
  worth trusting for comparisons; the catalog's staleness arithmetic runs on ticks.
- **MySQL datetime columns are `datetime(6)`**: plain `datetime` has second precision, insufficient for
  heartbeat arithmetic.
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
`Given_a_domain_database_remembering_a_crashed_processes_lease.cs` pin the lease conflict and the silent
stale takeover.
