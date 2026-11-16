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
   - **Isolation by a private copy for each unit of work,** Magnus's choice, confirmed after two independent reviews
     chose it for Compze and JAStudio alike. Code holds taggregate-references across units of work, and indexes are
     defined by the taggregates' state and read their own unit of work's changes. Its cost to JAStudio: every place that
     edits a note changes.

   The design: [the-taggregate-store.md](the-taggregate-store.md).
2. **Tevent routing decided once per class, not set up per instance.** Without it, 80,000 notes cost 0.6 to 1.1 GB
   more than as plain objects. The prototypes bring that to about 27 MB, and load a note with components and shared
   tomponents 9 to 12 times faster in Release. Every get for update constructs a copy of a note, so it matters more
   with the store: [the-design.md](the-design.md), section 1.
3. **`ITaggregateTevent` as a plain `ITevent`.** Today every taggregate tevent is an `IExactlyOnceTevent`, and Tessaging
   accepts only async handlers for those. A synchronous application such as JAStudio needs synchronous handlers.

**Whether JAStudio should make its notes taggregates at all is a separate question, and still open.** JAStudio's side
says Compze can be made to carry the notes, but adopting taggregates was never sized, or compared with a smaller route.
The private copy adds every place in JAStudio that edits a note to the size. See JAStudio's shared folder.
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

1. **The go-ahead to build, and the order above.** Magnus, 2026-11-16: steps 1 and 3 agreed as design decisions, which
   is not yet the go-ahead to build them on a branch that isn't exploratory. **Then, the same day: a spike, decided.**
   - Steps 1 to 4, through the JSON store, on an exploratory branch in Compze's main checkout, `C:\Dev\Compze`, each
     step specified before it's built. Every branch worked on is pushed as it goes: "I don't want to lose any work if
     my machine burns". Alternatives may be tried on as many branches as helps, kept as a record of the results, and an
     agent or two may build alternative versions in worktrees of their own, minding the subscription's cost.
   - JAStudio migrates on its own branch, referencing the spike's projects directly. Not one part only: jastudio-rep-opus
     supports JAStudio's agents "until they have completed the entire migration and everything they can test
     themselves work".
   - The spike's branch isn't merged. Whether it's cleaned up and merged, or the real build starts afresh from what it
     showed, is decided when it's done.
2. **Isolation: decided.** Magnus, 2026-11-16: a private copy for each unit of work, unless JAStudio's sessions find a
   strong reason for shared live instances. Two independent reviewers chose it for both Compze and JAStudio
   ([reviews/isolation-review-by-an-opus-agent.md](reviews/isolation-review-by-an-opus-agent.md),
   [reviews/isolation-review-by-a-fable-agent.md](reviews/isolation-review-by-a-fable-agent.md)). The teams'
   earlier recommendation of shared live instances, "read uncommitted", was mainly to spare JAStudio's refactoring.
   A change outside a transaction is refused, by construction.
3. **Derived state, such as JAStudio's indexes:** defined as a function of the taggregates' state, provided by Compze,
   kept by the store at commit, and reading its own unit of work's changes at lookup. Both reviewers proposed it, and
   it is prototyped: a committed lookup costs 0.05 to 0.18 µs and allocates nothing, and one inside a unit of work that
   holds copies of its type grows with them until the overlay keeps each copy's keys.
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
8. **Declared, or by convention: decided.** Magnus, 2026-11-16: "Definitely tables. One killer thing is ordering
   dependencies. Another the method bloat." Both teams and both reviewers had recommended the declared table, with its
   safeguards.
