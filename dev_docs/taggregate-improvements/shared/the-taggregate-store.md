# The taggregate store

**Status: designed by jastudio-rep-opus and jastudio-rep-fable on Magnus's direction, 2026-11-16, with JAStudio's facts
from taggregates-fable.** Isolation by a private copy for each unit of work was chosen by Magnus after two independent
reviews ([reviews/isolation-review-by-an-opus-agent.md](reviews/isolation-review-by-an-opus-agent.md),
[reviews/isolation-review-by-a-fable-agent.md](reviews/isolation-review-by-a-fable-agent.md)). The in-memory store is
prototyped and specified on the branch `taggregate-store-opus`. The JSON store and the snapshot API are designed, not
built. Every name is a placeholder for Magnus's pass over the names.

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
- **`Commit` never runs domain code; `Save` is not a domain mutation,** and causes no tevents.
- **Snapshots carry no tevents:** the implementor writes its own serializable snapshot type, and its own restoring.
- **Isolation by a private copy for each unit of work** (his option (a)), JSON as the durable truth, and a full rebuild
  allowed as one transaction, since it takes 1 to 2 s.

## The programming model

```csharp
//The composition is a sketch, designed in the spike: each store declares the taggregate classes it keeps.
builder.Registrar.StateStore(store => store.Taggregate<VocabNote>(VocabNote.CreateEmpty).Index<VocabNote, string>(it => it.Forms));
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
  it completes, `Save`, and `Delete<TTaggregate>(id)`. No `IDisposable`. The name says what a read must never cost, a
  lock and a copy, and matches `ITaggregateReference.ForUpdate()` and the Typermedia tuery `GetForUpdate`.
- **`ITeventStoreReader : ITaggregateStoreReader`** adds `GetHistory` and `GetReadonlyCopyOfVersion`. Its
  `GetReadonlyCopy` becomes the reader's `Get`.
- **Each store declares the taggregate classes it keeps,** and the updater routes each class to its store. A store's
  requirements are compile-time constraints there: the state store's classes are snapshotable, with a factory; the
  tevent store's have tevents with identity (`ITaggregate<ITaggregateTeventWithIdentity>`). New for the tevent store,
  which takes any taggregate today. A class declared for no store fails at its first `Get`, naming the stores.
  `Delete` names the class, since the updater routes by class.
- **Public:** `ITaggregateReference<TTaggregate>`, what code holds across units of work (see the isolation below), and
  `ITaggregateIndex<TTaggregate, TKey>`, with their implementations private. Internal: the ambient unit of work, in
  `Compze.DependencyInjection`. The persistence seams stay separate.
- **Where:** the shared interfaces in `Compze.Teventive` for the spike; whether they get a package of their own is
  decided after it.
- **The new store's name: the state store,** beside the tevent store.
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
public interface ITaggregateReference<out TTaggregate> where TTaggregate : ITaggregate
{
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds a copy, that copy; otherwise the committed instance.</summary>
   TTaggregate Current { get; }

   ///<summary>The ambient unit of work's copy, which it may change. Throws when there is no ambient transaction.</summary>
   TTaggregate ForUpdate();
}
```

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
  exactly this job: "a gate held across awaits, or released on another thread". A Compze lock owned by a transaction,
  with diagnostics naming the holding transaction and where it took the lock, would be better: a recommendation, not a
  blocker.
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
- **The session's interface is the tevent store's:** `Get` for update, `TryGet`, `Save`, `Delete`, and in both stores a
  unit of work's instances are its own.

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
- **The store keeps the committed index:** at prepare, once the queue is empty, it computes the keys of each taggregate
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
  - the store keeps one reference for each taggregate;
  - `HasAny(key)` checks a key without resolving a reference;
  - the overlay runs only when the unit of work holds a copy of the index's taggregate type.
- **The cost, measured in Release:**

  | | Time | Allocated |
  |---|---:|---:|
  | A lookup outside any unit of work | 0.05 to 0.18 µs | 0 B |
  | `HasAny` outside any unit of work | 0.05 µs | 0 B |
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
- **The consumer says how an empty instance is built,** where the class is named, with a factory or by resolving it
  from the container: `AddTaggregate<VocabNote>(() => new VocabNote(noteServices))`. A parameterless constructor won't
  do: JAStudio's notes reach the collection, the dictionary and the configuration through `JPNote.Services`, and
  nothing may be static, since the specifications run several JAStudios in one process. The same holds for restoring
  outside any store, as the MCP server does with the serializer's services. From taggregates-fable.
- **A non-generic face for the store,** which holds taggregates of many types: default interface members that take a
  snapshot, restore from one, and copy.
