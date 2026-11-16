# The taggregate store

**Status: designed by jastudio-rep-opus and jastudio-rep-fable on Magnus's direction, 2026-11-16, with JAStudio's facts
from taggregates-fable.** The in-memory store is prototyped and specified on the branch `taggregate-store-opus`. The
JSON store and the snapshot API are designed, not built. Every name is a placeholder for Magnus's pass over the names.

## The headline

**A Compze taggregate store keeps taggregates as their state, not their history, under the tevent store's programming
model: a session for each unit of work, and the unit of work a System.Transactions transaction, even with no storage
at all.** It replaces three parts of the earlier conclusions:
- the hook at the end of a publish cascade, and its queue;
- the snapshot made of tevents;
- JAStudio's hand-written commit, save and dispatch.

What the prototype shows:

- **The programming model holds without storage.** A unit of work that gets a taggregate for update, changes it and
  commits costs 8.5 to 12.5 µs in Release, of which the transaction and scope are 1.2 to 1.8 µs. That is nothing for
  JAStudio's edits, which are rare and one at a time.
- **Deferred dispatch works as designed.** Participation handlers run once the unit of work commits, never inside a
  domain method, and what they cause is dispatched in the same pass, in publish order. 23 specified behaviours pass on
  both pluggable component combinations.
- **The two reentrancy bugs can't arise,** since nothing calls out from inside `Publish` or `Commit`.

**The cost JAStudio's answers show is the private copy.** Under the isolation Magnus chose, the committed instance is
read only, and every commit swaps in a new one. Few things hold a note across edits. But every place that edits one
changes: today an edit is a call on the note one has, and with the store it is a call on the copy the unit of work
gets for update. That reopens the choice of isolation, which is decision 1 below.

## What Magnus set

- **A Compze component manages a process's taggregates** and routes their tevents through Tessaging, so that JAStudio
  "just teals in Teventive and Tessaging components".
- **An `ITaggregateStore` with an `ITaggregateStoreUpdater` session,** mirroring `ITeventStore` and `ITeventStoreUpdater`:
  the same programming model, implemented in memory and backed by JSON, with the storage behind a narrow interface.
- **Transactions as the unit of work even with no storage:** "The .NET machinery is super battle tested."
- **Tevents escape a taggregate only once its cascade is settled.**
- **`Commit` never runs domain code; `Save` is not a domain mutation,** and causes no tevents.
- **Snapshots carry no tevents:** the implementor writes its own serializable snapshot type, and its own restoring.
- **Isolation by a private copy for each unit of work** (his option (a)), JSON as the durable truth, and a full rebuild
  allowed as one transaction, since it takes 1 to 2 s.

## The programming model

```csharp
builder.Registrar.InMemoryTaggregateStore();   // or the JSON store
builder.Registrar.LocalTessagingEngine(engine => engine
   .RegisterTessageBusHandlers(handle => handle.ForTevent((IVocabMeaningChanged it, ITaggregateStoreUpdater taggregates) => ...))
   .ObserveTevents(observe => observe.ForTevent((IVocabMeaningChanged it) => ...)));

container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
   unitOfWork.Resolve<ITaggregateStoreUpdater>().Get<VocabNote>(id).ChangeMeaning("to eat"));

var vocab = container.RootResolver.Resolve<ITaggregateStoreReader>().Get<VocabNote>(id);   // committed, read only
```

- **`ITaggregateStoreUpdater`,** scoped, one for each unit of work: `Get`, `TryGet`, `Save` and `Delete`, as
  `ITeventStoreUpdater` has.
- **`ITaggregateStoreReader`,** for the container: the committed instance itself, at no cost.
- **`LocalTessagingEngine`** is all of Tessaging a consumer like JAStudio composes: no endpoint, no network, and no type
  mappings for tevent types that never leave the process. The prototype maps none.
- **Possibly one interface for both stores:** `ITeventStoreUpdater` has the same four members, so it could be
  `ITaggregateStoreUpdater` with the tevent store as its other implementation. A naming and structure question for
  Magnus.

## Isolation: a private copy for each unit of work

- **A private copy** is the instance one unit of work changes: `Get` builds it from the committed instance through the
  taggregate's snapshot, in 0.7 to 1.2 µs for the prototype's small note, and less once tevent routing is decided per
  class, since on `main` each new instance still builds its dispatchers.
