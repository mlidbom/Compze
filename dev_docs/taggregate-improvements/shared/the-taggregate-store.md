# The taggregate store

**Status: designed by jastudio-rep-opus and jastudio-rep-fable on Magnus's direction, 2026-11-16, with JAStudio's facts
from taggregates-fable, and built on the spike, `taggregate-store-spike`, as the memory image state store, with its JSON
files, its MemoryPack cache and the snapshot API.** First prototyped on the branch `taggregate-store-opus`. Isolation by
a private copy for each unit of work was chosen by Magnus after two independent reviews
([reviews/isolation-review-by-an-opus-agent.md](reviews/isolation-review-by-an-opus-agent.md),
[reviews/isolation-review-by-a-fable-agent.md](reviews/isolation-review-by-a-fable-agent.md)). The names are those the
spike's end review decided, 2026-11-18: [the-naming-pass.md](the-naming-pass.md) and
[reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

This is the design as it is, kept current. What each step of the spike specified and built, with the names of its
time, is in [the-taggregate-store-build-log.md](the-taggregate-store-build-log.md).

## The headline

**A Compze taggregate store keeps taggregates as their state, not their history, under the tevent store's programming
model: a session for each unit of work, and the unit of work a System.Transactions transaction, even with no storage
at all.** It replaces three parts of the earlier conclusions:
- the hook at the end of a publish cascade, and its queue;
- the snapshot made of tevents;
- JAStudio's hand-written commit, save and dispatch.

- **The programming model holds without storage.** A unit of work that gets a taggregate for update, changes it and
  commits costs 8.5 to 12.5 µs in Release, of which the transaction and scope are 1.2 to 1.8 µs, as the prototype
  measured. That is nothing for JAStudio's edits, which are rare and one at a time.
- **Dispatch is synchronous, as each publish cascade ends** (item 32, Magnus, 2026-11-16), as Compze has always run
  participation handlers: inside the domain call, once the taggregate's own cascade has ended and its invariants hold,
  so a handler's failure comes out of the domain call. See [Dispatch](#dispatch-synchronous-as-each-publish-cascade-ends).
- **The reentrancy bugs are fixed without deferring:** a taggregate hands its tevents out through a queue, `Commit`
  takes them out before calling back, and a store tracks a new taggregate before publishing what it published before
  it was added.
- **Isolation is by a private copy for each unit of work, Magnus's choice, confirmed after two independent reviews.** The
  teams had recommended shared live instances, "read uncommitted", to spare JAStudio changing every place that edits a
  note. Asked whether that was the best design or a choice that minimized JAStudio's refactoring, jastudio-rep-opus
  answered the latter, and both reviewers, given the options neutrally, chose the private copy for Compze and for
  JAStudio alike. What code holds across units of work is a taggregate-reference, and indexes are defined by the
  taggregates' state and read their own unit of work's changes.

## What Magnus set

- **A Compze component manages a process's taggregates** and routes their tevents through Tessaging, so that JAStudio
  "just teals in Teventive and Tessaging components".
- **An `ITaggregateStore` with an `ITaggregateStoreUpdater` session,** mirroring `ITeventStore` and the tevent store's
  updater: the same programming model, implemented in memory and backed by JSON, with the storage behind a narrow
  interface. `ITaggregateStore` is internal, the face every store shows the shared reader and updater, and
  `ITaggregateStoreUpdater` is now the tevent store's updater too.
- **Transactions as the unit of work even with no storage:** "The .NET machinery is super battle tested."
- **Tevents escape a taggregate only once its cascade is settled.**
- **`Commit` never runs domain code; adding a taggregate, the updater's `Add`, is not a domain mutation,** and causes no
  tevents.
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
and why getting for update had become a plain `Get`), with what the end review changed:

- **`ITaggregateStoreReader`, shared by every store:** `Get`, `TryGet` and `Reference`. A singleton, one for the
  container, which routes each class to the store that keeps it. Inside a unit of work, `Get` returns what a reference's
  `Current` does: the unit of work's own instance if it holds one, else the taggregate as last committed. So a unit of
  work reads its own writes. Outside one, the taggregate as last committed, read only.