- **Each taggregate class is named once where the store is composed,** such as `AddTaggregate<VocabNote>()`, so that
  loading knows which class restores from which snapshot type.
- **`Taggregate` gets a protected way to say it was restored,** with its id. Today that needs the `[Obsolete]` internal
  `Id` setter of `Entity`. The prototype's is `RestoreIdentity(id, version)`.
- **A snapshot holds the taggregate's whole state,** not only what is stored, since every copy is made from it. A
  component's and a shared tomponent's state are nested in the root's, as in JAStudio's DTOs. A snapshot can share every
  immutable value with the committed instance.
- **A snapshot holds only what the taggregate owns.** An object several taggregates share, changed in place, changes
  what each stores without a tevent: every instance, copy and committed instance alike, takes a snapshot holding the
  change, so the store finds nothing to write, and the committed instance isn't read only through it. Shared state
  belongs in a taggregate of its own, referred to by id, or is changed by a tevent on each taggregate that holds it. The
  round trip can't catch it, since the copy refers to the same object. Found by taggregates-opus at JAStudio's switch: a
  shared condition renamed in place left the edit's copy unchanged, so nothing was written; JAStudio now publishes for
  that edit outright. In `ISnapshotable`'s documentation too.
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
  - it calls `Commit` when the transaction commits, then swaps in the copies and applies the index changes, then
    releases the locks;
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

- A second session of one transaction getting a taggregate another session of it holds: it waits for its own
  transaction until the timeout. It should be refused at once, or both sessions share the instance.
- A wait for observers to be at rest that needs neither a testing host nor stopping the engine:
  `ILocalTessagingEngine.StopAsync` stops it. JAStudio's specifications need one.
- A database and the JSON store in one transaction.
- The storage seam's error when `Commit` fails anyway.
- A short way for specifications to run an edit as a unit of work. JAStudio's specifications edit notes directly today.
- The ambient unit of work, so that a reference finds its session without the updater being resolved first.
- Keeping each held copy's keys in the index's overlay.

**Loading everything again while running gets easier with references:** a reference outlives a reload, where today
`JPCollection.ReloadFromBackend` replaces every note object and every holder looks it up again.

## What the spike built in step 2a

Branch `taggregate-store-spike`. Step 2 is split in three: 2a, the state store, which is what JAStudio needs; 2b, the tevent
store joining the shared updater and reader; 2c, `IUnitOfWorkTeventPublisher` deferring every tevent (decision 4). 2a is
built and specified; the calls it made:

- **The new project `Compze.Teventive.StateStore`**, beside `Compze.Teventive.TeventStore`. The shared interfaces are in
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
  `Taggregate` infers the class and its snapshot type from what the factory builds: no type arguments.
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
    resolved from the root each time an instance is built;
  - `IStateStore.ReloadFromStorage()`, loading everything again while running. It keeps every reference, re-files the
    indexes, dispatches no tevent, and reports the ids added, changed and removed. It waits for open units of work, and
    holds new ones back until it's done. A change is found by a comparison of snapshots declared with the class,
    `snapshotsAreEqual` (772979289), which JAStudio asked for since a hand-written `Equals` on its thirty nested DTO
    classes fails silently when someone adds a property; it compares their serialized bytes. Without one, the snapshot
    type's own `Equals`, and a type without one makes the reload throw.
- **Not yet:** a second container in one transaction (a `todo:` at the code), and the overlay's kept keys (a
  `performance:` at the code).

## What the spike built: the snapshot round trip

Built after step 3's core, since JAStudio's every get for update copies through a snapshot. In `Compze.Teventive`,
namespace `Compze.Teventive.Taggregates`:

```csharp
SnapshotRoundTrip.AssertLosesNothing(vocab, () => new VocabNote(services), VocabSnapshot.HoldTheSameState);
```

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
- **Then the snapshots,** as a reload compares them, by the declared comparison or the snapshot type's `Equals`: a copy
  whose snapshot doesn't equal the original's would make every reload report the class as changed.
- **It throws naming each difference,** with the path from the taggregate: `Book.Chapters._chapters[0]: "first" in the
  original, "second" in the copy`.
- **Specified** with a book that has state in itself, in a component and in a shared tomponent in a slot of its class,
  and a mistake for each: `Compze.Tests.Unit`, `CQRS/Taggregates/SnapshotRoundTrips`.
- **A value created on its first read,** a `LazyCE` or a `Lazy<T>`, is compared by its value, read in both: JAStudio's
  `VocabNoteForms` holds six. Stronger than leaving such fields out, which taggregates-opus proposed instead: a derived
  value that comes out different is a real difference.