- **The committed instance** is what readers get. Publishing on it throws. The commit makes the copy the committed
  instance; a rollback discards the copy, and the committed instance was never touched.
- **A lock for each taggregate makes writers wait for each other.** It is owned by the unit of work's transaction:
  taken at the first get for update and released when the transaction completes, possibly on another thread.
  - **It is a `SemaphoreSlim`** with a timeout that names the taggregate. The locking standard gives `SemaphoreSlim`
    exactly this job: "a gate held across awaits, or released on another thread".
  - **A Compze lock owned by a transaction,** with diagnostics naming the holding transaction and where it took the
    lock, would be better: a recommendation, not a blocker.
  - **Only held locks are kept,** in a table of the taggregates being updated, not a lock for each of 80,000 notes. The
    prototype keeps one for each taggregate ever updated.
  - **JAStudio's race between a background reparse and a UI edit** then serializes, instead of corrupting the vocab or
    throwing.
- **A committed instance is read by many threads at once,** so what a taggregate computes lazily and keeps must be
  safe for that. JAStudio's forms are such a case.
- **The version:** the copy continues the committed instance's version, which the store sets after restoring, and no
  version is persisted. JAStudio would see one only as noise in every diff and a conflict in every merge.

### What the private copy costs JAStudio, from taggregates-fable's answers

- **Every edit site changes.** UI, MCP tools, importers, batches and nearly every specification edit by calling a
  method on the note they hold. With read-only committed instances, each edit gets the unit of work's copy instead. Not
  counted, and larger than the 51 `NoteGuard.Update` sites counted so far.
- **What holds a note across edits must follow the swap:** the caches' indexes, six view model classes, and the MCP
  server for one tool call. Other notes hold only ids or `WordReference`s. JAStudio already has the rule that a note
  object can be replaced (`JPCollection.ReloadFromBackend`), so the UI's holders follow it today.
- **The indexes are the costly holder.** Their sets are `HashSet<TNote>`, copied to be changed, since lookups take no
  lock. A new instance at each commit would copy every set the note is in, and a common word's set holds thousands of
  sentences.
- **A taggregate handle,** a small object the store keeps for each taggregate, is taggregates-fable's recommendation
  over bare ids:
  - indexes and view models hold handles, which never change;
  - inside a unit of work, a handle resolves to that unit of work's copy if it has one, and to the committed instance
    otherwise.
  - An edit in JAStudio reads, in its middle, notes it has already changed: `PerfectSynonyms` walks its web through
    lookups. A handle gives it its own changes in one place.
- **What the notes hold that isn't stored,** what Anki says of their cards, would leave the notes: taggregates-fable
  would move it out, rather than have it in a snapshot.

### The alternative: shared live instances, locked, rolled back from a snapshot

Magnus's option (b), set aside before these facts were known. **Get for update** returns the live instance, locked for
the unit of work, and takes a snapshot of it at the first change. **A rollback** restores the instance from that
snapshot. **A change made outside any unit of work** becomes a unit of work of its own, which taggregates-fable asks for.

| | Private copy (a) | Shared live instances (b) |
|---|---|---|
| Edit sites in JAStudio | Every one changes, to get the copy | Unchanged |
| References to notes | Handles, or ids | Unchanged, and the indexes keep their sets |
| What readers see | Committed state only | Changes not yet committed, as today, and changes that are then rolled back |
| A rollback | Discards the copy | Restores the shared instance in place, while other threads may read it |
| Committed instances | Never change, so reading them is safe on any thread | Change under readers, as today |
| Same model as the tevent store | Yes: each session has instances of its own | No: `Get` returns the instance every session shares |

**Our recommendation stays (a), with handles,** because it is the tevent store's model and the only one in which a
reader never sees state that is rolled back. But its cost to JAStudio is the larger one. Whether that cost is worth
paying is part of the open question of whether JAStudio adopts taggregates at all: see JAStudio's shared folder.

## Dispatch: deferred to the commit

- **Deferred dispatch:** a tracked copy's tevents are only collected, in the order published, from its `TeventStream`.
  `Save` registers a new taggregate and takes the tevents it published before, and dispatches nothing.