- **`ITaggregateStoreUpdater : ITaggregateStoreReader`,** scoped, one for each unit of work, for every store:
  `GetForUpdate` and `TryGetForUpdate`, which give the unit of work its own instance of the taggregate, which no other
  writer can get until the unit of work completes: in the memory image state store a private copy under a lock in
  memory, in the tevent store an instance built from the history under the database's update lock. And `Add`, which adds
  a new taggregate. No `IDisposable`. The name `GetForUpdate`
  says what a read must never cost, a lock and a copy, and matches the Typermedia tuery `GetForUpdate`.
  - **`Add` takes no type parameter:** it only adds, a change being saved by the commit, and the store is chosen by the
    taggregate's class ([ten-trims-of-the-public-api.md](ten-trims-of-the-public-api.md)).
  - **No `Delete`:** a deletion is a tevent. See [Deletion is a tevent](#deletion-is-a-tevent).
- **`ITaggregateStoreBulkReader : ITaggregateStoreReader`,** for every taggregate of a class: see
  [The bulk reader](#the-bulk-reader-every-taggregate-of-a-class).
- **`ITeventStoreReader`** has only what the tevent store adds, the history: `GetHistory` and
  `GetReadonlyCopyOfVersion`. Item 22 had it extend `ITaggregateStoreReader`; the end review took that back, since its
  `Get` then read every store (Q8, [the-tevent-store-reader-alone.md](the-tevent-store-reader-alone.md)).
- **Each store declares the taggregate classes it keeps,** and the reader and updater route a class to the one store
  that keeps it or its subclasses, so `Get<JPNote>` finds a vocab. A store's requirements are compile-time constraints
  there: the memory image state store's classes are snapshotable, with a factory; the tevent store's have tevents with
  identity (`ITaggregate<ITaggregateTeventWithIdentity>`). A class declared for no store fails at its first `Get`,
  naming each store and its classes.
- **Public:** `ITaggregateReference<TTaggregate>`, what code holds across units of work (see the isolation below), and
  `ITaggregateIndex<TTaggregate, TKey>`, with their implementations private. Internal: `ITaggregateStore`, and the
  ambient unit of work, in `Compze.DependencyInjection`. The persistence seams stay separate.
- **Where:** the shared interfaces in `Compze.Teventive`, namespace `Compze.Teventive.TaggregateStores`, with the bulk
  reader, `ITaggregateStoreBulkReader`, beside the reader, and `ISnapshotable<TSelf, TSnapshot>` in
  `Compze.Teventive.Taggregates`. Whether the shared interfaces get a package of their own is decided after the spike.
- **`LocalTessagingEngine`** is all of Tessaging a consumer like JAStudio composes: no endpoint, no network, and no type
  mappings for tevent types that never leave the process.
- **The shared interfaces say what every store gives;** each store's documentation says its costs. The private copy,
  the lock in memory and the read without a lock below are the memory image state store's; the tevent store's are in
  [The tevent store beside it](#the-tevent-store-beside-it).

### The unit of work

- **One taggregate unit of work for each transaction** (`TaggregateUnitOfWork`, internal), not one for each store's
  session. It holds each store's session, and is the transaction's only participant for the stores: a participant that
  enlists with `EnlistDuringPrepareRequired`, so that at the transaction's prepare, phase 0, it may still work inside the
  transaction, and has each session prepare its writes then.
- **Two scopes of one transaction share it,** since the unit of work belongs to the transaction, not to a scope: they
  get the same instance of a taggregate. A typermedia tuery `GetForUpdate` run inside a tommand handler's transaction
  gets the handler's instance.
- **The ambient unit of work** is in `Compze.DependencyInjection`, internal: `ExecuteUnitOfWork` records itself for the
  transaction when it begins it, not when it joins one, since a joining unit of work's scope ends before the commit. So
  references, indexes and the updater all find the unit of work from the ambient transaction alone, with no session
  passed down to call sites, as JAStudio's two sessions asked.
- **A transaction must be begun by `ExecuteUnitOfWork`:** one begun outside it, such as by a `TransactionScope` of its
  own, is refused, loudly, when a taggregate is got for update in it. So the exactly-once inbox begins each handler's
  transaction through `ExecuteUnitOfWorkAsync`.
- **A failed publish rolls the unit of work back,** even if the code that published caught the exception: the session
  refuses to prepare, with "A publish failed on ... in this unit of work". The consumer replaces no references after a
  throw.
- **Effects that can't be rolled back,** such as telling Anki to delete a study note, belong after the commit and
  before the edit returns: `IUnitOfWorkResolver.OnCommittedSuccessfully`, which runs once the copies are swapped in, and
  before `ExecuteUnitOfWork` returns.
- **Not in Compze: `Edit`, or `ForUpdate` from an instance.** Where units of work begin is an application's policy, and
  an instance-based `ForUpdate` would invite holding instances. JAStudio's `note.Edit(it => ...)` joins the running unit
  of work or begins one, and gets the copy by the note's id. The end review added nothing for it. jastudio-rep-opus's
  four points on it hold for any such helper: inside it, read only the instance it gives, since a stale outer instance
  gives a lost update; edits that reach other taggregates in a parallel rebuild can deadlock until the lock's timeout;
  UI subscribers to the tevents must marshal to the UI thread; and a lookup inside a unit of work that rolls back gave
  the discarded copy.
- **An updater holds no unit of work of its own:** it finds the ambient one, so using one in two transactions is no
  misuse.

## Dispatch: synchronous, as each publish cascade ends

Item 32, Magnus, 2026-11-16. Participation handlers run synchronously when a taggregate's publish cascade ends, as
Compze has always run them: after the taggregate's own cascade has ended and its invariants hold, inside the domain
call. A handler's failure comes out of the domain call.

- **The four reentrancy bugs are fixed without deferring** ([bugs-and-findings.md](bugs-and-findings.md)):
  1. `Taggregate` hands its tevents out through a queue: a publish an observer makes on the same taggregate during the
     hand-out has its tevents handed out after those before them, never from inside the observer. A hand-out gives up,
     with "without end", after the handlers it reaches have published 100 more tevents on the taggregate.
  2. `ITaggregate.Commit` takes the tevents out before calling back, so one published from inside the callback stays
     uncommitted for the next commit.
  3. and 4. A store tracks a new taggregate before it publishes the tevents the taggregate published before it was
     added, and a tevent a handler publishes on it meanwhile is published after them, in the order published: in the
     updater's `Add`, in both stores.
- **The memory image state store** dispatches each tevent as its taggregate hands it out (`TaggregateUnitOfWork.Dispatch`);
  at prepare the sessions only prepare their writes. **The tevent store** saves each tevent, then dispatches it, as its
  taggregate hands it out.
- **Observers observe committed tevents only:** a tevent is queued for its observers when the unit of work commits, and
  they observe after the commit, off the publishing thread, in order for each observer: cause before consequence.
- **`ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()`** (named by Magnus) waits for the observers, as `StopAsync`
  does, and leaves the engine running: for specifications that assert on what an observer did and go on, and for an
  application's batch step or tool that waits until the observers have caught up (Q6,
  [the-testing-namespace-and-the-json-files.md](the-testing-namespace-and-the-json-files.md)). "In flight" is the term
  the tracker of tessages in flight already uses for this condition. In the local engine the only tessages in flight are
  committed tevents waiting for their observers; the broader name holds if other things are ever in flight (Magnus,
  2026-11-17: "Who knows one day there might be other things in flight.").
- **`ITaggregate.Commit` may go** (a `todo:` at it, Magnus): a store gets a new taggregate's first tevents through it
  only because they went by on a stream nobody listened to. Until then `Commit`'s callback and `TeventStream` stay
  public.
- **Considered and set aside: dispatch deferred to the commit,** which the spike built first: a taggregate's tevents
  collected, and dispatched at the transaction's prepare, so that nothing ran while a domain method was on the stack.
  - In a unit of work that changes many taggregates, deferral had handlers run only when the commit starts, and Magnus
    expects in-memory subscribers to cascade synchronously after a taggregate's method publishes.
  - Code later in the same unit of work didn't see what participation handlers did, and a handler's failure surfaced
    where the transaction completed, as a `TransactionAbortedException`, far from the domain call.
  - None of the four bugs needs it.
  - One order for every tevent of a unit of work would have needed `IUnitOfWorkTeventPublisher` to defer too, since a
    tevent published straight through it overtook a taggregate's deferred ones. That was prototyped as step 2c, and
    stopped: deferring tevents while exactly-once tommands are written at the send would let a tommand overtake a tevent
    on a peer's ordered stream; the 45-second transaction timeout would cover every handler; and best-effort failures
    would move from the publish to the commit.

  The record: [Dispatch: deferred to the commit](the-taggregate-store-build-log.md#dispatch-deferred-to-the-commit),
  and [step 2c](the-taggregate-store-build-log.md#step-2c-specified-every-tevent-of-a-unit-of-work-dispatched-when-it-commits).

## Isolation: a private copy for each unit of work

**Magnus's choice, 2026-11-16,** made after both independent reviewers chose it. It holds unless JAStudio's sessions find
a strong reason for shared live instances, which Magnus doesn't expect.

- **A private copy** is the instance one unit of work changes. `GetForUpdate` builds it from the committed instance
  through the taggregate's snapshot, in 0.7 to 1.2 µs for the prototype's small note.
- **The committed instance** is what readers get, and publishing on it throws, saying it is read only, before the tevent
  changes any state. The commit makes the copy the committed instance; a rollback discards the copy, and the committed
  instance was never touched.
- **A change outside a transaction is refused by construction,** since a committed instance is read only. Magnus:
  "any changes with no transaction is a bug".
- **Domain methods are correct as naturally written:** a method that reads a condition and then publishes runs on a
  copy that its unit of work holds the lock of. The Fable reviewer's point.
- **A committed instance never changes,** so any thread reads one consistent version of it without a lock.
- **A copy that published nothing changes nothing:** it isn't written, and the committed instance stays.

### The taggregate-reference

A **taggregate-reference** is the stable object that stands for one taggregate's identity, where an instance is one
version of its state. Proposed by taggregates-fable as a handle, and made a typed, first-class part of the design by the
Fable reviewer.

```csharp
///<summary>Stands for one taggregate of a store for as long as it exists: what code holds across units of work, never an instance.</summary>
public interface ITaggregateReference<out TTaggregate> where TTaggregate : class, ITaggregate
{
   TaggregateId Id { get; }

   ///<summary>Inside a unit of work that holds it, the unit of work's own instance; otherwise the taggregate as last committed, read only.</summary>
   TTaggregate Current { get; }
}
```

- **A reference only reads:** the unit of work's updater is the one way to change a taggregate,
  `updater.GetForUpdate<TTaggregate>(reference.Id)`. The end review removed its `ForUpdate()`, a second way, which
  JAStudio never used (Q10).
- **Lookups and indexes return references,** so the types say "don't hold an instance across units of work". A stale
  instance is at worst old and consistent, and throws on publish.
- **Reading and getting for update work from the ambient transaction alone,** with no session passed down to call sites,
  as JAStudio's two sessions ask: a reference finds the ambient unit of work.
- **Two references are equal when they stand for the same taggregate of the same store,** whatever class each was
  asked for as, so they can be kept in sets and used as keys (defect D2 of the end review).
- **An index that holds references is touched at a commit only when the note's keys change,** and a reference outlives
  a load from storage: `LoadFromStorage` keeps every reference.
- **A reference to a deleted taggregate has nothing to give:** `Current` throws once the deletion is committed, and
  inside the unit of work that deletes it from its Deleted tevent on. So a view that may outlive a deletion, such as a UI
  row read between the commit and the notification that reaches it, keeps the last instance it got and refreshes it when
  notified. Found by taggregates-opus in JAStudio's step 3: its search rows, and two specifications that delete their
  note, do so.
- **Each read of `Current` is a read of the store, at the store's cost.** In the memory image state store, `Current`
  outside any unit of work costs 0.08 µs and allocates nothing, the check for an ambient transaction included, as the
  prototype measured. That meets JAStudio's requirement that the committed read path takes no lock: parsing reads vocab
  from many threads all the time. In the tevent store it builds a new instance from the history. So code that uses the
  taggregate several times in a row reads it once.

### The lock for each taggregate

- **It makes writers wait for each other.** It is owned by the unit of work's transaction: taken at the first get for
  update and released when the transaction completes, possibly on another thread. A unit of work takes each id's lock
  once.
- **It is a `SemaphoreSlim`** with a timeout that names the taggregate. The locking standard gives `SemaphoreSlim`
  exactly this job: "a gate held across awaits, or released on another thread". A second writer waits 30 seconds, then
  gets a `TimeoutException` naming the taggregate. **Decided by Magnus, 2026-11-17 (item 17): kept as built.** A
  Compze lock owned by a transaction, whose timeout names the holding transaction and where it took the lock, is an
  effort of its own after the spike, in Compze.Threading: the diagnostics Compze.Threading's other locks give, for a
  lock that a transaction holds across threads.
- **Only the locks held or waited for are kept,** in a table of the taggregates being updated, not a lock for each of
  80,000 notes.
- **JAStudio's race between a background reparse and a UI edit** then serializes, instead of corrupting the vocab or
  throwing. The thread assertion of the earlier conclusions isn't needed.
- **A unit of work must not wait for the UI thread while it holds a taggregate's lock:** with the UI thread waiting
  for that taggregate, both stand still until the timeout. JAStudio's progress presenters call
  `InvokeSynchronouslyOnUIThread` when a task or a batch starts. Its batches start their progress before they touch
  a note, so a unit of work for each note is safe, and a whole rebuild as one transaction is not.

### Also

- **What a taggregate computes lazily and keeps must be safe for many readers,** since a committed instance is read by
  many threads at once. JAStudio's forms are such a case.
- **A cache computed from other taggregates goes stale when they commit,** under any isolation: JAStudio's
  `_notOwnedByOtherVocab` is one. So such a value leaves the taggregate, for derived state: see
  [Derived state](#derived-state-indexes-defined-by-the-taggregates-state).
- **The version** is kept in memory, continued by each copy, and not persisted, so a memory image state store's
  taggregate restarts at version 1 at every load (Q7, documented on `ITaggregate.Version`). JAStudio would see a stored
  one only as noise in every diff and a conflict in every merge.
- **In both stores a unit of work's instances are its own,** got through the one `ITaggregateStoreUpdater` both share.

### What it costs a consumer: JAStudio's case

From taggregates-fable's answers and the reviews, before JAStudio moved:
- **Every edit site changes,** to get the note for update inside a unit of work: UI, MCP tools, importers, batches and
  nearly every specification. The 51 `NoteGuard.Update` sites, the mutating methods inside the notes' parts, become
  publishes. The units of work go at their callers, outside the notes, and those weren't counted.
- **What holds a note across edits holds a taggregate-reference:** the caches' indexes, six view model classes, and the
  MCP server for one tool call. Other notes hold only ids or `WordReference`s. JAStudio already had the rule that a note
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

### Open

- **Reads of taggregates not got for update are read committed:** what they read can change under the unit of work, so
  a decision made from them isn't protected. It could be made correct by construction by recording the versions read
  and checking them at prepare. From the Opus reviewer. Not designed.
- **Atomic visibility:** a unit of work's copies are swapped in one at a time, so a reader can briefly see half of a
  change that spans several taggregates, and a taggregate's new state under its old keys in an index. Whether to promise
  more is open. From the Fable reviewer.

## Derived state: indexes defined by the taggregates' state

Derived state is what a consumer keeps in memory beside its taggregates and computes from them. Both reviewers proposed
the same design, which replaces the transactional map that participation handlers would write.

- **An index is defined as a function of a taggregate's state:** which keys it is filed under, given "from its state", as
  `MemoryImageStateStoreBuilder.Index` documents, and as JAStudio's `NoteIndex`'s `keysOf(snapshot)` already was.
  Compze provides it, declared with the store,
  `store.Index<VocabNote, string>(it => it.Forms)`, and its `Lookup` returns taggregate-references. One index for each
  pair of taggregate class and key type: a second is refused where it is declared.
- **The store keeps the committed index:** for each key, the references filed under it, and nothing more. At prepare, it
  computes the keys of each taggregate the unit of work changed, created or deleted, the old ones from its committed
  instance and the new ones from its copy, which runs consumer code and may fail; at commit, with the swap, it files by
  their difference, which can't fail. A new taggregate has no committed instance, and a deleted one no copy. See
  [The index keeps no keys of its own](#the-index-keeps-no-keys-of-its-own).
- **Inside a unit of work, a lookup reads its own writes:** the committed entries for the key, minus the taggregates the
  unit of work holds copies of, plus those copies whose current keys match. After a vocab is renamed from x to y, a lookup
  of x doesn't find it and a lookup of y does, as JAStudio's two sessions ask.
- **No order of handlers becomes a rule,** and an index is correct at any moment, also in the middle of a domain call.
- **A lookup gives its references in no particular order:** see [Neither read gives an order](#neither-read-gives-an-order).
- **What JAStudio's parsing asks of it,** measured by taggregates-fable with JAStudio's own tools on the real corpus:
  analyzing every sentence takes 14.2 processor-seconds and allocates 4.6 GB, and makes 1.2 to 2.3 million lookups of
  vocab by form, and 640,000 or more existence checks by prefix, which allocated nothing before. A sentence parsed inside
  a unit of work of its own holds only that sentence while it looks up vocab.
- **So the committed path allocates nothing,** since a first prototype at 0.65 to 0.86 µs and 576 B a lookup would have
  added 5 to 14% to parsing's processor time and 15 to 29% to its garbage:
  - each key's committed references are an array, replaced whole at commit and returned as it is. A caller that casts
    it back and writes to it misbehaves; a read-only wrapper for each key would cost an object for each key held;
  - the store keeps one reference for each taggregate, made at the first request for it; a request for it as a class
    that reference can't be cast to gets a new one, equal to it;
  - the overlay runs only when the unit of work holds a copy of the index's taggregate type.
- **No `HasAny`:** it is `Lookup(key).Count > 0`, at the same cost (Q10).
- **The cost, measured in Release, on the prototype:**

  | | Time | Allocated |
  |---|---:|---:|
  | A lookup outside any unit of work | 0.05 to 0.18 µs | 0 B |
  | A vocab lookup inside a unit of work holding only a sentence | 0.11 to 0.12 µs | 0 B |
  | A lookup inside a unit of work holding 1 copy of its type | 0.6 to 0.8 µs | 0.7 KB |
  | … holding 100 copies | 5.6 to 6.4 µs | 3.1 KB |
  | … holding 1,000 copies | 54 to 58 µs | 25 KB |

  For JAStudio's parsing that is 0.1 to 0.4 processor-seconds for its lookups, 1 to 3%, and no garbage.
- **Inside a unit of work that holds copies of the index's type,** a lookup recomputes each copy's keys, so it costs in
  proportion to the copies held: nothing that matters for a unit of work for each note, too much for batches of
  thousands. The fix is the one designed: keep each copy's keys, recomputed only when it publishes (a `performance:`
  remark at `TaggregateIndex.Lookup`). Such a lookup also builds the key's group anew, about 25 ms and 8 MB at a
  million, and each commit that changes a key copies its array, 8 MB at a million.
- **Recording reads to check their versions at prepare,** the open point from the Opus reviewer, must stay off this path.
- **It covers every index JAStudio has:** all nine of the vocab cache's indexes, the prefix index included, are
  key-to-notes indexes with a function of keys. JAStudio's `LookupRecorder` records what lookups return, so a lookup
  must be wrappable, or sit behind JAStudio's collection classes: `ITaggregateIndex` is an interface, so either works.
- **What a taggregate computes lazily from other taggregates leaves it,** for derived state outside it, kept with the
  commit, or computed at each use: a committed instance is read only and shared, so such a value on it stays stale for
  as long as it is committed. In JAStudio, the forms of a vocab that no other vocab owns, and `RelatedVocab`'s compounds.
- **Considered and set aside:** participation handlers writing plain memory, which a rollback doesn't undo; observers,
  which leave readers seeing new notes with old indexes; a call from the store at commit, which runs consumer code that
  must not fail; transactional state written by participation handlers, which lags the body of the unit of work and
  makes the order of handlers a rule.

### The index keeps no keys of its own

**Magnus, 2026-11-17 (item 41),** after JAStudio's step 12 measured its load 2.4 s slower than its main branch's, 2.8 s
of the store's 4.2 s in filing its ~20 indexes, and found the largest new thing in its heap to be the index's record of
each taggregate's keys: an `ImmutableHashSet` for each, about 105 MB for 1.6 million keys, 0.8 s to build at load.

- **What the record was for:** when a unit of work commits a changed taggregate, the index takes it out from under the
  keys it no longer has. The old keys now come from the key function run on the committed instance.
- **Why that is sound:** the committed instance stays the old one until the commit, and the unit of work holds the
  taggregate's lock, so no other unit of work commits it in between; a load from storage waits for open units of work.
  It relies only on what `MemoryImageStateStoreBuilder.Index` documents, keys given "from its state", which a lookup
  inside a unit of work already relies on when it computes the keys of the copies it holds.
- **What it costs:** the key function runs once more for each changed taggregate at each commit. A key function that
  isn't a function of the taggregate's own state already gives inconsistent lookups inside a unit of work.
- **A key function that breaks the contract fails the unit of work while it can still roll back:** at prepare, the
  index asserts that the taggregate is filed under each old key it computes, with a message saying what a key function
  must be, instead of leaving the committed index wrong or failing after the storage has committed. It catches a key
  that the committed state gives and the index doesn't file; one the index files and the state no longer gives can't be
  seen without a record.
- **Measured in JAStudio** (taggregates-opus, headless JAStudio in Debug, alternating with JAStudio's main on the same
  corpus): the warm load is 4.31 to 4.38 s against main's 2.63 to 2.67 s, about 1.7 s slower, from 2.35 s slower with
  the record. The managed heap is 730 MB, against 850 MB with it and main's 878 MB. The index's largest structures left
  are about 42 MB in all: the dictionaries' nodes and buckets, and the arrays of references. The assertion never fired
  through edits that change keys of five indexes.
- **Weighed by the rules on consumers' requests:** no public API changed and nothing was promised anew. The gain grows
  with how much any consumer stores, the cost with how much it changes. JAStudio's other suggestions, presizing the
  dictionaries and filing the indexes in parallel, are tuning for its load, and not taken.

### Neither read gives an order

**Magnus, 2026-11-17 (items 36 and 37):** `ITaggregateIndex.Lookup` and the bulk reader's `ReferencesToAll` return
`IReadOnlyCollection<ITaggregateReference<TTaggregate>>` in no particular order, and nothing sorts: not at load, not at
commit, not in a unit of work's view. Both were first built in id order, after a JAStudio lookup that took the first of
several vocab changed 1,172 sentences in a rebuild (item 35, undone).

**The rule behind it, in his words:** "we should not do anything that costs CPU or memory that is not pretty certain to
be what the great majority of clients want and need. Even if it is the great majority, if they can do it themselves
with the same performance with a single call to a sort.... Then they should... And if it is for a search, they would do
the sort after the filter and then instantly the cost dropped a lot since the collection is smaller." It is the rule
[an API spends nothing on what callers can do as well](../../../.claude/rules/02-universal-compze/01-strategy/013-an-api-spends-nothing-on-what-callers-can-do-as-well.md).

- **A library can't know its callers' sizes,** and a store of millions is entirely possible, in JAStudio too. The bulk
  reader's sort, measured in Release, two runs each: at 80,000 ids, gathering them takes 1.4 ms, and sorting added
  8.5 ms and 2.9 MB at each call; at 1,000,000, gathering takes 23 to 27 ms, and sorting added about 137 ms and 36 MB,
  six times the gathering; at 5,000,000, estimated, about 0.8 s and 180 MB, all of it large-object-heap garbage.
- **Nothing keeps a key's group small:** an index by a tag or a note type may file most of a store under one key. Inside
  a unit of work that holds any taggregate of the class, even one got for update and not changed, every lookup built
  the key's whole group anew and sorted it, about 137 ms and 36 MB on top at a million. Each commit compared what it
  filed with the whole group, and each load sorted every group.
- **The order meant nothing in the domain.** It made JAStudio's rebuild reproducible, but which vocab a sentence takes
  when several are filed under one form was still an arbitrary choice, fixed by random Guids.
- **The type says so:** an `IReadOnlyList` has positions, which read as an order whatever its documentation says. An
  `IReadOnlyCollection` has a count and enumeration only.
- **A consumer that needs an order sorts where it needs it.** JAStudio sorts by id in its one call of `Lookup`,
  `NoteIndex.NotesWith`, skipping groups of one or none, so its rebuild stays reproducible by construction, and a
  specification of its own pins it. taggregates-opus first argued for keeping the promise, since JAStudio's migration
  rests on "a full rebuild changes no corpus file", and an unordered lookup would hide an order dependence until an
  edit re-files a note; that one line answers it, and it withdrew the reason. A warm rebuild's processor time stayed the
  same within noise, 38 to 44 processor-seconds.
- **What JAStudio relies on the order for,** from taggregates-opus: the tokenizer's godan check takes the first vocab
  with a form (唱う is a form of 歌う, 唄う and 詠う), 3 sentences; a sentence's stored parse lists two matching vocab in
  lookup order, 1,169 sentences; possibly a splitter's loop over vocab sharing a question; display lists. The godan
  check wants a grammar rule over all of them whatever the order, which is Magnus's call, in a `review:` comment there.

## The bulk reader: every taggregate of a class

**Magnus, 2026-11-17 (item 34):** every taggregate of a class is read through a reader of its own, which extends the
shared reader, as the document db's `IDocumentDbBulkReader` extends `IDocumentDbReader`. Reading everything is, for most
storages, a mistake outside exceptional circumstances, so it shouldn't sit on the standard reader looking as ordinary as
`Get`.

- **`ITaggregateStoreBulkReader : ITaggregateStoreReader`,** in Compze.Teventive beside the reader, with
  `IReadOnlyCollection<ITaggregateReference<TTaggregate>> ReferencesToAll<TTaggregate>()`: a reference to every
  `TTaggregate` the stores hold, in no particular order. Routed by class to the store that keeps it, as `Get` is. A
  singleton, as the reader is, and the same object. Named in the naming pass, to say that it gives references; the
  document db's `GetAll` gives instances.
- **Why a reader of its own:** code that reads every taggregate of a class says so in what it depends on, and a grep
  finds it. For the tevent store, and for any state store whose storage isn't all in memory, it is a full scan.
- **Why shared, and not on the memory image state store:** code that reads taggregates doesn't depend on which store
  keeps a class.
- **The unit of work's view, in both stores:** inside a unit of work, what it has added is included, and what it has
  deleted is left out. Outside one, the committed taggregates. As `Get` and an index's `Lookup` are.
- **A collection, not an `IEnumerable`,** as the document db's `GetAll` returns, which could stream without building a
  collection: a lazy result reads the store when it is enumerated, not when `ReferencesToAll` is called, so inside a
  unit of work two enumerations could differ. The collection is the store as it was at the call.
- **In the memory image state store:** its committed instances of the class, minus those the unit of work holds, plus the
  instances of the class it holds. It enumerates its dictionary of taggregates, rather than reading its `Values`, which
  takes every lock of the dictionary and copies every taggregate of every class.
- **In the tevent store:** the ids of the histories whose creation tevent is a tevent of the class, from
  `StreamTaggregateIdsInCreationOrder` with the class's tevent interface, in the order its rows come in, creation order,
  which isn't promised either. It reads through the ambient transaction, so it sees what the unit of work has written,
  which the tevent store writes at each publish, and has deleted. The references load nothing until `Current` is read.
  The class is the one declared class that is a `TTaggregate`, as for `Get`. When another declared class's tevents are
  also tevents of that class, the two classes' histories can't be told apart by their creation tevents, so
  `ReferencesToAll` throws and says so.
- **Not on the updater:** `ITaggregateStoreUpdater` extends the reader, not the bulk reader. Code inside a unit of work
  that reads every taggregate of a class depends on both.

## Deletion is a tevent

**Magnus, 2026-11-17 (item 42):** "When thinking in tevent sourcing terms, a delete is just another tevent." Found by
taggregates-opus replacing JAStudio's `NoteUpdated` with tevents: a note's own `Delete` published `INoteTevent.Deleted`,
and the code then called the updater's `Delete<TTaggregate>(id)`, one fact said twice. The updater's `Delete` went.

- **A taggregate leaves its store when it publishes a tevent that implements `ITaggregateDeletedTevent`,** effective when
  the unit of work commits. A deletion is a domain method that publishes, like every other change, and observers and
  participation handlers see it as the tevent.
- **Inside the unit of work, in both stores:** from the Deleted tevent on, the taggregate is gone: `Get` and
  `GetForUpdate` throw as for one no store holds, `TryGet` and `TryGetForUpdate` return false, and `ReferencesToAll` and
  the indexes leave it out. A rolled-back unit of work leaves the taggregate as it was.
- **A tevent published on it after its Deleted tevent fails the unit of work,** naming the taggregate and the tevent.
  The refusal is the taggregate's own: `Taggregate` asserts it in `Publish`, for both stores and outside any store,
  before the tevent changes the state.
- **A handler of a taggregate's own Deleted tevent can't read the taggregate:** the deletion takes effect inside the unit
  of work as the tevent is published, before its participation handlers run, so they get `TaggregateNotFoundException`.
  What such a handler needs goes on the Deleted tevent, as tevent sourcing has it: JAStudio's handler that takes a
  deleted vocab out of its web of perfect synonyms reads its name from `IVocabTevent.Deleted`. Decided by Magnus,
  2026-11-17, after weighing keeping it readable until prepare: "That might make sense for the subscriber to the deleted
  tevent, but anyone that has no clue that happened and gets that instance served up is working with some seriously
  messed up data."
- **The memory image state store** keeps no history, so it removes the taggregate at the commit, from its committed
  taggregates, its indexes and its storage. A taggregate created and deleted in the same unit of work writes nothing.
  It forgets a deleted id.
- **The tevent store keeps the history,** as tevent sourcing does: the Deleted tevent is appended like any other, and a
  history whose last tevent is a Deleted tevent is no taggregate. `Get`, `TryGet`, `GetForUpdate` and `ReferencesToAll`
  treat it as absent; `ITeventStoreReader.GetHistory` and `GetReadonlyCopyOfVersion` still give the whole history, its
  last version included. This is what Compze's query model generator already assumed:
  `SingleTaggregateQueryModelGenerator` reads a history that ends in a Deleted tevent as no model.
  - `ReferencesToAll` leaves out deleted histories with one more query, the type of each taggregate's last effective
    tevent, one row a taggregate: the same portable SQL in the four SQL layers, a join on each taggregate's highest
    effective version. `StreamTaggregateIdsInCreationOrder` stays as it is, since persisting migrations needs every id.
  - An id once used stays used: adding a new taggregate with a deleted one's id fails, as for any id it holds.
- **Erasing a history stays possible,** for what must not be kept, such as personal data the law says to erase:
  `ITeventStore.DeleteTaggregate(id)`. It is not what a domain deletion does.

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
  A parameterless constructor won't do: JAStudio's notes reach the collection, the dictionary and the configuration
  through `JPNote.Services`, and nothing may be static, since the specifications run several JAStudios in one process.
  The same holds for restoring outside any store, as the MCP server does with the serializer's services. From
  taggregates-fable.
- **Each taggregate class is named once where the store is composed,** with `store.Taggregate(...)`, so that loading
  knows which class restores from which snapshot type: the store finds it from the class's
  `ISnapshotable<TTaggregate, TSnapshot>`, and from what the factory builds, so no type arguments are written.
- **A non-generic face for the store,** which holds taggregates of many types: default interface members that take a
  snapshot, restore from one, and copy.
- **`Taggregate` has a protected way to say it was restored,** with its id: `RestoreIdentity(id)`, which also sets the
  version to 1. `RestoreFrom` calls it first; the store checks that it did, and that the id is the one asked for, and
  continues a copy's version from the committed instance.
- **A restored taggregate's entities** are added by its `RestoreFrom` with `AddWhileRestoring(id)` on their collection,
  publishing nothing, only while the taggregate is being restored:
  [snapshots-of-every-taggregate-shape.md](snapshots-of-every-taggregate-shape.md).
- **A snapshot holds the taggregate's whole state,** not only what is stored, since every copy is made from it. A
  component's and a shared tomponent's state are nested in the root's, as in JAStudio's DTOs. A snapshot can share every
  immutable value with the committed instance.
- **A snapshot holds only what the taggregate owns.** An object several taggregates share, changed in place, changes
  what each stores without a tevent: every instance, copy and committed instance alike, takes a snapshot holding the
  change, so the store finds nothing to write, and the committed instance isn't read only through it. Shared state
  belongs in a taggregate of its own, referred to by id, or is changed by a tevent on each taggregate that holds it. The
  round trip can't catch it, since the copy refers to the same object. In `ISnapshotable`'s documentation too. Found by
  taggregates-opus at JAStudio's switch: a shared condition renamed in place left the edit's copy unchanged, so nothing
  was written. JAStudio first published for that edit outright; since its step 10 (its 9c82dbd67) it takes the second
  way: renaming a shared condition publishes `MatchingConfiguration.SharedConditionRenamed`, a tevent that sets nothing,
  on each vocab that uses the condition by name, so the store writes each of them.
- **Restoring works outside any store:** JAStudio's MCP server builds notes from two git versions of a file.
- **Loading at a start runs no tevents:** the 0.4 GB of garbage a start made in both teams' prototypes goes, and with it
  decision 11 of the earlier conclusions.

### The snapshot round trip

**Two ways of setting state exist,** the tevent appliers and the restoring, which Magnus has decided for. What guards
them is `SnapshotRoundTrip`, in `Compze.Teventive`, namespace `Compze.Teventive.Taggregates`. **It is a hard
requirement under the private copy:** every get for update copies through the snapshot, so a property the snapshot
misses is dropped at the next edit, and the commit saves the loss. From taggregates-opus.

```csharp
SnapshotRoundTrip.DifferencesBetweenItAndItsCopy(vocab, () => new VocabNote(services)).Must().BeEmpty();
```

- **It makes the store's own copy,** through the class the store keeps for each taggregate class
  (`SnapshotableTaggregateClass`, internal, in `Compze.Teventive`): what it checks is what a unit of work gets.
- **It compares the taggregate and the copy, not their snapshots.** Comparing snapshots catches one of the three
  mistakes that lose state: a field `RestoreFrom` leaves out. A field the snapshot type has no place for, and one
  `TakeSnapshot` leaves out, give two equal snapshots, both without it. Comparing the instances catches all three. So a
  snapshot type needs no `Equals` (Q5, [the-files-of-a-state-store.md](the-files-of-a-state-store.md)).
- **What is compared:** every field of the taggregate's own classes, and of every object reached from them, parts
  included, field by field; collections item by item, in order for lists and arrays, in any order for sets and
  dictionaries; entity collections as state, each entity with its id, in creation order. Strings, numbers and .NET's
  other simple values by their own equality. An object both refer to, such as the services the taggregates are built
  with, is the same state. Not compared: the fields of Compze.Teventive's types, which publish and apply the tevents,
  and delegates. A reference back, such as a component's to its taggregate, is followed once.
- **A value created on its first read,** a `LazyCE` or a `Lazy<T>`, is compared by its value, read in both: JAStudio's
  `VocabNoteForms` holds six. Stronger than leaving such fields out, which taggregates-opus proposed instead: a derived
  value that comes out different is a real difference.
- **Each difference names its path** from the taggregate: `Book.Chapters._chapters[0]: "first" in the original,
  "second" in the copy`.
- **It returns the differences, and a specification asserts with its own assertions that they are none** (Q10, Q11):
  that also serves a tool that checks many taggregates, such as JAStudio's MCP tool
  `find_what_copies_of_the_notes_lose`, which checks all 79,896 notes in 1.3 s. Its first run found a real loss:
  JPNote's flushing bookkeeping, which a copy restored from a snapshot doesn't have.

## The memory image state store

**The store that keeps taggregates as their state,** in the package `Compze.Teventive.MemoryImageStateStore`: it holds
every taggregate in memory, as an instance, for the container's life, rebuilt at its start from a snapshot of each,
which its storage keeps. Named by Magnus, 2026-11-18, for Martin Fowler's memory image (Q2,
[the-memory-image-state-store.md](the-memory-image-state-store.md)); "state store" stays the word for the kind of store,
state rather than history. Its types: `IMemoryImageStateStore`, `MemoryImageStateStoreBuilder` and the registrar's
`MemoryImageStateStore(...)`.

```csharp
registrar.MemoryImageStateStore(store => store
   .Taggregate(() => new VocabNote())
   .Index<VocabNote, string>(it => it.Forms)
   .StoredInJsonFiles(folder, jsonOptions, files => files.FolderOf<VocabNote>("vocab")));
```

- **It loads everything at its first use,** not when the container is built: on that use's thread, and inside its
  transaction. It restores every taggregate on one thread: JAStudio's 80,000 notes in 5.0 s, where JAStudio's old cache
  filled its three classes in parallel. Without a storage it keeps its taggregates in memory only.
- **`IMemoryImageStateStore.LoadFromStorage(Action? changeTheStorageFirst = null)` loads everything again while
  running,** as JAStudio does around a git fast-forward: no tevent describes such a change, so it is a store operation of
  its own.
  - It runs the change once the open units of work have completed, while new ones wait, then loads, so that no unit of
    work can write a taggregate over the change. If the change throws, nothing is loaded and the exception comes out.
  - It keeps every reference, re-files the indexes, dispatches no tevent, and reports the `ChangesFoundInStorage`: the
    ids added, changed and removed, as collections, with no order.
  - It tells what changed by comparing only the taggregates whose files changed since the store last read or wrote them,
    by what the store's serializer writes, so a snapshot type needs no `Equals` (Q5,
    [the-files-of-a-state-store.md](the-files-of-a-state-store.md)).
  - Refused for a store composed with no storage, and inside a unit of work that has changed the store's taggregates.
- **`IMemoryImageStateStore.WriteAllToStorage()` writes every taggregate anew,** as the serializer and the consumer's
  options write its snapshot now, in one commit through the journal: for a consumer that has changed its snapshot types
  or its options, or wants to see in version control that loading and saving changes no file (Q5, named by the naming
  pass). It writes through the store's own file system, on the disk or in memory.
- **The store waits 30 seconds,** for a taggregate's lock, for a load, and for the units of work open during one, then
  throws.

### Storage: an internal seam

```csharp
///<summary>Where a memory image state store keeps what it has committed. Narrow: it knows snapshots, not taggregates.</summary>
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

- **Internal,** in `Compze.Teventive.MemoryImageStateStore._private.Storage`. Designed public, so that a consumer could
  keep its own storage behind it, and made internal once Compze's own JSON files covered JAStudio, as Magnus approved:
  "It feels like we now have half of a json backend, we build half and JAStudio the other half... I'm also not too sure
  that implementing the storage part in JAStudio is how we want things in the long run." The seam had been shaped by
  JAStudio's existing files, not by a store of Compze's own. Its two implementations are the JSON files, and `NoStorage`.
- **Two steps, because a transaction has two:**
  - the session calls `Prepare` when the transaction prepares, once the unit of work's tevents are dispatched and it has
    taken its copies' snapshots;
  - it calls `Commit` when the transaction commits, then swaps in the copies and applies the index changes, then
    releases the locks. The JSON files, unless they accept non-atomic commits with a database, have committed by then,
    as a durable participant of their own that commits in a single phase (Q1, under "The JSON files" below);
  - it calls `Discard` at a rollback.
- **Everything that can fail belongs in `Prepare`:** serializing, writing, a full disk, a file another program holds.
- **In memory, `Prepare` does nothing.**
- **Every specification of the store is said through the public API:** another program's changes are files written or
  deleted behind the store, on a file system in memory, and a failure to prepare a write is a real disk's, a file
  standing where the store's own folder goes. No `*.InternalSpecifications` project was needed.

### The JSON files

```csharp
registrar.MemoryImageStateStore(store => store
   .Taggregate(() => new VocabNote())
   .Taggregate(() => new KanjiNote())
   .StoredInJsonFiles(corpusFolder, NoteSerializer.JsonOptions, files => files
      .FolderOf<VocabNote>("vocab")     //<corpus>/vocab/<first two hex digits of the id>/<id>.json
      .FolderOf<KanjiNote>("kanji")));  //<corpus>/kanji/<first two hex digits of the id>/<id>.json
```

- **The files' format is the consumer's,** since JAStudio's corpus is reviewed as diffs: a file holds exactly what
  `JsonSerializer.Serialize(snapshot, options)` gives with the consumer's options, in UTF-8 without a byte order mark,
  the bytes JAStudio wrote before, so the corpus's files in git stayed as they were. **Writing several files as one,**
  the part easy to get wrong, is Compze's.
- **`StoredInJsonFiles(DirectoryInfo folder, JsonSerializerOptions options, Action<TaggregateFilesBuilder> declare)`:**
  the store's folder, the consumer's JSON options, and each class's files. An overload takes the folder from a service
  of the container, for a host that knows its folder only once another service has started, such as JAStudio's corpus
  folder: the service is resolved from the container's root when the store is built, at its first use.
- **`FolderOf<TTaggregate>(string folder)`:** the class's folder, under the store's folder. Each class the store keeps is
  declared, two classes don't share a folder, a class's folder isn't the store's own, and no class's folder lies inside
  another's, since loading reads a class's folder at any depth: all asserted when the container is built.
- **The file of a taggregate is its class's folder and its id:** `<first two hex digits of the id>/<id>.json`,
  JAStudio's layout. A taggregate keeps its file for its whole life, so a commit never moves one. A file computed by the
  consumer went in the end review (Q5): placement, such as across drives, would be a function of the id, not of the
  state, and adding it later breaks no one.
- **`ITaggregateJsonFiles`,** which the container resolves whenever the store is composed with `StoredInJsonFiles`, gives
  the path of a taggregate's file, `PathOf<TTaggregate>(id)`, and `PathOf(taggregate)` for one in hand, and the
  taggregate whose file a path is, `TaggregateWhoseFileIs`, from the path alone (Q6, Q13,
  [the-testing-namespace-and-the-json-files.md](the-testing-namespace-and-the-json-files.md)).
- **The options for all the files** are declared in the same chain, and written first in every example, so that none
  reads as an option of the class declared before it: `StartingFromAMemoryPackCache()`,
  `AcceptingNonAtomicCommitsWithADatabase()`, and, in the `Testing` namespace, `OnAFileSystemInMemory()`.

**All or nothing across the files of one commit:**
1. at prepare, each file is written whole under a temporary name, in the store's own folder, `<folder>/.state-store`,
   on the same volume as the files, so a rename moves no data. A consumer whose files are in version control ignores it;
2. at commit, a journal is written first, listing each temporary file with the name it is to get, and each file to
   delete. **The journal is the commit:** once it exists, the write has happened. It is itself written under a temporary
   name and renamed, so a crash while writing it leaves no journal, and the commit didn't happen;
3. then the renames and deletions, then the journal is deleted;
4. at start, before anything is loaded, a journal that exists is carried out and deleted, and temporary files without
   one are deleted.

- **The journal is written at commit, not at prepare:** written at prepare, a crash before a rollback had removed it
  would make the next start carry out a write that was rolled back.
- **The journal holds paths relative to the store's folder,** so it still works if the folder moved since the crash.
- **Each commit has a journal of its own,** named uniquely, so units of work on different taggregates can commit at the
  same time. Their files never overlap: a taggregate's lock is held until its unit of work completes.
- **A failure writing the journal rolls the unit of work back;** a failure after the journal is written stops the
  process with `Environment.FailFast`, and the next start finishes the write. A rename that fails because another
  program holds the file is retried first: on Windows a virus scanner or an editor can.
- **A transaction with a database in it too is refused by default** (Q1, Magnus, 2026-11-18,
  [a-state-store-in-files-and-a-database-in-one-transaction.md](a-state-store-in-files-and-a-database-in-one-transaction.md)).
  The two aren't atomic across a crash: the database can commit and the process die before the files' journal is
  written. So a unit of work in which the store writes files and a database connection of Compze's takes part is
  refused, by whichever joins second. The files are a durable participant that commits in a single phase. The opt-out,
  `files => files.AcceptingNonAtomicCommitsWithADatabase()`, makes them a volatile participant, and a crash between the
  commits then loses the files' change. JAStudio has no database.
- **Not flushed to the disk:** a commit survives the process dying, not the machine losing power. Flushing 80,000 files
  would take minutes for a rebuild, and JAStudio's corpus is in git. A setting to flush waits for a consumer that needs
  it.
- **What writing under a temporary name costs, measured by taggregates-opus on Windows** in JAStudio's own files, written
  the same way: about 0.6 ms a file, against 0.15 ms for overwriting in place. Rewriting the whole corpus takes about
  50 s instead of 12. An edit writes one file, so JAStudio accepts it. In return a single file is always written whole,
  where a file written in place and truncated makes the next start fail.

**What it loads:**
- **Every `*.json` file under each class's folder,** recursively, read as the class's snapshot type with the consumer's
  options. The taggregate is then restored from it, and its id is the restored taggregate's. Other files are ignored.
- **A file that can't be read or restored fails the load, naming the file.** So do two files that hold the same id.

**JAStudio on the JSON files, measured:** a full rebuild changes no file, writing every note changes none of the 79,896,
and an edit undone leaves its file byte for byte as before. Its start, headless, from warm files took 7.69 and 7.81 s,
and cold, the first read after a reboot, 37.65 s, against 4.01 to 4.45 s from its own MemoryPack cache of all its notes.
The cold start is Anki's case after every reboot. So the store has a cache.

### Starting fast: the MemoryPack cache

```csharp
.StoredInJsonFiles(folder, options, files => files.StartingFromAMemoryPackCache()
                                                   .FolderOf<VocabNote>("vocab"))
```

Any store that loads thousands of files at its start needs a cache, so it belongs to the store. MemoryPack is a
third-party library, and it requires `[MemoryPackable]` on the consumer's snapshot types, so it is in a package of its
own that a consumer plugs in. Magnus: "I don't think I have a problem with a new project with that dependency that a
consumer can plug in if they want this type of cache... Assuming that you cannot match its performance with that
serializer, and I don't think you can."

- **`Compze.Teventive.MemoryImageStateStore.MemoryPack`,** depending on MemoryPack (1.21.4, JAStudio's version).
  `StartingFromAMemoryPackCache()` is an extension of `TaggregateFilesBuilder` in it. It plugs into an internal seam of
  the store, `Compze.Teventive.MemoryImageStateStore._internal` (`ITaggregateFilesLoadCache`, `ICachedFilesOfAClass`,
  `ListedFile`, `LoadedSnapshot`), shared through `InternalsVisibleTo`.
- **Each declared class's snapshot type must be MemoryPack-serializable:** asserted at composition, naming the type.
  That is: the type is `[MemoryPackable]` (implements `IMemoryPackFormatterRegister`), or MemoryPack has a formatter
  registered for it.
- **A cache file for each class, in the store's own folder,** `.state-store/<class folder>.load-cache`. For each file of
  the class, it holds the file's path relative to the class's folder, its length, its last write time and the snapshot
  it held.
- **At load:** the class's files are listed with their lengths and times, which the folder listing gives without
  opening them. A file whose length and time are those in the cache takes its snapshot from the cache. Any other file
  is read as JSON, as without the cache. A file the cache has and the folder doesn't is gone. Then each snapshot is
  restored, as without the cache. Comparing each file's own length and time also catches a file restored with an older
  time, which JAStudio's old cache, comparing each file's time with the cache file's, missed.
- **The cache is written after a load that read any file, or found one gone.** Commits don't write it: a file a commit
  wrote has another length or time at the next start, and is read then.
- **The store owns the cache's file; the cache only turns content into snapshots and back.** The seam hands the cache
  the bytes it last wrote, and takes back the bytes to write, or none. So the store writes it whole with the same
  temporary name and retried rename as a commit's files: a crash leaves the old cache or the new, and the start after a
  crash deletes a half-written cache as it deletes a commit's temporary files.
- **The cache's content:** the length of a listing part in four bytes, the listing part (the fingerprint, then each
  file's path, length and last write time in ticks), then the snapshots as one MemoryPack list in the same order. The
  fingerprint is read before any snapshot.
- **A changed snapshot type makes the cache unusable:** MemoryPack reads by position, and a cache written for the type
  before a change may read back wrong values without failing. So the cache records a fingerprint of each class's
  snapshot type, and a cache whose fingerprint differs is not read, and is written anew after the load. A change that
  doesn't matter to MemoryPack costs one slow start, never wrong data. The fingerprint is a SHA-256 of a description of
  the snapshot type:
  - a type of the base class library (namespace `System` or `System.*`) is described by its name, with a generic one's
    type arguments described, so a collection's elements are;
  - an enum by its name, underlying type, and each name with its value; an array by its rank and element type;
  - any other type by its name, its instance fields, public or not, from the base class down, each class's in
    declaration order, each with its name and its type described, and the types of its `[MemoryPackUnion]`s. Fields
    catch properties through their backing fields;
  - a type met again is named, not described again, which also ends a cycle. Names hold no assembly version.
- **A cache that can't be read is a failure, not a fallback.** It is renamed into place whole, and a changed type is
  caught by the fingerprint, so an unreadable one means something is wrong, and the load fails, naming it. Deleting
  it gives a slow start.
- **JAStudio on the cache, measured** by taggregates-opus, with other processes at 0.5 to 0.8 cores of 32: it starts in
  3.95, 4.06 and 4.13 s, against 4.01 to 4.45 s from its old `snapshot.bin`; the start that wrote the cache, from warm
  files, took 10.09 s. It reads 80 MB of MemoryPack across the three classes, against 74 MB, plus the folder listings.
  Cold with the cache wasn't measured: emptying the system's file cache takes administrator rights.

### On a file system in memory, for specifications

```csharp
using Compze.Teventive.MemoryImageStateStore.Testing;

.StoredInJsonFiles(folder, options, files => files.OnAFileSystemInMemory()
                                                   .FolderOf<VocabNote>("vocab"))

var files = container.RootResolver.Resolve<IFileSystemInMemory>();
var path = container.RootResolver.Resolve<ITaggregateJsonFiles>().PathOf<VocabNote>(id);
files.ReadAllText(path); files.WriteAllText(path, json); files.Delete(path); files.Paths;
```

**Decided by Magnus, 2026-11-17:** a consumer's specifications run on the JSON files with the files kept in memory, not
on a storage of the consumer's own: "Then go with that. We can always refactor if it turns out not to work well."
JAStudio's specifications, run on the JSON files with a fresh folder each, had taken 150 to 186 s against 56 to 59 s in
memory, most of it in 74 of about 2,500 specifications, which start with the specifications' full collection of 1,035
notes, each note added in a unit of work of its own: about 5,000 file operations each, since each commit makes about
five. Since the end review, `OnAFileSystemInMemory()` and `IFileSystemInMemory` are in the package's `Testing`
namespace, seen only where it is imported (Q6,
[the-testing-namespace-and-the-json-files.md](the-testing-namespace-and-the-json-files.md)).

- **The same store, with its file operations behind an internal seam,** `IFileSystem`, with `FilesOnDisk` and
  `FileSystemInMemory`: writing, renaming, deleting, listing a folder's files with their lengths and last write times,
  and reading. Every file operation of the store, its journal and the MemoryPack cache's file goes through it; what is
  left outside builds paths only. With `OnAFileSystemInMemory()`, the store, its layout, the consumer's JSON options,
  the journal and the MemoryPack cache all work as on disk, and nothing touches the disk. `folder` is then only where
  the paths begin. One file system for each container.
- **`IFileSystemInMemory`, resolvable only with `OnAFileSystemInMemory()`:** a specification reads what the store wrote by
  path, and changes the files behind the store's back, as a program changing the files on disk would, to specify loads:
  `Paths`, `ReadAllText`, `WriteAllText`, `Delete`. Files written before the store's first use are what it loads.
  - It takes full paths as strings, compared ignoring case, as `File`'s methods do. Its `ReadAllText`, `WriteAllText`
    and `Delete` behave as theirs, missing files included.
  - A write sets the file's last write time, so the cache sees it changed. The memory clock is strictly increasing: a
    write within the same tick as the last gets a time one tick later.
  - "A second load reads no unchanged file" is pinned on the disk only: in memory, a specification can't keep a file's
    time while changing it, so it can't tell a cached snapshot from a read one. The code path is the same.
- **JAStudio's specifications on it, measured:** the whole suite in 67.0 and 58.1 s, against 68.7 and 59.9 s on its own
  storage in memory, which is deleted. The total is unchanged, since the MCP specifications, which don't use the mini
  corpus, set the wall time; Web's are about 40% faster. Its mini corpus is made once per test process for each data
  set, and its files are written into each specification's app before a load, as a start loads them (Magnus: "The tests
  should load their mini corpus the same way as the real app if possible").

## The tevent store beside it

The tevent store keeps each taggregate as its history, and serves the same `ITaggregateStoreUpdater`,
`ITaggregateStoreReader` and `ITaggregateStoreBulkReader`, so one unit of work can change a taggregate of each store.
Its history, which only it has, is read through `ITeventStoreReader` (Q8,
[the-tevent-store-reader-alone.md](the-tevent-store-reader-alone.md)).

```csharp
registrar.TeventStore(connectionName, store => store.Taggregate<User>());
```

- **It declares the classes it keeps,** each a `ITaggregate<ITaggregateTeventWithIdentity>`, checked with the type
  validator when the store is built, so `Get` and `Add` don't validate on each call. The typermedia registration's
  `HandleTaggregate<TTaggregate, TTevent>()` also declares its class, so an endpoint that uses it declares nothing more;
  it declares on the store's builder after `TeventStore` has run, since the builder is read when the container builds
  the store.
- **A history doesn't say what class it is the history of,** so the tevent store builds the declared class that is the
  class asked for, or the one declared class derived from it. Asked for a base class that several declared classes
  derive from, it throws, saying to get the taggregate as its own class. Asked for an id whose history is of another
  declared class, it throws before applying the history, naming the id and both classes, as the memory image state store
  does (D12, [a-taggregate-asked-for-as-another-class.md](a-taggregate-asked-for-as-another-class.md)).
- **Outside a unit of work, `Get` builds the taggregate from its history, read only:** publishing on it throws, as on the
  memory image state store's committed instances, and so does publishing on what `GetReadonlyCopyOfVersion` returns. The
  message is store-neutral: "is an instance a taggregate store serves to readers, which is read only".
  `GetReadonlyCopyOfVersion` asked for a version past the latest throws.
- **References:** `Current` is the unit of work's instance if it holds one, else a read-only instance built from the
  history. The tevent store keeps no instances in memory, so each `Current` outside a unit of work builds one; its
  history cache spares the database.
- **Dispatch, synchronous:** each tevent is saved, then dispatched, as its taggregate hands it out. A new taggregate's
  tevents are saved and dispatched at `Add`.
- **The writes** are made at the publish through the transaction's database connection, so the database commits or rolls
  them back with the transaction. The session has nothing to prepare or commit.
- **The locking is the database's:** `GetForUpdate` reads the history with the update lock; no lock in memory. On MySQL,
  whose InnoDB at read committed locks no gap for an id with no history, a read for update also writes the id's row in a
  lock table of the schema, `INSERT … ON DUPLICATE KEY UPDATE`, which takes a row lock InnoDB releases at commit or
  rollback.
- **Its session belongs to the transaction's `TaggregateUnitOfWork`,** not to a scope, so two scopes of one transaction
  get the same instance of a taggregate, where each scope's updater once loaded its own.
- **One `TaggregateNotFoundException` for both stores.** It keeps the tevent store's base class,
  `ArgumentOutOfRangeException`, which is what a consumer can catch, since the exception itself is internal.
- **`ITeventStore` is a singleton.** It is stateless, and the shared reader is a singleton that needs it. It reads its
  migrations through the function it is registered with at each use, so the migration specifications that change them
  while a container lives still can.
- **Its internal classes:** `TaggregateTeventStore`, the tevent store as an `ITaggregateStore`, `TeventStoreSession`, its
  part of a unit of work, and `TeventStoreReader`. `TaggregateReference` is in `Compze.Teventive`, shared by both stores.

## A rebuild

One transaction for a whole rebuild means a lock and a copy for every note until the commit, and every handler run on
one thread at the end. Two shapes remain, a consumer's to measure and choose:

- **A unit of work for each note** costs about 10 µs and 9.5 KB, so about 0.8 s of one core and 0.76 GB of garbage for
  80,000. It never waits for a lock while it holds one. taggregates-fable would take it first. In JAStudio a full rebuild
  so took 53 processor-seconds, against 36 with its old saving: the scope, the transaction, the copy, two snapshots,
  hashing, and the keys of ten vocab indexes.
- **Batches of some hundreds of notes** make less garbage. But a batch holds every note it changed until it commits, so
  a UI edit that needs two notes can wait for a batch that waits for it, until the timeout. Meanwhile, other threads
  look the batch's notes up by their old keys.

Generated data can be generated again, so a failed unit of work costs little either way.

## What isn't designed yet

- **Keeping each held copy's keys in the index's overlay** (a `performance:` remark at `TaggregateIndex.Lookup`).
- **Reads of taggregates not got for update, and atomic visibility:** see [Open](#open), under the isolation.
- **Two containers in one transaction:** the unit of work that began it may belong to the other container (a `todo:`
  at `TaggregateUnitsOfWork`).
- **Whether `ITaggregate.Commit` should exist** (a `todo:` at it).
- **A Compze lock owned by a transaction,** in Compze.Threading, to replace the `SemaphoreSlim`: an effort after the
  spike.
- **Whether the shared interfaces get a package of their own,** decided after the spike.
- **A commit of a single file needs no journal:** writing it under a temporary name and renaming it is already all or
  nothing. Most commits write one file, and it would take them from about five file operations to two. Noticed, not
  designed.
