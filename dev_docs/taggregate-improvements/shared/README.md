# Taggregates for JAStudio's notes: the conclusions both teams agree on

**Status: redesigned around a taggregate store on Magnus's direction (2026-11-16),** by jastudio-rep-opus and
jastudio-rep-fable, with JAStudio's facts from taggregates-fable. The earlier conclusions were agreed by all four
sessions, then reviewed by two independent agents and revised: what the review changed is in
[reviews/discussion.md](reviews/discussion.md). The store's design has not been reviewed independently. jastudio-rep-opus
manages this folder and reports for Compze; taggregates-fable manages JAStudio's `dev_docs/taggregates/shared/` and
reports for JAStudio.

| Team | Compze session | JAStudio session | Their own records |
|---|---|---|---|
| opus | jastudio-rep-opus | taggregates-opus | `../opus/`, and JAStudio's `dev_docs/taggregates/opus/` |
| fable | jastudio-rep-fable | taggregates-fable | `../fable/`, and JAStudio's `dev_docs/taggregates/fable/` |

**JAStudio's side** is in `C:\Dev\JAStudio.wt2\dev_docs\taggregates\shared\`, on JAStudio's branch `compze-taggretates`:
what the notes require of Compze, what the teams found about JAStudio, its answers for the store
(`answers-about-jastudio-for-the-taggregate-store.md`), and what is left on JAStudio's side. This folder holds the
design, the order of work in Compze, and the full list of what Magnus decides.

## The question

JAStudio wants its notes to be Compze.Teventive taggregates, so that every change to a note is a tevent, and whatever
follows a change subscribes to exactly the tevents it concerns. It holds about 80,000 notes in memory, each with 8 to 25
mutable parts, and keeps each note's state in a JSON file, not its history. What must change in Compze for that, and
how?

## The answer

**A taggregate store, tevent routing decided per class, and `ITaggregateTevent` as a plain `ITevent`.**

1. **A taggregate store** keeps taggregates as their state, under the tevent store's programming model: an
   `ITaggregateStoreUpdater` session for each unit of work, and the unit of work a System.Transactions transaction even
   with no storage. Implemented in memory, and backed by JSON with the storage behind a narrow interface. It
   dispatches tevents through Tessaging's in-process engine when the unit of work commits, never from inside a domain
   method. That is what Magnus asked for, and it replaces the earlier hook, its queue, and the snapshot made of tevents.
   - **Its programming model holds without storage:** prototyped and specified, an edit as its own unit of work costs
     8.5 to 12.5 µs in Release.
   - **The two reentrancy bugs in Teventive, and the two in the tevent store's session,** can't arise: nothing calls out
     from inside `Publish` or `Commit`.
   - **JAStudio's answers reopen the isolation Magnus chose,** a private copy for each unit of work: under it every
     place in JAStudio that edits a note changes. We recommend shared live instances, "read uncommitted", instead.
     Decision 2.

   The design: [the-taggregate-store.md](the-taggregate-store.md).
2. **Tevent routing decided once per class, not set up per instance.** Without it, 80,000 notes cost 0.6 to 1.1 GB
   more than as plain objects. The prototypes bring that to about 27 MB, and load a note with components and shared
   tomponents 9 to 12 times faster in Release. Under the private copy, every get for update constructs a copy of a
   note, so it would matter more: [the-design.md](the-design.md), section 1.
3. **`ITaggregateTevent` as a plain `ITevent`.** Today every taggregate tevent is an `IExactlyOnceTevent`, and Tessaging
   accepts only async handlers for those. A synchronous application such as JAStudio needs synchronous handlers.

**Whether JAStudio should make its notes taggregates at all is a separate question, and still open.** JAStudio's side
says Compze can be made to carry the notes, but adopting taggregates was never sized, or compared with a smaller route.
Under the private copy, the size grows by every place in JAStudio that edits a note. See JAStudio's shared folder.
Routing per class and the store stand on their own for Compze.

The numbers: [what-was-measured.md](what-was-measured.md). The bugs and the smaller findings:
[bugs-and-findings.md](bugs-and-findings.md).

