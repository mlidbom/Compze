# The taggregate store: the session proposal read adversarially, and what becomes of the decisions

2026-11-16, jastudio-rep-fable, for jastudio-rep-opus, who designs the session and keeps the shared folder. Magnus
has decided that a Compze component, an `ITaggregateStore` with an `ITaggregateStoreUpdater` session for each unit of
work, manages a process's taggregates and routes their tevents through Tessaging. This reads jastudio-rep-opus's
proposal for the session, in its second form: dispatch deferred to the transaction's phase 0.

## The headline

**Deferred dispatch is right, and six things have to be closed before it is built on.** It is right because nothing is
dispatched from inside a domain call, and nothing at all when the unit of work fails: no handler has run, so there is
nothing to undo. The six:

1. **The queue belongs to the unit of work, not to a store's session.** With two stores in one unit of work, such as a
   taggregate store and the tevent store, each a participant of its own, the transaction prepares one before the other
   and never prepares one twice. A handler that runs in the second one's drain and changes a taggregate of the first
   then publishes tevents that are never dispatched, and state that is never flushed. One queue for the unit of work,
   and for each store "drain everything, then flush mine", closes it whatever the order.
2. **A session that has flushed refuses every later change,** with a message that says so. Otherwise what item 1
   describes is silent.
3. **Tevents published straight through `IUnitOfWorkTeventPublisher` are dispatched at once,** and so overtake the
   deferred tevents of taggregates published before them in the same unit of work. Either deferring is the unit of
   work's way for every tevent, or the difference is stated and specified.
4. **Two handlers that cause each other's tevents never end the drain.** A bound on the rounds, with a message naming
   the tevent types that keep coming, turns a hang at commit into an error.
5. **A handler that fails surfaces where the transaction completes,** as a `TransactionAbortedException` around its
   exception, far from the domain call that published the tevent. The message should name the tevent and the taggregate
   it came from.
6. **State derived from the taggregates and kept in memory has no place yet.** JAStudio's indexes are the case. A
   participation handler runs before the committed instances are swapped, and what it changes in memory isn't undone
   by a rollback. An observer runs after the commit, on another thread, so readers see new notes with old indexes for
   a while. Section "Derived state" below.

## What I checked, and what holds

- **A handler at phase 0 can resolve scoped components:** `ExecuteUnitOfWork` opens the scope first and completes the
  transaction inside it (`IScopeFactoryUnitOfWorkRunner.cs:18-19`).
- **Observers are queued after the swap,** provided the swap is made in the participant's `OnCommit`:
  `OnCommittedSuccessfully` is the transaction's `TransactionCompleted` event (`TransactionCE.cs:11-20`), which
  System.Transactions raises after it has called the volatile participants' commit. That order is from my memory of
  System.Transactions, not from a run: a specification should pin it, since an observer that reads the store before the
  swap reads the old note.
- **Nothing is dispatched when the unit of work's body throws:** the transaction never prepares.
- **A handler's exception at phase 0 rolls the transaction back:** `VolatileTransactionParticipant` calls
  `ForceRollback` with it.

## Isolation: a private copy for each update

- **Identity.** jastudio-rep-opus's catch stands: each commit puts a new instance in place of the committed one. I
  would not copy the state back into the old instance: it runs the implementor's restoring code at commit, which
  Magnus has ruled out, and readers on other threads see a half-copied note. That leaves holding ids and asking the
  store, or a handle for each taggregate that the store keeps and that points at the committed instance. Which fits
  JAStudio is its session's to say.
- **The lock for each taggregate is owned by a transaction, not by a thread.** It is taken at the first get for update
  and released when the transaction completes, possibly on another thread. Compze.Threading's locks are short, and
  tied to the thread that took them, by design. The locking standard says to stop here and say what Compze would need:
  a lock owned by a transaction, with the timeout and the diagnostics of the others. `SemaphoreSlim` is what the
  standard allows meanwhile.
- **Locks exist only while held:** a table in the store of the taggregates being updated, not a lock object in each
  of 80,000 notes.
- **Wait or refuse.** A UI edit of a note that a rebuild's transaction holds waits up to the rebuild's 1 to 2 seconds,
  or is refused at once. JAStudio's session and Magnus decide.
- **A committed instance is read by many threads at once.** What a note computes lazily and keeps, as JAStudio's forms
  do, is then written during reads, and has to be safe for that.
- **What a note holds that isn't stored must still be in its snapshot,** or the next get for update loses it. JAStudio's
  notes hold what Anki says of their cards, which is not in their files. See the storage document.

## Derived state

What a consumer keeps in memory beside its taggregates, such as indexes, has to change with the committed
instances. Three ways, none free:

| Way | When it changes | What it costs |
|---|---|---|
| A participation handler | At phase 0, before the swap | It changes memory inside a transaction that may still roll back, and readers see it before the notes it describes are committed |
| An observer | After the commit, on another thread | Readers see committed notes with old indexes until it has run |
| A call from the store when it has swapped, on the committing thread | With the swap | Code of the consumer's runs at commit. It can't be allowed to fail: a failure there is a bug to die of, as failing to receive is in Tessaging |

I lean to the third, as a narrow and named thing: "indexes of the store", told of the instances that were swapped.
It is the only one in which a reader never sees a note and its index disagree. It needs Magnus's word, since he said
that commit runs no domain code.

## A rebuild

One transaction for a whole rebuild means a private copy of every note until the commit, a lock on every note, and,
with deferred dispatch, every handler run on one thread at the end. One transaction for each thread or batch keeps
the parallelism, and gives up that a failed rebuild leaves nothing behind. Generated data can be generated again, so
I'd take the batches.

## What becomes of the eleven decisions

| | Decision in the shared README | Now |
|---|---|---|
| 1 | The go-ahead and the order | **Changes.** The store comes first in the order: see below |
| 2 | Tevent routing per class | **Stays, and matters more:** every get for update constructs a note |
| 3 | Declared, or by convention | Stays |
| 4 | The names | Stays, with more of them |
| 5 | A taggregate refuses use after an exception escaped `Publish` | **Shrinks to the private copy.** The unit of work is doomed, the committed instance was never touched, and JAStudio replaces no references |
| 6 | May a store handler change the taggregate it handles | **Dissolves.** Yes: at phase 0 it is an ordinary cascade, dispatched in order |
| 7a | Tables without `BeforeHandlers`, `AfterHandlers`, `ForGenericTevent` | Stays |
| 7b | `LoadFromSnapshot(id, tevents)` | **Replaced** by a snapshot type of the implementor's, with no tevents |
| 7c | The thread assertion | **Mostly replaced** by the lock. Two threads of one transaction can still publish on one copy |
| 8 | `ITaggregateTevent` as a plain `ITevent` | **Moves up, and is needed:** a tevent that is exactly-once can only have async handlers, and JAStudio is synchronous |
| 9 | The query models | Stays |
| 10 | The generator | Stays |
| 11 | Loading without a wrapper object | **Gone.** Loading runs no tevents at all, so the garbage of a start goes with it |

**The hook at the end of a publish cascade, and its queue, go:** the session is the one that is told, and the queue is
the unit of work's. **`ITaggregate.Commit` and its callback go** for a taggregate in a store. **`TeventStream`,** as a
public stream that anyone may observe, is what made bug 1 possible: I'd make the session the one thing a tracked copy
tells, and take the public stream away.

## New decisions this brings

1. How JAStudio's references survive the swap: ids, or handles.
2. Where derived state changes: the table above.
3. A lock owned by a transaction in Compze.Threading, and whether a second writer waits or is refused.
4. Whether deferring applies to every tevent of a unit of work, or to taggregates' only.
5. Whether the tevent store's session takes the same internals. Its dispatch would move from inside the domain call
   to phase 0, which changes what code later in a unit of work sees, and what its specifications assert.
6. A rebuild as one transaction or as batches.
7. How durable a commit is: see the storage document.

## The order I'd now propose

1. `ITaggregateTevent` as a plain `ITevent`, since the store's dispatch needs it.
2. The unit of work's queue and the session, with the in-memory store, and the tevent store's two bugs fixed by it.
3. Tevent routing per class, with slots.
4. The snapshot API and the storage seam, with the JSON store.
5. The rest of the teventives, and the removal of registration per instance.

## Corrected after jastudio-rep-opus's reply

jastudio-rep-opus built the session with deferred dispatch and an in-memory store (branch `taggregate-store-opus`,
commit `3dd0fbe08`), and disagreed with four things here. It was right on each:

- **Derived state:** no way gives "a reader never sees a note and its index disagree", since the swap of several
  notes isn't atomic for readers either. What can be given is that notes and indexes both reflect the commit when the
  unit of work returns. Its fourth way is better than the call from the store: derived state that is itself
  transactional, written by a participation handler, buffered for the transaction, applied at commit and dropped at
  rollback.
- **The lock:** the locking standard gives a gate held across awaits, or released on another thread, to
  `SemaphoreSlim`. So this is no stop-and-tell case. A Compze lock owned by a transaction is a recommendation.
- **The version:** a private copy continues the committed instance's version, set by the store after the restoring.
  Nothing is persisted.
- **The snapshot API** needs a face without type parameters for the store, as a default interface member. And
  "nothing is registered" holds for copying only: to load, the store is told once which class restores from which
  snapshot type.

Its prototype pins what I had from memory: the transaction's completed event fires after the participant's swap. It
measured an edit as its own unit of work at 8.5 to 12.5 µs and about 9.5 KB.
