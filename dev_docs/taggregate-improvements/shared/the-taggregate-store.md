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

**JAStudio's answers reopen one choice Magnus made: isolation.** He chose a private copy for each unit of work. Under
it every commit swaps in a new instance, so the note object JAStudio's code holds can't stay the same, and every place
that edits a note changes. JAStudio's two sessions say that this decides the cost for JAStudio. **We recommend shared
live instances instead, "read uncommitted",** as Magnus's decision, with the trade shown below. The prototype implements
the private copy; the rest of the design holds for either.

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

var vocab = container.RootResolver.Resolve<ITaggregateStoreReader>().Get<VocabNote>(id);
```

- **`ITaggregateStoreUpdater`,** scoped, one for each unit of work: `Get`, `TryGet`, `Save` and `Delete`, as
  `ITeventStoreUpdater` has.
- **`ITaggregateStoreReader`,** for the container: the instance itself, at no cost. Under isolation (a) it is the
  committed instance, read only; under (b), the live instance.
- **`LocalTessagingEngine`** is all of Tessaging a consumer like JAStudio composes: no endpoint, no network, and no type
  mappings for tevent types that never leave the process. The prototype maps none.
- **Possibly one interface for both stores:** `ITeventStoreUpdater` has the same four members, so it could be
  `ITaggregateStoreUpdater` with the tevent store as its other implementation. A naming and structure question for
  Magnus.

## Isolation: Magnus's decision, reopened by JAStudio's answers

**Three ways for a store to keep a unit of work's changes from other code.** Magnus chose (a) before JAStudio's answers
were known. We recommend (b). (c) would give both, and is a redesign.

| | (a) A private copy for each unit of work | (b) Shared live instances: "read uncommitted" | (c) Stable objects, state versioned per transaction |
|---|---|---|---|
| Edit sites in JAStudio | Every one changes, to get the copy | Unchanged: `vocab.Question.SetText("x")` | Unchanged |
| The note object code holds | Replaced at every commit: holders keep handles or ids | The same object for good | The same object for good |
| What readers on other threads see | Committed state only | Changes not yet committed, and some later rolled back | Committed state only |
| A rollback | Discards the copy | Restores the live instance in place | Discards the transaction's state |
| A rebuild in memory | A second copy of every note it changes | No copy | A second state of every note it changes |
| Same model as the tevent store | Yes | The session reads the same, the isolation differs | Yes |
| In Compze | Prototyped | Designed, not built | A taggregate's state held as an immutable value, its snapshot type, with appliers as transitions: how every taggregate is written changes. Not designed |

**Why we recommend (b):** the store exists for a consumer that holds live objects for good. Its only consumer says that
keeping the object it holds decides the cost. (a) is the cleaner model, but it is the tevent store's model for code that
never keeps a taggregate across units of work, which is the opposite of JAStudio. (b) also costs less to run: no copy for
each update, no second corpus in memory during a rebuild, and no handles. **And (b) keeps (c) open, which (a) doesn't:**
under both the note object and the edit sites stay, so going from (b) to (c) later changes Compze and the insides of the
taggregates, not their callers. taggregates-fable's point.

### What every way shares

- **A lock for each taggregate makes writers wait for each other.** It is owned by the unit of work's transaction: taken
  at the first get for update, or the first change, and released when the transaction completes, possibly on another
  thread.
  - **It is a `SemaphoreSlim`** with a timeout that names the taggregate. The locking standard gives `SemaphoreSlim`
    exactly this job: "a gate held across awaits, or released on another thread".
  - **A Compze lock owned by a transaction,** with diagnostics naming the holding transaction and where it took the
    lock, would be better: a recommendation, not a blocker.
  - **Only held locks are kept,** in a table of the taggregates being updated, not a lock for each of 80,000 notes. The
    prototype keeps one for each taggregate ever updated.
  - **JAStudio's race between a background reparse and a UI edit** then serializes, instead of corrupting the vocab or
    throwing. The thread assertion of the earlier conclusions isn't needed.
  - **A unit of work must not wait for the UI thread while it holds a taggregate's lock:** with the UI thread waiting
    for that taggregate, both stand still until the timeout. JAStudio's progress presenters call
    `InvokeSynchronouslyOnUIThread` when a task or a batch starts. Today's batches start their progress before they
    touch a note, so a unit of work for each note is safe, and a whole rebuild as one transaction is not.
- **A taggregate is read by many threads at once,** so what it computes lazily and keeps must be safe for that.
  JAStudio's forms are such a case.
- **The version** is kept in memory and not persisted. JAStudio would see one only as noise in every diff and a conflict
  in every merge.
- **The session's interface stays the tevent store's:** `Get` for update, `TryGet`, `Save`, `Delete`. Code that uses
  sessions reads the same against either store. What differs is the isolation each store gives, stated on each.

### (b): shared live instances, read uncommitted

- **`Get` for update returns the live instance,** locked for the unit of work.
- **A change made with no session in hand joins the ambient unit of work,** and is a unit of work of its own when there
  is none. So JAStudio's call sites stay as written. That needs the instance to reach its store: a reference in the
  instance, never anything static, since specifications run several JAStudios in one process. The same field can say
  whether the instance is new, tracked or committed.
- **A unit of work of its own spans one publish cascade only,** since nothing tells the store where a domain method
  ends. A domain method that publishes twice, called outside a unit of work, is committed after its first publish, with
  its participation handlers run between the two on a half-done operation, and if the second publish fails the first
  stays committed. That is the argument for deferred dispatch, returning at every call site that opens no unit of work.
  So Magnus chooses, within decision 2, between:
  - **the automatic unit of work, with that limit stated:** an operation that publishes more than once must be run in a
    unit of work;
  - **refusing a change outside any unit of work,** as (a) refuses it. JAStudio already scopes its edits explicitly, with
    `NoteGuard.Update` at the 51 sites counted, which would become units of work, so refusing may cost it less than it
    sounds. The edits outside a guard aren't counted.
- **At the first change in a unit of work,** the store takes the lock and a snapshot of the instance.
- **A rollback restores the live instance in place from that snapshot,** keeping the identity of every part callers
  hold, such as `vocab.Question`.
- **(b)'s price in Compze is restoring in place:** a rollback restores the live instance, keeping the identity of every
  part. It would be a second, harder restoring, run rarely, exactly when something has already gone wrong, unless it is
  the only restoring there is. So **loading restores in place too:** it builds the taggregate empty, then restores it in
  place from its snapshot. That restoring is then written once for each part, and every start runs it for every note. It
  asks that a part's state can be set after the part is built, which the tevent appliers need anyway.
  taggregates-fable's point. Further:
  - the specification helper covers it: take a snapshot, change, restore in place, take another, compare;
  - **restoring in place resets what the taggregate computed lazily and kept,** which the helper can't see: JAStudio has
    seven such values in `VocabNoteForms` and one in `RelatedVocab`. A rule to state, and the consumer's specifications
    to guard it;
  - a restoring in place that throws stops the process, as a failed commit does.
- **What readers on other threads see, plainly:**
  - changes before they are committed, and some that are then rolled back;
  - with deferred dispatch, changed notes whose participation handlers haven't run yet, for as long as the unit of work
    lasts, so notes and derived state disagree meanwhile.

  JAStudio has both today, in a rebuild.
- **What a writer reads before its first change isn't covered by the lock.** A second unit of work's method reads the
  note, uncommitted changes of the first included, decides, and only then reaches its first publish and waits for the
  lock. Once it has the lock, it applies a decision made from state that has since changed, or was rolled back in place.
  So under (b), **code that decides from a taggregate's state before changing it gets it for update first,** which takes
  the lock before the reading.
- **Lookups inside a unit of work see its changes,** since they are on the live instances. Only derived state keyed by
  what changed must still read its own writes: see below.

### (a): a private copy for each unit of work, as prototyped

- **A private copy** is the instance one unit of work changes. `Get` builds it from the committed instance through the
  taggregate's snapshot, in 0.7 to 1.2 µs for the prototype's small note.
- **The committed instance** is what readers get, and publishing on it throws. The commit makes the copy the committed
  instance; a rollback discards the copy.
- **What it costs JAStudio,** from taggregates-fable's answers:
  - **every edit site changes.** UI, MCP tools, importers, batches and nearly every specification edit by calling a
    method on the note they hold. Not counted, and larger than the 51 `NoteGuard.Update` sites counted so far;
  - **what holds a note must follow the swap:** the caches' indexes, six view model classes, and the MCP server for one
    tool call. Other notes hold only ids or `WordReference`s;
  - **the indexes are the costly holder:** their sets are copied to be changed, since lookups take no lock, and a new
    instance at each commit would copy every set the note is in;
  - **a taggregate handle** for each taggregate, which resolves to the unit of work's copy if it has one and to the
    committed instance otherwise, is how JAStudio would hold notes under (a).
- **What the notes hold that isn't stored,** what Anki says of their cards, would leave the notes, rather than be in a
  snapshot. That holds under (b) too, where the rollback restores from a snapshot.

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
  - a participation handler may change the taggregate whose tevent it handles, since that is an ordinary cascade,
    dispatched in order. Decision 6 of the earlier conclusions dissolves.
- **What it costs:** code later in the same unit of work doesn't see what participation handlers do. JAStudio has
  such code: two creations of a vocab with one question in one unit of work would both miss the index. Either a limit
  to state, or derived state that reads its own unit of work's changes: see below.
- **A failed publish rolls the unit of work back,** even if the consumer catches its exception: the session refuses to
  prepare, with "A publish failed on ... in this unit of work". The refusal rule of the earlier conclusions becomes a
  rollback, and JAStudio replaces no references after a throw.

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
| A participation handler writing plain memory | At phase 0, before the commit | A rollback doesn't undo it |
| An observer | After the commit, off the publishing thread | Readers see new notes with old indexes until it has run |
| A call from the store when it commits, on the committing thread | With the commit | Consumer code runs at commit, and must not fail |
| **Derived state that is itself transactional** (recommended) | Written by a participation handler at phase 0, applied at commit | It must be built: a small transactional in-memory map from Compze, or the consumer's own `VolatileTransactionParticipant` |

- **The fourth way keeps Magnus's premise,** transactions as the unit of work: a rollback undoes it, and no consumer code
  runs at commit.
- **Its changes are kept as differences and applied at commit,** not as copies prepared ahead. Two units of work that
  each prepare a copy of one index set would otherwise lose each other's change.
- **A lookup inside a unit of work reads its own writes,** which removes the limit above. JAStudio's two sessions ask for
  it: after a vocab is renamed from x to y, a lookup of x must not find it and a lookup of y must. A lookup takes the
  committed index's entries for the key, drops the notes the unit of work has changed that no longer have the key, and
  adds those it has changed that have it. For that, **the session lists the taggregates it holds, by type:** those got for
  update, those created, and those deleted. JAStudio's `NoteIndex` can then do the rest itself.
- **JAStudio's `NoteIndex` nearly works so already:** its changes go to copies, and `PublishChanges` puts them in place.
- **Under (a), an index entry must follow the swap:** the copy becomes the committed instance, but entries under keys
  that didn't change still point at the old one. An index holds handles or ids, or files the whole note again, as
  JAStudio does today. Under (b) the entries stay right.
- **Effects that can't be rolled back,** such as telling Anki to delete a study note, belong after the commit and before
  the edit returns: `IUnitOfWorkResolver.OnCommittedSuccessfully`, which runs before `ExecuteUnitOfWork` returns.

## Snapshots

```csharp
///<summary>A taggregate whose whole state can be taken as a snapshot, and restored from one.</summary>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>, new()
{
   TSnapshot TakeSnapshot();
   void RestoreFrom(TSnapshot snapshot);
}
```

- **One restoring, in place,** under isolation (b): loading builds the taggregate empty, then restores it from its
  snapshot, and a rollback restores the live instance from the snapshot taken at its first change. Under (a), with no
  rollback in place, it could be a static member, `static abstract TSelf RestoreFrom(TSnapshot)`, which builds the
  instance. Either way a concrete class in a hierarchy, such as a vocab under `JPNote`, restores from its own snapshot
  type.
- **A non-generic face for the store,** which holds taggregates of many types: default interface members that take a
  snapshot, restore from one, and under (a) copy.
- **Each taggregate class is named once where the store is composed,** such as `AddTaggregate<VocabNote>()`, so that
  loading knows which class restores from which snapshot type.
- **`Taggregate` gets a protected way to say it was restored,** with its id. Today that needs the `[Obsolete]` internal
  `Id` setter of `Entity`. The prototype's is `RestoreIdentity(id, version)`.
- **A snapshot holds the taggregate's whole state,** not only what is stored, since every copy under (a), and every
  rollback under (b), is made from it. A
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
  - it calls `Commit` when the transaction commits, then, under (a), swaps in the copies, then releases the locks;
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

One transaction for a whole rebuild means a lock for every note until the commit, under (a) a copy of every note too,
and every handler run on one thread at the end. Two shapes remain, JAStudio's to measure and choose:

- **A unit of work for each note** costs about 10 µs and 9.5 KB, so about 0.8 s of one core and 0.76 GB of garbage for
  80,000. It never waits for a lock while it holds one. taggregates-fable would take it first.
- **Batches of some hundreds of notes** make less garbage. But a batch holds every note it changed until it commits, so
  a UI edit that needs two notes can wait for a batch that waits for it, until the timeout. Meanwhile, other threads
  look the batch's notes up by their old keys.

Generated data can be generated again, so a failed unit of work costs little either way.

## What isn't designed yet

- Loading everything again while running.
- A second session of one transaction getting a taggregate another session of it holds: it waits for its own
  transaction until the timeout. It should be refused at once, or both sessions share the instance.
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
- **Not in it:** isolation (b), the queue for the unit of work, the bound on rounds, the JSON store, handles,
  transactional derived state, and tevent routing per class.

| Measured in Release, three runs, the machine at 3 to 7% load | Time, each | Allocated, each |
|---|---:|---:|
| An empty unit of work | 1.2 to 1.8 µs | 1.9 KB |
| A unit of work that gets a vocab, changes it once and commits | 8.5 to 12.5 µs | 9.5 KB |
| A copy of a two-string vocab through its snapshot | 0.7 to 1.2 µs | 1,160 B |
| A new vocab, through its creation tevent | 1.8 to 2.4 µs | 2,552 B |

Each range covers the two DI containers of the pluggable component combinations, over the three runs.