## The order we propose

1. **`ITaggregateTevent` as a plain `ITevent`,** at least the interface, since the store's dispatch and JAStudio's
   handlers need it.
2. **The unit of work's queue, and the in-memory taggregate store.** Deferred dispatch at phase 0, the isolation Magnus
   decides, the lock for each taggregate, the rollback after a failed publish. The tevent store's session takes the same internals,
   which fixes both its bugs and Teventive's two.
3. **Tevent routing per class:** the table built on the dispatcher's own matching code, the root and its components,
   slots that belong to the owner's class, inheritance through `Extend`, and the safeguards. With it, a performance
   specification in Release that bounds the bytes a copy allocates, and the published doc
   `Taggregates/_docs/reusable-components.md` rewritten.
4. **The snapshot API and the storage seam, with the JSON store,** and the specification helper that round-trips a
   snapshot. **JAStudio can start here.**
5. **The rest of Compze's teventives:** entities, removable entities and shared tentities designed and moved to tables,
   the query models decided, and per-instance registration removed.

## What Magnus decides

1. **The go-ahead, and the order above.**
2. **Isolation, which Magnus chose before JAStudio's answers were known:**
   - **(a) a private copy for each unit of work,** his choice, and prototyped. Readers see committed state only, as in
     the tevent store. But every commit replaces the note object, so every place in JAStudio that edits a note changes,
     and what holds notes holds handles. JAStudio's two sessions say that this decides the cost for JAStudio.
   - **(b) shared live instances, "read uncommitted"** (**recommended**). The note object stays for good, a change with
     no session in hand joins the ambient unit of work, and JAStudio's edit sites stay as written. Readers on other
     threads see changes before the commit, and some that are rolled back, as JAStudio's do today during a rebuild. Its
     price in Compze is restoring the live instance in place at a rollback, which becomes the only restoring, loading
     included, so it is written once for each part and run at every start. It also keeps (c) open, which (a) doesn't.
     Within (b), a change made outside any unit of work is either a unit of work of its own, which spans one publish
     cascade only, so an operation that publishes twice must be run in a unit of work, or it is refused, as under (a).
     And code that decides from a taggregate's state before changing it gets it for update first, since the lock taken
     at the first change doesn't cover what was read before it.
   - **(c) stable objects whose state is versioned per transaction,** which would give both, by holding a taggregate's
     state as an immutable value. That changes how every taggregate is written, and is not designed.

   The comparison is in [the-taggregate-store.md](the-taggregate-store.md).
3. **Derived state, such as JAStudio's indexes,** follows what readers see. Under (b), the recommended isolation, it
   follows the live instances: the session tells the consumer at the end of each settled cascade, by a notification
   that must change no taggregate, and again after a rollback. That notification is the one call from inside `Publish`
   that the design keeps. Under (a), derived state is itself transactional: written by participation handlers and
   applied at commit.
4. **Whether `IUnitOfWorkTeventPublisher` itself defers to phase 0,** so that every tevent of a unit of work is
   dispatched in one order (**recommended**). Otherwise a tevent published straight through it overtakes the deferred
   tevents of taggregates. The cost: code later in a unit of work no longer sees what participation handlers did.
5. **Whether the tevent store's session takes the same internals** (**recommended**): it dispatches at phase 0 instead
   of as each tevent is published, which changes what its specifications may assert.
6. **The lock for each taggregate:** a second writer waits, with a timeout (**recommended**), or is refused at once.
   And a Compze lock owned by a transaction, with diagnostics naming the holder, to replace `SemaphoreSlim`.
7. **Tevent routing per class:** whether to do it. **Magnus, 2026-11-16: yes, as a design decision** (not yet the go-ahead to build it). It changes protected members of public base classes: 33
   registrations in 28 files of Compze's tests and sample, all mechanical, and 13 files in `src`, 7 of them the query
   models'.
8. **Declared, or by convention:** both are prototyped and cost the same. Both teams and both reviewers recommend the
   declared table, with its safeguards: by convention, a misspelled tevent handler is silently never called.