9. **`ITaggregateTevent` as a plain `ITevent`:** **Magnus, 2026-11-16: yes, as a design decision.** Still open, which of two variants:
   - the interface only, `ITaggregateTevent : ITevent, ITessageWithIdentity`;
   - the identity moved too (**recommended**, since Magnus's question of 2026-11-16): only the tevent store and
     Tessaging's delivery guarantees read a tevent's `Id`, so a taggregate that is only dispatched in-process never
     needs one, and an API that demands it of every taggregate tevent claims what isn't true. `ITessageWithIdentity`
     already exists, and the at-least-once, at-most-once and exactly-once interfaces already extend it. The tevent
     store's hierarchies declare it, and their classes take a base class with the `Id`. About 45 tevent classes and 28
     root tevent interfaces in Compze's code and tests, of which only the tevent store's change. Its downsides: the
     tevent store can check it only at a taggregate's first use, and an author picks one of two base classes.
10. **Changes to public API, and their names, to veto or accept.** Every name is a placeholder: say which to change.
    - **Compze.Teventive:**
      - tevent routing per class (decided as design, declared tables decided): `TeventRouting<TClass, TTevent>`, made
        by `DeclareAppliers` / `DeclareHandlers` and `Extend<TSubclass>`, with the entries `For`, `IgnoreUnhandled`,
        `Component`, `Entities` and `SharedTomponent`; the protected virtual `TeventAppliers` and `TeventHandlers`
        replace `RegisterTeventAppliers()` and `RegisterTeventHandlers()`; no `BeforeHandlers`, `AfterHandlers` or
        `ForGenericTevent` on the tables, which `IMutableTeventDispatcher` keeps;
      - `ISnapshotable<TSelf, TSnapshot>` with `TakeSnapshot` and `RestoreFrom`, a factory for an empty instance given
        where the class is named, and the protected `RestoreIdentity`;
      - `ITaggregateTevent` as a plain `ITevent` (decision 9);
      - for a taggregate in a store, `Commit`'s callback gone, and `TeventStream` no longer public;
      - committed instances read only, and a failed publish rolling the unit of work back even when caught.
    - **Shared by every store,** in Compze.Teventive itself or an abstractions package of their own (Magnus agreed):
      - `ITaggregateStoreUpdater`, the renamed `ITeventStoreUpdater`, without `IDisposable`;
      - `ITaggregateStoreReader` (`Get`, `TryGet`, `Reference`), with `ITeventStoreReader` extending it by the history;
      - `ITaggregateReference<TTaggregate>` (`Id`, `Current`, `ForUpdate`).
    - **The store that keeps taggregates as their state,** which needs a name of its own, such as "state store", since
      the tevent store is a taggregate store too: `ITaggregateIndex<TTaggregate, TKey>` with `Lookup` and `HasAny`;
      `ITaggregateStorage` with `IPreparedWrite`; and its composition, `AddTaggregate<T>(factory)` and `Index<T, TKey>`.
    - **Gone:** `LoadFromSnapshot` and its snapshot marker, the end-of-cascade hook, and the one-thread assertion.
11. **The query models:** move them to tables in step 5, or keep their per-instance registration.
12. **Tevent implementation classes: an analyzer with a code fix that adds only what is missing. Decided by Magnus,
    2026-11-16,** as work to do: `dev_docs/TODO/an-analyzer-for-tevent-classes-and-type-mappings.md`. And, decided by Magnus: a serialized name is declared on the tevent
    interface's property. See [generating-tevent-classes.md](generating-tevent-classes.md).
13. **The names:** merged into decision 10.
14. **Two choices inside tevent routing per class, which the teams made and Magnus hasn't.** Both teams chose the
    variant that costs an instance nothing, and the two reviewers of JAStudio's folder objected that it was chosen over
    the simpler-looking one without asking him. Handed over by taggregates-fable, 2026-11-16.
    - **How an instance finds its class's table:** a virtual property, as `WrapperTeventImplementation` is found today
      (**recommended**), or an argument of the base constructor, passed up through every abstract base class: 8 B per
      teventive, about 9 MB for JAStudio's corpus (counted, not measured). Recommended with taggregates-fable's
      tightening: the property is abstract, and abstract classes leave it so. A concrete class that forgets it then
      doesn't compile, as with the argument. Only a concrete class that inherits from another concrete class can forget
      it, and the exact-class assertion fails it at its first tevent.
    - **Magnus's question, 2026-11-16: an explicit implementation of a Compze interface instead,** since a protected
      member shows in every taggregate's member list. Tried in a scratch project: a subclass that lists the interface
      again re-implements it, and the base class, calling through the interface, gets the most derived class's table,
      over three levels. The compiler requires the interface listed with the member (CS0540). An explicit implementation
      can't be abstract (CS0106), so forgetting it compiles, and the class gets its base class's table. The base
      constructor can see that, and assert the exact class at every construction. The analyzer already decided could
      report it at compile time.
    - **What a slot is:** a static of the owner's class (**recommended**), or an object in each instance, as
      `SharedTomponentSlot` is in 0.6.0-alpha: a field and two constructor lines per slot, and 48 B a slot (measured),
      about 26 MB for the corpus. Recommended with a change of jastudio-rep-opus's, not built: the slots declared in a
      nested static class, such as `Slots.SeeAlso`, which removes the static slot's rule of order. See
      [the-design.md](the-design.md#shared-tomponents-and-their-slots).
    - **Not for the bytes:** in the code, the chosen variants are no more than the alternatives, and a slot holds
      nothing about one instance, so it describes the class, as the table does.

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

These documents and a few todo comments are merged to Compze's `main`; no prototype is. The prototypes are
experiments, each on a branch of its own, all pushed to origin. Their worktrees were removed on 2026-11-16, except
`taggregate-store-opus`'s, in `C:\Dev\Compze.worktrees\taggregate-store-opus`. The worktree folders that the
documents name are where the branches were checked out; `git worktree add <folder> <branch>` checks one out again.
Their API isn't the designed one where the design changed after they were built.

| Branch | Built by | What it holds |
|---|---|---|
| `taggregate-store-opus` | jastudio-rep-opus | The in-memory taggregate store with deferred dispatch and the private copy (3dd0fbe08), then taggregate-references and the index defined by state (3ac56efa8), and a committed lookup that allocates nothing (56474f568), specified and measured. `Compze.Tests.Unit` passes, 417 of 417; the store's 40 behaviours pass on both combinations. |
| `taggregate-experiments-opus` | jastudio-rep-opus | Routing per class in the real base classes: components, entity collections, slots, inheritance. Full suite passes. |
| `taggregate-experiments-opus-convention` | jastudio-rep-opus | The same, by convention, for comparison. Full suite passes. |
| `taggregates-opus/snapshot-and-hook` | taggregates-opus | The superseded snapshot of tevents and hook with its queue, both bug fixes, the assertion of one thread, and the refusal after a failed cascade (4b42470ba). Full suite passes. |
| `taggregates-opus/combined` | taggregates-opus | The two above merged, and measured. Full suite passes (604c00df4). |
| `taggregate-experiments-fable` | jastudio-rep-fable | A standalone prototype of routing per class with static slots, and the claims run against 0.6.0-alpha. |
| `taggregate-experiments-fable-static-slots` | jastudio-rep-fable | Static slots in the real base classes, built on the combined branch, and measured (fb75d60a0). `Compze.Tests.Unit` passes, 496 of 496; the other test assemblies weren't run on it. |
| `review-taggregate-improvements` | the Opus reviewer | Two specifications showing the thread assertion and the hook's base call against the tevent store (861474504). |
| `review-taggregate-conclusions` | the Fable reviewer | Seven checks against a Release build of the static-slots prototype (a440911a7). |