- **At phase 0** the session dispatches them through `IUnitOfWorkTeventPublisher`, so participation handlers run
  inside the transaction. Phase 0 is the transaction's prepare phase, in which a `VolatileTransactionParticipant` with
  `EnlistDuringPrepareRequired` may still do work in the transaction, as `EnlistInAmbientTransactionUsageGuard` already
  does.
  - **What handlers cause** is collected in turn, and dispatched in the same pass, until nothing is left.
  - **Observers** are queued as each tevent is dispatched, and observe after the commit, off the publishing thread, in
    order for each observer: cause before consequence.
- **Why at the commit, not at the end of each cascade:** a domain method that publishes twice would otherwise have its
  participation handlers run between the two, see a half-done operation, and change the taggregate under the method's
  feet. That is the callout behind both bugs, moved.
- **What it gives:**
  - nothing runs while a domain method is on the stack;
  - nothing is dispatched when the body of the unit of work throws, so there is nothing to undo;
  - a participation handler may change the taggregate whose tevent it handles, since that is an ordinary cascade on
    the copy, dispatched in order. Decision 6 of the earlier conclusions dissolves.
- **What it costs:** code later in the same unit of work doesn't see what participation handlers do. JAStudio has
  such code: two creations of a vocab with one question in one unit of work would both miss the index. Either a limit
  to state, or derived state that reads its own unit of work's changes: see below.
- **A failed publish rolls the unit of work back,** even if the consumer catches its exception: the session refuses to
  prepare, with "A publish failed on ... in this unit of work". The refusal rule of the earlier conclusions shrinks to the
  private copy, and JAStudio replaces no references after a throw.

### What the deferred dispatch must also do, from jastudio-rep-fable's adversarial read

1. **The queue belongs to the unit of work, not to a store's session.** With two stores in one unit of work, the
   transaction prepares one before the other, and never one twice. A handler in the second one's dispatch that changes
   a taggregate of the first would publish tevents never dispatched, and state never written. So: one queue for the
   unit of work, and each store's prepare dispatches everything, then writes its own.
2. **A session that has written refuses every later change,** with a message that says so.
3. **Tevents published straight through `IUnitOfWorkTeventPublisher` are dispatched at once,** and so overtake the
   deferred tevents of taggregates. One order for the whole unit of work needs the publisher itself to defer: decision 4.
4. **A bound on how long one dispatch may go on,** in rounds of tevents caused by tevents, with a message naming the tevent
   types that keep coming: two handlers that cause each other's tevents otherwise hang the commit.
5. **A handler that fails surfaces where the transaction completes,** as a `TransactionAbortedException`, far from
   the domain call. The message names the tevent and its taggregate.

### Derived state, such as JAStudio's indexes

Derived state is what a consumer keeps in memory beside its taggregates and computes from them. JAStudio needs its
indexes changed with the commit, before the edit returns. Nothing short of a lock for readers makes several notes and
their indexes change at one instant for every reader, since the notes themselves are swapped one at a time. What can be
given is that when `ExecuteUnitOfWork` returns, notes and indexes both show the commit.

| Way | When it changes | What it costs |
|---|---|---|
| A participation handler writing plain memory | At phase 0, before the swap | A rollback doesn't undo it |
| An observer | After the commit, off the publishing thread | Readers see new notes with old indexes until it has run |
| A call from the store at the swap, on the committing thread | With the swap | Consumer code runs at commit, and must not fail |
| **Derived state that is itself transactional** (recommended) | Written by a participation handler at phase 0, applied at commit | It must be built: a small transactional in-memory map from Compze, or the consumer's own `VolatileTransactionParticipant` |

- **The fourth way keeps Magnus's premise,** transactions as the unit of work: a rollback undoes it, and no consumer code
  runs at commit.
- **Its changes are kept as differences and applied at commit,** not as copies prepared ahead. Two units of work that
  each prepare a copy of one index set would otherwise lose each other's change.
- **A lookup inside a unit of work reads its own writes,** which removes the limit above. JAStudio's two sessions ask for
  it: after a vocab is renamed from x to y, a lookup of x must not find it and a lookup of y must. A lookup takes the
  committed index's entries for the key, drops the notes whose unit-of-work copy no longer has the key, and adds that
  unit of work's copies that have it. For that, **the session lists the taggregates it holds, by type:** those got for
  update, those created, and those deleted. JAStudio's `NoteIndex` can then do the rest itself.