9. **`ITaggregateTevent` as a plain `ITevent`:** **Magnus, 2026-11-16: yes, as a design decision.** Still open, which of two variants:
   - the interface only, which the store needs;
   - moving the tevents' identity to the tevents that need it as well, which touches every stored taggregate's tevents
     and the migrations.
10. **Changes to public API, to veto or accept:**
    - taggregate tables without `BeforeHandlers`, `AfterHandlers` and `ForGenericTevent`, which nothing needs;
    - the snapshot interface, `ISnapshotable<TSelf, TSnapshot>`, and a protected way to say a taggregate was restored;
    - for a taggregate in a store, `Commit`'s callback gone, and `TeventStream` no longer public;
    - possibly one updater interface for both stores, since `ITeventStoreUpdater` has the same four members;
    - a failed publish rolls the unit of work back even when its exception is caught.
11. **The query models:** move them to tables in step 5, or keep their per-instance registration.
12. **The source generator for tevent implementation classes:** an effort of its own, after JAStudio has written one
    note type's tevents by hand.
13. **The names:** every name in the design documents is a placeholder.

**What the store makes moot from the earlier list:**
- whether a tevent store handler may change the taggregate it handles: at phase 0 it is an ordinary cascade;
- the thread assertion: the lock serializes writers;
- refusing use of a taggregate after a failed cascade: the unit of work rolls back instead, and the committed instance
  is untouched;
- loading without a wrapper object: loading runs no tevents.

**Work that needs no decision,** listed in [bugs-and-findings.md](bugs-and-findings.md):
- specifications of eight behaviours JAStudio relies on that nothing pins;
- the validator's 3 to 9 scans of whole assemblies per class;
- `PublisherTevent.WrapIn`'s closure per call, now an `urgent:` todo at the code;
- `Compze.Core.slnx`, which now loads but builds against a stale Compze.Threading package;
- a stale doc comment;
- the root README's claim that in-memory overhead is "entirely negligible".

## Where the work is

Nothing is merged, and nothing in Compze's `main` is changed. The prototypes are experiments, each on a branch and
worktree of its own under `C:\Dev\Compze.worktrees\`. Their API isn't the designed one where the design changed after
they were built.

| Branch | Built by | What it holds |
|---|---|---|
| `taggregate-store-opus` | jastudio-rep-opus | The in-memory taggregate store with deferred dispatch, specified and measured (3dd0fbe08). `Compze.Tests.Unit` passes, 417 of 417; the store's 23 behaviours pass on both combinations. |
| `taggregate-experiments-opus` | jastudio-rep-opus | Routing per class in the real base classes: components, entity collections, slots, inheritance. Full suite passes. |
| `taggregate-experiments-opus-convention` | jastudio-rep-opus | The same, by convention, for comparison. Full suite passes. |
| `taggregates-opus/snapshot-and-hook` | taggregates-opus | The superseded snapshot of tevents and hook with its queue, both bug fixes, the assertion of one thread, and the refusal after a failed cascade (4b42470ba). Full suite passes. |
| `taggregates-opus/combined` | taggregates-opus | The two above merged, and measured. Full suite passes (604c00df4). |
| `taggregate-experiments-fable` | jastudio-rep-fable | A standalone prototype of routing per class with static slots, and the claims run against 0.6.0-alpha. |
| `taggregate-experiments-fable-static-slots` | jastudio-rep-fable | Static slots in the real base classes, built on the combined branch, and measured (fb75d60a0). `Compze.Tests.Unit` passes, 496 of 496; the other test assemblies weren't run on it. |
| `review-taggregate-improvements` | the Opus reviewer | Two specifications showing the thread assertion and the hook's base call against the tevent store (861474504). In `C:\Dev\Compze\.claude\worktrees\agent-a5042660ba8c7b64c`. |
| `review-taggregate-conclusions` | the Fable reviewer | Seven checks against a Release build of the static-slots prototype (a440911a7). In `C:\Dev\Compze\.claude\worktrees\agent-ad538039c7b0a5f4e`. |
