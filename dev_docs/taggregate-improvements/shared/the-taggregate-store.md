# The taggregate store

**Status: designed by jastudio-rep-opus and jastudio-rep-fable on Magnus's direction, 2026-11-16, with JAStudio's facts
from taggregates-fable.** Isolation by a private copy for each unit of work was chosen by Magnus after two independent
reviews ([reviews/isolation-review-by-an-opus-agent.md](reviews/isolation-review-by-an-opus-agent.md),
[reviews/isolation-review-by-a-fable-agent.md](reviews/isolation-review-by-a-fable-agent.md)). First prototyped on the
branch `taggregate-store-opus`, then built on the spike, `taggregate-store-spike`, as the memory image state store, with
its JSON files, its MemoryPack cache and the snapshot API. The sections that say what a step specified or built are the
record of that step, with the names of the time, and today's beside them where a reader would otherwise be lost. The
names are those the spike's end review decided, 2026-11-18: [the-naming-pass.md](the-naming-pass.md) and
[reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

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
- **Dispatch is synchronous, as each publish cascade ends** (item 32, Magnus, 2026-11-16), as Compze has always run
  participation handlers: inside the domain call, once the taggregate's own cascade has ended and its invariants hold,
  so a handler's failure comes out of the domain call. The prototype first deferred them to the commit; that design is
  superseded, and kept below as its record.
- **The reentrancy bugs are fixed without deferring:** a taggregate hands its tevents out through a queue, `Commit`
  takes them out before calling back, and a store tracks a new taggregate before publishing what it published before
  it was added. See [what the spike built: synchronous dispatch](#what-the-spike-built-synchronous-dispatch).

**Isolation is by a private copy for each unit of work, Magnus's choice, confirmed after two independent reviews.** The
teams had recommended shared live instances, "read uncommitted", to spare JAStudio changing every place that edits a
note. Asked whether that was the best design or a choice that minimized JAStudio's refactoring, jastudio-rep-opus
answered the latter, and both reviewers, given the options neutrally, chose the private copy for Compze and for JAStudio
alike. What JAStudio holds across units of work becomes a taggregate-reference, and its indexes are defined by the
notes' state and read their own unit of work's changes.

## What Magnus set

- **A Compze component manages a process's taggregates** and routes their tevents through Tessaging, so that JAStudio
  "just teals in Teventive and Tessaging components".
- **An `ITaggregateStore` with an `ITaggregateStoreUpdater` session,** mirroring `ITeventStore` and `ITeventStoreUpdater`:
  the same programming model, implemented in memory and backed by JSON, with the storage behind a narrow interface.
- **Transactions as the unit of work even with no storage:** "The .NET machinery is super battle tested."
- **Tevents escape a taggregate only once its cascade is settled.**
- **`Commit` never runs domain code; `Save` is not a domain mutation,** and causes no tevents. (`Save` is the updater's
  `Add` since the end review.)
- **Snapshots carry no tevents:** the implementor writes its own serializable snapshot type, and its own restoring.
- **Isolation by a private copy for each unit of work** (his option (a)), JSON as the durable truth, and a full rebuild
  allowed as one transaction, since it takes 1 to 2 s.

## The programming model

```csharp
//Each store declares the taggregate classes it keeps.
builder.Registrar.MemoryImageStateStore(store => store.Taggregate(() => VocabNote.CreateEmpty()).Index<VocabNote, string>(it => it.Forms));
builder.Registrar.LocalTessagingEngine(engine => engine
   .RegisterTessageBusHandlers(handle => handle.ForTevent((IVocabMeaningChanged it, ITaggregateStoreUpdater taggregates) => ...))
   .ObserveTevents(observe => observe.ForTevent((IVocabMeaningChanged it) => ...)));

container.ScopeFactory.ExecuteUnitOfWork(unitOfWork =>
   unitOfWork.Resolve<ITaggregateStoreUpdater>().GetForUpdate<VocabNote>(id).ChangeMeaning("to eat"));

var vocab = container.RootResolver.Resolve<ITaggregateStoreReader>().Get<VocabNote>(id);
```

**The structure, decided by Magnus, 2026-11-16** (his item 22, after asking why the updater didn't implement the reader,
and why getting for update had become a plain `Get`):

- **`ITaggregateStoreReader`, shared by every store:** `Get`, `TryGet` and `Reference`. Inside a unit of work, `Get`
  returns what a reference's `Current` does: the unit of work's copy if it holds one, else the committed instance. So a
  unit of work reads its own writes. Outside one, the committed instance, read only, at no cost.
- **`ITaggregateStoreUpdater : ITaggregateStoreReader`,** scoped, one for each unit of work, for every store:
  `GetForUpdate` and `TryGetForUpdate`, which make the unit of work's private copy and take the taggregate's lock until
  it completes, and `Add`, which adds a new taggregate (`Save` until the end review's trims, Q10).
  `Delete<TTaggregate>(id)` was built here and went: a deletion is a tevent (see "Deletion is a tevent" below). No
  `IDisposable`. The name `GetForUpdate` says what a read must never cost, a lock and a copy, and matches the Typermedia
  tuery `GetForUpdate`.
- **`ITeventStoreReader`** has only what the tevent store adds, the history: `GetHistory` and
  `GetReadonlyCopyOfVersion`. Its `GetReadonlyCopy` became the shared reader's `Get`. Item 22 had it extend
  `ITaggregateStoreReader`; the end review took that back, since its `Get` then read every store
  (Q8, [the-tevent-store-reader-alone.md](the-tevent-store-reader-alone.md)).
- **Each store declares the taggregate classes it keeps,** and the updater routes each class to its store. A store's
  requirements are compile-time constraints there: the memory image state store's classes are snapshotable, with a factory; the
  tevent store's have tevents with identity (`ITaggregate<ITaggregateTeventWithIdentity>`). New for the tevent store,
  which takes any taggregate today. A class declared for no store fails at its first `Get`, naming the stores.
- **Public:** `ITaggregateReference<TTaggregate>`, what code holds across units of work (see the isolation below), and
  `ITaggregateIndex<TTaggregate, TKey>`, with their implementations private. Internal: the ambient unit of work, in
  `Compze.DependencyInjection`. The persistence seams stay separate.
- **Where:** the shared interfaces in `Compze.Teventive`, namespace `Compze.Teventive.TaggregateStores`, with the bulk
  reader, `ITaggregateStoreBulkReader`, beside the reader; whether they get a package of their own is decided after the
  spike.
- **The new store's name: the state store,** beside the tevent store, in item 22. **The memory image state store since
  the end review** (Q2, [the-memory-image-state-store.md](the-memory-image-state-store.md)): it holds every taggregate in
  memory for the container's life, rebuilt at its start from a snapshot of each. Its package is
  `Compze.Teventive.MemoryImageStateStore`, and its types `IMemoryImageStateStore`, `MemoryImageStateStoreBuilder` and
  the registrar's `MemoryImageStateStore(...)`. "State store" stays the word for the kind of store, state rather than
  history.
- **`LocalTessagingEngine`** is all of Tessaging a consumer like JAStudio composes: no endpoint, no network, and no type
  mappings for tevent types that never leave the process. The prototype maps none.

## Isolation: a private copy for each unit of work

**Magnus's choice, 2026-11-16,** made after both independent reviewers chose it. It holds unless JAStudio's sessions find
a strong reason for shared live instances, which Magnus doesn't expect.

- **A private copy** is the instance one unit of work changes. `Get` for update builds it from the committed instance
  through the taggregate's snapshot, in 0.7 to 1.2 µs for the prototype's small note.
- **The committed instance** is what readers get, and publishing on it throws. The commit makes the copy the committed
  instance; a rollback discards the copy, and the committed instance was never touched.
- **A change outside a transaction is refused by construction,** since a committed instance is read only. Magnus:
  "any changes with no transaction is a bug".
- **Domain methods are correct as naturally written:** a method that reads a condition and then publishes runs on a
  copy that its unit of work holds the lock of. The Fable reviewer's point.
- **A committed instance never changes,** so any thread reads one consistent version of it without a lock.

### The taggregate-reference

A **taggregate-reference** is the stable object that stands for one taggregate's identity, where an instance is one
version of its state. Proposed by taggregates-fable as a handle, and made a typed, first-class part of the design by the
Fable reviewer.

```csharp
///<summary>Stands for one taggregate of a store, for as long as it exists: hold this, never an instance, across units of work.</summary>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate
{
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds a copy, that copy; otherwise the committed instance.</summary>
   TTaggregate Current { get; }
}
```

- **A reference only reads.** It had a `ForUpdate()`, the ambient unit of work's copy; the end review removed it (Q10),
  so the unit of work's updater is the one way to change a taggregate: `updater.GetForUpdate<TTaggregate>(reference.Id)`.

- **Lookups and indexes return references,** so the types say "don't hold an instance across units of work". A stale
  instance is at worst old and consistent, and throws on publish.
- **Reading and getting for update work from the ambient transaction alone,** with no session passed down to call sites,
  as JAStudio's two sessions ask. The store finds the session of the ambient transaction.
- **An index that holds references is touched at a commit only when the note's keys change.**
- **A reference to a deleted taggregate has nothing to give:** `Current` throws once the deletion is committed. So a view
  that may outlive a deletion, such as a UI row read between the commit and the notification that reaches it, keeps the
  last instance it got and refreshes it when notified. Found by taggregates-opus in JAStudio's step 3: its search rows,
  and two specifications that delete their note, do so.
- **Prototyped** (`3ac56efa8`): inside a unit of work the reference gives its copy, on another thread the committed
  instance, and after the commit the same reference gives the new committed instance. `Current` outside any unit of
  work costs 0.08 µs and allocates nothing, the check for an ambient transaction included. That meets JAStudio's
  requirement that the committed read path takes no lock: parsing reads vocab from many threads all the time.
- **A gap in the prototype:** the store finds a transaction's session only once the unit of work has resolved its
  `ITaggregateStoreUpdater`, since a session needs the unit of work's scope to dispatch. The design's answer is an
  ambient unit of work in Compze.DependencyInjection: `ExecuteUnitOfWork` makes its resolver findable from the ambient
  transaction, and the store resolves the session itself. Not designed.

### The lock for each taggregate

- **It makes writers wait for each other.** It is owned by the unit of work's transaction: taken at the first get for
  update and released when the transaction completes, possibly on another thread.
- **It is a `SemaphoreSlim`** with a timeout that names the taggregate. The locking standard gives `SemaphoreSlim`
  exactly this job: "a gate held across awaits, or released on another thread". A second writer waits 30 seconds, then
  gets a `TimeoutException` naming the taggregate. **Decided by Magnus, 2026-11-17 (item 17): kept as built.** A
  Compze lock owned by a transaction, whose timeout names the holding transaction and where it took the lock, is an
  effort of its own after the spike, in Compze.Threading: the diagnostics Compze.Threading's other locks give, for a
  lock that a transaction holds across threads.
- **Only held locks are kept,** in a table of the taggregates being updated, not a lock for each of 80,000 notes. The
  prototype keeps one for each taggregate ever updated.
- **JAStudio's race between a background reparse and a UI edit** then serializes, instead of corrupting the vocab or
  throwing. The thread assertion of the earlier conclusions isn't needed.
- **A unit of work must not wait for the UI thread while it holds a taggregate's lock:** with the UI thread waiting
  for that taggregate, both stand still until the timeout. JAStudio's progress presenters call
  `InvokeSynchronouslyOnUIThread` when a task or a batch starts. Today's batches start their progress before they touch
  a note, so a unit of work for each note is safe, and a whole rebuild as one transaction is not.

### Also

- **What a taggregate computes lazily and keeps must be safe for many readers,** since a committed instance is read by
  many threads at once. JAStudio's forms are such a case.
- **A cache computed from other taggregates goes stale when they commit,** under any isolation: JAStudio's
  `_notOwnedByOtherVocab` is one. So such a value leaves the taggregate, for derived state: see "Derived state" below.
- **The version** is kept in memory, continued by each copy, and not persisted. JAStudio would see one only as noise in
  every diff and a conflict in every merge.
- **The session's interface is the tevent store's:** `GetForUpdate`, `TryGetForUpdate` and `Add`, in the one
  `ITaggregateStoreUpdater` both stores share, and in both stores a unit of work's instances are its own.

### Open

- **Reads of taggregates not got for update are read committed:** what they read can change under the unit of work, so
  a decision made from them isn't protected. It could be made correct by construction by recording the versions read
  and checking them at prepare. From the Opus reviewer. Not designed.
- **Atomic visibility:** a unit of work's copies are swapped in one at a time, so a reader can briefly see half of a
  change that spans several taggregates. Whether to promise more is open. From the Fable reviewer.

### What it costs JAStudio

From taggregates-fable's answers and the reviews:
- **Every edit site changes,** to get the note for update inside a unit of work: UI, MCP tools, importers, batches and
  nearly every specification. The 51 `NoteGuard.Update` sites, the mutating methods inside the notes' parts, become
  publishes. The units of work go at their callers, outside the notes, and those haven't been counted.
- **What holds a note across edits holds a taggregate-reference:** the caches' indexes, six view model classes, and the
  MCP server for one tool call. Other notes hold only ids or `WordReference`s. JAStudio already has the rule that a note
  object can be replaced (`JPCollection.ReloadFromBackend`).
- **Changes that reach other notes leave the domain call,** into participation handlers: `PerfectSynonyms` pushing an
  answer to the other synonyms, and renaming word references, which should first find the vocab that refer to the old
  name and get only those for update. Both reviewers call this a better design in its own right.
- **What the notes hold that isn't stored,** `_cardStatus` and `AnkiTags`, Anki's state, leaves the notes for a home of
  its own, since a copy through the snapshot drops it.

### Considered and set aside

| | Why set aside |
|---|---|
| **Shared live instances, "read uncommitted"** | Correct only by rules no one can enforce: get for update before reading what you decide from; restore the live instance in place at a rollback, which runs rarely, so its mistakes stay latent; and a notification from inside `Publish` for derived state. It breaks what Compze already states: a cascade's temporarily invalid state, and the next unit of work's uncommitted state, become visible to observers that "observe committed facts only", and an applier refilling a list gives torn reads. Its appeal was mainly sparing JAStudio's refactoring |
| **Stable objects with state versioned per transaction** | Second best: isolation and stable identity. But two reads can fall on either side of a commit, its natural form separates state values from behaviour, against the object-oriented standard, and the tevent store's taggregates would need the same redesign. To reconsider if taggregates prove too large to copy, references noisy in consumer code, or a UI must bind to parts such as `vocab.Question` for good |

## Dispatch: deferred to the commit

> **Superseded: handlers run synchronously as each publish cascade ends** (Magnus, 2026-11-16, after the spike built the
> deferral). In a unit of work that changes many taggregates, deferral had handlers run only when the commit starts, and
> Magnus expects in-memory subscribers to cascade synchronously after a taggregate's method publishes. None of the four
> bugs needs the deferral: see [what the spike built: synchronous dispatch](#what-the-spike-built-synchronous-dispatch).
> And the stopped 2c prototype found that deferring tevents while exactly-once tommands are written at the send would
> let a tommand overtake a tevent on a peer's ordered stream. What follows is kept as the record of the deferral.

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
- **What it costs:** code later in the same unit of work doesn't see what participation handlers do, since they run
  when its body has returned. Indexes defined by state read their own writes, so two creations of a vocab with one
  question in one unit of work don't both miss the index: see below. But a change that a participation handler makes
  is seen only after the commit. Once JAStudio's cross-note changes are participation handlers, a vocab's perfect
  synonyms don't have its new answer until the unit of work commits. taggregates-fable found no JAStudio code that reads
  it in between, without searching for it.
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

### Derived state, such as JAStudio's indexes: defined by the taggregates' state

Derived state is what a consumer keeps in memory beside its taggregates and computes from them. Both reviewers proposed
the same design, which replaces the transactional map that participation handlers would write.

- **An index is defined as a function of a taggregate's state:** which keys it is filed under, as `NoteIndex`'s
  `keysOf(snapshot)` already is. Compze provides it, registered with the store, and it returns taggregate-references.
- **The store keeps the committed index:** at prepare, it computes the keys of each taggregate
  the unit of work changed, created or deleted, which runs consumer code and may fail; at commit, with the swap, it
  applies the differences, which can't fail.
- **Inside a unit of work, a lookup reads its own writes:** the committed entries for the key, minus the taggregates the
  unit of work holds copies of, plus those copies whose current keys match. After a vocab is renamed from x to y, a lookup
  of x doesn't find it and a lookup of y does, as JAStudio's two sessions ask.
- **No order of handlers becomes a rule,** and an index is correct at any moment, also in the middle of a domain call,
  where participation handlers, deferred to the commit, would be too late.
- **Prototyped** (`3ac56efa8`): a rename is seen as the unit of work left it inside it, as committed outside it, and
  as renamed after the commit; a rollback leaves the index as it was; two creations for one question in one unit of work
  make one vocab; a delete is filed nowhere.
- **What JAStudio's parsing asks of it,** measured by taggregates-fable with JAStudio's own tools on the real corpus:
  analyzing every sentence takes 14.2 processor-seconds and allocates 4.6 GB, and makes 1.2 to 2.3 million lookups of
  vocab by form, and 640,000 or more existence checks by prefix, which allocate nothing today. A sentence parsed inside
  a unit of work of its own holds only that sentence while it looks up vocab.
- **So the committed path allocates nothing,** after a first prototype at 0.65 to 0.86 µs and 576 B a lookup would have
  added 5 to 14% to parsing's processor time and 15 to 29% to its garbage (`56474f568`):
  - each key's committed references are an array, replaced whole at commit and returned as it is;
  - the store keeps one reference for each taggregate, made at the first request for it; a request for it as a class
    that reference can't be cast to gets a new one, equal to it (defect D2 of the end review);
  - the overlay runs only when the unit of work holds a copy of the index's taggregate type.

  The prototype also had `HasAny(key)`, which checked a key without resolving a reference. The end review removed it
  (Q10): it is `Lookup(key).Count > 0`, at the same cost.
- **The cost, measured in Release, on the prototype:**

  | | Time | Allocated |
  |---|---:|---:|
  | A lookup outside any unit of work | 0.05 to 0.18 µs | 0 B |
  | `HasAny`, since removed, outside any unit of work | 0.05 µs | 0 B |
  | A vocab lookup inside a unit of work holding only a sentence | 0.11 to 0.12 µs | 0 B |
  | A lookup inside a unit of work holding 1 copy of its type | 0.6 to 0.8 µs | 0.7 KB |
  | … holding 100 copies | 5.6 to 6.4 µs | 3.1 KB |
  | … holding 1,000 copies | 54 to 58 µs | 25 KB |

  For JAStudio's parsing that is 0.1 to 0.4 processor-seconds for its lookups, 1 to 3%, and no garbage.
- **Inside a unit of work that holds copies of the index's type,** the prototype recomputes each copy's keys at every
  lookup, so a lookup costs in proportion to the copies held: nothing that matters for a unit of work per note, too
  much for batches of thousands. The fix is the one designed: keep each copy's keys, recomputed only when it publishes.
- **Recording reads to check their versions at prepare,** the open point from the Opus reviewer, must stay off this path.
- **It covers every index JAStudio has:** all nine of the vocab cache's indexes, the prefix index included, are
  key-to-notes indexes with a function of keys. JAStudio's `LookupRecorder` records what lookups return, so a lookup
  must be wrappable, or sit behind JAStudio's collection classes: `ITaggregateIndex` is an interface, so either works.
- **What a taggregate computes lazily from other taggregates leaves it,** for derived state outside it, kept with the
  commit, or computed at each use: a committed instance is read only and shared, so such a value on it stays stale for
  as long as it is committed. In JAStudio, the forms of a vocab that no other vocab owns, and `RelatedVocab`'s compounds.
- **JAStudio's `NoteIndex` nearly works so already:** its changes go to copies, and `PublishChanges` puts them in place.
- **Effects that can't be rolled back,** such as telling Anki to delete a study note, belong after the commit and before
  the edit returns: `IUnitOfWorkResolver.OnCommittedSuccessfully`, which runs before `ExecuteUnitOfWork` returns.
- **Considered and set aside:** participation handlers writing plain memory, which a rollback doesn't undo; observers,
  which leave readers seeing new notes with old indexes; a call from the store at commit, which runs consumer code that
  must not fail; transactional state written by participation handlers, which lags the body of the unit of work and
  makes the order of handlers a rule.

## Snapshots

```csharp
///<summary>A taggregate whose whole state can be taken as a snapshot, and restored from one.</summary>
public interface ISnapshotable<TSelf, TSnapshot> where TSelf : ISnapshotable<TSelf, TSnapshot>
{
   TSnapshot TakeSnapshot();
   void RestoreFrom(TSnapshot snapshot);
}
```

- **Restoring is into a new, empty instance,** for loading and for each copy alike: the store builds it, then calls
  `RestoreFrom`. A part's state can then be set after the part is built, which the tevent appliers need anyway. A
  concrete class in a hierarchy, such as a vocab under `JPNote`, restores from its own snapshot type.
- **The consumer says how an empty instance is built,** where the class is named, with a factory, or with a registration
  of the container for a class whose instances need services (Q3,
  [taggregates-with-dependencies.md](taggregates-with-dependencies.md)): `store.Taggregate(() => new VocabNote())`, or
  `store.Taggregate(TrackedTransient.For<VocabNote>().CreatedBy((NoteServices services) => new VocabNote(services)))`.
  A parameterless constructor won't do: JAStudio's notes reach the collection, the dictionary and the configuration through `JPNote.Services`, and
  nothing may be static, since the specifications run several JAStudios in one process. The same holds for restoring
  outside any store, as the MCP server does with the serializer's services. From taggregates-fable.
- **A non-generic face for the store,** which holds taggregates of many types: default interface members that take a
  snapshot, restore from one, and copy.
- **Each taggregate class is named once where the store is composed,** with `store.Taggregate(...)`, so that loading
  knows which class restores from which snapshot type: the store finds it from the class's
  `ISnapshotable<TTaggregate, TSnapshot>`.
- **`Taggregate` gets a protected way to say it was restored,** with its id. Before, that needed the `[Obsolete]`
  internal `Id` setter of `Entity`. The prototype's was `RestoreIdentity(id, version)`; as built, `RestoreIdentity(id)`,
  which sets the version to 1, so a memory image state store's taggregate restarts at version 1 at every load (Q7,
  documented on `ITaggregate.Version`).
- **A restored taggregate's entities** are added by its `RestoreFrom` with `AddWhileRestoring(id)` on their collection,
  publishing nothing: [snapshots-of-every-taggregate-shape.md](snapshots-of-every-taggregate-shape.md).
- **A snapshot holds the taggregate's whole state,** not only what is stored, since every copy is made from it. A
  component's and a shared tomponent's state are nested in the root's, as in JAStudio's DTOs. A snapshot can share every
  immutable value with the committed instance.
- **A snapshot holds only what the taggregate owns.** An object several taggregates share, changed in place, changes
  what each stores without a tevent: every instance, copy and committed instance alike, takes a snapshot holding the
  change, so the store finds nothing to write, and the committed instance isn't read only through it. Shared state
  belongs in a taggregate of its own, referred to by id, or is changed by a tevent on each taggregate that holds it. The
  round trip can't catch it, since the copy refers to the same object. Found by taggregates-opus at JAStudio's switch: a
  shared condition renamed in place left the edit's copy unchanged, so nothing was written; JAStudio now publishes for
  that edit outright. In `ISnapshotable`'s documentation too. Since JAStudio's step 10 (its 9c82dbd67), the second way:
  renaming a shared condition publishes `MatchingConfiguration.SharedConditionRenamed`, a tevent that sets nothing, on
  each vocab that uses the condition by name, so the store writes each of them.
- **Restoring works outside any store:** JAStudio's MCP server builds notes from two git versions of a file.
- **Two ways of setting state now exist,** the tevent appliers and the restoring, which Magnus has decided for. What
  guards them: a specification helper from Compze that takes a snapshot, restores from it, takes another and compares
  the two, for every taggregate class and every part. **It is a hard requirement under the private copy:** every get
  for update copies through the snapshot, so a property the snapshot misses is dropped at the next edit, and the commit
  saves the loss. From taggregates-opus. **As built, it compares the taggregate and the copy themselves, not only their
  snapshots:** see [what the spike built: the snapshot round trip](#what-the-spike-built-the-snapshot-round-trip).
- **Loading at a start runs no tevents:** the 0.4 GB of garbage a start made in both teams' prototypes goes, and with it
  decision 11 of the earlier conclusions.

**JAStudio already has all three parts, by other names:** `INoteRepository` is the storage seam, `VocabDto`,
`SentenceDto` and `KanjiDto` are the snapshot types, and its converters take and restore.

## Storage

```csharp
///<summary>Where a taggregate store keeps what it has committed. Narrow: it knows snapshots, not taggregates.</summary>
interface ITaggregateStorage
{
   IReadOnlyList<TaggregateSnapshot> LoadAll();
   bool HoldTheSameState(TaggregateSnapshot loaded, object heldSnapshot);
   IPreparedWrite Prepare(IReadOnlyList<TaggregateSnapshot> toSave, IReadOnlyList<TaggregateId> toDelete);
   void WriteAll(IReadOnlyList<TaggregateSnapshot> everything);
}

interface IPreparedWrite
{
   void Commit();   // must not fail for a reason Prepare could have found
   void Discard();
}
```

- **Internal, as built:** designed public, so that a consumer could keep its own storage behind it, then made internal
  once Compze's own JSON files covered JAStudio (c5c910e06), in `Compze.Teventive.MemoryImageStateStore._private.Storage`.
  The JSON files and a store with no storage are its two implementations. `HoldTheSameState` and `WriteAll` came with
  the end review's fifth question, for a load that tells what changed and for `WriteAllToStorage`.

- **Two steps, because a transaction has two:**
  - the session calls `Prepare` at phase 0, once the unit of work's queue is empty and it has taken its copies' snapshots;
  - it calls `Commit` when the transaction commits, then swaps in the copies and applies the index changes, then
    releases the locks;
  - it calls `Discard` at a rollback.
- **Everything that can fail belongs in `Prepare`:** serializing, writing, a full disk, a file another program holds.
- **In memory, `Prepare` does nothing.**
- **`LoadAll` was to be the consumer's** where it has its own way to start fast. JAStudio's `snapshot.bin`, a cache of
  all notes, was read first, then only the files written since. As built, the store starts fast from a MemoryPack cache
  of its own, `StartingFromAMemoryPackCache()`: see "The MemoryPack cache" below.
- **Loading everything again while running,** as JAStudio does around a git fast-forward, needs a store operation of its
  own: no tevent describes such a change. Built as `IMemoryImageStateStore.LoadFromStorage(changeTheStorageFirst)`,
  which reports the `ChangesFoundInStorage`: see "Three gaps for JAStudio's switch" and
  [the-files-of-a-state-store.md](the-files-of-a-state-store.md).

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
- **A transaction with a database in it too: refused by default** (Q1, Magnus, 2026-11-18,
  [a-state-store-in-files-and-a-database-in-one-transaction.md](a-state-store-in-files-and-a-database-in-one-transaction.md)).
  The two aren't atomic across a crash: the database can commit and the process die before the files' journal is
  written. So a unit of work in which the store writes files and a database connection of Compze's takes part is
  refused, by whichever joins second. The files are a durable participant that commits in a single phase. The opt-out,
  `files => files.AcceptingNonAtomicCommitsWithADatabase()`, makes them a volatile participant, as first built, and a
  crash between the commits then loses the files' change. JAStudio has no database.
- **What writing under a temporary name costs, measured by taggregates-opus on Windows** (JAStudio's `CorpusNoteFiles`,
  increment 2, which writes each file as `<id>.being-written` at prepare and renames it into place at commit): about
  0.6 ms a file, against 0.15 ms for overwriting in place. Rewriting the whole corpus takes about 50 s instead of 12. An
  edit writes one file, so JAStudio accepts it; the JSON store, written the same way, costs the same.

## The tevent store takes the same internals

> **Superseded by [step 2b, specified](#step-2b-specified-the-tevent-store-joins-the-shared-updater-and-reader):** with
> synchronous dispatch nothing is deferred, so the tevent store keeps dispatching as each tevent is published. What
> follows is kept as the record of the deferred design.

- **Its session would dispatch at phase 0 too,** from the unit of work's queue, and stop calling out from `Commit`'s
  callback and from `TeventStream`. Both of its bugs found by the reviews are then fixed by construction.
- **It changes what code later in a unit of work sees:** today participation handlers run as each tevent is published.
  Some tevent store specifications may assert that. Not counted. Decision 5.
- **`Commit`'s callback goes,** and `TeventStream` as a public stream any code may observe goes with it: the session is
  then the only thing a tracked taggregate tells. A public stream is what made bug 1 possible.

## A rebuild

One transaction for a whole rebuild means a lock and a copy for every note until the commit, and every handler run on
one thread at the end. Two shapes remain, JAStudio's to measure and choose:

- **A unit of work for each note** costs about 10 µs and 9.5 KB, so about 0.8 s of one core and 0.76 GB of garbage for
  80,000. It never waits for a lock while it holds one. taggregates-fable would take it first.
- **Batches of some hundreds of notes** make less garbage. But a batch holds every note it changed until it commits, so
  a UI edit that needs two notes can wait for a batch that waits for it, until the timeout. Meanwhile, other threads
  look the batch's notes up by their old keys.

Generated data can be generated again, so a failed unit of work costs little either way.

## What isn't designed yet

- Keeping each held copy's keys in the index's overlay (a `performance:` remark at `TaggregateIndex.Lookup`).

Designed and built since: a failure of a storage's commit, which stops the process with `Environment.FailFast` once
the commit's journal is written, the next start finishing the commit from the journal, and since Q1 rolls the unit of
work back when writing the journal fails; and a database and the JSON files in one transaction, refused by default
(Q1, above under "The JSON store").

Designed and built since (2026-11-17): two sessions of one transaction share the unit of work's instance, since the
unit of work belongs to the transaction; `ILocalTessagingEngine.AwaitNoTessagesInFlightAsync` waits for the observers
and leaves the engine running; JAStudio's specifications run an edit with its own `note.Edit(...)`; and the ambient unit
of work lets a reference find its session from the ambient transaction alone.

**Loading everything again while running gets easier with references:** a reference outlives a reload, where today
`JPCollection.ReloadFromBackend` replaces every note object and every holder looks it up again.

## What the spike built in step 2a

Branch `taggregate-store-spike`. Step 2 is split in three: 2a, the state store, which is what JAStudio needs; 2b, the tevent
store joining the shared updater and reader; 2c, `IUnitOfWorkTeventPublisher` deferring every tevent (decision 4). 2a is
built and specified; the calls it made:

- **The new project `Compze.Teventive.StateStore`** (`Compze.Teventive.MemoryImageStateStore` since Q2), beside
  `Compze.Teventive.TeventStore`. The shared interfaces are in
  `Compze.Teventive`, namespace `Compze.Teventive.TaggregateStores`, and `ISnapshotable<TSelf, TSnapshot>` in
  `Compze.Teventive.Taggregates`.
- **One taggregate unit of work for each transaction** (`TaggregateUnitOfWork`, internal), not one for each store's session.
  It holds each store's session and the one queue of tevents, and is the transaction's only participant for the stores:
  at phase 0 it dispatches in rounds, cause before consequence, then has each session prepare. The bound on rounds is 100.
  A handler's failure is wrapped in an exception naming the tevent, its version and its taggregate.
- **The ambient unit of work** is in `Compze.DependencyInjection`, internal: `ExecuteUnitOfWork` records itself for the
  transaction when it begins it, not when it joins one, since a joining unit of work's scope ends before the commit. So
  references, indexes and the updater all find the unit of work from the ambient transaction, and a transaction begun
  outside `ExecuteUnitOfWork` is refused, loudly, when a taggregate is got for update in it.
- **Routing by class:** each store declares its classes, and the reader and updater route a class to the one store that
  keeps it or its subclasses, so `Get<JPNote>` finds a vocab. A class no store keeps fails naming each store and its classes.
- **The composition:** `StateStore(store => store.StoredIn(storage).Taggregate(() => new VocabNote(services)).Index<VocabNote, string>(...))`.
  `Taggregate` infers the class and its snapshot type from what the factory builds: no type arguments. Today:
  `MemoryImageStateStore(store => store.Taggregate(...).Index<VocabNote, string>(...).StoredInJsonFiles(...))`, since
  `StoredIn` went with the storage seam.
- **Snapshots:** `RestoreFrom` calls the protected `RestoreIdentity(id)` first, which also sets the version to 1. The store
  checks that it did, and that the id is the one asked for, and continues a copy's version from the committed instance.
- **Storage:** `LoadAll` runs at the store's first use, not when the container is built. The seam passes snapshots as
  `object`, with the taggregate's class and id (`TaggregateSnapshot`, `TaggregateToDelete`). Without a storage the store
  keeps its taggregates in memory only.
- **A copy that published nothing changes nothing:** it isn't written, and the committed instance stays, as agreed with
  taggregates-opus.
- **The lock table keeps only the locks held or waited for,** not one for each taggregate ever updated.
- **JAStudio's two blockers, built after it moved to 2a (82d8b420d):**
  - a factory that takes a service of the container, `Taggregate((NoteServices services) => new VocabNote(services))`,
    resolved from the root each time an instance is built. Replaced in the end review by a registration of the
    container, for any number of services (Q3);
  - `IStateStore.ReloadFromStorage()`, loading everything again while running, `IMemoryImageStateStore.LoadFromStorage()`
    today (Q2, Q11). It keeps every reference, re-files the indexes, dispatches no tevent, and reports the ids added,
    changed and removed. It waits for open units of work, and holds new ones back until it's done. A change was found by
    a comparison of snapshots declared with the class, `snapshotsAreEqual` (772979289), which JAStudio asked for since a
    hand-written `Equals` on its thirty nested DTO classes fails silently when someone adds a property; it compares
    their serialized bytes. Without one, the snapshot type's own `Equals`, and a type without one made the reload throw.
    The end review removed the comparison (Q5): a load compares only the taggregates whose files changed since the store
    last read or wrote them, by what the store's serializer writes, so a snapshot type needs no `Equals`
    ([the-files-of-a-state-store.md](the-files-of-a-state-store.md)).
- **Not yet:** a second container in one transaction (a `todo:` at the code), and the overlay's kept keys (a
  `performance:` at the code).

## What the spike built: the snapshot round trip

Built after step 3's core, since JAStudio's every get for update copies through a snapshot. In `Compze.Teventive`,
namespace `Compze.Teventive.Taggregates`:

```csharp
SnapshotRoundTrip.DifferencesBetweenItAndItsCopy(vocab, () => new VocabNote(services)).Must().BeEmpty();
```

Built as `SnapshotRoundTrip.AssertLosesNothing(vocab, createEmpty, VocabSnapshot.HoldTheSameState)`, which threw, and
`DifferencesOf`, which returned the differences. The end review kept only the second, as
`DifferencesBetweenItAndItsCopy` (Q10, Q11), and took the snapshot comparison out with `snapshotsAreEqual` (Q5): a
specification asserts with its own assertions that the differences are none.

- **It makes the store's own copy,** through the class the store keeps for each taggregate class, which moved from the
  store to `Compze.Teventive` for it (`SnapshotableTaggregateClass`, internal): what it checks is what a unit of work
  gets.
- **It compares the taggregate and the copy, not only their snapshots.** The design said to take a snapshot, restore,
  take another and compare the two. That catches one of the three mistakes that lose state: a field `RestoreFrom` leaves
  out. A field the snapshot type has no place for, and one `TakeSnapshot` leaves out, give two equal snapshots, both
  without it. Comparing the instances catches all three.
- **What is compared:** every field of the taggregate's own classes, and of every object reached from them, parts
  included, field by field; collections item by item, in order for lists and arrays, in any order for sets and
  dictionaries. Strings, numbers and .NET's other simple values by their own equality. An object both refer to, such
  as the services the taggregates are built with, is the same state. Not compared: the fields of Compze.Teventive's
  types, which publish and apply the tevents, and delegates. A reference back, such as a component's to its taggregate,
  is followed once.
- **Then the snapshots,** as a reload compared them, by the declared comparison or the snapshot type's `Equals`: a copy
  whose snapshot didn't equal the original's would have made every reload report the class as changed. Gone with the
  comparison (Q5).
- **Each difference names its path** from the taggregate: `Book.Chapters._chapters[0]: "first" in the original,
  "second" in the copy`.
- **Specified** with a book that has state in itself, in a component and in a shared tomponent in a slot of its class,
  and a mistake for each: `Compze.Tests.Unit`, `CQRS/Taggregates/SnapshotRoundTrips`.
- **A value created on its first read,** a `LazyCE` or a `Lazy<T>`, is compared by its value, read in both: JAStudio's
  `VocabNoteForms` holds six. Stronger than leaving such fields out, which taggregates-opus proposed instead: a derived
  value that comes out different is a real difference.
- **`SnapshotRoundTrip.DifferencesOf`** (`DifferencesBetweenItAndItsCopy` today) returns the differences instead of
  throwing, for a tool that checks many taggregates, such as JAStudio's MCP tool `find_what_copies_of_the_notes_lose`, which checks all 79,896 notes in 1.3 s.
  Its first run found a real loss: JPNote's flushing bookkeeping, which a copy restored from a snapshot doesn't have.

## What the spike built for JAStudio's increments 3 and 4

JAStudio's increments 3 and 4 are one step: once the store keeps the notes, every edit runs in a unit of work.
taggregates-opus's proposal for how JAStudio code edits a note is `JAStudio/dev_docs/taggregates/shared/how-jastudio-edits-a-note.md`:
`note.Edit(it => ...)` at each edit, joining the running unit of work or beginning one, and getting the copy by the
note's id; lookups that give the note, and holders that hold references. jastudio-rep-opus reviewed it and agreed, with
four points: inside `Edit`, read only `it`, since a stale outer instance gives a lost update; cross-note edits in the
parallel rebuild can deadlock until the lock's timeout; `NoteUpdated`'s UI subscribers must marshal to the UI thread;
a lookup inside a unit of work that rolls back gives the discarded copy.

- **`Taggregate.AssertCanBeChanged()`** (protected, 3d3410a5d): until JAStudio's last part is moved to tevents, a part's
  change is made before the interim tevent is published, so `Publish`'s assertion came after a committed note was
  changed. The guard now asserts first. `Publish` asserts through it too. Private since the end review (Q10), once
  JAStudio's classes changed their state only by tevents.
- **`ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()`** (named by Magnus; built as `AwaitObserversAsync` in 26990b991,
  then `AwaitNoTeventsInFlightAsync`, renamed in 3df5ee7f8): waits for the observers as `StopAsync` does, and leaves the
  engine running, for specifications that assert on what an observer did and go on. "In flight" is the term the tracker of
  tessages in flight already uses for this condition. In the local engine the only tessages in flight are committed tevents
  waiting for their observers; the broader name holds if other things are ever in flight (Magnus, 2026-11-17: "Who knows
  one day there might be other things in flight.").
- **`TaggregateToDelete`'s constructor is public** (6767165c2), as `TaggregateSnapshot`'s is, so a storage's own
  specifications can make one. Moot since c5c910e06: the seam is internal, and `TaggregateToDelete` is gone.
- **Not in Compze, for now:** `Edit` and `ForUpdate` from an instance. `Edit` decides where units of work begin, which is
  an application's policy, and an instance-based `ForUpdate` would invite holding instances. JAStudio keeps it as its
  own; after the migration it is a candidate for the naming and API pass. The end review added nothing for it, and
  removed `ITaggregateReference.ForUpdate()` too (Q10).

## Step 2c, specified: every tevent of a unit of work dispatched when it commits

> **Superseded, not built:** with synchronous dispatch nothing is deferred, so nothing can overtake anything. The
> prototype was stopped unfinished, on `taggregate-store-spike-2c` (325118dfd): 11 existing specifications changed, none
> revealing a bug, and it found the tommand-overtakes-tevent hazard, the 45-second transaction timeout covering every
> handler, and best-effort failures moving from the publish to the commit.

Decision 4, built provisionally on the spike and brought back with the count of specifications it changed. Today a tevent
published straight through `IUnitOfWorkTeventPublisher` reaches its participation handlers at once, while a taggregate's
tevents wait for the commit, so the straight one overtakes them. After 2c, every tevent of a unit of work is dispatched
at phase 0, in one order.

- **`Publish` and `PublishAsync` check at once, and dispatch at the commit.** At once, as today: that there is an ambient
  transaction, that the engine hasn't stopped, that the tevent is valid to execute locally, and that the endpoint wires
  the delivery leg its contract demands. So a wrong publish still fails in the domain call. Then the tevent is queued.
- **The queue is the transaction's, in Compze.Tessaging:** one for each transaction, whichever unit of work published,
  since a nested unit of work joins the transaction with a scope of its own. It's a phase-0 participant
  (`EnlistDuringPrepareRequired`). At prepare it dispatches in rounds, as the taggregate unit of work does now: the
  tevents of a round in the order published, each to its participation handlers and then to its remote delivery leg;
  what the handlers publish is the next round's; at most 100 rounds. Handlers run in the scope of the unit of work that
  began the transaction, which lives until the commit.
- **The taggregate unit of work keeps no queue of its own.** It hands each tevent its taggregates publish to the publisher
  as it's collected, and prepares the stores' sessions once the queue has dispatched everything. The seam is internal, in
  Compze.Tessaging.Abstractions, which grants Compze.Teventive its internals already: the queue runs what is registered
  with it once every tevent is dispatched, before any participant votes. So the order of a commit is: dispatch until no
  tevent is left, then the stores prepare their writes, then the vote.
- **Observers, unchanged:** a tevent is queued for its observers when the unit of work commits, in the order published.
- **Exactly-once tevents:** their outbox rows are written at phase 0, inside the transaction, in the order published,
  bridged synchronously, as the tevent store and the state store already do. The same transaction holds the business
  data and the rows, so the exactly-once, in-order guarantee holds: atomic, and ordered by the one queue.
- **`IIndependentTeventPublisher`:** each publish is its own unit of work, so its caller sees no change; the handlers run
  when that unit of work prepares.

**What it changes for code that uses Compze**, which the count of changed specifications measures:
- code later in a unit of work no longer sees what participation handlers did;
- a handler's failure no longer comes out of `Publish`: it aborts the transaction at prepare, and the caller gets a
  `TransactionAbortedException` with the handler's exception inside, where the unit of work completes. The state store's
  tevents already behave so.

## What the spike built: synchronous dispatch

Item 32, Magnus, 2026-11-16. Participation handlers run synchronously when a taggregate's publish cascade ends, as Compze
has always run them: after the taggregate's own cascade has ended and its invariants hold, inside the domain call. A
handler's failure comes out of the domain call.

- **The four bugs, fixed without deferring** ([bugs-and-findings.md](bugs-and-findings.md)):
  1. `Taggregate` hands its tevents out through a queue: a publish an observer makes on the same taggregate during the
     hand-out has its tevents handed out after those before them, never from inside the observer. A hand-out gives up,
     with "without end", after the handlers it reaches have published 100 more tevents on the taggregate: the bound
     that the deferral's 100 rounds were.
  2. `ITaggregate.Commit` takes the tevents out before calling back, so one published from inside the callback stays
     uncommitted for the next commit.
  3. and 4. A store tracks a new taggregate before it publishes the tevents the taggregate published before its save,
     and a tevent a handler publishes on it meanwhile is published after them, in the order published: in the state
     store's `Save` and the tevent store's, the updater's `Add` since Q10.
- **The state store** dispatches each tevent as its taggregate hands it out (`TaggregateUnitOfWork.Dispatch`); at
  prepare the sessions only prepare their writes. Private copies, the lock for each taggregate and read-only committed
  instances are as before.
- **`TaggregateTeventDispatchFailedException` is gone:** it named the tevent because the failure surfaced far from the
  domain call, which it no longer does.
- **Specifications:** 5 of the state store's changed, from the deferred timing to the synchronous one; new ones for the
  hand-out queue and `Commit` (`Compze.Tests.Unit`, `CQRS/Taggregates/HandingOutAndCommitting`), for a handler that changes
  a new taggregate during its save in each store.
- **`ITaggregate.Commit` may go** (a `todo:` at it, Magnus): a store gets a new taggregate's first tevents through it only
  because they went by on a stream nobody listened to.

## Step 2b, specified: the tevent store joins the shared updater and reader

jastudio-rep-opus, 2026-11-16, after synchronous dispatch. One `ITaggregateStoreUpdater` and one `ITaggregateStoreReader`
serve both stores, so one unit of work can change a taggregate of each. The structure is the one Magnus decided (item
22): it was built for the state store in 2a, and 2b brings the tevent store in.

**What changes for a consumer of the tevent store:**

- **`ITeventStoreUpdater` goes;** `ITaggregateStoreUpdater` replaces it. `Get` becomes `GetForUpdate`, `TryGet` becomes
  `TryGetForUpdate`, `Save` stays, and `Delete(id)` becomes `Delete<TTaggregate>(id)`, since the updater routes by class.
  `IDisposable` goes with it, which settles the `urgent:` at the interface. (Since then `Delete` went, a deletion being
  a tevent, and `Save` became `Add`, Q10.)
- **`ITeventStoreReader : ITaggregateStoreReader`,** a singleton like the shared reader. `GetHistory` and
  `GetReadonlyCopyOfVersion` stay; `GetReadonlyCopy` goes, and the reader's `Get` replaces it. (Since the end review,
  `ITeventStoreReader` no longer extends the shared reader, Q8.) Outside a unit of work,
  `Get` builds the taggregate from its history, read only: publishing on it throws, as on the state store's committed
  instances. So does publishing on what `GetReadonlyCopyOfVersion` returns. That settles the `urgent:` at the tevent
  store's readonly copy.
- **The tevent store declares the classes it keeps:** `TeventStore(connectionName, store => store.Taggregate<User>())`,
  each a `ITaggregate<ITaggregateTeventWithIdentity>`. A class declared for no store fails at its first `Get`, naming the
  stores and their classes, as it already does for the state store. The typermedia registration's
  `HandleTaggregate<TTaggregate, TTevent>()` also declares its class, so an endpoint that uses it declares nothing more.
- **References work for the tevent store's taggregates too:** `Current` is the unit of work's instance if it holds one,
  else a read-only instance built from the history. The tevent store keeps no instances in memory, so each `Current`
  outside a unit of work builds one; its history cache spares the database.
- **One `TaggregateNotFoundException` for both stores.** It keeps the tevent store's base class,
  `ArgumentOutOfRangeException`, which is what a consumer can catch, since the exception itself is internal.

**What stays as it is:**

- **Dispatch,** synchronous: each tevent is saved, then dispatched, as its taggregate hands it out. A new taggregate's
  tevents are saved and dispatched at `Save`, with the ordering fixes of item 32.
- **The writes,** made at the publish through the transaction's database connection, so the database commits or rolls
  them back with the transaction. The session has nothing to prepare or commit.
- **The locking,** the database's: `GetForUpdate` reads the history with the update lock. No lock in memory.

**What changes in how it works:**

- **The tevent store's session belongs to the transaction's `TaggregateUnitOfWork`,** not to a scope. Two scopes in one
  transaction get the same instance of a taggregate, where today each scope's updater loads its own. A typermedia tuery
  `GetForUpdate` run inside a tommand handler's transaction gets the handler's instance.
- **A failed publish rolls the unit of work back,** even if the code that published caught the exception: new for the
  tevent store, as the state store does.
- **A transaction must be begun by `ExecuteUnitOfWork`,** as for the state store: one begun by a `TransactionScope` of its
  own is refused, loudly, when a taggregate is got for update in it.
- **The exactly-once inbox begins each handler's transaction through `ExecuteUnitOfWorkAsync`.** Today it begins a scope,
  then a transaction, itself: the same two things in the same order, but unrecorded, so the shared updater refuses a
  remote exactly-once handler. That is already so for the state store, and would be for the tevent store after 2b.
- **`ITeventStore` becomes a singleton.** It is stateless, and the shared reader is a singleton that needs it. It reads
  its migrations through the function it is registered with at each use, so the migration specifications that change
  them while a container lives still can.

**Specifications:**

- **Renamed calls:** the tevent store's specifications move to `GetForUpdate`, `TryGetForUpdate` and
  `Delete<TTaggregate>`, and to units of work begun by `ExecuteUnitOfWork` where they begin transactions themselves.
- **Two go:** `ThrowsIfUsedByMultipleTransactions` and `If_an_updater_is_used_in_two_transactions_an_exception_is_thrown`.
  The updater no longer holds a unit of work, so using one in two transactions is no misuse. The misuse left, a
  transaction not begun by `ExecuteUnitOfWork`, is specified for the state store already.
- **New, in BDD style:** one unit of work saves a user in the tevent store and a vocab in the state store, and the
  reader gets both after the commit, neither after a rollback; publishing on what the reader gets outside a unit of work
  throws, in the tevent store; a typermedia tuery run inside a tommand handler gets the handler's instance.

## What the spike built in step 2b

Built as specified above (2a919c6b4), full suite green. The calls it made, and where it departs from the spec:

- **The classes:** `TaggregateTeventStore` (the tevent store as an `ITaggregateStore`), `TeventStoreSession` (its part of
  a unit of work) and `TeventStoreReader` replace `TeventStoreUpdater`. `TeventStoreBuilder` declares the classes, and
  checks each with the type validator when the store is built, so `Get` and `Save` no longer validate on each call.
  `TaggregateReference` moved to `Compze.Teventive`, shared by both stores.
- **A history doesn't say what class it is the history of,** so the tevent store builds the declared class that is the
  class asked for, or the one declared class derived from it. Asked for a base class that several declared classes
  derive from, it throws, saying to get the taggregate as its own class.
- **The typermedia registration's `HandleTaggregate` declares on the store's builder after `TeventStore` has run:** the
  builder is read when the container builds the store, so a declaration made until then counts.
- **The read-only message is store-neutral:** "is an instance a taggregate store serves to readers, which is read
  only", since the tevent store's read-only instances aren't committed instances.
- **A fix on the way:** `GetReadonlyCopyOfVersion` returned the latest version when asked for one past it. It throws now.
- **The typermedia tuery's spec became a direct one:** what it relied on is that two scopes in one transaction get the
  same instance, and `Given_a_tevent_store_and_a_state_store` specifies that, with 10 more specifications for one unit of
  work across both stores. The endpoint specifications whose tommand handler navigates the `GetForUpdate` tuery, and the
  sample's, pass through the exactly-once inbox's new unit of work.
- **Not changed:** `Commit`'s callback and `TeventStream` stay public, as the superseded section above proposed to remove
  them; `ITaggregate.Commit`'s own `todo:` covers it.

## Three gaps for JAStudio's switch, specified

Asked by taggregates-opus for JAStudio's step 4, the switch to the state store, 2026-11-16. Each blocks a part of it.
Built as specified (21012570a), with 14 specifications in `Given_a_state_store_whose_storage_holds_a_vocab_and_a_sentence`,
whose storage is now a service of the container that the store is declared to be stored in:

- **A storage built from a service of the container:** `StoredIn<TService>(Func<TService, ITaggregateStorage>)`, as
  `Taggregate` takes a factory with a service. JAStudio's corpus files need the corpus folder, which its host knows only
  once another service has started. The service is resolved from the container's root when the store is built, at its
  first use. Gone with the seam (c5c910e06): `StoredInJsonFiles<TService>(folderWith, ...)` takes the folder from a
  service instead.
- **Every taggregate of a class:** `IStateStore.All<TTaggregate>()`, references to every `TTaggregate` the store holds,
  as an index's lookup gives. Inside a unit of work, the unit of work's view: what it saved included, what it deleted
  left out. JAStudio's searches, its tag tree and its check of whether a media file is used go through every note. On
  `IStateStore`, not on the shared reader: the state store holds everything in memory, where a tevent store would load
  every history. **Moved to a bulk reader that both stores implement:** see the next section but one.
- **Reloading around a change to the storage:** `IStateStore.ReloadFromStorageAfter(Action changeToTheStorage)` runs the
  change once the open units of work have completed, while new ones wait, then reloads, as `ReloadFromStorage` does.
  JAStudio fast-forwards its corpus checkout there, so that no unit of work can write a note over the change. If the
  change throws, nothing is reloaded and the exception comes out. `ReloadFromStorage()` is the same with no change.
  Since the end review, the two are one member, `IMemoryImageStateStore.LoadFromStorage(Action? changeTheStorageFirst = null)`
  (Q10, Q11).

## What JAStudio's switch found

taggregates-opus, 2026-11-16: JAStudio's notes are in the state store (JAStudio's 49cae1b8a, on the spike's 21012570a).
Its whole suite passes. On the real corpus, a full rebuild and saving every note change no file, and copies of all 79,896
notes lose nothing.

- **An index lookup has no defined order:** the store's own, by hash at load and by commit after. A JAStudio lookup that
  takes the first of several vocab changed 1,172 sentences in a rebuild until JAStudio sorted each lookup by id, the
  order its old caches gave. **Specified, Magnus, 2026-11-16 (item 35): `Lookup` returns its references in id order,**
  the order of the ids' Guids, which is the order of their text. That holds at load, after each commit, and inside a
  unit of work for its own view. Each key's array is already replaced whole when a commit changes it, so it is kept
  sorted as it is replaced, and loading sorts each key's references once. JAStudio's sort can then go. **Undone by
  Magnus, 2026-11-17 (item 37):** see "Neither read gives an order", below.
- **Loading restores every taggregate on one thread:** 80,000 notes in 5.0 s. JAStudio's old cache filled its three
  classes in parallel.
- **A unit of work for each note costs more than JAStudio's old saving:** a full rebuild takes 53 processor-seconds,
  against 36 before. That covers the scope, the transaction, the copy, two snapshots, hashing, and the keys of ten vocab
  indexes. JAStudio's step 12 measures it properly.

## Every taggregate of a class: the bulk reader, specified

**Magnus, 2026-11-17 (item 34):** every taggregate of a class moves from `IStateStore.All` to a reader of its own, which
extends the shared reader, as the document db's `IDocumentDbBulkReader` extends `IDocumentDbReader`. He asked why `All`
was on the store and not on the reader, and recalled why the document db has a bulk reader: reading everything is, for
most storages, a mistake outside exceptional circumstances, so it shouldn't sit on the standard reader looking as
ordinary as `Get`.

Built as specified (79884ea33). Seven new specifications, six of them in `Given_a_tevent_store_and_a_state_store`, for
the tevent store and the routing; the six of `IStateStore.All` now read through the bulk reader.

- **`ITaggregateStoreBulkReader : ITaggregateStoreReader`,** in Compze.Teventive beside the reader, with
  `IReadOnlyCollection<ITaggregateReference<TTaggregate>> All<TTaggregate>()`, `ReferencesToAll<TTaggregate>()` since
  the naming pass: a reference to every `TTaggregate` the stores hold, in no particular order. Routed by class to the store that keeps it, as `Get` is. A singleton, as the
  reader is, and the same object. First built returning an `IReadOnlyList` in id order; changed as the bullet on its
  order says.
- **Why a reader of its own:** code that reads every taggregate of a class says so in what it depends on, and a grep
  finds it. For the tevent store, and for any state store whose storage isn't all in memory, it is a full scan.
- **Why shared, and not on `IStateStore`:** code that reads taggregates doesn't depend on which store keeps a class.
  `IStateStore.All` goes.
- **The unit of work's view, in both stores:** inside a unit of work, what it has saved is included, and what it has
  deleted is left out. Outside one, the committed taggregates. As `Get` and an index's `Lookup` are.
- **In no particular order, as a collection: Magnus, 2026-11-17 (item 36),** built in e86642f24. It was first built in id order, as
  `Lookup` gives its references, so that the order wouldn't depend on which store keeps the class. But the state store
  sorts at each call, its committed taggregates being kept by id in no order, and the sort grows faster than the store:
  - **Measured** (Release, two runs each): at 80,000 ids, gathering them takes 1.4 ms, and sorting adds 8.5 ms and
    2.9 MB at each call. At 1,000,000, gathering takes 23 to 27 ms, and sorting adds about 137 ms and 36 MB, six times
    the gathering. At 5,000,000, an estimate, not measured: about 0.8 s and 180 MB, all of it large-object-heap garbage.
  - **Magnus:** a store of millions is entirely possible, in JAStudio too, and the sort may then cost more than what the
    caller does with the taggregates. Code that doesn't care about the order shouldn't pay for it; code that does sorts
    the collection itself, at the same cost, only where it needs it.
  - **The type says so:** an `IReadOnlyList` has positions, which read as an order whatever its documentation says. An
    `IReadOnlyCollection` has a count and enumeration only.
  - **Not an `IEnumerable`,** as the document db's `GetAll` returns, which could stream without building a collection: a
    lazy result reads the store when it is enumerated, not when `All` is called, so inside a unit of work two
    enumerations could differ. The collection is the store as it was at the call.
  - **The tevent store** gives the order its rows come in, creation order, which isn't promised either.
- **The state store:** its committed instances of the class, minus those the unit of work holds, plus the instances of
  the class it holds, as `IStateStore.All` did.
- **The tevent store:** the ids of the histories whose creation tevent is a tevent of the class, from
  `StreamTaggregateIdsInCreationOrder` with the class's tevent interface. It reads through the ambient transaction, so it
  sees what the unit of work has written, which the tevent store writes at each publish, and has deleted. The references
  load nothing until `Current` is read. The class is the one declared class that is a `TTaggregate`, as for `Get`. When
  another declared class's tevents are also tevents of that class, the two classes' histories can't be told apart by
  their creation tevents, so `All` throws and says so.
- **Not on the updater:** `ITaggregateStoreUpdater` extends the reader, not the bulk reader. Code inside a unit of work
  that reads every taggregate of a class depends on both.
- **JAStudio** depends on the bulk reader where it reads every note: its searches, its tag tree and its media check.
  **Done** (JAStudio's 1eab8a810, all green): one class, `StoredNotes`, which every scan reaches through the
  collections, resolves the bulk reader instead of `IStateStore`, and so says that it scans; it uses it for its `TryGet`
  too, since the bulk reader is a reader. Nothing else in JAStudio called `All`. `IStateStore` is now used in JAStudio only
  for `ReloadFromStorageAfter`. **With no order** (JAStudio's 933b8bb54, green): one caller had taken an order from
  `All` after all, `KanjiNote.PopulateRadicalsFromMnemonicTags`, which stored the names of a kanji's `<rad>` tags in
  `All`'s order. It sorts them by their characters now, one line, and a full rebuild still changes no corpus file.
- **For the review (item 9):** the name. The document db says `GetAll`, and gives instances; `All` gives references.
  **Decided in the naming pass:** `ReferencesToAll`, which says that it gives references.

## Neither read gives an order: `Lookup` too, specified

**Magnus, 2026-11-17 (item 37):** `ITaggregateIndex.Lookup` returns
`IReadOnlyCollection<ITaggregateReference<TTaggregate>>` in no particular order, as `All` does, and the index sorts
nothing: not at load, not at commit, not in a unit of work's view. Item 35's id order goes. Built as specified
(f0ed1ccf6); the id-order specifications became specifications that the lookup gives all that is filed.

**The rule behind it, in his words:** "we should not do anything that costs CPU or memory that is not pretty certain to
be what the great majority of clients want and need. Even if it is the great majority, if they can do it themselves
with the same performance with a single call to a sort.... Then they should... And if it is for a search, they would do
the sort after the filter and then instantly the cost dropped a lot since the collection is smaller."

- **What the order cost:** nothing outside a unit of work. But nothing keeps a key's group small: an index by a tag or a
  note type may file most of a store under one key. Inside a unit of work that holds any taggregate of the class, even
  one got for update and not changed, every lookup built the key's whole group anew and sorted it, about 137 ms and
  36 MB on top at a million. Each commit compared what it filed with the whole group, and each load sorted every group.
- **What stays, being the index's design and not the order's:** inside such a unit of work, each lookup still builds the
  key's group anew (about 25 ms and 8 MB at a million), and each commit that changes a key still copies its array
  (8 MB at a million).
- **The order meant nothing in the domain.** It made JAStudio's rebuild reproducible, but which vocab a sentence takes
  when several are filed under one form was still an arbitrary choice, fixed by random Guids.
- **JAStudio keeps the order it needs** in its one call of `Lookup`, `NoteIndex.NotesWith`: one line, ordering by id,
  so every lookup it makes gives today's order, its rebuild stays reproducible by construction, and no corpus file
  changes. taggregates-opus argued for keeping the promise, since the migration rests on "a full rebuild changes no
  corpus file", and an unordered lookup would hide an order dependence until an edit re-files a note. That one line
  answers it, at a cost JAStudio can judge for its own groups. taggregates-opus looked for a hole in that, found none,
  and withdrew its reason. Its largest groups are sentences by particle, up to 16,346; sorting every vocab's sentences
  in a full rebuild it estimates at 50 to 150 ms, against a rebuild of 3.5 s. It skips the sort for groups of one or
  none, which the parsing's lookups mostly are, adds a specification that the collection's lookups give id order, and
  measures a rebuild before and after. **Done** (JAStudio's 700d57b81, green): the specification files eight vocab
  under one form with random ids, and failed before the sort went in; the rebuild changes no corpus file; a warm
  rebuild's processor time is the same within noise, 38 to 44 processor-seconds before and after.
- **What JAStudio relies on the order for,** from taggregates-opus: the tokenizer's godan check takes the first vocab
  with a form (唱う is a form of 歌う, 唄う and 詠う), 3 sentences; a sentence's stored parse lists two matching vocab in
  lookup order, 1,169 sentences; possibly a splitter's loop over vocab sharing a question; display lists. The godan
  check wants a grammar rule over all of them whatever the order, which is Magnus's call, in a `review:` comment there.

## The index keeps no keys of its own, specified

**Magnus, 2026-11-17 (item 41),** after JAStudio's step 12 measured its load 2.4 s slower than its main branch's, 2.8 s
of the store's 4.2 s in filing its ~20 indexes, and found the largest new thing in its heap to be the index's record of
each taggregate's keys: an `ImmutableHashSet` for each, about 105 MB for 1.6 million keys, 0.8 s to build at load.

- **What the record was for:** when a unit of work commits a changed taggregate, the index takes it out from under the
  keys it no longer has. It read the old keys from the record.
- **Now:** the index keeps only, for each key, the references filed under it. When the unit of work prepares, the old
  keys come from the key function run on the taggregate's committed instance, and the new ones, as before, from its copy;
  at the commit the index files by their difference. A new taggregate has no committed instance, and a deleted one no
  copy.
- **Why that is sound:** the committed instance stays the old one until the commit, and the unit of work holds the
  taggregate's lock, so no other unit of work commits it in between; a reload waits for open units of work. It relies
  only on what `MemoryImageStateStoreBuilder.Index` documents, keys given "from its state", which a lookup inside a
  unit of work already relies on when it computes the keys of the copies it holds.
- **What it costs:** the key function runs once more for each changed taggregate at each commit. A key function that
  isn't a function of the taggregate's own state already gives inconsistent lookups inside a unit of work.
- **Built (53d24b46c), with one thing beyond this:** at prepare, the index asserts that the taggregate is filed under
  each old key it computes. A key function that breaks the contract then fails the unit of work while it can still roll
  back, with a message saying what a key function must be, instead of leaving the committed index wrong or failing
  after the storage has committed. Specified in
  `Given_a_state_store_whose_index_key_function_reads_more_than_the_vocabs_state`. It catches a key that the committed
  state gives and the index doesn't file; one the index files and the state no longer gives can't be seen without a
  record.
- **Measured in JAStudio at 53d24b46c** (taggregates-opus, headless JAStudio in Debug, alternating with JAStudio's main
  on the same corpus): the warm load is 4.31 to 4.38 s against main's 2.63 to 2.67 s, about 1.7 s slower, from 2.35 s
  slower before. The managed heap is 730 MB, against 850 MB before and main's 878 MB, so about 15% less than main. 138 MB
  went, more than the sets' 105 MB, since 1.6 million key objects only the sets held went with them. The index's largest
  structures left are about 42 MB in all: the dictionaries' nodes and buckets, and the arrays of references. Every check
  passes, and the new assertion never fired through edits that change keys of five indexes: JAStudio's key functions
  read only their note's own state.
- **Weighed by the rules on consumers' requests:** no public API changes and nothing is promised anew. The gain grows
  with how much any consumer stores, the cost with how much it changes. JAStudio's other suggestions, presizing the
  dictionaries and filing the indexes in parallel, are tuning for its load, and not taken.
- **Specifications:** the index's as they are, which cover a renamed key, a deletion and a creation, inside and outside
  the unit of work, after a load and after commits. taggregates-opus measures JAStudio's load and heap again.

## Deletion is a tevent, specified

**Magnus, 2026-11-17 (item 42):** "When thinking in tevent sourcing terms, a delete is just another tevent." And: "Unless
it turns out to be problematic somehow, Delete goes away." Found by taggregates-opus replacing JAStudio's `NoteUpdated`
with tevents: a note's own `Delete` published `INoteTevent.Deleted`, and the code then called the updater's
`Delete<TTaggregate>(id)`, one fact said twice.

- **`ITaggregateStoreUpdater.Delete<TTaggregate>(id)` goes.** A taggregate leaves its store when it publishes a tevent
  that implements `ITaggregateDeletedTevent`, effective when the unit of work commits, as `Delete` was. A deletion is then a
  domain method that publishes, like every other change, and observers and participation handlers see it as the tevent.
- **Inside the unit of work, in both stores:** from the Deleted tevent on, the taggregate is gone: `Get` and
  `GetForUpdate` throw as for one no store holds, `TryGet` and `TryGetForUpdate` return false, and `All` (the bulk
  reader's `ReferencesToAll` today) and the indexes
  leave it out. A tevent published on it after its Deleted tevent fails the unit of work, naming the taggregate and the
  tevent. A rolled-back unit of work leaves the taggregate as it was.
- **The state store** keeps no history, so it removes the taggregate at the commit, from its committed taggregates, its
  indexes and its storage, as `Delete` did. A taggregate created and deleted in the same unit of work writes nothing.
- **The tevent store keeps the history,** as tevent sourcing does: the Deleted tevent is appended like any other, and a
  history whose last tevent is a Deleted tevent is no taggregate. `Get`, `TryGet`, `GetForUpdate` and `All` treat it as
  absent; `ITeventStoreReader.GetHistory` and `GetReadonlyCopyOfVersion` still give the whole history, its last version
  included. This is what Compze's query model generator already assumed: `SingleTaggregateQueryModelGenerator` reads a
  history that ends in a Deleted tevent as no model. Before, the tevent store wrote a Deleted tevent and went on serving
  the taggregate, while the updater's `Delete` erased the history.
- **Erasing a history stays possible,** for what must not be kept, such as personal data the law says to erase:
  `ITeventStore.DeleteTaggregate(id)`, as before. It is no longer what a domain deletion does.
- **The tevent store's `All`** leaves out deleted histories with one more query in each of the four SQL layers: the type
  of each taggregate's last effective tevent, one row a taggregate. `All` was already a scan of every taggregate's
  creation tevent. `StreamTaggregateIdsInCreationOrder` stays as it is, since persisting migrations needs every id.
- **An id once used stays used:** saving a new taggregate with a deleted one's id fails in the tevent store, as for any
  id it holds. The state store forgets a deleted id.
- **For JAStudio:** a note's `Delete` publishes `INoteTevent.Deleted`, which implements `ITaggregateDeletedTevent`, and
  the call to the updater's `Delete` after it goes.
- **A handler of a taggregate's own Deleted tevent can't read the taggregate:** the deletion takes effect inside the unit
  of work as the tevent is published, before its participation handlers run, so they get
  `TaggregateNotFoundException`. What such a handler needs goes on the Deleted tevent, as tevent sourcing has it. Found
  by taggregates-opus (2026-11-17): JAStudio's handler that takes a deleted vocab out of its web of perfect synonyms
  read the vocab, and now reads its name from `IVocabTevent.Deleted`. Before, the updater's `Delete` came after the
  publish, so the handlers could still read it, by accident of order. Kept as built: a unit of work sees its own
  changes at once, its deletions included. Decided by Magnus, 2026-11-17, after weighing keeping it readable until
  prepare: "That might make sense for the subscriber to the deleted tevent, but anyone that has no clue that happened
  and gets that instance served up is working with some seriously messed up data."
- **Specifications:** every specification that deleted through the updater deletes through a domain method that
  publishes a Deleted tevent. New ones, in both stores: the deletion hides the taggregate inside the unit of work, a
  publish after it fails the unit of work, a rolled-back deletion leaves the taggregate, and `All` leaves a deleted one
  out; in the tevent store, the history is kept and ends with the Deleted tevent.
- **Built as specified (5e9f293e4),** 23 specifications more, the suite green on the local MsSql combination. Where it
  went beyond the specification:
  - **The refusal to publish after a deletion is the taggregate's own:** `Taggregate` asserts it in `Publish`, for both
    stores and outside any store, before the tevent changes the state. So the sessions only mark the taggregate deleted.
  - **`TryGetForUpdate` of a taggregate deleted in the same unit of work returns false** in both stores. The state
    store's session used to throw "deleted in this unit of work" there.
  - **The query of each taggregate's last tevent** is the same portable SQL in the four layers, a join on each
    taggregate's highest effective version. Only MsSql runs locally: CI runs the others.

## Compze's JSON store, specified

Magnus, 2026-11-17, approving it now instead of deferring it: "It feels like we now have half of a json backend, we build
half and JAStudio the other half... I'm also not too sure that implementing the storage part in JAStudio is how we want
things in the long run." The storage seam was shaped by JAStudio's existing files, not by a store of Compze's own. Once
Compze's store covers JAStudio, the seam goes internal: `ITaggregateStorage`, `IPreparedWrite`, `TaggregateSnapshot`
and `TaggregateToDelete`, four public types fewer, and `StateStoreBuilder.StoredIn` with them.

The order: specify it here, build it in Compze.Teventive.StateStore, have taggregates-opus switch JAStudio to it,
measure JAStudio's start against its `snapshot.bin`, build the cache below if it is needed, then make the seam internal.

### The composition

```csharp
registrar.MemoryImageStateStore(store => store
   .Taggregate(() => new VocabNote())
   .Taggregate(() => new KanjiNote())
   .StoredInJsonFiles(corpusFolder, NoteSerializer.JsonOptions, files => files
      .FolderOf<VocabNote>("vocab")     //<corpus>/vocab/<first two hex digits of the id>/<id>.json
      .FolderOf<KanjiNote>("kanji")));  //<corpus>/kanji/<first two hex digits of the id>/<id>.json
```

Specified with placeholder names, `StateStore`, `StoredInFiles` and `TaggregateFiles.Of`, and with a file of each
instance that the consumer could compute, `fileOf`. The names are the naming pass's and Q2's; `fileOf` went in the end
review (Q5, [the-files-of-a-state-store.md](the-files-of-a-state-store.md)).

- **`StoredInJsonFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFilesBuilder> declare)`:**
  the store's folder, the consumer's JSON options, and each class's files. An overload takes the folder from a service
  of the container, for a host that knows its folder only once another service has started.
- **`FolderOf<TTaggregate>(string folder)`:** the class's folder, under the store's folder. Each class the store keeps is
  declared, and two classes don't share a folder: both asserted when the container is built.
- **The file of a taggregate is its class's folder and its id:** `<first two hex digits of the id>/<id>.json`,
  JAStudio's layout. A taggregate keeps its file for its whole life, so a commit never moves one.
  `ITaggregateJsonFiles`, which the container resolves whenever the store is composed with `StoredInJsonFiles`, gives
  the path of a taggregate's file, `PathOf`, and the taggregate whose file a path is, `TaggregateWhoseFileIs`, from the
  path alone (Q6, Q13, [the-testing-namespace-and-the-json-files.md](the-testing-namespace-and-the-json-files.md)).
- **The options for all the files** are declared in the same chain, and written first in every example, so that none
  reads as an option of the class declared before it: `StartingFromAMemoryPackCache()`,
  `AcceptingNonAtomicCommitsWithADatabase()`, and, in the `Testing` namespace, `OnAFileSystemInMemory()`.

### What it writes, and how

- **A file holds exactly what `JsonSerializer.Serialize(snapshot, options)` gives,** in UTF-8 without a byte order
  mark: the bytes JAStudio writes today, so the corpus's files in git stay as they are.
- **All or nothing across the files of one commit,** as designed above under "The JSON store":
  - temporary files at prepare, in the store's own folder;
  - at commit, a journal first, then the renames and deletions, then the journal is deleted;
  - at start, a journal that exists is carried out, and temporary files without one are deleted.
- **The store's own folder is `<folder>/.state-store`,** on the same volume as the files, so a rename moves no data. A
  consumer whose files are in version control ignores it.
- **Each commit has a journal of its own,** named uniquely, so units of work on different taggregates can commit at the
  same time. Their files never overlap: a taggregate's lock is held until its unit of work completes.
- **A failure after the journal is written stops the process** with `Environment.FailFast`. The next start finishes the
  write. A rename that fails because another program holds the file is retried first.
- **Not flushed to the disk.** A setting to flush waits for a consumer that needs it.

### What it loads

- **Every `*.json` file under each class's folder,** recursively, read as the class's snapshot type with the consumer's
  options. The taggregate is then restored from it, and its id is the restored taggregate's. Other files, such as
  JAStudio's `snapshot.bin`, are ignored.
- **A file that can't be read or restored fails the load, naming the file.** So do two files that hold the same id.
- **`IMemoryImageStateStore.LoadFromStorage` reads the files again** in the same way, and tells what changed by
  comparing only the files whose length or last write time changed since the store last read or wrote them (Q5).
- **`IMemoryImageStateStore.WriteAllToStorage()` writes every file anew,** as the serializer and the consumer's options
  write its snapshot now, in one commit through the journal: for a consumer that has changed its snapshot types or its
  options (Q5, named by the naming pass).

### Starting fast: a cache, as a package of its own

JAStudio starts from a MemoryPack binary cache of all its notes, then reads only the files changed since. Any store
that loads thousands of files at its start needs such a cache, so it belongs to the store. MemoryPack is a third-party
library, and it requires `[MemoryPackable]` on the consumer's snapshot types, so it goes in a project of its own that a
consumer plugs in. Magnus: "I don't think I have a problem with a new project with that dependency that a consumer can
plug in if they want this type of cache... Assuming that you cannot match its performance with that serializer, and I
don't think you can."

- **The simple version first:** the store reads every file. JAStudio's start is measured with it against today's.
- **Then, if it's needed,** `Compze.Teventive.StateStore.MemoryPack` with `files.CachedWithMemoryPack()`, today
  `Compze.Teventive.MemoryImageStateStore.MemoryPack` with `files.StartingFromAMemoryPackCache()` (Q2, Q11):
  - a cache file for each class in the store's own folder, written after a load;
  - it records each file's length and last write time, and at the next start only the files whose length or time
    differs, and new files, are read; files that are gone are dropped;
  - JAStudio's cache compares each file's time with the cache file's, and misses a file restored with an older time.
    Comparing each file's own length and time doesn't.
  - The seam it plugs into is internal, shared with the plug through `InternalsVisibleTo`.

### Specifications

- A taggregate saved is in its file, under its class's folder, with the default layout and with a declared one (the
  declared layout went with `fileOf`, Q5).
- The file holds exactly what the serializer gives with the consumer's options.
- A store started on a folder loads what an earlier store wrote.
- A deletion deletes the file. A change that gives the taggregate another file moves it (gone with `fileOf`).
- A rolled-back unit of work writes nothing, and leaves no temporary file.
- A journal left by a crash is carried out at the next start. Temporary files left without a journal are deleted.
- A file outside its class's folder, two taggregates with one file, and an unreadable file each fail, naming the file.

### Built: the simple version, without a cache

Built as specified on branch `json-store` (830fca831), not yet merged into the spike; 29 specifications, the suite green
on the local MsSql combination. Where it went beyond the specification:

- **The storage gets the taggregate, not only its snapshot:** `TaggregateSnapshot` carries it internally, set by the
  session when it hands over a unit of work's changes, since a file may depend on the taggregate. At load, the storage
  restores each taggregate to learn its id, and the store uses that instance instead of restoring it again. (The
  taggregate going out went with `fileOf`, Q5: a file depends on the id alone.)
- **A journal is written under a temporary name and renamed,** so a crash while writing it leaves no journal and the
  commit didn't happen. A failure anywhere in the commit, before the journal or after, stops the process: the
  transaction can no longer fail. (Since Q1, a failure writing the journal rolls the unit of work back instead.)
- **The journal holds paths relative to the store's folder,** so it still works if the folder moved since the crash.
- **A class's folder may not be the store's own, and no class's folder may lie inside another's,** asserted at
  composition, since loading reads a class's folder at any depth.
- **Not guarded:** two units of work committing at the same time whose taggregates get the same new file. Each checks
  the files committed before it, not the other's pending write. Only a declared layout can produce it, and none can
  since `fileOf` went (Q5).
- **Merged into the spike** as 1b4864ffc and 679775f97, then the inspections fixed and a spec of the overload with a
  folder a service gives (3ff814272).
- **Two bugs, found by taggregates-opus switching JAStudio:**
  - **A storage that handed back the snapshots it was given handed back live taggregates** (fixed in d248ebd66): the
    taggregate a snapshot carried went both ways, so a reload from JAStudio's in-memory spec storage took back the
    committed instances, never restored, and saw no change. Now `TaggregateToWrite` goes out, and `RestoredTaggregate`,
    which only Compze's file storage sets, comes in.
  - **A reload dropped every taggregate whose class folder didn't exist at the store's first load** (fixed in
    4ccdfb6b7): a `DirectoryInfo` keeps what it first learned, so a store started on an empty folder kept reading no
    folder. It now asks the disk by path.

### JAStudio on the JSON store: measured

JAStudio runs on `StoredInFiles` (JAStudio 1fecefad5): a full rebuild changes no file, `save_every_note` changes none
of the 79,896, and an edit undone leaves its file byte for byte as before. Its start, headless, "Loading the notes",
measured by taggregates-opus with the machine otherwise near idle (32 cores):

| Start | Seconds |
|---|---|
| Today's `snapshot.bin` | 4.01, 4.16, 4.45 |
| JSON files, warm | 7.69, 7.81 |
| JSON files, cold, the first read after a reboot | 37.65 (other processes at 2 cores) |

The cold start is Anki's case after every reboot. So the cache is needed, as Magnus expected.

### The MemoryPack cache, specified

```csharp
.StoredInJsonFiles(folder, options, files => files.StartingFromAMemoryPackCache()
                                                   .FolderOf<VocabNote>("vocab"))
```

Specified as `.StoredInFiles(folder, options, files => files.Of<VocabNote>("vocab").CachedWithMemoryPack())`, its
names placeholders, in a package `Compze.Teventive.StateStore.MemoryPack`; the names above are the naming pass's and
Q2's.

- **A project of its own, `Compze.Teventive.MemoryImageStateStore.MemoryPack`,** depending on MemoryPack (1.21.4,
  JAStudio's version). `StartingFromAMemoryPackCache()` is an extension of `TaggregateFilesBuilder` in it. It plugs into an internal seam of the
  store, shared through `InternalsVisibleTo`. Each declared class's snapshot type must be MemoryPack-serializable:
  asserted at composition, naming the type.
- **A cache file for each class, in the store's own folder.** For each file of the class, it holds the file's path
  relative to the class's folder, its length, its last write time and the snapshot it held.
- **At load:** the class's files are listed with their lengths and times, which the folder listing gives without
  opening them. A file whose length and time are those in the cache takes its snapshot from the cache. Any other file
  is read as JSON, as without the cache. A file the cache has and the folder doesn't is gone. Then each snapshot is
  restored, as without the cache.
- **The cache is written after a load that read any file, or found one gone.** It is written under a temporary name and
  renamed, so a crash leaves the old cache or the new. Commits don't write it: a file a commit wrote has another length
  or time at the next start, and is read then.
- **A changed snapshot type makes the cache unusable:** MemoryPack reads by position, and a cache written for the type
  before a change may read back wrong values without failing. So the cache records a fingerprint of each class's
  snapshot type: every instance field of the type, and of each type reachable through them, collections' elements
  included, with their names, types and order. A cache whose fingerprint differs is not read, and is written anew
  after the load. Fields catch properties through their backing fields; a change that doesn't matter to MemoryPack
  costs one slow start, never wrong data.
- **A cache that can't be read is a failure, not a fallback.** It is renamed into place whole, and a changed type is
  caught by the fingerprint, so an unreadable one means something is wrong, and the load fails, naming it. Deleting
  it gives a slow start.
- **Specifications:** a store started a second time with the cache reads no file whose length and time are unchanged
  (shown by a spec that changes a file's content behind the store while keeping its length and time, so it must load
  the cached snapshot); it reads a changed file, a new file, and drops a gone one; a cache written for a snapshot type
  with other fields is not used; the cache file is replaced whole.
- **Then JAStudio switches to it,** and taggregates-opus measures the start, warm and cold, against `snapshot.bin`.

#### Built (962a05221)

`Compze.Teventive.StateStore.MemoryPack` 0.1.0-alpha.1, in the `/Compze.Teventive/StateStore/` solution folder, and
`files.Of<T>(folder).CachedWithMemoryPack()`. Today `Compze.Teventive.MemoryImageStateStore.MemoryPack`, in the
`/Compze.Teventive/MemoryImageStateStore/` solution folder, at 0.1.0-alpha, which went to the pending-release feed with
the store's own package, and `files.StartingFromAMemoryPackCache()` (Q2, Q11). 9 specifications, the suite green on the
local MsSql combination. Where it went beyond the specification:

- **The store owns the cache's file; the cache only turns content into snapshots and back.** The internal seam
  (`Compze.Teventive.StateStore._internal`, `Compze.Teventive.MemoryImageStateStore._internal` today:
  `ITaggregateFilesLoadCache`, `ICachedFilesOfAClass`, `ListedFile`, `LoadedSnapshot`) hands the cache the bytes it last wrote, and takes back the bytes to write, or none. So the store
  writes it whole with the same temporary name and retried rename as a commit's files, and the start after a crash
  deletes a half-written cache as it deletes a commit's temporary files. The cache is
  `.state-store/<class folder>.load-cache`.
- **The cache's content:** the length of a listing part in four bytes, the listing part (the fingerprint, then each
  file's path, length and last write time in ticks), then the snapshots as one MemoryPack list in the same order. The
  fingerprint is read before any snapshot.
- **The fingerprint's rules:** a SHA-256 of a description of the snapshot type. A type of the base class library
  (namespace `System` or `System.*`) is described by its name, with a generic one's type arguments described, so a
  collection's elements are. An enum by its name, underlying type, and each name with its value. An array by its rank
  and element type. Any other type by its name, its instance fields, public or not, from the base class down, each
  class's in declaration order, each with its name and its type described, and the types of its `[MemoryPackUnion]`s.
  A type met again is named, not described again, which also ends a cycle. Names hold no assembly version.
- **"MemoryPack serializes it"** is checked as: the type is `[MemoryPackable]` (implements
  `IMemoryPackFormatterRegister`), or MemoryPack has a formatter registered for it.
- **FlexRef excludes `.claude/worktrees`** (61641024b): `C-FlexRef-Sync` found the projects of an isolated agent's
  worktree there a second time, and wrote references into the other checkout into about 130 csproj files, in both
  checkouts. Reverted, and excluded in `FlexRef.config.xml`.

### JAStudio on the MemoryPack cache: measured

JAStudio runs on the cache (JAStudio 819834ea4, against 962a05221), all its suites green. Its start, measured by
taggregates-opus as before, with other processes at 0.5 to 0.8 cores of 32:

| Start | Seconds |
|---|---|
| From the cache | 3.95, 4.06, 4.13 |
| From `snapshot.bin`, before | 4.01, 4.16, 4.45 |
| The start that wrote the cache, from warm files | 10.09 (other processes at 1.6 cores) |

Cold with the cache wasn't measured: emptying the system's file cache takes administrator rights. It reads what
`snapshot.bin`'s start read: 80 MB of MemoryPack across the three classes, against 74 MB, plus the folder listings.
JAStudio's own `JsonFilesystemObjectRepository` and `snapshot.bin` are gone; the corpus's `.gitignore` gets
`/.state-store/` in their place when JAStudio's branch is merged.

### The storage for consumers' specifications: the JSON store with its files in memory, specified

JAStudio's specifications, run on `StoredInFiles` with a fresh folder each, took 150 to 186 s against 56 to 59 s in
memory (other processes at 2.6 to 3.9 cores against 1.2 to 1.4). Most of the slowdown came from 74 of about 2,500
specifications, which start with the specifications' full collection of 1,035 notes, built anew for each, each note
added in a unit of work of its own: about 5,000 file operations each, since each commit makes about five. The other
2,400 or so start with no notes and create the few they need. (Corrected by taggregates-opus, JAStudio a60ef2c23: it
first said most start with about 900. The 230 parsing cases parse against the stripped-down corpus, files extracted
from the real one, loaded once for all of them, as Magnus remembered.) So JAStudio keeps its specifications on its own
in-memory storage, `NotesStoredInMemory`, and only those about files on files. That storage implements the seam, which
is to go internal. taggregates-opus has proposed to Magnus preparing the full collection once per test process as
files, which each specification then loads as a start does; with the files in memory, a specification can write them
before the store's first use.

Decided by Magnus, 2026-11-17: the JSON store with its files kept in memory. "Then go with that. We can always refactor
if it turns out not to work well." (He also asked taggregates-opus how JAStudio's specifications get their notes: he
thought most parsing specifications ran on a subset extracted from the corpus.)

```csharp
using Compze.Teventive.MemoryImageStateStore.Testing;

.StoredInJsonFiles(folder, options, files => files.OnAFileSystemInMemory()
                                                   .FolderOf<VocabNote>("vocab"))

var files = container.RootResolver.Resolve<IFileSystemInMemory>();
var path = container.RootResolver.Resolve<ITaggregateJsonFiles>().PathOf<VocabNote>(id);
files.ReadAllText(path); files.WriteAllText(path, json); files.Delete(path); files.Paths;
```

Specified as `KeptInMemory()` and `ITaggregateFilesInMemory`, placeholders; since the end review (Q6,
[the-testing-namespace-and-the-json-files.md](the-testing-namespace-and-the-json-files.md)) they are
`OnAFileSystemInMemory()` and `IFileSystemInMemory`, in the package's `Testing` namespace, seen only where it is
imported. "Kept in memory" would read as what a memory image state store does with every taggregate; what these give
is a file system that isn't the disk.

- **The same store, with its file operations behind an internal seam:** writing, renaming, deleting, listing a folder's
  files with their lengths and last write times, and reading. The disk implements it, and so does a file system kept in
  memory, one for each container. With `OnAFileSystemInMemory()`, the store, its layout, the consumer's JSON options,
  the journal and the MemoryPack cache all work as on disk, and nothing touches the disk. `folder` is then only where
  the paths begin.
- **`IFileSystemInMemory`, resolvable only with `OnAFileSystemInMemory()`:** a specification reads what the store wrote by
  path, and changes the files behind the store's back, as a program changing the files on disk would, to specify
  reloads: `Paths`, `ReadAllText`, `WriteAllText`, `Delete`. A write sets the file's last write time, so the cache sees
  it changed. Files written before the store's first use are what it loads.
- **Then the seam goes internal:** `ITaggregateStorage`, `IPreparedWrite`, `TaggregateSnapshot`, `TaggregateToDelete`
  and `StoredIn`, as Magnus approved with the JSON store. Compze's own specifications that use the seam move to the
  files in memory where they can. Those that need a storage to fail, such as a write that fails at prepare, go to an
  `*.InternalSpecifications` project, the rule's last resort, unless the files in memory can fail in a way a consumer's
  disk can too.
- **Specifications:** a taggregate saved is in its file, read back by path, holding exactly what the options make of
  it, and nothing is on the disk; a file written before the store's first use is loaded; a file changed behind the
  store is what a reload gives; a deletion removes the file; with the cache too, a second load reads no unchanged file.
- **Then taggregates-opus replaces `NotesStoredInMemory` with it, and times JAStudio's suite** against today's.
- **Built as specified (77c3cdaf8),** 16 specifications, the suite green on the local MsSql combination, the seam still
  public. Where it went beyond the specification:
  - **The internal seam is `IFileSystem`,** with `FilesOnDisk` and `FilesInMemory` (`FileSystemInMemory` since Q6).
    Every file operation of the store, its journal and the MemoryPack cache's file goes through it; what is left
    outside builds paths only.
  - **`ITaggregateFilesInMemory` (`IFileSystemInMemory`) takes full paths as strings,** compared ignoring case, as
    `File`'s methods do. Its `ReadAllText`, `WriteAllText` and `Delete` behave as theirs, missing files included.
  - **The memory clock is strictly increasing:** a write within the same tick as the last gets a time one tick later. A
    specification pins it: a file changed behind the store to text of the same length, at once, is read anew despite
    the cache.
  - **"A second load reads no unchanged file" is pinned on the disk only:** in memory, a specification can't keep a
    file's time while changing it, so it can't tell a cached snapshot from a read one. The code path is the same.
- **The seam made internal (c5c910e06),** once JAStudio no longer used it (JAStudio 062e9bfff):
  - `ITaggregateStorage`, `IPreparedWrite` and `TaggregateSnapshot` are in `Compze.Teventive.StateStore._private.Storage`
    (`Compze.Teventive.MemoryImageStateStore._private.Storage` today), with `NoStorage`; no other library uses them, so
    `_private`. Both `StoredIn` overloads are gone.
  - **`TaggregateToDelete` is gone too:** with no consumer's storage to describe a deletion to, only its id was read, so
    the storage takes the deleted ids.
  - **Every specification of the seam is said through the public API, so no `InternalSpecifications` project was
    needed.** The old storage's specifications are now `Given_a_state_store_whose_files_hold_a_vocab_and_a_sentence`, on
    the files in memory: another program's changes are files written or deleted behind the store, and what the old
    storage recorded of each write is what the files hold. A store that writes only what changed leaves a file another
    program rewrote as that program wrote it. The failure to prepare a write is a real disk's, in
    `Given_a_state_store_kept_in_files`: a file stands where the store's own folder goes. 81 specifications before, 82
    after: the discarded write's became two, the file as before and no temporary file left.
  - **What can no longer be specified:** a storage that hands back the snapshots it was given, the trap d248ebd66 fixed.
    Only Compze's own storages exist now, so it can't be built from outside.

### Noticed, for later

- **A commit of a single file needs no journal:** writing it under a temporary name and renaming it is already all or
  nothing. Most commits write one file, and it would take them from about five file operations to two.
- **Writing every taggregate anew:** JAStudio's `save_every_note`, and Magnus's "Write file system repository", write
  every note, changed or not, to see in git that loading and saving changes no file, after a change to the snapshot
  types, the serializer options or a format migration. JAStudio does it with five lines of its own that must agree
  with the store's layout and format (marked `todo:compze:`). A candidate for the end review: something such as
  `IStateStore.WriteEverythingAnew()`. taggregates-opus adds: if one public interface for the store's files were
  resolvable however they're kept, on disk or in memory, as the internal `IFileSystem` already is, JAStudio's write
  would go through it in every environment, instead of to the disk even in a specification whose files are in memory.
  **Built in the end review:** `IMemoryImageStateStore.WriteAllToStorage()` (Q5, named by the naming pass), which
  writes through the store's own file system, on the disk or on a file system in memory; and `ITaggregateJsonFiles`, for
  code that needs the path of a taggregate's file (Q6).

### JAStudio's specifications on the files in memory: measured

JAStudio's specifications run on `KeptInMemory()` (JAStudio 062e9bfff), composed as the hosts compose the store,
MemoryPack cache included; `NotesStoredInMemory` is deleted. Only the corpus repository's MCP specifications, which run
git, keep files on disk. The mini corpus is made once per test process for each data set, by an app that creates the
notes, and its files are written into each specification's app before a reload, as a start loads them (Magnus: "The
tests should load their mini corpus the same way as the real app if possible"). JAStudio no longer uses the storage
seam. The whole suite, measured by taggregates-opus, alternating runs:

| Suite | Total | Web | Core | Mcp |
|---|---|---|---|---|
| `NotesStoredInMemory` | 68.7 s, 59.9 s | 34, 30 s | 19, 19 s | 66, 57 s |
| Files in memory | 67.0 s, 58.1 s | 22, 18 s | 21, 17 s | 62, 55 s |

The total is unchanged, since the MCP specifications, which don't use the mini corpus, set the wall time. Web's are
about 40% faster.


## What the prototype is

Branch `taggregate-store-opus`, commit `3dd0fbe08`, in `C:\Dev\Compze.worktrees\taggregate-store-opus`, based on `main`.

- **The store,** in `src/Compze.Teventive.TeventStore/TaggregateStore/`: `ITaggregateStoreUpdater`, `ITaggregateStoreReader`,
  a snapshot interface simpler than the designed one, and the session as a `VolatileTransactionParticipant`.
- **In Teventive:** a read-only mode for committed instances, a record that a publish failed, and `RestoreIdentity`, behind
  an internal `ITaggregateStoreInternals`.
- **Taggregate-references and the index defined by state** (`3ac56efa8`), and a committed lookup that allocates
  nothing (`56474f568`), in the same folder.
- **The specifications,** in `test/Compze.Tests.Integration/CQRS/TaggregateStore/`: 40 behaviours on both pluggable
  component combinations, three runs. Among them:
  - a reader during a unit of work sees the committed state;
  - a body that throws runs no handler and no observer;
  - the copy is swapped in before `OnCommittedSuccessfully` runs;
  - two units of work on two threads serialize on the lock;
  - a rename, a rollback, two creations and a delete, seen through the index inside and outside the unit of work.
- **`Compze.Tests.Unit` passes,** 417 of 417. The other test assemblies weren't run on it.
- **Not in it:** the queue for the unit of work, the bound on rounds, the JSON store, the ambient unit of work, the
  overlay's kept keys, and tevent routing per class.

| Measured in Release, three runs, the machine at 3 to 7% load | Time, each | Allocated, each |
|---|---:|---:|
| An empty unit of work | 1.2 to 1.8 µs | 1.9 KB |
| A unit of work that gets a vocab, changes it once and commits | 8.5 to 12.5 µs | 9.5 KB |
| A copy of a two-string vocab through its snapshot | 0.7 to 1.2 µs | 1,160 B |
| A new vocab, through its creation tevent | 1.8 to 2.4 µs | 2,552 B |

Each range covers the two DI containers of the pluggable component combinations, over the three runs.
