# The memory image state store kept in a database's tables

**Status: built by state-store-in-tables, 2026-11-24, for several memory images sharing the tables, and merged to main that
day (f1d3af771). Since 2026-11-26 one memory image at a time uses them, on Magnus's go to his item 43, built on the branch
`one-memory-image-per-storage`:** the store takes a process lock of the domain database at its first load, and what
existed only for sharing is gone. What that was, and why it went, is in
[Several memory images sharing the tables, 2026-11-24 to 2026-11-26](#several-memory-images-sharing-the-tables-2026-11-24-to-2026-11-26).
The store as a whole is [the-taggregate-store.md](the-taggregate-store.md); this is its storage in tables.

**Magnus, 2026-11-26**, on two processes sharing a store's storage: "It is specifically built to be blazing fast for ONE
process using it." "Trying to make it reliable for multiple processes would make complexity explode." "I figure compze
should forbid it and enforce it and we have to have some sort of handover." "I think it needs to be a database based lock
somehow or it will not prevent another machine from violating this on the other three sql backends."

**Magnus, 2026-11-24:** "We support all from scratch and as always we test using the matrix test attributes." SQL Server,
PostgreSQL, MySQL and SQLite, every specification a `[PCT]`.

## The headline

- **Every taggregate is held in memory,** read with no database round trip, as with the files.
- **A commit is one database transaction** in the domain database, the one an exactly-once endpoint keeps its inbox and
  outbox in: the state and the tessages it sends commit as one.
- **One memory image at a time uses the tables,** whichever process, on whichever machine, it is in: its first load takes
  a process lock of the domain database, and the store holds it until its container is disposed. What it holds in memory
  is what the tables hold, so it reads them only as it loads.

## What a consumer writes

```csharp
//The domain database, and the store's SQL layer for it, beside the document db's and the tevent store's:
registrar.SqliteDomainDatabase(connectionStringName)
         .SqliteMemoryImageStateStoreSqlLayer();   //or MsSql..., PgSql..., MySql...MemoryImageStateStoreSqlLayer()

registrar.MemoryImageStateStore(store => store
   .Taggregate(() => new VocabNote())
   .Taggregate(() => new KanjiNote())
   .WaitingForAnotherMemoryImageToReleaseTheStorage(WaitTimeout.Seconds(30))   //for a hand-over, if any
   .StoredInTheDomainDatabase(NoteSerializer.JsonOptions, rows => rows.ClassNamed<VocabNote>("vocab")
                                                                       .ClassNamed<KanjiNote>("kanji")));
```

- **`MemoryImageStateStoreBuilder.StoredInTheDomainDatabase(JsonSerializerOptions, Action<TaggregateRowsBuilder>)`,**
  beside `StoredInJsonFiles`. "The domain database" is Compze's term: the database an endpoint joins, declared with
  `SqliteDomainDatabase(...)` and its siblings, whose connection every SQL layer the endpoint registers uses.
- **`TaggregateRowsBuilder.ClassNamed<TTaggregate>(string name)`:** each class's name in the rows, as `FolderOf` names a
  class's folder, so that renaming or moving a class leaves its rows readable. Every class the store keeps is named, and no
  two share a name: asserted where the store is composed.
- **`MemoryImageStateStoreBuilder.WaitingForAnotherMemoryImageToReleaseTheStorage(WaitTimeout atMost)`,** for the files and
  the tables alike: see [One memory image at a time](#one-memory-image-at-a-time-the-tables-lock).
- **`SqliteMemoryImageStateStoreSqlLayer()`,** `MsSqlMemoryImageStateStoreSqlLayer()`, `PgSqlMemoryImageStateStoreSqlLayer()`
  and `MySqlMemoryImageStateStoreSqlLayer()`, in `Compze.Teventive.MemoryImageStateStore.Sqlite`, `.MicrosoftSql`,
  `.PostgreSql` and `.MySql`: each contributes its schema and registers the store's SQL layer, internal, on the domain
  database's connection pool, and registers the domain database's process locks.
- **The JSON is the consumer's,** as in the files: a row holds exactly what `JsonSerializer.Serialize(snapshot, options)`
  gives, readable through the database's own tools.

## The tables

| Table | Columns |
|---|---|
| `MemoryImageStateStoreTaggregate` | `Id` (the taggregate's id, primary key), `ClassName` (the declared name), `Snapshot` (the JSON) |
| `DatabaseProcessLockHolder` | the database server's process locks' holders: `LockName` (primary key), `Holder`. Not on SQLite: see below |

- **A deleted taggregate's row is deleted.**
- **A store reads only the rows of the class names it declares:** the rows of a class it no longer keeps are left as they
  are. Two stores can't use one database, even with classes of their own: the tables' lock is one for the database.
- **Each SQL layer contributes its schema** through its database's schema contribution, created before the database's
  first use, as the document db's and the tevent store's are.

## One memory image at a time: the tables' lock

**The store's first load takes the domain database's process lock `Compze.Teventive.MemoryImageStateStore`, and the
storage holds it until disposed.** The lock is the one an exactly-once endpoint holds, under another name: Compze.Sql.Common's
`IDatabaseProcessLocks`, which each engine's database package provides, moved out of Tessaging for this (2026-11-26).

| Database | The lock | Its holder's record |
|---|---|---|
| SQL Server | `sp_getapplock`, owned by the session, on a connection of its own | the `DatabaseProcessLockHolder` row, written on that session |
| PostgreSQL | `pg_try_advisory_lock`, keyed by a hash of the database's name and the lock's | the same |
| MySQL | `GET_LOCK`, named by such a hash, since its names are server-wide and at most 64 characters | the same |
| SQLite | Compze.Threading.Interprocess's `IProcessLock`, a file beside the database file | the lock's own file, as every `IProcessLock` keeps it |

- **A live holder holds it, never a lease:** the server engines' lock lives exactly as long as its session, which the hold
  keeps open, unpooled and never enlisted, and pings every 30 seconds; the operating system releases SQLite's file with
  its process. So no pause loses a live holder its lock, and a dead holder's is free at once.
- **A second memory image is refused at its first load,** in another process, on another machine, or in another container
  of the same process, since no lock is reentrant: a `TimeoutException` saying the tables are used by another memory image,
  held for what, by which process, on which machine, and since when, as the holder recorded itself, and how to declare a
  wait. A first load that fails takes nothing, and the next use tries again.
- **`WaitingForAnotherMemoryImageToReleaseTheStorage(atMost)`** has the first load wait for the holder to release the lock,
  trying every 100 milliseconds on the server engines and every 50 on SQLite, so it isn't fair. For a hand-over, such as the
  tooling hub's: Compze doesn't tell the holder that another waits, the consumer's own hand-over does.
- **SQLite's holder records itself in the lock's file, not in a table:** a write to the database waits for SQLite's write
  lock, which a unit of work could already hold on the same database when the store's first use comes inside it.
  `IProcessLock.LastHoldersDescription()` was added to Compze.Threading.Interprocess for this.
- **A lost lock stops the commits:** should the session holding the lock die under a live holder, as when the database
  becomes unreachable, another memory image may take the tables. The store logs it, and every unit of work that changes it,
  and `WriteAllToStorage`, fails from then on.

## Writing

**A unit of work's rows are written in its transaction, as the store's part of it prepares**, at phase 0, once its tevents
are dispatched: an update or an insert of each changed or added taggregate's row, and a delete of each deleted one's.
They commit, or roll back, with the transaction, whose decision the database's commit is.

- **No lock in the database for a taggregate:** the unit of work holds each taggregate's lock in memory, and the memory
  image holding the tables' lock is the only one that writes them, so no other transaction writes the same row meanwhile.
  At read committed, which Compze's transactions use, two units of work writing different rows wait for nothing of each
  other's.
- **Nothing serializes the commits that write the store,** as the commit counter did: forty units of work adding a sentence
  each from forty threads at once is a specification.

## Memory follows only a committed transaction

**The database commits as the transaction's decision, on all four databases; memory installs the copies afterwards, in the
transaction's second phase, before the units of work's locks are released.**

What .NET's transactions do, verified on .NET 11 with Compze's participants:

- **SQL Server's connection** enlists as the transaction's durable participant and commits in a single phase: the
  decision, after every participant has prepared and before any is told the outcome.
- **PostgreSQL's connection,** Npgsql's, enlists as a volatile participant that can commit in a single phase, and does when
  it is the only one of its phase. A commit that fails aborts the transaction.
- **Compze's SQLite and MySQL connections** enlist as the transaction's durable participant (8095f168a), which .NET asks to
  commit in a single phase after every volatile participant has prepared, however many there are. For MySQL that takes the
  connection's enlistment from MySqlConnector (`AutoEnlist=false`): Compze begins the transaction at the isolation level of
  the ambient transaction, and commits it.

- **Memory installs in the second phase,** so `OnCommittedSuccessfully` and the tevents' observers, which run at the
  transaction's completion, see the installed copies by construction, on every database.
- **A transaction in doubt** leaves memory behind the database if the database did commit, until the next start, or a
  `LoadFromStorage`, reads the row. Memory is behind, never ahead.
- **The files, `AcceptingNonAtomicCommitsWithADatabase()`,** commit after the database on every database.

## Loading, and writing everything anew

- **The first load takes the tables' lock, then reads every row of the declared names,** outside any transaction.
- **`LoadFromStorage`** reads every row again, and reports a taggregate changed when the state its row holds isn't the state
  the store held, as the store's serializer and the consumer's options write state: the row's text is compared with what
  the store would write for the taggregate it held, and where it differs, the snapshot loaded from it is serialized too, as
  for files (`SnapshotJson.HoldTheSameState`). For what another program changed, such as a restored backup.
- **`WriteAllToStorage`** writes every row anew, as the consumer's options write the snapshot now, in one transaction of its
  own, while no unit of work of the store is open.

## Crash windows

| | What happens |
|---|---|
| A crash before the database commits | The database rolls back; the process's memory dies with it; its next start loads the tables |
| A crash after the database commits, before memory installs | Memory dies with the process; the next start loads the tables, which have the commit |
| The database's commit fails | The transaction aborts: memory installs nothing; the locks are released at completion |
| A process dies holding the tables | The database releases the lock with its session, or the operating system SQLite's lock file with the process; the next memory image takes them at once |
| The session holding the lock dies under a live holder | The holder is told, logs it, and commits nothing more; another memory image may take the tables |

## Costs and limits

- **No round trip for a read or a get for update;** a commit's round trips are its rows' writes.
- **On SQLite, every transaction that writes the database runs alone,** across processes and endpoints: Compze's gate
  within a process, `BEGIN IMMEDIATE` across them. Already true of everything Compze does on SQLite.
- **One store per database,** and one memory image at a time of it.
- **A reload serializes each taggregate the store held,** once or twice.

## Several memory images sharing the tables, 2026-11-24 to 2026-11-26

What was built first, and removed on Magnus's go to his item 43 (2026-11-26), since the store is built to be fast for one
process, and making it reliable for several "would make complexity explode":

- **Each taggregate's lock in the database** at a get for update or an add (`sp_getapplock` owned by the transaction,
  `pg_advisory_xact_lock`, a row of MySQL's `MemoryImageStateStoreTaggregateLock`), and its row read back when another
  memory image had committed it since.
- **A commit counter** (`MemoryImageStateStoreCommitCounter`), taken at the very end of a transaction's prepare by a phase-0
  participant of its own and held until the commit, so that commits committed in the order of their numbers; a
  `CommitNumber` on every row; a deleted taggregate's row kept, with no snapshot, so that others found the deletion by
  number; and each memory image catching up, at a unit of work's first change of the store, with the rows above the last
  number it had seen. Commits that wrote the store committed one at a time.
- **`rows.CatchingUpAsOtherMemoryImagesOnThisMachineCommit()`,** catching up within about 50 ms of another memory image's
  commit on the same machine, woken by an interprocess signal in the user's temporary folder, and
  **`IMemoryImageStateStore.ChangesCommittedByOtherMemoryImages`,** telling what catching up found.
- **What stays from it:** in PostgreSQL each command holds one statement. Npgsql 9 sends the second and later statements of
  a synchronous command, and flushes the whole command, from the thread pool, while the calling thread waits for the
  answer: with every pool thread blocked, the server received nothing, and units of work timed out. Found 2026-11-24 with
  timings, `dotnet-stack` and `pg_stat_activity`. What remains of the kind is
  `dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md`.
- **The MySQL catch-up flake,** in which `Given_two_memory_images_catching_up_as_other_memory_images_on_this_machine_commit`
  timed out in 3 of 8 MySQL CI jobs before main's connection pool fixes, went with the specification and what it specified.
