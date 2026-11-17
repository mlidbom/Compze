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

## Before the spike ends: review every public API change, for its design as much as its names

**Magnus, 2026-11-16:** "we need to go through all public API changes very carefully at the end of this with an eye
towards whether this is good API design or kitchen sink style types and/or methods crammed in to make JAStudio's life a
bit easier, or even just to prevent refactorings in JAStudio that would actually result in better code than the
shortcuts our added stuff gave them."

For each public type and member the spike added or changed, the review asks: would Compze have it for any consumer, as
an abstraction of its own? Or does it exist because it spared JAStudio a refactoring, and would JAStudio's code be better
had it made that refactoring instead? The names are reviewed in the same pass (Magnus's item 9).

**Not only whether each type and member should exist, but whether its shape and its guarantees are what they should be
(Magnus, 2026-11-17).** Its shape: the signature, the parameter and return types, where it lives, which interface it is
on. Its guarantees: what it promises a caller, such as an order, what a unit of work sees, when it loads, what it costs
at each call, what it does when it fails. The spike's own examples: `All` was first on `IStateStore`, not on a reader;
it and `Lookup` returned an `IReadOnlyList` in id order, which promised an order that cost every call a sort.

**Is the state store designed for an application that keeps its whole database in memory? (Magnus, 2026-11-17)** "If
so that does not make the need or the implementation bad, but it means that we should consider things like naming
and/or whether it should be a specific mode for a store that does not always work like that. For instance, I would
assume that having all-in-memory indexes would not play well unless all the objects are loaded." As built, it is: it
loads every taggregate at its first use and holds them all, and its indexes and `All` work over what it holds. The
review, the agents' included, weighs what that means for its name, its public surface and its documentation, and
whether an all-in-memory store is a mode of something more general or a store of its own.

**First, independent reviews (Magnus, 2026-11-17):** "I absolutely want that on the public API, with the new rules in
front of mind. Both opus and fable." One Opus and one Fable reviewer, each on its own, review every public API change of
the spike, with Compze's rules 011, 012 and 013 in front of them: the frameworks keep their public surface minimal,
public types are designed for every consumer and weigh what one asks for, and an API spends nothing on what callers can
do as well themselves. The same two review the store design as a whole, which has had no independent review beyond
isolation (Magnus, 2026-11-17): taggregate-references, indexes defined by state, the storage seam, synchronous dispatch,
the tevent store taking the same internals, the bulk reader. jastudio-rep-opus consolidates their findings for the pass
with Magnus.

**Started 2026-11-18, on the spike at 46cc881a6.** Both reviewers got the same brief,
[reviews/end-review-brief.md](reviews/end-review-brief.md), and the same list of facts,
[reviews/public-api-changes-of-the-spike.md](reviews/public-api-changes-of-the-spike.md): every public type and member
the spike added, removed or changed, from comparing the built assemblies of the spike's merge-base with `main`
(e29fd6673) and of the spike. The brief withholds the builders' views, the last column of the table below, until a
reviewer has written its own verdicts. Their reviews go to `reviews/end-review-by-an-opus-agent.md` and
`reviews/end-review-by-a-fable-agent.md`.

**A second list, from `C-Compare-PublicApi`,**
[reviews/public-api-changes-of-the-spike-from-C-Compare-PublicApi.md](reviews/public-api-changes-of-the-spike-from-C-Compare-PublicApi.md),
made after the reviewers started by the DevScripts command written for it (428de3b9c). It compares DocFX's declarations
instead of the built assemblies, and each list has what the other misses. It has the changed constraints of seven
methods in `Compze.Teventive.TeventStore.Typermedia`, which the first list misses. It lacks the members of C# 14
extension blocks, which DocFX skips: for those, such as the `ForEachUnitOfWork` overloads in
`TeventObservationRegistrarCE`, the first list has them.

What JAStudio asked for, the first to look at:

| API | Asked for | Why JAStudio needed it | The question for the review |
|---|---|---|---|
| `Taggregate.AssertCanBeChanged()` | increment 3 | Its classes change some state other than by tevents while their parts move to tevents. | A transitional API. Does it go once JAStudio's classes change state only by tevents? |
| `StateStoreBuilder.Taggregate(...)` with a service | step 2a's blockers | Every note is built with the services it uses. | Is a factory with a service the right way for a taggregate to get its services? |
| `IStateStore.ReloadFromStorage()`, `snapshotsAreEqual` | step 2a's blockers | It loads the notes again around a git fast-forward of the corpus. | Is reloading everything a store's job? Or should a consumer whose storage another program changes compose a new store? |
| `IStateStore.ReloadFromStorageAfter(change)` | the switch | The fast-forward must run while no unit of work can write a note. | Every change goes through JAStudio's own `Edit`. Could JAStudio hold its own edits back around the fast-forward, and call `ReloadFromStorage`? taggregates-opus: it could, with a shared/exclusive gate around its outermost unit of work, in 20 to 30 lines. That would be a second gate beside the store's own, and it would need a shared/exclusive lock from Compze.Threading, which may not have one. The guarantee would then rest on JAStudio's assertion that nothing changes a note outside its units of work, not on the store. Its view: keep it, since the program itself changing the storage is the general case of a reload. Dropping it costs it little. |
| `ITaggregateStoreBulkReader.All<TTaggregate>()`, first `IStateStore.All` | the switch | Its searches, tag tree and media check go through every note. | Moved to a bulk reader both stores implement (item 34), as the document db's `IDocumentDbBulkReader`. Is every taggregate of a class the stores' to give, or an index's, such as one with a single key? taggregates-opus: without it, an index of every note under one constant key. That works, but it is a fake index, and every commit refiles each changed note under it. The only other way is a list of ids JAStudio keeps itself, which is the cache the switch removes. |
| `StateStoreBuilder.StoredIn<TService>(...)` | the switch | The corpus folder is known only once the host has started. | Built the way DI factory registrations and `Taggregate(...)` with a service are. Or should JAStudio's host know its paths when it composes the container? taggregates-opus: it can't, since the standalone host gets the corpus folder from the Anki beside it once that has started. Without it: a storage wrapper in JAStudio that resolves the paths at its first load, about 20 lines, a second laziness beside the store's own. |
| `ITaggregateIndex.Lookup` in id order | the switch, as data | A lookup that takes the first of several vocab changed 1,172 sentences in a rebuild while the order was the store's own. | Done (item 37): Magnus dropped it, and `Lookup` returns an `IReadOnlyCollection` in no particular order, as `All` does. JAStudio orders in its one call of `Lookup`. |
| `SnapshotRoundTrip.DifferencesOf` | the corpus check | A tool that checks every note lists the differences, instead of throwing at the first. | |
| `ILocalTessagingEngine.AwaitNoTessagesInFlightAsync()` | JAStudio's specifications | They assert on what observers did, and go on. | |
| `TaggregateToDelete`'s public constructor | JAStudio's storage specifications | They make the values the store hands a storage. | Moot: the seam is internal and `TaggregateToDelete` gone (c5c910e06). |
| `ITeventObservation.ObserveTevents(...)`, runtime observation | replacing `NoteUpdated` | View models come and go with the UI. Deskmancer needs it too, for its specifications' awaits. | Approved by Magnus (items 44 and 46). Observation only. `Dispose` doesn't wait for a call already taken, so one may start just after it returns. |
| `TeventObservationRegistrar.ForEachUnitOfWork<TTevent>(...)` | replacing `NoteUpdated` | A view refreshes once per unit of work, not once per tevent. | Approved by Magnus (item 45). No information about the unit of work, until a consumer names a need. |
| `TeventObservationRegistrar.AtRuntime<TTevent>()`, and the rule that every runtime observer's types are declared with it | Deskmancer's remote awaits | An endpoint receives a remote tevent only of the types it declared at composition. | Approved by Magnus (item 46), the rule jastudio-rep-opus's call. **Magnus deferred the rule to this review, as unclear to him (2026-11-17): explain it plainly here.** Without the rule, a runtime observer of a type no one declared would get the tevents published in its own process and silently none from other endpoints. With it, `ObserveTevents` fails at once for such a type, and what can get into the engine is still all declared at composition. The cost: a line at composition, such as `observe.AtRuntime<INoteTevent>()` in JAStudio, even in a plain container that has no other endpoints. |
| `StateStoreBuilder.StoredInFiles(...)`, `TaggregateFiles.Of<TTaggregate>(folder, fileOf)` | Magnus (item 33), JAStudio the first consumer | Compze's own JSON store replaces JAStudio's `CorpusNoteFiles`. | Once JAStudio uses it, the storage seam (`ITaggregateStorage`, `IPreparedWrite`, `TaggregateSnapshot`, `TaggregateToDelete`, `StoredIn`) goes internal, leaving the rows above about it moot. Done in c5c910e06. |
| `ITaggregateStoreUpdater.Delete` removed | taggregates-opus (item 42) | A deletion is the taggregate's own Deleted tevent. | Done. |
| `TeventRouting<TClass, TTevent>`, made only by the base classes' protected `DeclareAppliers` and `DeclareHandlers` | step 5 | A class's first table starts with its base class's routes, such as `Tentity`'s id, and can't skip them. | Typed on the tevent root, so a route to another hierarchy's tevent doesn't compile. Is a factory on each base class, hidden with `new` by the subclasses that add routes, the right shape? `SharedTomponent`'s takes the class as a type argument, `DeclareAppliers<TagSet>`, having no self type. |
| `TeventApplyingRoot<TRoot, TTevent>`, `TeventApplyingComponent<TComponent, TComponentTevent>` | Magnus, option B of step 5 | The query models and taggregates share their code. | Names are placeholders. Magnus: "a taggregate is not a view model". |
| The query models' parts keep no parent; their entities take no constructor argument | step 5 | A query model publishes nothing. | Their nested `NestedComponent`, `NestedEntity` and `RemovableNestedEntity`, and their own collection types, are gone. |
| Removed: registration per instance, `ISharedTomponentSlot`, `SharedTomponentSlot`, three members of `ITeventiveInternals` | step 5 | Every class routes through its table. | `IMutableTeventDispatcher` stays, for subscriptions made at run time. |
| `AddRestored(id)` on `IEntityCollectionManager` and `SharedTentityCollection` | Magnus's bug, 2026-11-17 | A taggregate with entities can be restored from a snapshot. | Name a placeholder. Allowed only while the taggregate is being restored, from `RestoreIdentity` until its first publish. |

The rest, such as the shared updater and reader, snapshots, the storage seam and tevent routing per class, came from the
design, but are reviewed all the same. The full list of names is item 9 in jastudio-rep-opus's unanswered list.

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
   the query models decided, and per-instance registration removed. Specified in
   [step-5-the-rest-of-the-teventives.md](step-5-the-rest-of-the-teventives.md), approved by Magnus, 2026-11-17.
   **Built** (37731c854 to 628f05c1e), the full suite green at each increment; the query models share taggregates' code
   through base classes that apply tevents (option B, Magnus).

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
   - JAStudio's session for the spike is taggregates-opus (Magnus). JAStudio builds against a Compze worktree of its own,
     `C:\Dev\Compze.worktrees\taggregate-store-spike-for-jastudio`, moved only to pushed commits of the spike. A step that
     adds a Compze project, or a reference between Compze projects, names it, since JAStudio's solution must list it.
   - Agreed with JAStudio: the storage seam `ITaggregateStorage` and the store's fill from `LoadAll` at start come in step 2,
     and JAStudio keeps its own files behind the seam, byte for byte. Typed ids: `Taggregate.Id` stays overridable by a
     covariant return, and the store keeps the id instances it's given.
   - **Progress:** step 1 built (a0552017d). Step 2 split in three: 2a, the state store, built and specified
     (3f1c611b6), Compze's full suite green, its calls in [the-taggregate-store.md](the-taggregate-store.md#what-the-spike-built-in-step-2a);
     2b, the tevent store joining the shared updater and reader, specified and built (2a919c6b4, [the-taggregate-store.md](the-taggregate-store.md#what-the-spike-built-in-step-2b));
     2c, the publisher deferring every tevent, superseded by synchronous dispatch. Step 3's core
     built before 2b and 2c, since JAStudio waits on it (cd3898468, full suite green): the experiment's per-class routing,
     the table found through an explicit implementation of `ITeventRouted` with the exact class asserted at construction,
     `Extend` asserting the direct base class, and static slots (`SharedTomponentSlotOfAClass`) in a nested `Slots`
     class. The performance specification of a copy's bytes, built after 2b (399f19d1f): `Allocating_a_book` bounds what an
     empty book (1,264 B), a copy restored from its snapshot (1,712 B) and a tevent published in the book, its component and
     its shared tomponent (768 to 936 B) allocate, at about a quarter above those. Measured in Debug, not Release as
     planned, since the suite and CI run Debug; the bounds hold in Release too. Not yet: the published doc on reusable
     components, which waits for the naming pass.
     Then the snapshot round-trip helper, `SnapshotRoundTrip.AssertLosesNothing`, which compares the taggregate and its
     copy themselves, since comparing their snapshots alone misses two of the three ways to lose state
     ([the-taggregate-store.md](the-taggregate-store.md#what-the-spike-built-the-snapshot-round-trip)).
   - **Built provisionally, as recommended, and brought back with what the spike showed** (Magnus: "Yes to everything
     you suggested"): after step 2, the publisher deferring every tevent to phase 0 (decision 4) and the lock's
     waiting with a timeout (decision 6); after step 3, the table through an explicit interface implementation, static
     slots in a nested class, and `Extend` asserting the direct base class (decision 14 and the-design.md); after step
     4, the snapshot round-trip helper (the serialization samples belong to the tevent store, not the spike). Names not
     yet decided stay placeholders until a naming pass after step 2. The slot bug (`SharedTomponentSlot`'s unchecked
     wrapper) is fixed on main first.
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
   dispatched in one order. **Moot, Magnus, 2026-11-16:** handlers run synchronously as each publish cascade ends, so
   nothing is deferred and nothing can overtake anything ([the-taggregate-store.md](the-taggregate-store.md#what-the-spike-built-synchronous-dispatch)).
5. **Whether the tevent store's session takes the same internals.** **Narrowed, 2026-11-16:** with synchronous
   dispatch, the tevent store keeps its dispatch, and has the two ordering fixes of its bugs. Joining the shared
   updater and reader (step 2b) remains.
6. **The lock for each taggregate:** a second writer waits, with a timeout (**recommended**), or is refused at once.
   And a Compze lock owned by a transaction, with diagnostics naming the holder, to replace `SemaphoreSlim`.
7. **Tevent routing per class:** whether to do it. **Magnus, 2026-11-16: yes, as a design decision** (not yet the go-ahead to build it). It changes protected members of public base classes: 33
   registrations in 28 files of Compze's tests and sample, all mechanical, and 13 files in `src`, 7 of them the query
   models'.
8. **Declared, or by convention: decided.** Magnus, 2026-11-16: "Definitely tables. One killer thing is ordering
   dependencies. Another the method bloat." Both teams and both reviewers had recommended the declared table, with its
   safeguards.
9. **`ITaggregateTevent` as a plain `ITevent`:** **Magnus, 2026-11-16: yes, as a design decision. The variant decided the same day: the identity moved,
   with the names below:** "Yes move it and go with your naming."
   - the interface only, `ITaggregateTevent : ITevent, ITessageWithIdentity`;
   - **the identity moved too (decided),** after Magnus's question: only the tevent store and
     Tessaging's delivery guarantees read a tevent's `Id`, so a taggregate that is only dispatched in-process never
     needs one, and an API that demands it of every taggregate tevent claims what isn't true. `ITessageWithIdentity`
     already exists, and the at-least-once, at-most-once and exactly-once interfaces already extend it. The tevent
     store's hierarchies declare it, and their classes take a base class with the `Id`. About 45 tevent classes and 28
     root tevent interfaces in Compze's code and tests, of which only the tevent store's change. The names, decided,
     after `ITessageWithIdentity`: `ITaggregateTeventWithIdentity`, which the tevent store's internals need, and the
     class `TaggregateTeventWithIdentity`, a `TaggregateTevent` with the `Id`, so `Taggregate`'s constraints accept
     both. The tevent store can require it at compile time: `ITaggregate<out TTevent>` is covariant, so it can
     constrain `TTaggregate : ITaggregate<ITaggregateTeventWithIdentity>`. Its downside: an author picks one of two
     tevent base classes.
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
    - **Shared by every store,** in Compze.Teventive itself or an abstractions package of their own (Magnus agreed),
      **structured as Magnus decided, 2026-11-16 (item 22),** in
      [the-taggregate-store.md](the-taggregate-store.md#the-programming-model):
      - `ITaggregateStoreUpdater`, the renamed `ITeventStoreUpdater`, without `IDisposable`, extending the reader, with
        `GetForUpdate` and `TryGetForUpdate` where it has `Get` and `TryGet` today, and `Delete` naming the class. Its `Save` only
        adds a new taggregate, which `Add` would say truthfully (jastudio-rep-opus, 2026-11-16, for the naming pass);
      - `ITaggregateStoreReader` (`Get`, `TryGet`, `Reference`), with `ITeventStoreReader` extending it by the history;
      - `ITaggregateReference<TTaggregate>` (`Id`, `Current`, `ForUpdate`).
    - **The state store** (named in item 22), which keeps taggregates as their state: `ITaggregateIndex<TTaggregate, TKey>` with `Lookup` and `HasAny`;
      `ITaggregateStorage` with `IPreparedWrite`; and its composition, `AddTaggregate<T>(factory)` and `Index<T, TKey>`.
    - **Gone:** `LoadFromSnapshot` and its snapshot marker, the end-of-cascade hook, and the one-thread assertion.
11. **The query models move to the tables, in step 5. Decided by Magnus, 2026-11-17:** `SelfGeneratingQueryModel` and
   its family apply tevents and hold their state the way taggregates do. "Taggregates and self generated view models share virtually all logical requirements. The only thing that
   taggregates add is the ability to publish tevents. The fact that, as far as I can remember, they share hardly any code
   beyond the event dispatcher is very frustrating to me. If possible I would love for them to share the great majority
   of their code. But I'm not aware of any way to achieve that without making the types even more complex than they
   already are... Yes, they should implement tevent appliers and state management from tevents in the same way that
   taggregates do."
   Sharing most of the code between the two, without making the types more complex, is for step 5's design to look
   for. Their users today: the AccountManagement sample and Compze's own specifications.
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

## Soon, after the end review

- **Done (46cc881a6): bug: snapshots of taggregates of any shape, entities and shared tentities included.** Magnus, 2026-11-17: "we should
  be able to use snapshotting with any taggregate barring something exceptional about it", and "This is not really adding
  API it is fixing a broken one": refactoring a part into entities makes a taggregate unusable with a state store. A
  `bug:` comment at `ISnapshotable` points here (0d857fa4d). **To be fixed before the end review** (Magnus, OK, 2026-11-18),
  specified in [snapshots-of-every-taggregate-shape.md](snapshots-of-every-taggregate-shape.md). Today a taggregate restored from
  a snapshot can't have entities or shared tentities, since their collections add one only by publishing its created
  tevent ([step 5, found while building](step-5-the-rest-of-the-teventives.md#found-while-building-for-magnus)). What it
  needs:
  - a way for a class's `RestoreFrom` to add a restored entity to `Tentity`'s collection, and a shared tentity to
    `SharedTentityCollection`, with its id and without publishing: public API, specified before it's built;
  - snapshot round-trip specifications (`SnapshotRoundTrip.AssertLosesNothing`) of the shapes Compze's specifications
    already have: the composite taggregates (components, nested components, entities, removable and nested entities,
    integer and Guid ids), the project with its checklist of shared tentities, and the inheriting animals.

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