- **JAStudio's `NoteIndex` nearly works so already:** its changes go to copies, and `PublishChanges` puts them in place.
- **An index entry must follow the swap:** the copy becomes the committed instance, but entries under keys that didn't
  change still point at the old one. An index holds handles or ids, or files the whole note again, as JAStudio does today.
- **Effects that can't be rolled back,** such as telling Anki to delete a study note, belong after the commit and before
  the edit returns: `IUnitOfWorkResolver.OnCommittedSuccessfully`, which runs before `ExecuteUnitOfWork` returns.

## Snapshots

```csharp
///<summary>A taggregate whose whole state can be taken as a snapshot, and restored from one.</summary>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>
{
   TSnapshot TakeSnapshot();
   static abstract TSelf RestoreFrom(TSnapshot snapshot);
}
```

- **The restoring is a static member of the class,** so a concrete class in a hierarchy, such as a vocab under
  `JPNote`, restores from its own snapshot type.
- **A non-generic face for the store,** which holds taggregates of many types: a default interface member that copies,
  as `TSelf.RestoreFrom(TakeSnapshot())`.
- **Each taggregate class is named once where the store is composed,** such as `AddTaggregate<VocabNote>()`, so that
  loading knows which class restores from which snapshot type.
- **`Taggregate` gets a protected way to say it was restored,** with its id. Today that needs the `[Obsolete]` internal
  `Id` setter of `Entity`. The prototype's is `RestoreIdentity(id, version)`.
- **A snapshot holds the taggregate's whole state,** not only what is stored, since every copy is made from it. A
  component's and a shared tomponent's state are nested in the root's, as in JAStudio's DTOs. A snapshot can share every
  immutable value with the committed instance.
- **Restoring works outside any store:** JAStudio's MCP server builds notes from two git versions of a file.
- **Two ways of setting state now exist,** the tevent appliers and the restoring, which Magnus has decided for. What
  guards them: a specification helper from Compze that takes a snapshot, restores from it, takes another and compares
  the two. Since every update goes through a snapshot, a property the snapshot misses also fails the first
  specification that updates twice.
- **Loading at a start runs no tevents:** the 0.4 GB of garbage a start made in both teams' prototypes goes, and with it
  decision 11 of the earlier conclusions.

**JAStudio already has all three parts, by other names:** `INoteRepository` is the storage seam, `VocabDto`,
`SentenceDto` and `KanjiDto` are the snapshot types, and its converters take and restore.

## Storage

```csharp
///<summary>Where a taggregate store keeps what it has committed. Narrow: it knows snapshots, not taggregates.</summary>
public interface ITaggregateStorage
{
   IReadOnlyList<StoredSnapshot> LoadAll();
   IPreparedWrite Prepare(IReadOnlyList<SnapshotToSave> toSave, IReadOnlyList<TaggregateId> toDelete);
}

public interface IPreparedWrite
{
   void Commit();   // must not fail for a reason Prepare could have found
   void Discard();
}
```

- **Two steps, because a transaction has two:**
  - the session calls `Prepare` at phase 0, once the unit of work's queue is empty and it has taken its copies' snapshots;
  - it calls `Commit` when the transaction commits, then swaps in the copies, then releases the locks;
  - it calls `Discard` at a rollback.
- **Everything that can fail belongs in `Prepare`:** serializing, writing, a full disk, a file another program holds.
- **In memory, `Prepare` does nothing.**
- **`LoadAll` is the consumer's** where it has its own way to start fast. JAStudio's `snapshot.bin`, a cache of all
  notes, is read first, then only the files written since.
- **Loading everything again while running,** as JAStudio does around a git fast-forward, needs a store operation of its
  own: no tevent describes such a change. Not designed yet.

### The JSON store

- **Two layers.** The files' layout and format are the consumer's, since JAStudio's corpus is reviewed as diffs. JAStudio
  keeps one file for each note, under `<corpus>/<vocab|kanji|sentences>/<two hex digits>/<id>.json`. Writing several
  files as one, the part easy to get wrong, is Compze's. Compze ships a plain default: a folder for each type, a file
  for each id, System.Text.Json.
- **All or nothing across files:**
  1. at prepare, each file is written whole under a temporary name, in a folder of the store's own on the same volume;
  2. at commit, a journal is written first, listing each temporary file with the name it is to get, and each file to
     delete. **The journal is the commit:** once it exists, the write has happened;
  3. then the renames and deletions, then the journal is deleted;
  4. at start, before anything is loaded, a journal that exists is carried out and deleted, and temporary files without
     one are deleted.
