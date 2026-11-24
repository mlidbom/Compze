# The memory image state store kept in a database's tables

**Status: designed and built by state-store-in-tables, 2026-11-24, on Magnus's go (passed on by compze-for-the-tooling),
on the branch `memory-image-state-store-in-tables`: increments 2 to 4 below, rebased onto main once increment 1, which
Magnus moved to a fix of its own, landed there (8095f168a).** Magnus approved the public names,
2026-11-24, with one rename: `ChangesCommittedElsewhere` became `ChangesCommittedByOtherMemoryImages`, since "elsewhere"
doesn't say where. The brief's design, by compze-for-the-tooling, is the starting
point; what this changed in it is in [What changed from the brief](#what-changed-from-the-brief), and what building it
added in [As built](#as-built). The store as a whole is [the-taggregate-store.md](the-taggregate-store.md); this is its
storage in tables.

**Magnus, 2026-11-24:** "We support all from scratch and as always we test using the matrix test attributes. Should
SQLite turn out to be unacceptable for this use, which would hardly surprise me it is not designed for parallelism, we
can switch to one of the others." SQL Server, PostgreSQL, MySQL and SQLite from the start, every specification a `[PCT]`.

## The headline

- **Every taggregate is still held in memory,** read with no database round trip, as with the files.
- **A commit is one database transaction** in the domain database, the one an exactly-once endpoint keeps its inbox and
  outbox in: the state and the tessages it sends commit as one.
- **Several memory images share the tables:** other processes, and other containers of the same process, such as two
  exactly-once endpoints. Each holds its own memory image, and catches up with the others' commits by number.
- **Correct by construction across them:** getting a taggregate for update locks it in the database and takes its state
  from its row when another memory image has committed it since, so no update is lost; commit numbers are taken in
  commit order, so catching up misses no commit; and a memory image installs its own commit only once the database has
  committed, so memory is never ahead of the database.

## What a consumer writes: the proposed public API

Names are Magnus's to weigh; these are proposals.

```csharp
//The domain database, and the store's SQL layer for it, beside the document db's and the tevent store's:
registrar.SqliteDomainDatabase(connectionStringName)
         .SqliteMemoryImageStateStoreSqlLayer();   //or MsSql..., PgSql..., MySql...MemoryImageStateStoreSqlLayer()

registrar.MemoryImageStateStore(store => store
   .Taggregate(() => new VocabNote())
   .Taggregate(() => new KanjiNote())
   .StoredInTheDomainDatabase(NoteSerializer.JsonOptions, rows => rows.ClassNamed<VocabNote>("vocab")
                                                                       .ClassNamed<KanjiNote>("kanji")));

//Told of what other memory images committed, as this one catches up with them:
container.RootResolver.Resolve<IMemoryImageStateStore>().ChangesCommittedByOtherMemoryImages.Subscribe(changes => ...);
```

- **`MemoryImageStateStoreBuilder.StoredInTheDomainDatabase(JsonSerializerOptions, Action<TaggregateRowsBuilder>)`,**
  beside `StoredInJsonFiles`. "The domain database" is Compze's term already: the database an endpoint joins, declared
  with `SqliteDomainDatabase(...)` and its siblings, whose connection every SQL layer the endpoint registers uses.
- **`TaggregateRowsBuilder.ClassNamed<TTaggregate>(string name)`:** each class's name in the rows, as `FolderOf` names a
  class's folder. A name the consumer chooses, not the class's .NET name, so that renaming or moving a class leaves its
  rows readable. Every class the store keeps is named, and no two share a name: asserted where the store is composed.
- **`SqliteMemoryImageStateStoreSqlLayer()`,** `MsSqlMemoryImageStateStoreSqlLayer()`, `PgSqlMemoryImageStateStoreSqlLayer()`
  and `MySqlMemoryImageStateStoreSqlLayer()`, in four new packages, `Compze.Teventive.MemoryImageStateStore.Sqlite`,
  `.MicrosoftSql`, `.PostgreSql` and `.MySql`, following `Compze.DocumentDb.Sqlite` and its siblings: each contributes its
  schema and registers the store's SQL layer, internal, on the domain database's connection pool.
- **`TaggregateRowsBuilder.CatchingUpAsOtherMemoryImagesOnThisMachineCommit()`:** catching up within about 50 ms of another
  memory image's commit on the same machine, while nothing else makes this one catch up: see
  [Catching up](#catching-up-with-the-other-memory-images).
- **`IMemoryImageStateStore.ChangesCommittedByOtherMemoryImages`, an `IObservable<ChangesFoundInStorage>`:** what catching up found,
  as the existing `ChangesFoundInStorage` reports it: the ids added, changed and removed. Told in the order found, off the
  thread that found them, one batch at a time. The hub pushes them to Cloyd. Not tevents: another memory image's
  tevents were dispatched in its own process.
- **The JSON is the consumer's,** as in the files: a row holds exactly what `JsonSerializer.Serialize(snapshot, options)`
  gives, readable through the database's own tools.
- **New packages at `0.1.0-alpha`:** new, and their API still being designed.

## The tables

In the domain database, shared by every memory image of every store that uses it:

| Table | Columns |
|---|---|
| `MemoryImageStateStoreTaggregate` | `Id` (the taggregate's id, primary key), `ClassName` (the declared name), `Snapshot` (the JSON; null for a deleted taggregate), `CommitNumber` (of the commit that last wrote the row; indexed) |
| `MemoryImageStateStoreCommitCounter` | one row: `LastCommitNumber` |
| `MemoryImageStateStoreTaggregateLock` | MySQL only: `Id`, a row for each taggregate ever locked (see [Locking](#no-lost-updates-the-taggregates-lock-in-the-database)) |

- **A deleted taggregate leaves its row,** with no snapshot, at the commit number of its deletion, so that the other
  memory images find the deletion by number. An id may be used again, as in memory.
- **A store reads only the rows of the class names it declares.** Two stores in one database that declare different
  classes don't see each other's rows; the commit counter is shared.
- **Each SQL layer contributes its schema** through its database's schema contribution, created before the database's
  first use, as the document db's and the tevent store's are. Column types follow theirs: `uniqueidentifier`, `uuid`,
  `CHAR(36)` and `TEXT` for ids; `nvarchar(max)`, `text`, `LONGTEXT` and `TEXT` for the JSON, which none of them reformats.

## No lost updates: the taggregate's lock in the database

**Getting a taggregate for update, and adding one, take the taggregate's lock in the database, then read its row,** inside
the unit of work's transaction, on its connection, after taking the lock in memory as today. When the row's commit number
isn't the one this memory image holds, another memory image has committed the taggregate since: its state is taken from
the row, installed as the committed instance, and reported as found, before the unit of work gets its copy.

The lock is on the id, not on a row, since a taggregate got for update that the store doesn't hold, or one being added,
has no row yet; and a get for update that finds nothing still keeps others from adding it until the unit of work
completes, as in memory today. Each database's way, as the tevent store already locks an id:

| Database | The taggregate's lock |
|---|---|
| SQL Server | `sp_getapplock` on the id, owned by the transaction; its result is checked, and a timeout or a deadlock throws |
| PostgreSQL | `pg_advisory_xact_lock(<the store's key>, hashtext(<id>))`: the two-key form, apart from the tevent store's one-key locks |
| MySQL | the id's row in `MemoryImageStateStoreTaggregateLock`, `INSERT … ON DUPLICATE KEY UPDATE`, held by InnoDB until commit or rollback |
| SQLite | none needed: every Compze SQLite transaction begins with `BEGIN IMMEDIATE`, behind a gate for each database in each process, so it holds the database's write lock from its start |

- **The row is read after the lock is held,** in a statement of its own, so under read committed it reads what the last
  holder of the lock committed. Its snapshot is read only when its commit number differs from the one held.
- **Adding a taggregate another memory image has added fails as in memory:** its row is found and installed, and `Add`
  throws, "already holds a taggregate".
- **Two units of work in different memory images that wait for each other's taggregates** deadlock in the database, which
  detects it and fails one; within one memory image, the lock in memory times out after 30 seconds, as today.

## A commit number that never skips a commit

**A commit takes its number from the commit counter at its very end, and writes its rows with it:** the counter's row lock
is held until the transaction commits, so numbers commit in order, and a rollback gives its number back. Every row with a
number at or below the last committed one is committed, so a memory image that reads the rows above the last number it
saw misses nothing.

- **The very end:** the store's part of the unit of work prepares at the transaction's phase 0, as today: it takes the
  snapshots, and enlists one more phase-0 participant, which takes the number and writes the rows. A participant enlisted
  during phase 0 prepares after every one enlisted before, verified on .NET 11: so after the document db's flush and any
  other work at phase 0.
- **No deadlock by construction:** a transaction takes the taggregates' locks while it runs, the counter last, and only
  then writes the rows, which only it can write, since it holds their locks. Holding the counter, it waits for nothing
  another transaction could hold while waiting for the counter. A reader that takes row locks, as SQL Server's read
  committed does, holds them for a moment and waits for nothing.
- **What it costs:** commits that write the store serialize on the counter from the moment they take a number until they
  have committed: the rows' writes and the commit itself. Commits that don't write the store don't take it.

## Catching up with the other memory images

**A memory image catches up by reading the rows above the last commit number it saw:** when a unit of work first gets a
taggregate for update or adds one, and, where the consumer declares it, while idle. Reads outside a unit of work never
wait for the database: they read what the memory image last caught up with.

- **It reads the counter first, then the rows above the last number seen,** outside the unit of work's transaction, on a
  connection of its own. Every commit up to the counter's value was committed before the rows were read, so the counter's
  value becomes the last number seen. A row above it that it read as well is read again next time, and skipped.
- **Each taggregate's commit number is held beside its committed instance,** as the files hold each file's length and
  last write time: a row is installed only when its number is above the one held. So nothing installs an older state over
  a newer one, whichever of catching up, a get for update and a commit gets there first. A deleted taggregate's number is
  kept, so a row read before its deletion can't bring it back.
- **A taggregate a unit of work of this memory image holds is never installed by catching up:** while it holds the
  taggregate's lock, no other memory image can commit it, so a newer row of it is this unit of work's own commit, which the
  commit installs. Catching up checks the lock and installs under one lock in memory, which commits, gets for update and
  loads also install under.
- **Installing re-files the indexes,** from the committed instance's keys to the new one's, as a commit does.
- **What it found is reported** through `ChangesCommittedByOtherMemoryImages`, and so is what a get for update took from a row.
- **A row that can't be read or restored fails the catch-up, naming the row,** as a file that can't be read fails a load.
  Caught up while idle, the failure is logged, and tried again at the next signal.
- **Catching up while idle, on the same machine, is woken by an interprocess signal, and runs only when the consumer
  declares it:** `rows => rows.CatchingUpAsOtherMemoryImagesOnThisMachineCommit()`. Asked by Magnus (2026-11-24): whether an
  interprocess object could give near-instant updates to memory images on the same machine. It can:
  - **Every memory image of a store kept in tables raises the database's signal** once its commit has committed and is
    installed: Compze.Threading.Interprocess's `InterprocessSignal`, a counter in a memory-mapped file, raised by an
    interlocked increment. Raised whether or not anything waits for it, since a memory image can't know whether one in
    another process does.
  - **A memory image that declares it waits for the signal,** polling a memory read on the signal's polling policy, which
    backs off to at most 50 ms, and catches up when the signal moved: within about 50 ms of the commit, and with no
    database round trip while nothing commits. A task of the store's own, stopped when the container disposes the store.
  - **The signal is named from the database's identity,** a hash of the connection string the domain database's pool
    opens with. Two spellings of one database get two signals; their memory images then catch up at their next unit of
    work, as without the declaration.
  - **It is a hint, never what correctness rests on:** catching up at a unit of work's first change, and the taggregate's
    lock in the database, stay what makes the store correct.
  - **Declared, and not on by default,** by Compze's rule that an API spends nothing on what most callers don't need: even
    a memory read wakes the processor every 50 ms, and a consumer with one memory image gets nothing from it. Set aside:
    waiting only while something subscribes to `ChangesCommittedByOtherMemoryImages`, which would tie how fresh reads are to whether
    anything subscribes, unseen; and querying the database at an interval, which costs a round trip each time and answers
    no faster than its interval.
  - **Memory images on other machines** would need the database queried at an interval, a declaration of its own: designed
    here, not built, until a consumer has memory images on several machines (YAGNI).
  - Without the declaration, a memory image catches up at each unit of work's first change of the store, and when a get
    for update finds a newer row.

## Memory follows only a committed transaction

**The database commits as the transaction's decision, on all four databases; memory installs the copies afterwards, in the
transaction's second phase, before the units of work's locks are released.**

What .NET's transactions do, verified on .NET 11 with Compze's participants:

- **SQL Server's connection** enlists as the transaction's durable participant and commits in a single phase: the
  decision, after every participant has prepared and before any is told the outcome.
- **PostgreSQL's connection,** Npgsql's, enlists as a volatile participant that can commit in a single phase, and does when
  it is the only one of its phase. A commit that fails aborts the transaction.
- **MySQL's connection,** MySqlConnector's without XA, and **Compze's SQLite connection** enlist as volatile participants
  that commit when told the outcome, in the second phase, after the store's part of the unit of work, which is told
  first. A commit that fails there throws out of the second phase: the participants after it aren't told, and the
  transaction's completion never comes, so the units of work's locks are never released.

**So Compze's SQLite and MySQL connections become participants that commit as the transaction's decision,** in a single
phase, as SQL Server's and PostgreSQL's do. A commit that fails aborts the transaction, or leaves it in doubt when the
connection broke during the commit; in both, the store installs nothing. As built on main (8095f168a), they enlist as
the transaction's durable participant, which .NET asks to commit in a single phase after every volatile participant has
prepared, however many there are; a second durable participant makes the transaction distributed, which .NET refuses by
default. For MySQL that takes the connection's enlistment from MySqlConnector (`AutoEnlist=false`): Compze
begins the transaction at the isolation level of the ambient transaction, and commits it.

- **Memory installs in the second phase, as today,** so `OnCommittedSuccessfully` and the tevents' observers, which run at
  the transaction's completion, see the installed copies by construction, on every database.
- **A transaction in doubt** leaves memory behind the database if the database did commit: the next catch-up, or get for
  update, installs it from the row. Memory is behind, never ahead.
- **No process needs stopping:** the brief proposed stopping the process when a database's commit fails after the
  decision. With the database as the decision, Compze's connections never commit after it.
- **The files, `AcceptingNonAtomicCommitsWithADatabase()`,** then commit after the database on every database, not only
  SQL Server.

## Loading, and writing everything anew

- **The first load, and `LoadFromStorage`,** read the counter, then every row of the declared names, outside any
  transaction: the counter's value becomes the last number seen, and each taggregate's commit number is held.
  `LoadFromStorage` reports a taggregate changed when its row's commit number differs from the one held: no JSON compared.
  `changeTheStorageFirst` runs as today, once the memory image's units of work have completed; other memory images keep
  committing.
- **`WriteAllToStorage`** writes every row anew, as the consumer's options write the snapshot now, as one commit of its
  own: it takes the commit counter first, so no other memory image can commit meanwhile, catches up, then writes every
  row with its number. Other memory images' units of work wait for the counter at their commit.

## Crash windows and interleavings

| | What happens |
|---|---|
| A crash before the database commits | The database rolls back; the process's memory dies with it; its next start loads the tables |
| A crash after the database commits, before memory installs | Memory dies with the process; the tables have the commit, and the other memory images catch up |
| The database's commit fails | The transaction aborts: memory installs nothing; the locks are released at completion |
| The commit is in doubt | Memory installs nothing; if the database did commit, catching up or a get for update installs it from the row |
| A process dies holding taggregates' locks | The database releases them with its connection: SQL Server's application lock, PostgreSQL's advisory lock and MySQL's row lock end with the transaction, SQLite's write lock with the process |
| A commit rolls back after taking its number | The counter rolls back too, and the next commit takes the same number: no gap |
| Two memory images update one taggregate | The second waits for the first's lock in the database, then reads the row the first committed: no lost update |
| Catching up meets a taggregate a local unit of work holds | It skips it: the unit of work brought it up to date when it got it for update, and its commit installs what it committed |
| Catching up and a get for update find the same row | Both install under one lock in memory, and only above the number held: the second does nothing |
| A slow catch-up installs after a newer state | Its rows are at or below the numbers held, and do nothing; a deleted taggregate's number is kept for this |
| `LoadFromStorage` while other memory images commit | It reads the counter first: a commit it partly saw is read again by the next catch-up |

## Costs and limits

- **A round trip at a unit of work's first change of the store,** to catch up, and one for each taggregate got for update
  or added, for its lock and its row.
- **On SQLite, every transaction that writes the database runs alone,** across processes and endpoints: Compze's gate
  within a process, `BEGIN IMMEDIATE` across them. Already true of everything Compze does on SQLite.
- **Commits that write the store serialize on the commit counter** while they write their rows and commit.
- **Each memory image holds every taggregate:** two memory images in one process hold two copies.

## What changed from the brief

- **The database commits as the transaction's decision, instead of the copies installing at the transaction's
  completion and the process stopping when a database's commit fails after the decision.** Installing at completion
  would have had other completion callbacks, such as the observers' queueing and `OnCommittedSuccessfully`, run before the
  copies were installed whenever they were registered first; and stopping the process when MySqlConnector's commit fails
  wasn't possible, since that commit is MySqlConnector's. With the database as the decision, the copies install before
  every completion callback, and nothing commits after the decision.
- **The taggregate's lock is on its id, not on its row,** since a taggregate being added, or got for update and not found,
  has no row: the tevent store's way on each database.
- **The rows are written after the counter is taken, at the very end, not as the transaction prepares:** written before
  it, a transaction would hold its rows' locks while it waits for the counter, and `WriteAllToStorage`, which holds the
  counter while it writes every row, would wait for them: a deadlock.
- **Catching up reads the counter before the rows,** so that a read that isn't one snapshot, as SQL Server's read committed
  isn't, can't move the last number seen past a commit it partly missed.
- **A memory image skips, when catching up, the taggregates its own units of work hold.**

## The increments

Each builds clean, passes the full suite, and is pushed and watched on CI.

1. **Compze's SQLite and MySQL connections commit as the transaction's decision;** `FailOnPrepare` at phase 0. **Moved out
   of this work by Magnus, 2026-11-24:** a bug every user of those databases has today, so it is fixed on its own and
   published as a patch, by the session commit-as-the-decision, from this design and state-store-in-tables' draft
   (branch `commit-as-the-decision-draft`). **Landed on main, 8095f168a:** the shared participant enlists as the
   transaction's durable participant, so it commits in a single phase after every volatile participant has prepared,
   however many there are, and `FailOnPrepare` stays at phase 1. The store's guarantee that memory follows only a
   committed transaction rests on it.
2. **The store's core holds each taggregate's commit number** and installs under one lock, the storage seam gains the
   lock for update and catching up: no change in behaviour with the files.
3. **The tables:** the store's SQL layer seam, the four SQL layers and packages, `StoredInTheDomainDatabase`, and the
   specifications, two memory images on one database included.
4. **Catching up while idle, woken by the interprocess signal, and `ChangesCommittedByOtherMemoryImages`.**
5. **The documentation:** the store's public docs, this document as built, the changelogs.

## As built

What building it settled that the design above doesn't say:

- **The storage seam** (`_private/Storage/ITaggregateStorage`) gained `LockForUpdate`, `CommittedAfter`, a `WriteAll` that
  hands the store what others committed before it takes every snapshot, and `IDisposable`. The files and `NoStorage`
  give nothing from the first two. The committed taggregates, their commit numbers and the indexes moved into a class of
  their own, `CommittedTaggregates`, which installs everything under one lock.
- **A unit of work's catch-up runs inside the gate that keeps units of work and loads apart,** so a unit of work that
  begins during `LoadFromStorage` waits there, as before; the gate is left again if the catch-up throws.
- **What catching up finds of a taggregate a local unit of work holds, or waits for,** is left for that unit of work, and
  the last commit number seen stays just below it, so that the next catch-up reads it again should the unit of work
  never get the lock.
- **The SQL layer seam** (`_internal/SqlLayer/IMemoryImageStateStoreSqlLayer`), with `CommitNumber` in `_internal`: the
  four layers in the four packages, each with its schema contribution. A row's snapshot is read only when its commit
  number isn't the one held. SQL Server's application lock waits at most 30 seconds, then fails naming the taggregate;
  PostgreSQL's and MySQL's locks wait as their databases' locks do, until the transaction's timeout or a deadlock.
- **In PostgreSQL each command holds one statement.** Npgsql 9 sends the second and later statements of a synchronous
  command, and flushes the whole command, from the thread pool, while the calling thread waits for the answer. With every
  pool thread blocked, as under forty units of work queued on one deck from `Task.Run`, the server received nothing: the
  catch-up's read and the lock's read waited 1.6 to 30 seconds, and units of work timed out. Found 2026-11-24 with
  timings, `dotnet-stack` and `pg_stat_activity`, which showed the server idle, in `ClientRead`, while the client waited
  in `recv`. The tevent store's PostgreSQL insert, the PostgreSQL schema scripts, and the four schema managers' synchronous
  `EnsureSchemaInitialized` had the same dependence on a free pool thread, fixed on main the same day (0c941e289 to
  dfc0a2e9d). What remains of the kind is `dev_docs/TODO/synchronous-paths-that-need-a-free-thread-pool-thread.md`.
- **The commit signal's file** is in the user's temporary folder, `Compze/memory-image-state-store-commit-signals`, named
  by a SHA-256 of the connection string Compze's connections to the database open with: the processes of one user on one
  machine share it. `InterprocessSignal` moved from `_private` to `_internal` in Compze.Threading.Interprocess for it.
- **The specifications** (`test/Compze.Tests.Integration/CQRS/MemoryImageStateStore`): a store kept in the domain
  database, two memory images on one database as clones of one container, which share its pooled database, and two
  memory images catching up as others commit; all `[PCT]`, passing on SQLite in memory and on disk, SQL Server,
  PostgreSQL and MySQL. Among them: forty units of work in two memory images adding cards to one deck from many threads at
  once, which lose none.

## Not in this work

The files' fixes (refusing a second process, replacing files as `FileReplacement` does), files shared by processes, a
System.Text.Json serializer for tessages, the endpoints' handover gap, and the SQLite gate's missing timeout, unless this
work needs it. Releases wait for Magnus's go.