- **`SnapshotRoundTrip.DifferencesOf`** returns the differences instead of throwing, for a tool that checks many
  taggregates, such as JAStudio's MCP tool `find_what_copies_of_the_notes_lose`, which checks all 79,896 notes in 1.3 s.
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
  changed. The guard now asserts first. `Publish` asserts through it too.
- **`ILocalTessagingEngine.AwaitNoTeventsInFlightAsync()`** (named by Magnus; built as `AwaitObserversAsync` in 26990b991):
  waits for the observers as `StopAsync` does, and leaves the engine running, for specifications that assert on what an
  observer did and go on. "In flight" is the term the tracker of tessages in flight already uses for this condition.
- **`TaggregateToDelete`'s constructor is public** (6767165c2), as `TaggregateSnapshot`'s is, so a storage's own
  specifications can make one.
- **Not in Compze, for now:** `Edit` and `ForUpdate` from an instance. `Edit` decides where units of work begin, which is
  an application's policy, and an instance-based `ForUpdate` would invite holding instances. JAStudio keeps it as its
  own; after the migration it is a candidate for the naming and API pass.

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
     store's `Save` and the tevent store's.
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
  `IDisposable` goes with it, which settles the `urgent:` at the interface.
- **`ITeventStoreReader : ITaggregateStoreReader`,** a singleton like the shared reader. `GetHistory` and
  `GetReadonlyCopyOfVersion` stay; `GetReadonlyCopy` goes, and the reader's `Get` replaces it. Outside a unit of work,
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
  first use.
- **Every taggregate of a class:** `IStateStore.All<TTaggregate>()`, references to every `TTaggregate` the store holds,
  as an index's lookup gives. Inside a unit of work, the unit of work's view: what it saved included, what it deleted
  left out. JAStudio's searches, its tag tree and its check of whether a media file is used go through every note. On
  `IStateStore`, not on the shared reader: the state store holds everything in memory, where a tevent store would load
  every history. **Moved to a bulk reader that both stores implement:** see the next section but one.
- **Reloading around a change to the storage:** `IStateStore.ReloadFromStorageAfter(Action changeToTheStorage)` runs the
  change once the open units of work have completed, while new ones wait, then reloads, as `ReloadFromStorage` does.
  JAStudio fast-forwards its corpus checkout there, so that no unit of work can write a note over the change. If the
  change throws, nothing is reloaded and the exception comes out. `ReloadFromStorage()` is the same with no change.

## What JAStudio's switch found

taggregates-opus, 2026-11-16: JAStudio's notes are in the state store (JAStudio's 49cae1b8a, on the spike's 21012570a).
Its whole suite passes. On the real corpus, a full rebuild and saving every note change no file, and copies of all 79,896
notes lose nothing.

- **An index lookup has no defined order:** the store's own, by hash at load and by commit after. A JAStudio lookup that
  takes the first of several vocab changed 1,172 sentences in a rebuild until JAStudio sorted each lookup by id, the
  order its old caches gave. **Specified, Magnus, 2026-11-16 (item 35): `Lookup` returns its references in id order,**
  the order of the ids' Guids, which is the order of their text. That holds at load, after each commit, and inside a
  unit of work for its own view. Each key's array is already replaced whole when a commit changes it, so it is kept
  sorted as it is replaced, and loading sorts each key's references once. JAStudio's sort can then go.
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
  `IReadOnlyList<ITaggregateReference<TTaggregate>> All<TTaggregate>()`: a reference to every `TTaggregate` the stores
  hold. Routed by class to the store that keeps it, as `Get` is. A singleton, as the reader is, and the same object.
- **Why a reader of its own:** code that reads every taggregate of a class says so in what it depends on, and a grep
  finds it. For the tevent store, and for any state store whose storage isn't all in memory, it is a full scan.
- **Why shared, and not on `IStateStore`:** code that reads taggregates doesn't depend on which store keeps a class.
  `IStateStore.All` goes.
- **The unit of work's view, in both stores:** inside a unit of work, what it has saved is included, and what it has
  deleted is left out. Outside one, the committed taggregates. As `Get` and an index's `Lookup` are.
- **In id order, in both stores,** as `Lookup` gives its references: the order of the ids' Guids, which is the order of
  their text. The order then doesn't depend on which store keeps the class, and a consumer that writes what it reads,
  such as JAStudio's rebuild, writes it in the same order each time. The state store sorts at each call: its committed
  taggregates are kept by id, in no order. **The cost, at JAStudio's size:** gathering 80,000 ids takes 1.4 ms, and
  sorting them adds 8.5 ms and 2.9 MB at each call (Release, the machine otherwise quiet, the same in two runs).
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
- **For the review (item 9):** the name. The document db says `GetAll`, and gives instances; `All` gives references.

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