- **The journal is written at commit, not at prepare:** written at prepare, a crash before a rollback had removed it
  would make the next start carry out a write that was rolled back.
- **A failure after the journal is written stops the process,** and the next start finishes the write. A rename that
  fails because another program holds the file is retried first: on Windows a virus scanner or an editor can.
- **Not flushed to the disk by default:** a commit survives the process dying, not the machine losing power. Flushing
  80,000 files would take minutes for a rebuild. JAStudio's corpus is in git. A setting for consumers that need more.
- **For JAStudio** a single file written whole is already a gain: today `File.WriteAllText` writes in place, and a
  truncated file makes the next start fail. All or nothing across notes matters less: what a person entered across
  notes, such as a rename with its word references, can be left half written today too, and git shows and reverts it.
- **Not settled:** a transaction with a database in it too. The JSON store is a volatile participant, so the database
  can commit and the process die before the journal is written. JAStudio has no database.

## The tevent store takes the same internals

- **Its session would dispatch at phase 0 too,** from the unit of work's queue, and stop calling out from `Commit`'s
  callback and from `TeventStream`. Both of its bugs found by the reviews are then fixed by construction.
- **It changes what code later in a unit of work sees:** today participation handlers run as each tevent is published.
  Some tevent store specifications may assert that. Not counted. Decision 5.
- **`Commit`'s callback goes,** and `TeventStream` as a public stream any code may observe goes with it: the session is
  then the only thing a tracked taggregate tells. A public stream is what made bug 1 possible.

## A rebuild

One transaction for a whole rebuild means a copy and a lock for every note until the commit, and every handler run on
one thread at the end. A unit of work for each note costs about 10 µs and 9.5 KB, so about 0.8 s of one core and 0.76 GB
of garbage for 80,000. **We'd take batches,** of some hundreds of notes for each unit of work, on many threads, and
measure that shape. Generated data can be generated again, so a failed batch costs little.

## What isn't designed yet

- Loading everything again while running.
- A second session of one transaction getting a taggregate another session of it holds: it waits for its own
  transaction until the timeout. It should be refused at once, or both sessions share the copy.
- A wait for observers to be at rest that needs neither a testing host nor stopping the engine:
  `ILocalTessagingEngine.StopAsync` stops it. JAStudio's specifications need one.
- A database and the JSON store in one transaction.
- The storage seam's error when `Commit` fails anyway.

## What the prototype is

Branch `taggregate-store-opus`, commit `3dd0fbe08`, in `C:\Dev\Compze.worktrees\taggregate-store-opus`, based on `main`.

- **The store,** in `src/Compze.Teventive.TeventStore/TaggregateStore/`: `ITaggregateStoreUpdater`, `ITaggregateStoreReader`,
  a snapshot interface simpler than the designed one, and the session as a `VolatileTransactionParticipant`.
- **In Teventive:** a read-only mode for committed instances, a record that a publish failed, and `RestoreIdentity`, behind
  an internal `ITaggregateStoreInternals`.
- **The specifications,** in `test/Compze.Tests.Integration/CQRS/TaggregateStore/`: 23 behaviours on both pluggable
  component combinations, among them that a reader during a unit of work sees the committed state, that a body that
  throws runs no handler and no observer, that the copy is swapped in before `OnCommittedSuccessfully` runs, and that
  two units of work on two threads serialize on the lock.
- **`Compze.Tests.Unit` passes,** 417 of 417. The other test assemblies weren't run on it.
- **Not in it:** the queue for the unit of work, the bound on rounds, the JSON store, handles, transactional derived
  state, and tevent routing per class.

| Measured in Release, three runs, the machine at 3 to 7% load | Time, each | Allocated, each |
|---|---:|---:|
| An empty unit of work | 1.2 to 1.8 µs | 1.9 KB |
| A unit of work that gets a vocab, changes it once and commits | 8.5 to 12.5 µs | 9.5 KB |
| A copy of a two-string vocab through its snapshot | 0.7 to 1.2 µs | 1,160 B |
| A new vocab, through its creation tevent | 1.8 to 2.4 µs | 2,552 B |

Each range covers the two DI containers of the pluggable component combinations, over the three runs.
